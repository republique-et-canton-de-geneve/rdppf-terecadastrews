/* $Rev: 14634 $ */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Threading;
using ESRI.ArcGIS.SOAP;
using DataExtract.Extract;
using Topomat.Web.Common;
using DataExtract.Gml.Simplified;
using Oereb.Report;

public class GetExtractReq
{
    private static string regexThemeCode = @"LandUsePlans|MotorwaysProjectPlaningZones|MotorwaysBuildingLines|RailwaysProjectPlanningZones|RailwaysBuildingLines|AirportsProjectPlanningZones|AirportsBuildingLines|AirportsSecurityZonePlans|ContaminatedSites|ContaminatedMilitarySites|ContaminatedCivilAviationSites|ContaminatedPublicTransportSites|GroundwaterProtectionZones|GroundwaterProtectionSites|NoiseSensitivityLevels|ForestPerimeters|ForestDistanceLines|(ch\.[A-Z]{2}\.[a-zA-Z][a-zA-Z0-9]*)|(ch\.[0-9]{4}\.[a-zA-Z][a-zA-Z0-9]*)|(fl\.[a-zA-Z][a-zA-Z0-9]*)";
    private static IDictionary<LawstatusCode, string> lawStatus = new Dictionary<LawstatusCode, string>
    {
        { LawstatusCode.inForce, "En vigueur"},
        { LawstatusCode.runningModifications, "En cours de modification"}
    };

    private LayerInfo mapLayerInfo;
    private LegendInfo mapLegendInfo;
    private QueryWorker queryWorker;
    private RestrictionWorker restrWorker;
    private string flavour;
    private bool returnGeometry;
    private GetExtractParamReq param;
    private XmlNode requestConfig;
    private XmlNode infoConfig;
    private XmlNode legalConfig;
    private MapPrintParams printParams;

    public GetExtractReq(GetExtractParamReq param, string flavour, bool returnGeometry)
    {
        string token = TokenManager.GetToken();

        this.mapLayerInfo = new LayerInfo(token, WebHelper.GetConfigValue("MapServiceUrl"));
        this.queryWorker = new QueryWorker(token);

        this.flavour = flavour;
        this.returnGeometry = returnGeometry;
        this.param = param;

        this.requestConfig = XmlHelper.GetConfig("request.xml", "RequestConfig");
        this.infoConfig = XmlHelper.GetConfig("information.xml", "InformationConfig");
        this.legalConfig = XmlHelper.GetConfig("legalProvision.xml", "LegalProvisionConfig");

        // call after mapLayerInfo and requestConfig has been instantiated
        this.mapLegendInfo = this.GetLegendInfo(token, WebHelper.GetConfigValue("MapServiceUrl"));
        this.restrWorker = new RestrictionWorker(this.mapLayerInfo, this.mapLegendInfo, this.queryWorker, param);
    }

    public QueryResult ProcessEGRID(string egrid)
    {
        int id = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName")).LayerID;
        string clause = string.Format("{0}='{1}'", WebHelper.GetConfigValue("ParcelleEGRIDFieldName"), egrid);

        return this.queryWorker.QueryAttrRequest(id, clause, true);
    }

    public QueryResult ProcessID(string identdn, string number)
    {
        int id = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName")).LayerID;
        string clause = string.Format("{0}={1} AND {2}={3}", WebHelper.GetConfigValue("ParcelleNoCommFieldName"), identdn,
            WebHelper.GetConfigValue("ParcelleNoFieldName"), number);

        return this.queryWorker.QueryAttrRequest(id, clause, true);
    }

    public ErrorResponseType ValidateRequest(QueryResult qResult)
    {
        if (this.flavour != "reduced")
        {
            return new ErrorResponseType(501);
        }
        if (qResult.features.Length != 1)
        {
            return new ErrorResponseType(204);
        }
        return new ErrorResponseType(200);
    }

    public GetExtractByIdResponseType GetResponseAsXml(QueryResult qResult)
    {
        Stopwatch timer = Stopwatch.StartNew();

        GetExtractByIdResponseType response = new GetExtractByIdResponseType();

        response.Item = this.GetExtract(qResult);

        timer.Stop();
        Helper.AppendToTrace(string.Format("GetExtractById, execution time: {0} ms.", timer.ElapsedMilliseconds));

        return response;
    }

    public JsonExtract.JsonExtract GetResponseAsJson(QueryResult qResult)
    {
        JsonExtract.JsonExtract response = new JsonExtract.JsonExtract();

        response.Item = this.GetExtract(qResult);

        return response;
    }

    public byte[] GetResponseAsPdf(QueryResult qResult)
    {
        Stopwatch extractTimer = Stopwatch.StartNew();

        Extract extract = this.GetExtract(qResult);

        extractTimer.Stop();
        Helper.AppendToTrace(string.Format("GetResponseAsPdf - get extract, execution time: {0} ms.", extractTimer.ElapsedMilliseconds));

        Stopwatch reportTimer = Stopwatch.StartNew();

        byte[] pdfData = ReportBuilder.GeneratePdf(XmlHelper.SerializeToString(typeof(Extract), extract), false, false);

        reportTimer.Stop();
        Helper.AppendToTrace(string.Format("GetResponseAsPdf - get report, execution time: {0} ms.", reportTimer.ElapsedMilliseconds));

        return pdfData;
    }

    private string GetIdentifier(QueryResultFeature feature)
    {
        string guid = Guid.NewGuid().ToString("N").ToUpper();
        guid = guid.Replace("-", "");
        guid = guid.Insert(4, "-");
        guid = guid.Insert(9, "-");
        guid = guid.Insert(14, "-");
        guid = guid.Substring(0, 18);

        string noCom = feature.attributes[WebHelper.GetConfigValue("ParcelleNoCommFieldName")];
        string noParc = feature.attributes[WebHelper.GetConfigValue("ParcelleNoFieldName")];

        return string.Format("{0}-{1}-{2}", guid, noCom, noParc);
    }

    private LegendInfo GetLegendInfo(string token, string url)
    {
        IList<int> layerIds = new List<int>();
        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
            layerIds.Add(this.mapLayerInfo.GetLayerInfo(layerName).LayerID);
        }

        return new LegendInfo(token, url, layerIds.ToArray(), true);
    }

    private Extract GetExtract(QueryResult qResult)
    {
        IDictionary<Thread, MapWorkerThread> dictMapThreads = new Dictionary<Thread, MapWorkerThread>();
        IDictionary<Thread, SurfaceWorkerThread> dictSurfThreads = new Dictionary<Thread, SurfaceWorkerThread>();

        // first get maps
        Extent geomExtent = this.queryWorker.GetGeometryExtent(qResult.features[0].geometry);
        this.printParams = new MapPrintParams(geomExtent);

        RestrictionResult[] restrictions = this.restrWorker.RunAnalyse(qResult, this.printParams.GetMapExtent());

        int[] ids = this.GetMapLayerIds(new string[] { "addMapLayer", "mainMapLayer" });
        dictMapThreads.Add(this.GetMapWorkerThread(this.printParams, geomExtent, ids, MapWorkerThread.TYPE_MAIN, string.Empty, string.Empty));

        ids = this.GetMapLayerIds(new string[] { "addMapLayer", "restrictionMapLayer" });
        dictMapThreads.Add(this.GetMapWorkerThread(this.printParams, geomExtent, ids, MapWorkerThread.TYPE_ROL, string.Empty, string.Empty));

        foreach (RestrictionResult restriction in restrictions)
        {
            IList<int> idList = new List<int>(new int[] { restriction.LayerId });

            XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", restriction.IdentResult.layerName);
            string additionalLayers = XmlHelper.GetXmlAttribute(node, "additionalLayers", false);
            if (restriction.isFirst && !string.IsNullOrEmpty(additionalLayers))
            {
                foreach (string additionalLayer in additionalLayers.Split(new char[] { ',' }))
                {
                    idList.Add(this.mapLayerInfo.GetLayerInfo(additionalLayer).LayerID);
                }
            }

            dictMapThreads.Add(this.GetMapWorkerThread(this.printParams, geomExtent, idList.ToArray(),
                    MapWorkerThread.TYPE_RESTRICTION, restriction.UniqueId, restriction.LayerDefs));
        }

        // compute surfaces
        IEnumerable<IGrouping<int, RestrictionResult>> groups = restrictions.GroupBy(rr => rr.LayerId);
        foreach (IList<RestrictionResult> restrList in groups)
        {
            string layerName = restrList.First().IdentResult.layerName;
            XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", layerName);

            bool isComplete = bool.Parse(XmlHelper.GetXmlAttribute(node, "complete", true));
            bool isOverlap = false;
            string overlapValue = XmlHelper.GetXmlAttribute(node, "overlap", false);
            if (!string.IsNullOrEmpty(overlapValue))
            {
                bool.TryParse(overlapValue, out isOverlap);
            }

            //SurfaceWorker surfWorker = new SurfaceWorker();
            //surfWorker.GetSurfaces(qResult.features[0], isComplete, isOverlap, restrList.ToArray());

            SurfaceWorkerThread swThread = new SurfaceWorkerThread(new SurfaceWorker());
            swThread.Init(qResult.features[0], isComplete, isOverlap, restrList.ToArray());

            Thread thread = new Thread(new ThreadStart(swThread.Start));
            thread.Start();

            dictSurfThreads.Add(thread, swThread);
        }

        // wait for threads to finish
        byte[] mainMapImage = null, rolMapImage = null;
        foreach (KeyValuePair<Thread, SurfaceWorkerThread> pair in dictSurfThreads)
        {
            pair.Key.Join();
            if (!pair.Value.IsSuccessfull())
            {
                throw new WsUserException(pair.Value.GetErrorMessage());
            }
        }
        foreach (KeyValuePair<Thread, MapWorkerThread> pair in dictMapThreads)
        {
            pair.Key.Join();
            if (!pair.Value.IsSuccessfull())
            {
                throw new WsUserException(pair.Value.GetErrorMessage());
            }

            if (pair.Value.GetMapType() == MapWorkerThread.TYPE_MAIN)
            {
                mainMapImage = pair.Value.GetResult();
            }
            else if (pair.Value.GetMapType() == MapWorkerThread.TYPE_ROL)
            {
                rolMapImage = pair.Value.GetResult();
            }
            else
            {
                RestrictionResult res = restrictions.First<RestrictionResult>(rr => string.Compare(rr.UniqueId,
                    pair.Value.GetEntityId()) == 0);
                res.Image = pair.Value.GetResult();
            }
        }

        Extract extract = new Extract();

        extract.CreationDate = DateTime.Now;
        //extract.Signature = this.GetSignature();
        extract.ConcernedTheme = this.GetConcernedTheme(restrictions, true);
        extract.NotConcernedTheme = this.GetConcernedTheme(restrictions, false);
        extract.ThemeWithoutData = this.GetThemeWithoutData();
        extract.isReduced = this.IsReduced();

        extract.Item = File.ReadAllBytes(System.IO.Path.Combine(WebHelper.GetConfigValue("LogoPath"),
            XmlHelper.GetXmlElementValue(this.infoConfig, "LogoPLRCadastre")));
        extract.Item1 = File.ReadAllBytes(System.IO.Path.Combine(WebHelper.GetConfigValue("LogoPath"),
            XmlHelper.GetXmlElementValue(this.infoConfig, "FederalLogo")));
        extract.Item2 = File.ReadAllBytes(System.IO.Path.Combine(WebHelper.GetConfigValue("LogoPath"),
            XmlHelper.GetXmlElementValue(this.infoConfig, "CantonalLogo")));
        extract.Item3 = File.ReadAllBytes(System.IO.Path.Combine(WebHelper.GetConfigValue("LogoPath"),
            XmlHelper.GetXmlElementValue(this.infoConfig, "MunicipalityLogo")));
        extract.ExtractIdentifier = this.GetIdentifier(qResult.features[0]);
        extract.Item4 = extract.Item3; // TODO, QRCode

        extract.GeneralInformation = this.GetLocalisedText(this.infoConfig, "GeneralInformation");
        extract.BaseData = this.GetBaseData();
        extract.Glossary = this.GetGlossary();

        extract.RealEstate = this.GetRealEstate(qResult, restrictions, mainMapImage, rolMapImage);
        extract.ExclusionOfLiability = this.GetExclusionOfLiability();
        extract.PLRCadastreAuthority = this.GetPLRCadastreAuthority(this.infoConfig, "PLRCadastreAuthority");

        return extract;
    }

    private KeyValuePair<Thread, MapWorkerThread> GetMapWorkerThread(MapPrintParams printParams, Extent extent,
        int[] layerIds, int type, string id, string layerDefs)
    {
        MapWorkerThread mapThread = new MapWorkerThread(new MapWorker(printParams), type, id);
        mapThread.Init(extent, layerIds, layerDefs);

        Thread thread = new Thread(new ThreadStart(mapThread.Start));
        thread.Start();

        return new KeyValuePair<Thread, MapWorkerThread>(thread, mapThread);
    }

    private bool IsReduced()
    {
        return string.Compare(this.flavour.ToUpper(), "REDUCED") == 0 ? true : false;
    }

    //private ExtractSignature[] GetSignature()
    //{
    //    IList<ExtractSignature> list = new List<ExtractSignature>();

    //    return list.ToArray();
    //}

    private LocalisedText[] GetBaseData()
    {
        IList<LocalisedText> list = new List<LocalisedText>();

        int dateDmoLayerId = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DateDmoLayerName")).LayerID;
        foreach (XmlNode node in this.infoConfig.SelectNodes("BaseData"))
        {
            string text = node.InnerText;
            if (text.Contains("###DMODATE###"))
            {
                QueryResult qr = this.queryWorker.QueryAttrRequest(dateDmoLayerId, "1=1", false);
                if (qr.features[0] != null &&
                    qr.features[0].attributes[WebHelper.GetConfigValue("DateDmoFieldName")] != null)
                {
                    string value = qr.features[0].attributes[WebHelper.GetConfigValue("DateDmoFieldName")];
                    double millisecs;
                    if (double.TryParse(value, out millisecs))
                    {
                        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                        DateTime date = epoch.AddMilliseconds(millisecs);
                        text = text.Replace("###DMODATE###", date.ToString("dd.MM.yyyy"));
                    }
                }
            }
            list.Add(new LocalisedText
            {
                Language = LanguageCode.fr,
                Text = text
            });
        }

        return list.ToArray();
    }

    private Theme[] GetConcernedTheme(RestrictionResult[] restrictions, bool concerned)
    {
        IList<Theme> list = new List<Theme>();
        IList<RestrictionResult> restrictionList = new List<RestrictionResult>(restrictions);
        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
            int id = this.mapLayerInfo.GetLayerInfo(layerName).LayerID;
            int count = restrictionList.Count<RestrictionResult>(rr => rr.IdentResult.layerId == id);
            if ((concerned == true && count > 0) || concerned == false && count == 0)
            {
                Theme theme = new Theme
                {
                    Code = XmlHelper.GetXmlElementValue(node, "Theme/Code"),
                    Text = new LocalisedText
                    {
                        Language = LanguageCode.fr,
                        Text = XmlHelper.GetXmlElementValue(node, "Theme/Text")
                    }
                };
                list.Add(theme);
            }
        }
        return list.ToArray();
    }

    private Theme[] GetThemeWithoutData()
    {
        IList<Theme> list = new List<Theme>();

        foreach (XmlNode node in this.infoConfig.SelectNodes("ThemeWithoutData"))
        {
            list.Add(this.GetTheme(node));
        }

        return list.ToArray();
    }

    private Theme GetTheme(XmlNode node)
    {
        Regex r = new Regex(GetExtractReq.regexThemeCode);

        string code = "Undefined";
        if (r.IsMatch(node.SelectSingleNode("Code").InnerText))
        {
            code = node.SelectSingleNode("Code").InnerText;
        }

        return new Theme
        {
            Code = code,
            Text = new LocalisedText
            {
                Language = LanguageCode.fr,
                Text = node.SelectSingleNode("Text").InnerText
            }
        };
    }

    private string GetMunicipalityLogoRef(QueryResult qResult)
    {
        return string.Empty;

        //XmlNode reNode = this.requestConfig.SelectSingleNode("RealEstate");
        //string commune = this.GetAttributeFromNode(qResult.features[0], reNode, "Municipality");

        //return string.Format(XmlHelper.GetXmlElementValue(this.infoConfig, "MunicipalityLogoRef"), commune);
    }

    private Lawstatus GetLawstatus(XmlNode node)
    {
        LawstatusCode code = LawstatusCode.runningModifications;
        if (string.Compare(node.SelectSingleNode("Lawstatus").InnerText, "1") == 0)
        {
            code = LawstatusCode.inForce;
        }

        return new Lawstatus
        {
            Code = code,
            Text = new LocalisedText
            {
                Language = LanguageCode.fr,
                Text = GetExtractReq.lawStatus[code]
            }
        };
    }

    private Lawstatus GetLawstatus(XmlNode node, RestrictionResult restriction)
    {
        LawstatusCode code = LawstatusCode.inForce;
        string dbStatus = this.GetAttributeFromAttribute(restriction.IdentResult.attributes, node, "field");
        if (!string.IsNullOrEmpty(dbStatus))
        {
            if (string.Compare(dbStatus, GetExtractReq.lawStatus[LawstatusCode.runningModifications]) == 0)
            {
                code = LawstatusCode.runningModifications;
            }
        }

        return new Lawstatus
        {
            Code = code,
            Text = new LocalisedText
            {
                Language = LanguageCode.fr,
                Text = GetExtractReq.lawStatus[code]
            }
        };
    }

    private LocalisedText[] GetLocalisedText(XmlNode root, string name)
    {
        IList<LocalisedText> list = new List<LocalisedText>();

        foreach (XmlNode node in root.SelectNodes(name))
        {
            list.Add(new LocalisedText
            {
                Language = LanguageCode.fr,
                Text = node.InnerText
            });
        }

        return list.ToArray();
    }

    private LocalisedUri[] GetLocalisedUri(XmlNode root, string name)
    {
        IList<LocalisedUri> list = new List<LocalisedUri>();

        foreach (XmlNode node in root.SelectNodes(name))
        {
            list.Add(new LocalisedUri
            {
                Language = LanguageCode.fr,
                Text = node.InnerText
            });
        }

        return list.ToArray();
    }

    private Glossary[] GetGlossary()
    {
        IList<Glossary> list = new List<Glossary>();

        foreach (XmlNode node in this.infoConfig.SelectNodes("Glossary"))
        {
            LocalisedText title = new LocalisedText
            {
                Language = LanguageCode.fr,
                Text = XmlHelper.GetXmlElementValue(node, "Title")
            };
            LocalisedText content = new LocalisedText
            {
                Language = LanguageCode.fr,
                Text = XmlHelper.GetXmlElementValue(node, "Content")
            };
            list.Add(new Glossary
            {
                Title = new LocalisedText[] { title },
                Content = new LocalisedText[] { content }
            });
        }

        return list.ToArray();
    }

    private RealEstate_DPR GetRealEstate(QueryResult qResult, RestrictionResult[] restrictions, byte[] image, byte[] rolImage)
    {
        RealEstate_DPR re = new RealEstate_DPR();

        XmlNode reNode = this.requestConfig.SelectSingleNode("RealEstate");

        re.Canton = CantonCode.GE;
        re.EGRID = this.GetAttributeFromNode(qResult.features[0], reNode, "EGRID");
        re.FosNr = this.GetAttributeFromNode(qResult.features[0], reNode, "FosNr");
        re.IdentDN = this.GetAttributeFromNode(qResult.features[0], reNode, "IdentDN");
        re.LandRegistryArea = this.GetAttributeFromNode(qResult.features[0], reNode, "LandRegistryArea");
        re.MetadataOfGeographicalBaseData = XmlHelper.GetXmlElementValue(reNode, "MetadataOfGeographicalBaseData");
        re.Municipality = this.GetAttributeFromNode(qResult.features[0], reNode, "Municipality");
        re.Number = this.GetAttributeFromNode(qResult.features[0], reNode, "Number");

        re.PlanForLandRegister = this.GetPlanForLandRegister(image);
        //re.Reference
        re.Limit = this.GetLimit(qResult.features[0]);

        re.RestrictionOnLandownership = this.GetRestrictionOnLandownership(restrictions, re.Municipality);
        re.SubunitOfLandRegister = string.Empty;
        re.Type = RealEstateType.RealEstate;

        re.extensions = this.GetRealEstateExtensions(rolImage);

        return re;
    }

    private RestrictionOnLandownership[] GetRestrictionOnLandownership(RestrictionResult[] restrictions, string municipality)
    {
        IList<RestrictionOnLandownership> list = new List<RestrictionOnLandownership>();

        foreach (RestrictionResult restriction in restrictions)
        {
            MapLayerInfo info = this.mapLayerInfo.GetLayerInfo(restriction.IdentResult.layerName);
            XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig,
                "RestrictionOnLandownership", "layer", restriction.IdentResult.layerName);

            if (node != null)
            {
                RestrictionOnLandownership rol = new RestrictionOnLandownership();
                rol.Information = this.GetLocalisedText(node, "Information");
                rol.Theme = this.GetTheme(node.SelectSingleNode("Theme"));
                rol.SubTheme = XmlHelper.GetXmlElementValue(node, "SubTheme");
                rol.TypeCode = restriction.Legend.TypeCode;
                rol.Lawstatus = this.GetLawstatus(node, restriction);
                rol.Area = restriction.Area.ToString();
                rol.PartInPercent = restriction.PartInPercent.ToString();
                rol.Item = restriction.Legend.Symbol;

                rol.Geometry = this.GetGeometries(node, restriction,
                    rol.Lawstatus, rol.ResponsibleOffice);
                rol.Map = this.GetMap(node, restriction, rol.Theme, rol.SubTheme);

                rol.ResponsibleOffice = this.GetOffice(node.SelectSingleNode("ResponsibleOffice"));
                IList<Document> legalProvisions = new List<Document>(
                    this.GetRegulations(node, restriction, municipality, rol.ResponsibleOffice));
                legalProvisions = legalProvisions.Concat<Document>(
                    this.GetInformations(node, restriction, municipality, rol.ResponsibleOffice)).ToList();
                legalProvisions = legalProvisions.Concat<Document>(
                    this.GetLaws(node, municipality, rol.ResponsibleOffice)).ToList();

                rol.LegalProvisions = legalProvisions.ToArray();

                list.Add(rol);
            }
        }

        return list.ToArray();
    }

    private extensions GetRealEstateExtensions(byte[] image)
    {
        XElement plan = this.GetMapExtension("PlanForROL");
        plan.Add(new XElement("Image", Convert.ToBase64String(image)));

        XElement realEstate = new XElement("RealEstateExtension");
        realEstate.Add(plan);

        return new extensions
        {
            Any = new XmlElement[] { XmlHelper.ToXmlElement(realEstate) }
        };
    }

    private Map GetPlanForLandRegister(byte[] image)
    {
        Map map = this.GetMap(WebHelper.GetConfigValue("MainLegendName"), image);

        map.extensions = new extensions
        {
            Any = new XmlElement[] { XmlHelper.ToXmlElement(this.GetMapExtension("MapExtension")) }
        };

        return map;
    }

    private Map GetMap(string legend, byte[] image)
    {

        Map map = new Map();

        map.Image = image;
        map.LegendAtWeb = new WebReference
        {
            Value = string.Format("{0}/{1}.htm", WebHelper.GetConfigValue("LegendUrl"), legend.ToLower())
        };

        return map;
    }

    private Map GetMap(XmlNode node, RestrictionResult restriction, Theme theme, string subTheme)
    {
        Map map = this.GetMap(restriction.IdentResult.layerName, restriction.Image);

        IList<LegendEntry> otherLegendList = new List<LegendEntry>();

        foreach (RestrictionLegend legend in restriction.OtherLegends)
        {
            otherLegendList.Add(new LegendEntry
            {
                Item = legend.Symbol,
                LegendText = new LocalisedText[]
                {
                    new LocalisedText
                    {
                        Language = LanguageCode.fr,
                        Text = legend.Text
                    }
                },
                TypeCode = legend.TypeCode,
                Theme = theme,
                SubTheme = subTheme
            });
        }
        map.OtherLegend = otherLegendList.ToArray();

        return map;
    }

    private XElement GetMapExtension(string name)
    {
        Extent extent = this.printParams.GetMapExtent();

        XElement elExtent = new XElement("Extent");
        elExtent.Add(new XElement("Xmin", extent.xmin));
        elExtent.Add(new XElement("Xmax", extent.xmax));
        elExtent.Add(new XElement("Ymin", extent.ymin));
        elExtent.Add(new XElement("Ymax", extent.ymax));

        XElement elem = new XElement(name);
        elem.Add(new XElement("Scale", this.printParams.GetScale()));
        elem.Add(new XElement("Blowfactor", MapPrintParams.printFactor));
        elem.Add(new XElement("Dpi", MapPrintParams.printDpi));
        elem.Add(elExtent);
        elem.Add(new XElement("Seq", 0));
        elem.Add(new XElement("Transparency", 0));

        return elem;
    }

    private int[] GetMapLayerIds(string[] layerNodeNames)
    {
        IList<int> layerIds = new List<int>();
        foreach (string layerNodeName in layerNodeNames)
        {
            foreach (XmlNode layerNode in this.requestConfig.SelectNodes(layerNodeName))
            {
                if (layerNode != null && !string.IsNullOrEmpty(layerNode.InnerText))
                {
                    layerIds.Add(this.mapLayerInfo.GetLayerInfo(layerNode.InnerText).LayerID);
                }
            }
        }
        return layerIds.ToArray();
    }

    private MultiSurfacePropertyType GetLimit(QueryResultFeature feature)
    {
        MultiSurfacePropertyType result = null;

        if (this.returnGeometry)
        {
            result = new MultiSurfacePropertyType();
            IList<SurfacePropertyType> surfaces = new List<SurfacePropertyType>();

            foreach (PolygonType poly in this.GetPolygons(feature.geometry))
            {
                surfaces.Add(new SurfacePropertyType
                {
                    Polygon = poly
                });
            }

            result.MultiSurface = new MultiSurfaceType
            {
                srsName = "urn:ogc:def:crs:EPSG::2056",
                surfaceMember = surfaces.ToArray()
            };
        }

        return result;
    }

    private Office GetOffice(XmlNode node)
    {
        return new Office
        {
            Name = this.GetLocalisedText(node, "Name"),
            OfficeAtWeb = new WebReference
            {
                Value = XmlHelper.GetXmlElementValue(node, "OfficeAtWeb")
            }
        };
    }

    private DataExtract.Extract.Geometry[] GetGeometries(XmlNode node, RestrictionResult restriction, Lawstatus status, Office office)
    {
        IList<DataExtract.Extract.Geometry> results = new List<DataExtract.Extract.Geometry>();

        if (this.returnGeometry)
        {
            if (restriction.IdentResult.geometryType == "esriGeometryPolygon")
            {
                foreach (PolygonType polygon in this.GetPolygons(restriction.IdentResult.geometry))
                {
                    DataExtract.Extract.Geometry result = this.InitGeometry(node, status, office);

                    polygon.srsName = "urn:ogc:def:crs:EPSG::2056";
                    result.Item = new SurfacePropertyType
                    {
                        Polygon = polygon
                    };

                    results.Add(result);
                }
            }
            else if (restriction.IdentResult.geometryType == "esriGeometryPolyline")
            {
                foreach (LinearStringType linear in this.GetPolylines(restriction.IdentResult.geometry))
                {
                    DataExtract.Extract.Geometry result = this.InitGeometry(node, status, office);

                    linear.srsName = "urn:ogc:def:crs:EPSG::2056";
                    result.Item = new CurvePropertyType
                    {
                        LineString = linear
                    };

                    results.Add(result);
                }
            }
            else
            {
                PointType point = this.GetPoint(restriction.IdentResult.geometry);
                DataExtract.Extract.Geometry result = this.InitGeometry(node, status, office);

                point.srsName = "urn:ogc:def:crs:EPSG::2056";
                result.Item = new PointPropertyType
                {
                    Point = point
                };

                results.Add(result);
            }
        }

        return results.ToArray();
    }

    private DataExtract.Extract.Geometry InitGeometry(XmlNode node, Lawstatus status, Office office)
    {
        return new DataExtract.Extract.Geometry
        {
            Lawstatus = status,
            MetadataOfGeographicalBaseData = XmlHelper.GetXmlElementValue(node, "MetadataOfGeographicalBaseData"),
            ResponsibleOffice = office
        };
    }

    private PolygonType[] GetPolygons(GeometryResult geometry)
    {
        IList<PolygonType> results = new List<PolygonType>();

        foreach (EsriToGmlPolygon polygon in GeometryHelper.GetPolygons(geometry.rings))
        {
            PolygonType result = new PolygonType();

            IList<LinearRingType> intRings = new List<LinearRingType>();

            result.exterior = new AbstractRingPropertyType
            {
                LinearRing = new LinearRingType
                {
                    posList = GeometryHelper.StringifyCoords(polygon.ExteriorCoords)
                }
            };
            if (polygon.InteriorRings.Count > 0)
            {
                IList<AbstractRingPropertyType> interiorList = new List<AbstractRingPropertyType>();

                foreach (double[][] ring in polygon.InteriorRings)
                {
                    interiorList.Add(new AbstractRingPropertyType
                    {
                        LinearRing = new LinearRingType
                        {
                            posList = GeometryHelper.StringifyCoords(ring)
                        }
                    });
                }

                result.interior = interiorList.ToArray();
            }

            results.Add(result);
        }

        return results.ToArray();
    }

    private LinearStringType[] GetPolylines(GeometryResult geometry)
    {
        IList<LinearStringType> linears = new List<LinearStringType>();
        foreach (double[][] path in geometry.paths)
        {
            linears.Add(new LinearStringType
            {
                posList = GeometryHelper.StringifyCoords(path)
            });
        }
        return linears.ToArray();
    }

    private PointType GetPoint(GeometryResult geometry)
    {
        PointType point = new PointType
        {
            pos = GeometryHelper.StringifyCoords(geometry.points)
        };
        return point;
    }

    private LegalProvisions[] GetRegulations(XmlNode root, RestrictionResult restriction,
        string municipality, Office responsibleOffice)
    {
        IList<LegalProvisions> list = new List<LegalProvisions>();

        foreach (XmlNode node in root.SelectNodes("regulationId"))
        {
            if (!string.IsNullOrEmpty(node.InnerText))
            {
                try
                {

                    XmlNode regNode = XmlHelper.GetNodeByAttribute(this.legalConfig.SelectSingleNode("Regulations"),
                        "LegalProvisions", "id", node.InnerText);
                    LegalProvisions lp = this.GetLegalProvisions(regNode, municipality, responsibleOffice);

                    IList<LocalisedUri> textAtWebList = new List<LocalisedUri>();
                    string textAtWeb = this.GetAttributeFromAttribute(restriction.IdentResult.attributes, regNode, "field");
                    if (!string.IsNullOrEmpty(textAtWeb))
                    {
                        textAtWebList.Add(new LocalisedUri
                        {
                            Language = LanguageCode.fr,
                            Text = textAtWeb
                        });
                        lp.TextAtWeb = textAtWebList.ToArray();
                        list.Add(lp);
                    }
                }
                catch (Exception)
                {
                    throw new WsUserException(string.Format(Resources.Resource.ERROR_REGULATIONS,
                        node.InnerText, restriction.IdentResult.layerName));
                }
            }
        }

        return list.ToArray();
    }

    private Document[] GetInformations(XmlNode root, RestrictionResult restriction,
        string municipality, Office responsibleOffice)
    {
        IList<Document> list = new List<Document>();

        foreach (XmlNode node in root.SelectNodes("informationId"))
        {
            if (!string.IsNullOrEmpty(node.InnerText))
            {
                try
                {
                    XmlNode lawNode = XmlHelper.GetNodeByAttribute(this.legalConfig.SelectSingleNode("Informations"),
                        "LegalProvisions", "id", node.InnerText);
                    Document doc = this.GetDocument(lawNode, municipality, responsibleOffice);

                    IList<LocalisedUri> textAtWebList = new List<LocalisedUri>();
                    string textAtWeb = this.GetAttributeFromAttribute(restriction.IdentResult.attributes, lawNode, "field");
                    if (!string.IsNullOrEmpty(textAtWeb))
                    {
                        textAtWebList.Add(new LocalisedUri
                        {
                            Language = LanguageCode.fr,
                            Text = textAtWeb
                        });
                        doc.TextAtWeb = textAtWebList.ToArray();
                        list.Add(doc);
                    }
                }
                catch (Exception)
                {
                    throw new WsUserException(string.Format(Resources.Resource.ERROR_INFORMATIONS,
                        node.InnerText, restriction.IdentResult.layerName));
                }
            }
        }

        return list.ToArray();
    }

    private Document[] GetLaws(XmlNode root, string municipality, Office responsibleOffice)
    {
        IList<Document> list = new List<Document>();

        foreach (XmlNode node in root.SelectNodes("lawId"))
        {
            if (!string.IsNullOrEmpty(node.InnerText))
            {
                XmlNode lawNode = XmlHelper.GetNodeByAttribute(this.legalConfig.SelectSingleNode("Laws"),
                    "LegalProvisions", "id", node.InnerText);
                list.Add(this.GetDocument(lawNode, municipality, responsibleOffice));
            }
        }

        return list.ToArray();
    }

    private Document GetDocument(XmlNode node, string municipality, Office responsibleOffice)
    {
        Document doc = new Document();
        doc.Abbrevation = this.GetLocalisedText(node, "Abbrevation");
        doc.Canton = CantonCode.GE;
        doc.Lawstatus = this.GetLawstatus(node);
        doc.Municipality = municipality;
        doc.OfficialNumber = XmlHelper.GetXmlElementValue(node, "OfficialNumber");
        doc.OfficialTitle = this.GetLocalisedText(node, "OfficialTitle");
        doc.ResponsibleOffice = responsibleOffice;
        doc.TextAtWeb = this.GetLocalisedUri(node, "TextAtWeb");
        doc.Title = this.GetLocalisedText(node, "Title");
        return doc;
    }

    private LegalProvisions GetLegalProvisions(XmlNode node, string municipality, Office responsibleOffice)
    {
        LegalProvisions lp = new LegalProvisions();
        lp.Abbrevation = this.GetLocalisedText(node, "Abbrevation");
        lp.Canton = CantonCode.GE;
        lp.Lawstatus = this.GetLawstatus(node);
        lp.Municipality = municipality;
        lp.OfficialNumber = XmlHelper.GetXmlElementValue(node, "OfficialNumber");
        lp.OfficialTitle = this.GetLocalisedText(node, "OfficialTitle");
        lp.ResponsibleOffice = responsibleOffice;
        lp.TextAtWeb = this.GetLocalisedUri(node, "TextAtWeb");
        lp.Title = this.GetLocalisedText(node, "Title");
        return lp;
    }

    private ExclusionOfLiability[] GetExclusionOfLiability()
    {
        IList<ExclusionOfLiability> list = new List<ExclusionOfLiability>();

        foreach (XmlNode node in this.infoConfig.SelectNodes("ExclusionOfLiability"))
        {
            LocalisedText title = new LocalisedText
            {
                Language = LanguageCode.fr,
                Text = XmlHelper.GetXmlElementValue(node, "Title")
            };
            LocalisedText content = new LocalisedText
            {
                Language = LanguageCode.fr,
                Text = XmlHelper.GetXmlElementValue(node, "Content")
            };
            list.Add(new ExclusionOfLiability
            {
                Title = new LocalisedText[] { title },
                Content = new LocalisedText[] { content }
            });
        }

        return list.ToArray();
    }

    private Office GetPLRCadastreAuthority(XmlNode root, string name)
    {
        XmlNode node = root.SelectSingleNode(name);

        return new Office
        {
            Name = this.GetLocalisedText(node, "Name"),
            OfficeAtWeb = new WebReference
            {
                Value = XmlHelper.GetXmlElementValue(node, "OfficeAtWeb")
            },
            City = XmlHelper.GetXmlElementValue(node, "City"),
            Number = XmlHelper.GetXmlElementValue(node, "Number"),
            PostalCode = XmlHelper.GetXmlElementValue(node, "PostalCode"),
            Street = XmlHelper.GetXmlElementValue(node, "Street")
        };
    }

    private string GetAttributeFromNode(QueryResultFeature feature, XmlNode parent, string name)
    {
        string result = string.Empty;

        XmlNode node = parent.SelectSingleNode(name);
        if (node != null && !string.IsNullOrEmpty(node.InnerText))
        {
            result = feature.attributes[node.InnerText];
        }

        return result;
    }

    private string GetAttributeFromAttribute(IDictionary<string, string> attributes, XmlNode node, string attribute)
    {
        string result = string.Empty;

        string xmlAttribute = XmlHelper.GetXmlAttribute(node, attribute, false);
        if (!string.IsNullOrEmpty(xmlAttribute))
        {
            string value = attributes[xmlAttribute];
            if (!(string.IsNullOrEmpty(value) || string.Compare(value, "Null") == 0))
            {
                result = value;
            }
        }

        return result;
    }
}