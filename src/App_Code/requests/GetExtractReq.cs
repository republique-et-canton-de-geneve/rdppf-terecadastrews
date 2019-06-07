/* $Rev: 21379 $ */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Threading;
using ESRI.ArcGIS.SOAP;
using ExtractData_v103;
using Topomat.Web.Common;
using DataExtract.Gml.Simplified;

public class GetExtractReq : CommonReq
{
    private static string regexThemeCode = @"LandUsePlans|MotorwaysProjectPlaningZones|MotorwaysBuildingLines|RailwaysProjectPlanningZones|RailwaysBuildingLines|AirportsProjectPlanningZones|AirportsBuildingLines|AirportsSecurityZonePlans|ContaminatedSites|ContaminatedMilitarySites|ContaminatedCivilAviationSites|ContaminatedPublicTransportSites|GroundwaterProtectionZones|GroundwaterProtectionSites|NoiseSensitivityLevels|ForestPerimeters|ForestDistanceLines|(ch\.[A-Z]{2}\.[a-zA-Z][a-zA-Z0-9]*)|(ch\.[0-9]{4}\.[a-zA-Z][a-zA-Z0-9]*)|(fl\.[a-zA-Z][a-zA-Z0-9]*)";
    private static IDictionary<LawstatusCode, string> lawStatus = new Dictionary<LawstatusCode, string>
    {
        { LawstatusCode.inForce, "En vigueur"},
        { LawstatusCode.runningModifications, "En cours de modification"}
    };

    private bool returnGeometry;

    public GetExtractReq(GetExtractParamReq param, string flavour, bool returnGeometry)
    {
        this.Init(param, flavour, false);
        this.returnGeometry = returnGeometry;
    }
    
    public GetExtractByIdResponseType GetResponseAsXml(QueryResult qResult)
    {
        GetExtractByIdResponseType response = new GetExtractByIdResponseType();

        response.Item = this.GetExtract(qResult);        

        return response;
    }

    public JsonExtract.JsonExtract GetResponseAsJson(QueryResult qResult)
    {
        JsonExtract.JsonExtract response = new JsonExtract.JsonExtract();

        response.Item = this.GetExtract(qResult);

        return response;
    }

    public GetExtractByIdResponseTypeEmbeddable GetEmbeddableResponseAsXml(byte[] report)
    {
        GetExtractByIdResponseTypeEmbeddable response = new GetExtractByIdResponseTypeEmbeddable();

        return this.GetEmbaddaleExtract(report);
    }

    public JsonExtract.JsonEmbeddableExtract GetEmbeddableResponseAsJson(byte[] report)
    {
        JsonExtract.JsonEmbeddableExtract response = new JsonExtract.JsonEmbeddableExtract();

        response.Item = this.GetEmbaddaleExtract(report);

        return response;
    }

    private Extract GetExtract(QueryResult qResult)
    {
        IDictionary<Thread, MapWorkerThread> dictMapThreads = new Dictionary<Thread, MapWorkerThread>();
        IDictionary<Thread, SurfaceWorkerThread> dictSurfThreads = new Dictionary<Thread, SurfaceWorkerThread>();

        // first get maps
        Extent geomExtent = this.queryWorker.GetGeometryExtent(qResult.features[0].geometry);
        this.printParams = new MapPrintParams(XmlHelper.GetMapPrintConfig(), geomExtent);

        RestrictionResult[] restrictions = this.restrWorker.RunAnalyse(qResult, this.printParams.GetMapExtent());

        int[] ids = this.GetMapLayerIds(new string[] { "addMapLayer", "mainMapLayer" });
        dictMapThreads.Add(this.GetMapWorkerThread(this.printParams, geomExtent, ids, MapWorkerThread.TYPE_MAIN,
            string.Empty, string.Empty, this.param.withImages));

        ids = this.GetMapLayerIds(new string[] { "markerMapLayer", "addMapLayer", "mainMapLayer" });
        int markerId = this.GetMapLayerIds(new string[] { "markerMapLayer" })[0];
        string layerDefs = string.Format("{0}:OBJECTID={1}", markerId, qResult.features[0].attributes["OBJECTID"]);

        dictMapThreads.Add(this.GetMapWorkerThread(this.printParams, geomExtent, ids, MapWorkerThread.TYPE_PAGE,
            string.Empty, layerDefs, this.param.withImages));

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
                    MapWorkerThread.TYPE_RESTRICTION, restriction.UniqueId, restriction.LayerDefs, this.param.withImages));
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

            SurfaceWorkerThread swThread = new SurfaceWorkerThread(new SurfaceWorker());
            swThread.Init(qResult.features[0], isComplete, isOverlap, restrList.ToArray());

            Thread thread = new Thread(new ThreadStart(swThread.Start));
            thread.Start();

            dictSurfThreads.Add(thread, swThread);
        }

        // wait for threads to finish
        byte[] mainMapImage = null, pageMapImage = null;
        string mainMapUrl = string.Empty, pageMapUrl = string.Empty;
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
                mainMapImage = pair.Value.GetResultAsImage();
                mainMapUrl = pair.Value.GetResultAsUrl();
            }
            else if (pair.Value.GetMapType() == MapWorkerThread.TYPE_PAGE)
            {
                pageMapImage = pair.Value.GetResultAsImage();
                pageMapUrl = pair.Value.GetResultAsUrl();
            }
            else
            {
                RestrictionResult res = restrictions.First<RestrictionResult>(rr => string.Compare(rr.UniqueId,
                    pair.Value.GetEntityId()) == 0);
                res.Image = pair.Value.GetResultAsImage();
                res.MapUrl = pair.Value.GetResultAsUrl();
            }
        }

        Extract extract = new Extract();

        extract.CreationDate = DateTime.Now;
        //extract.Signature = this.GetSignature();
        extract.ConcernedTheme = this.GetConcernedTheme(restrictions, true);
        extract.NotConcernedTheme = this.GetConcernedTheme(restrictions, false);
        extract.ThemeWithoutData = this.GetThemeWithoutData();
        extract.isReduced = this.IsReduced();

        if (this.param.withImages)
        {
            extract.Item = File.ReadAllBytes(System.IO.Path.Combine(WebHelper.GetConfigValue("LogoPath"),
                XmlHelper.GetXmlElementValue(this.infoConfig, "LogoPLRCadastre")));
            extract.Item1 = File.ReadAllBytes(System.IO.Path.Combine(WebHelper.GetConfigValue("LogoPath"),
                XmlHelper.GetXmlElementValue(this.infoConfig, "FederalLogo")));
            extract.Item2 = File.ReadAllBytes(System.IO.Path.Combine(WebHelper.GetConfigValue("LogoPath"),
                XmlHelper.GetXmlElementValue(this.infoConfig, "CantonalLogo")));
            extract.Item3 = File.ReadAllBytes(System.IO.Path.Combine(WebHelper.GetConfigValue("LogoPath"),
                XmlHelper.GetXmlElementValue(this.infoConfig, "MunicipalityLogo")));
        }
        else
        {
            extract.Item = WebHelper.GetConfigValue("LogoUrl") + "/" +
                XmlHelper.GetXmlElementValue(this.infoConfig, "LogoPLRCadastre");
            extract.Item1 = WebHelper.GetConfigValue("LogoUrl") + "/" +
                XmlHelper.GetXmlElementValue(this.infoConfig, "FederalLogo");
            extract.Item2 = WebHelper.GetConfigValue("LogoUrl") + "/" +
                XmlHelper.GetXmlElementValue(this.infoConfig, "CantonalLogo");
            extract.Item3 = WebHelper.GetConfigValue("LogoUrl") + "/" +
                XmlHelper.GetXmlElementValue(this.infoConfig, "MunicipalityLogo");
        }
        extract.ExtractIdentifier = this.GetNormalizedString(this.GetIdentifier(qResult.features[0]), 50);
        extract.Item4 = extract.Item3; // TODO, QRCode

        extract.GeneralInformation = this.GetLocalisedMText(this.infoConfig, "GeneralInformation");
        extract.BaseData = this.GetBaseData();
        extract.Glossary = this.GetGlossary();

        if (this.param.withImages)
        {
            extract.RealEstate = this.GetRealEstate(qResult, restrictions, mainMapImage, pageMapImage);
        }
        else
        {
            extract.RealEstate = this.GetRealEstate(qResult, restrictions, mainMapUrl, pageMapUrl);
        }
        extract.ExclusionOfLiability = this.GetExclusionOfLiability();
        extract.PLRCadastreAuthority = this.GetPLRCadastreAuthority(this.infoConfig, "PLRCadastreAuthority");

        return extract;
    }

    private GetExtractByIdResponseTypeEmbeddable GetEmbaddaleExtract(byte[] report)
    {
        Office authority = this.GetPLRCadastreAuthority(this.infoConfig, "PLRCadastreAuthority");
        DateTime? dmoDate = this.GetDMODate();

        IList<GetExtractByIdResponseTypeEmbeddableDatasource> datasources =
            new List<GetExtractByIdResponseTypeEmbeddableDatasource>();
        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            Office office = this.GetOffice(node.SelectSingleNode("ResponsibleOffice"));

            datasources.Add(new GetExtractByIdResponseTypeEmbeddableDatasource
            {
                dataownerName = office.Name[0].Text,
                topic = this.GetTheme(node.SelectSingleNode("Theme")),
                transferFromSource = (DateTime)dmoDate
            });
        }

        return new GetExtractByIdResponseTypeEmbeddable
        {
            cadasterOrganisationName = authority.Name[0].Text,
            cadasterState = (DateTime)dmoDate,
            dataownerNameCadastralSurveying = authority.Name[0].Text,
            datasource = datasources.ToArray(),
            pdf = report,
            transferFromSourceCadastralSurveying = (DateTime)dmoDate
        };
    }

    private KeyValuePair<Thread, MapWorkerThread> GetMapWorkerThread(MapPrintParams printParams, Extent extent,
        int[] layerIds, int type, string id, string layerDefs, bool withImages)
    {
        MapWorkerThread mapThread = new MapWorkerThread(new MapWorker(printParams), type, id, withImages);
        mapThread.Init(extent, layerIds, layerDefs);

        Thread thread = new Thread(new ThreadStart(mapThread.Start));
        thread.Start();

        return new KeyValuePair<Thread, MapWorkerThread>(thread, mapThread);
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
                        LanguageSpecified = true,
                        Language = LanguageCode.fr,
                        Text = XmlHelper.GetXmlElementValue(node, "Theme/Text")
                    }
                };
                list.Add(theme);
            }
        }
        return list.ToArray();
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
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = GetExtractReq.lawStatus[code]
            }
        };
    }

    private Lawstatus GetLawstatus(XmlNode node, RestrictionResult restriction)
    {
        LawstatusCode code = LawstatusCode.inForce;
        string dbStatus = XmlHelper.GetAttributeFromAttribute(restriction.IdentResult.attributes, node, "field");
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
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = GetExtractReq.lawStatus[code]
            }
        };
    }

    private LocalisedUri[] GetLocalisedUri(XmlNode root, string name)
    {
        IList<LocalisedUri> list = new List<LocalisedUri>();

        foreach (XmlNode node in root.SelectNodes(name))
        {
            list.Add(new LocalisedUri
            {
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = node.InnerText
            });
        }

        return list.ToArray();
    }

    private RealEstate_DPR GetRealEstate(QueryResult qResult, RestrictionResult[] restrictions, Map mainMap, Map printMap)
    {
        RealEstate_DPR re = new RealEstate_DPR();

        XmlNode reNode = this.requestConfig.SelectSingleNode("RealEstate");

        re.Canton = CantonCode.GE;
        re.EGRID = this.GetNormalizedString(XmlHelper.GetAttributeFromNode(qResult.features[0], reNode, "EGRID"), 14);
        re.FosNr = XmlHelper.GetAttributeFromNode(qResult.features[0], reNode, "FosNr");
        re.IdentDN = this.GetNormalizedString(XmlHelper.GetAttributeFromNode(qResult.features[0], reNode, "IdentDN"), 12);
        re.LandRegistryArea = XmlHelper.GetAttributeFromNode(qResult.features[0], reNode, "LandRegistryArea");
        re.MetadataOfGeographicalBaseData = XmlHelper.GetXmlElementValue(reNode, "MetadataOfGeographicalBaseData");
        re.Municipality = this.GetNormalizedString(XmlHelper.GetAttributeFromNode(qResult.features[0], reNode, "Municipality"), 60);
        re.Number = this.GetNormalizedString(XmlHelper.GetAttributeFromNode(qResult.features[0], reNode, "Number"), 12);

        // layerIndex quand on a plusieurs couches ?
        mainMap.layerIndex = "-1";
        mainMap.layerOpacity = 1.0;
        re.PlanForLandRegister = mainMap;
        printMap.layerIndex = "-1";
        printMap.layerOpacity = 1.0;
        re.PlanForLandRegisterMainPage = printMap;
        re.Limit = this.GetLimit(qResult.features[0]);

        re.RestrictionOnLandownership = this.GetRestrictionOnLandownership(restrictions, re.FosNr);
        re.SubunitOfLandRegister = string.Empty; // si définit, utiliser: this.GetNormalizedString("", 60);
        re.Type = RealEstateType.RealEstate;

        return re;
    }

    private RealEstate_DPR GetRealEstate(QueryResult qResult, RestrictionResult[] restrictions, byte[] mainImage, byte[] printImage)
    {
        Map mainMap = this.GetPlanForLandRegister(mainImage);
        Map printMap = this.GetPlanForLandRegister(printImage);

        return this.GetRealEstate(qResult, restrictions, mainMap, printMap);
    }

    private RealEstate_DPR GetRealEstate(QueryResult qResult, RestrictionResult[] restrictions, string mapUrl, string printUrl)
    {
        Map mainMap = this.GetPlanForLandRegister(mapUrl);
        Map pageMap = this.GetPlanForLandRegister(printUrl);

        return this.GetRealEstate(qResult, restrictions, mainMap, pageMap);
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
                rol.Information = this.GetLocalisedMText(node, "Information");
                rol.Theme = this.GetTheme(node.SelectSingleNode("Theme"));
                rol.SubTheme = this.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "SubTheme"), 60);
                rol.TypeCode = this.GetNormalizedString(restriction.Legend.TypeCode, 40);
                rol.Lawstatus = this.GetLawstatus(node, restriction);
                rol.AreaShare = restriction.Area.ToString();
                rol.LengthShare = restriction.Length.ToString();
                if (restriction.PartInPercent > 0.0f)
                {
                    rol.PartInPercentSpecified = true;
                    rol.PartInPercent = (decimal)restriction.PartInPercent;
                }
                else
                {
                    rol.PartInPercentSpecified = false;
                }
                if (this.param.withImages == true)
                {
                    rol.Item = restriction.Legend.Symbol;
                }
                else
                {
                    rol.Item = restriction.Legend.SymbolRef;
                }

                rol.Geometry = this.GetGeometries(node, restriction,
                    rol.Lawstatus, this.GetOffice(node.SelectSingleNode("ResponsibleOffice")));
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

    private Map GetPlanForLandRegister(byte[] image)
    {
        Map map = this.GetMap(WebHelper.GetConfigValue("MainLegendName"), image);

        return map;
    }

    private Map GetPlanForLandRegister(string url)
    {
        Map map = this.GetMap(WebHelper.GetConfigValue("MainLegendName"), url);

        return map;
    }

    private Map GetMap(string legend)
    {
        Map map = new Map();

        map.LegendAtWeb = new WebReference
        {
            Value = string.Format("{0}/{1}.htm", WebHelper.GetConfigValue("LegendUrl"), legend.ToLower())
        };

        return map;
    }

    private Map GetMap(string legend, byte[] image)
    {
        Map map = this.GetMap(legend);

        map.Image = image;

        return map;
    }

    private Map GetMap(string legend, string url)
    {
        Map map = this.GetMap(legend);

        map.ReferenceWMS = url;

        return map;
    }

    private LegendEntry[] GetOtherLegends(RestrictionResult restriction, Theme theme, string subTheme)
    {
        IList<LegendEntry> otherLegendList = new List<LegendEntry>();

        foreach (RestrictionLegend legend in restriction.OtherLegends)
        {
            LegendEntry entry = new LegendEntry
            {
                LegendText = new LocalisedText[]
                {
                    new LocalisedText
                    {
                        LanguageSpecified = true,
                        Language = LanguageCode.fr,
                        Text = legend.Text
                    }
                },
                TypeCode = this.GetNormalizedString(legend.TypeCode, 40),
                TypeCodelist = "-",
                Theme = theme,
                SubTheme = this.GetNormalizedString(subTheme, 60)
            };

            if (this.param.withImages == true)
            {
                entry.Item = legend.Symbol;
            }
            else
            {
                entry.Item = legend.SymbolRef;
            }

            otherLegendList.Add(entry);
        }
        return otherLegendList.ToArray();
    }

    private Map GetMap(XmlNode node, RestrictionResult restriction, Theme theme, string subTheme)
    {
        Map map = null;

        if (this.param.withImages)
        {
            map = this.GetMap(restriction.IdentResult.layerName, restriction.Image);
        }
        else
        {
            map = this.GetMap(restriction.IdentResult.layerName, restriction.MapUrl);
        }

        map.OtherLegend = this.GetOtherLegends(restriction, theme, subTheme);
        map.layerIndex = restriction.LayerId.ToString();
        map.layerOpacity = 1.0;

        return map;
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

    private ExtractData_v103.Geometry[] GetGeometries(XmlNode node, RestrictionResult restriction, Lawstatus status, Office office)
    {
        IList<ExtractData_v103.Geometry> results = new List<ExtractData_v103.Geometry>();

        if (this.returnGeometry)
        {
            if (restriction.IdentResult.geometryType == "esriGeometryPolygon")
            {
                foreach (PolygonType polygon in this.GetPolygons(restriction.IdentResult.geometry))
                {
                    ExtractData_v103.Geometry result = this.InitGeometry(node, status, office);

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
                    ExtractData_v103.Geometry result = this.InitGeometry(node, status, office);

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
                ExtractData_v103.Geometry result = this.InitGeometry(node, status, office);

                point.srsName = "urn:ogc:def:crs:EPSG::2056";
                result.Item = new PointPropertyType
                {
                    Point = point
                };

                results.Add(result);
            }
        }
        else
        {
            ExtractData_v103.Geometry result = this.InitGeometry(node, status, office);
            results.Add(result);
        }

        return results.ToArray();
    }

    private ExtractData_v103.Geometry InitGeometry(XmlNode node, Lawstatus status, Office office)
    {
        return new ExtractData_v103.Geometry
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

    private Document[] GetRegulations(XmlNode root, RestrictionResult restriction, string municipality, Office responsibleOffice)
    {
        IList<Document> list = new List<Document>();

        foreach (XmlNode node in root.SelectNodes("regulationId"))
        {
            if (!string.IsNullOrEmpty(node.InnerText))
            {
                try
                {
                    XmlNode regNode = XmlHelper.GetNodeByAttribute(this.legalConfig.SelectSingleNode("Regulations"),
                        "LegalProvisions", "id", node.InnerText);
                    Document doc = this.GetDocument(regNode, municipality, responsibleOffice);
                    doc.DocumentType = DocumentBaseDocumentType.LegalProvision;

                    IList<LocalisedUri> textAtWebList = new List<LocalisedUri>();
                    string textAtWeb = XmlHelper.GetAttributeFromAttribute(restriction.IdentResult.attributes, regNode, "field");
                    if (!string.IsNullOrEmpty(textAtWeb))
                    {
                        textAtWebList.Add(new LocalisedUri
                        {
                            LanguageSpecified = true,
                            Language = LanguageCode.fr,
                            Text = textAtWeb
                        });
                        doc.TextAtWeb = textAtWebList.ToArray();
                        list.Add(doc);
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
                    doc.DocumentType = DocumentBaseDocumentType.Hint;

                    IList<LocalisedUri> textAtWebList = new List<LocalisedUri>();
                    string textAtWeb = XmlHelper.GetAttributeFromAttribute(restriction.IdentResult.attributes, lawNode, "field");
                    if (!string.IsNullOrEmpty(textAtWeb))
                    {
                        textAtWebList.Add(new LocalisedUri
                        {
                            LanguageSpecified = true,
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
                Document doc = this.GetDocument(lawNode, municipality, responsibleOffice);
                doc.DocumentType = DocumentBaseDocumentType.Law;

                list.Add(doc);
            }
        }

        return list.ToArray();
    }

    private Document GetDocument(XmlNode node, string municipality, Office responsibleOffice)
    {
        Document doc = new Document();
        doc.Abbreviation = this.GetLocalisedText(node, "Abbreviation");
        doc.CantonSpecified = true;
        doc.Canton = CantonCode.GE;
        doc.Lawstatus = this.GetLawstatus(node);
        doc.Municipality = municipality;
        doc.OfficialNumber = this.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "OfficialNumber"), 20);
        doc.OfficialTitle = this.GetLocalisedText(node, "OfficialTitle");
        doc.ResponsibleOffice = responsibleOffice;
        doc.TextAtWeb = this.GetLocalisedUri(node, "TextAtWeb");
        doc.Title = this.GetLocalisedText(node, "Title");
        return doc;
    }
}