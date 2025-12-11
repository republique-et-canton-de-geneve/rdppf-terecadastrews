/* $Rev: 31768 $ */
using ExtractDataModel_v20;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using Topomat.Web.Common;

public class GetExtractReq : CommonReq
{
    private static IDictionary<LawstatusCode, string> lawStatus = new Dictionary<LawstatusCode, string>
    {
        { LawstatusCode.inForce, "En vigueur"},
        { LawstatusCode.changeWithoutPreEffect, "Modification sans effet anticipé"},
        { LawstatusCode.changeWithPreEffect, "Modification avec effet anticipé"}
    };
    private IDictionary<string, LawstatusCode> invLawStatus = new Dictionary<string, LawstatusCode>();

    public GetExtractReq(GetExtractParamReq param)
    {
        foreach (LawstatusCode key in lawStatus.Keys)
        {
            invLawStatus.Add(lawStatus[key], key);
        }
        this.Init(param, false);
    }

    public GetExtractByIdResponseType GetResponseAsXml(QueryResultFeature feature)
    {
        GetExtractByIdResponseType response = new GetExtractByIdResponseType();

        response.Extract = this.GetExtract(feature);

        return response;
    }

    public string GetResponseAsUrl(QueryResultFeature feature)
    {
        XmlNode node;
        if (feature.type == ParcelleType.BienFonds)
        {
            node = this.requestConfig.SelectSingleNode("RealEstate/Parcelle");
        }
        else
        {
            node = this.requestConfig.SelectSingleNode("RealEstate/DDP");
        }
        string ideddp = string.Format("{0}:{1}", XmlHelper.GetAttributeFromNode(feature, node, "IdentDN"), XmlHelper.GetAttributeFromNode(feature, node, "Number"));

        return string.Format(WebHelper.GetConfigValue("SITGExtractUrl"), ideddp);
    }

    public JsonExtract.JsonExtract GetResponseAsJson(QueryResultFeature feature)
    {
        JsonExtract.JsonExtract response = new JsonExtract.JsonExtract();

        response.Item = this.GetExtract(feature);

        return response;
    }

    private Extract GetExtract(QueryResultFeature feature)
    {
        IList<MapWorker> mapWorkers = new List<MapWorker>();
        Extent geomExtent = this.queryWorker.GetGeometryExtent(feature.geometry);
        this.printParams = new MapPrintParams(XmlHelper.GetMapPrintConfig(), geomExtent);

        RestrictionResult[] restrictions = this.restrWorker.RunAnalyse(feature, this.printParams.GetMapExtent());

        string marker = "markerMapLayer";
        if (feature.type == ParcelleType.DDP)
        {
            marker = "markerDDPMapLayer";
        }

        int[] ids = this.GetMapLayerIds(new string[] { "addMapLayer", "mainMapLayer" });
        mapWorkers.Add(InitMapWorker(this.printParams, MapWorker.MapWorkerTypes.mainBasemap, string.Empty, ids, string.Empty));

        ids = this.GetMapLayerIds(new string[] { marker, "addMapLayer", "mainMapLayer" });
        int markerId = this.GetMapLayerIds(new string[] { marker })[0];
        string layerDefs = string.Format("\"{0}\":\"OBJECTID={1}\"", markerId, feature.attributes["OBJECTID"]);
        mapWorkers.Add(InitMapWorker(this.printParams, MapWorker.MapWorkerTypes.marker, string.Empty, ids, layerDefs));

        foreach (RestrictionResult restriction in restrictions)
        {
            layerDefs = string.Format("\"{0}\":\"{1}={2}\"", restriction.IdentResult.layerId, restriction.OIDFieldName, restriction.OID);
            mapWorkers.Add(InitMapWorker(this.printParams, MapWorker.MapWorkerTypes.restriction, restriction.UniqueId, new int[] { restriction.IdentResult.layerId }, layerDefs));
        }

        // calcul des surfaces
        Parallel.ForEach(restrictions.GroupBy(rr => rr.IdentResult.layerId), restrList =>
        {
            if (!restrList.First().isAdditionalLegend)
            {
                bool isComplete = false;
                bool isOverlap = false;

                if (!restrList.First().isAdditionalResult)
                {
                    XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", restrList.First().IdentResult.layerName);

                    isComplete = bool.Parse(XmlHelper.GetXmlAttribute(node, "complete", true));
                    string overlapValue = XmlHelper.GetXmlAttribute(node, "overlap", false);
                    if (!string.IsNullOrEmpty(overlapValue))
                    {
                        bool.TryParse(overlapValue, out isOverlap);
                    }
                }

                SurfaceWorker worker = new SurfaceWorker();
                worker.GetSurfaces(feature, isComplete, isOverlap, restrList.ToArray());
            }
        });

        // récupération des cartes
        byte[] mainMapImage = null, pageMapImage = null;
        string mainMapUrl = string.Empty, pageMapUrl = string.Empty;

        if (this.param.withImages)
        {
            Parallel.ForEach(mapWorkers, worker =>
            {
                byte[] imageData = worker.GetExtractMapAsImage(geomExtent);
                switch (worker.GetWorkerType())
                {
                    case MapWorker.MapWorkerTypes.mainBasemap:
                        mainMapImage = imageData;
                        break;
                    case MapWorker.MapWorkerTypes.marker:
                        pageMapImage = imageData;
                        break;
                    default:
                        RestrictionResult res = restrictions.First<RestrictionResult>(rr => string.Compare(rr.UniqueId, worker.GetEntityId()) == 0);
                        res.Image = imageData;
                        break;
                }
            });
        }
        else
        {
            Parallel.ForEach(mapWorkers, worker =>
            {
                string url = worker.GetExtractMapAsUrl(geomExtent);
                switch (worker.GetWorkerType())
                {
                    case MapWorker.MapWorkerTypes.mainBasemap:
                        mainMapUrl = url;
                        break;
                    case MapWorker.MapWorkerTypes.marker:
                        pageMapUrl = url;
                        break;
                    case MapWorker.MapWorkerTypes.restriction:
                        RestrictionResult res = restrictions.First<RestrictionResult>(rr => string.Compare(rr.UniqueId, worker.GetEntityId()) == 0);
                        res.MapUrl = url;
                        break;
                }
            });
        }

        IList<RestrictionTheme> cfgThemes = GetThemes();
        string munLogoUrl = GetMunicipalityLogoUrl(feature);

        Extract extract = new Extract();

        extract.CreationDate = DateTime.Now;
        //extract.Signature = this.GetSignature();
        extract.ConcernedTheme = this.GetConcernedTheme(cfgThemes, restrictions, true);
        extract.NotConcernedTheme = this.GetConcernedTheme(cfgThemes, restrictions, false);
        extract.ThemeWithoutData = this.GetThemeWithoutData();

        if (this.param.withImages)
        {
            extract.Item = Convert.FromBase64String(oerebHelper.GetLogo("ch.plr", this.param.lang));
            extract.Item1 = Convert.FromBase64String(oerebHelper.GetLogo("ch", this.param.lang));
            extract.Item2 = File.ReadAllBytes(Path.Combine(WebHelper.GetConfigValue("LogoPath"), "LOGORCGE_rvb300dpi_FRU.jpg"));
            using (WebClient client = new WebClient())
            {
                extract.Item3 = client.DownloadData(munLogoUrl);
            }
        }
        else
        {
            extract.Item = string.Format("{0}/ch.plr.{1}.png", WebHelper.GetConfigValue("LogoUrl"), this.param.lang);
            extract.Item1 = string.Format("{0}/ch.{1}.png", WebHelper.GetConfigValue("LogoUrl"), this.param.lang);
            extract.Item2 = string.Format("{0}/LOGORCGE_rvb300dpi_FRU.jpg", WebHelper.GetConfigValue("LogoUrl"));
            extract.Item3 = munLogoUrl;
        }
        extract.ExtractIdentifier = Helper.GetNormalizedString(this.GetIdentifier(feature), 50);
        extract.Item4 = string.Empty; // TODO, QRCode

        IList<string> infos = new List<string>();
        foreach (InformationText it in GetInformationConfig("Information"))
        {
            foreach (string content in it.Contents)
            {
                infos.Add(content);
            }
        }
        extract.GeneralInformation = this.GetLocalisedMText(infos.ToArray());
        extract.Glossary = this.GetGlossary();

        if (this.param.withImages)
        {
            extract.RealEstate = this.GetRealEstate(cfgThemes, feature, restrictions, mainMapImage, pageMapImage);
        }
        else
        {
            extract.RealEstate = this.GetRealEstate(cfgThemes, feature, restrictions, mainMapUrl, pageMapUrl);
        }
        extract.Disclaimer = this.GetDisclaimer();
        extract.PLRCadastreAuthority = this.GetPLRCadastreAuthority(this.infoConfig, "PLRCadastreAuthority");
        extract.UpdateDateCS = DateTime.Now;

        return extract;
    }

    private MapWorker InitMapWorker(MapPrintParams printParams, MapWorker.MapWorkerTypes type, string id, int[] layerIds, string layerDefs)
    {
        MapWorker worker = new MapWorker(printParams);
        worker.Init(type, id, layerIds, new string[] { layerDefs });
        return worker;
    }

    private Theme[] GetConcernedTheme(IList<RestrictionTheme> themes, RestrictionResult[] restrictions, bool concerned)
    {
        IList<Theme> list = new List<Theme>();

        foreach (RestrictionTheme theme in themes)
        {
            int id = mapLayerInfo.GetLayerInfo(theme.Layer, true).LayerID;
            int count = restrictions.Count(rr => rr.LayerId == id);

            if ((concerned == true && count > 0) || concerned == false && count == 0)
            {
                list.Add(GetTheme(theme));
            }
        }

        return list.ToArray();
    }

    private Lawstatus GetLawstatus(XmlNode node)
    {
        LawstatusCode code = LawstatusCode.inForce;
        string cfgCode = node.SelectSingleNode("Lawstatus").InnerText;
        switch (cfgCode)
        {
            case "2":
                code = LawstatusCode.changeWithoutPreEffect;
                break;
            case "3":
                code = LawstatusCode.changeWithPreEffect;
                break;
        }

        return new Lawstatus
        {
            Code = code,
            Text = GetLocalisedText(lawStatus[code])
        };
    }

    private Lawstatus GetLawstatus(XmlNode node, RestrictionResult restriction)
    {
        LawstatusCode code = LawstatusCode.inForce;
        string dbStatus = XmlHelper.GetAttributeFromAttribute(restriction.IdentResult.attributes, node, "field");
        if (!string.IsNullOrEmpty(dbStatus) && invLawStatus.ContainsKey(dbStatus))
        {
            code = invLawStatus[dbStatus];
        }

        return new Lawstatus
        {
            Code = code,
            Text = GetLocalisedText(lawStatus[code])
        };
    }

    private RealEstate_DPR GetRealEstate(IList<RestrictionTheme> themes, QueryResultFeature feature, RestrictionResult[] restrictions, Map mainMap, Map printMap)
    {
        RealEstate_DPR re = new RealEstate_DPR();

        string xpath = "RealEstate/Parcelle";
        if (feature.type == ParcelleType.DDP)
        {
            xpath = "RealEstate/DDP";
        }
        XmlNode reNode = this.requestConfig.SelectSingleNode(xpath);

        re.Canton = CantonCode.GE;
        re.EGRID = Helper.GetNormalizedString(XmlHelper.GetAttributeFromNode(feature, reNode, "EGRID"), 14);
        re.IdentDN = Helper.GetNormalizedString(XmlHelper.GetAttributeFromNode(feature, reNode, "IdentDN"), 12);
        re.LandRegistryArea = XmlHelper.GetAttributeFromNode(feature, reNode, "LandRegistryArea");
        re.MetadataOfGeographicalBaseData = XmlHelper.GetXmlElementValue(reNode, "MetadataOfGeographicalBaseData");

        MunicipalityInfo munInfo = GetMunicipalityInfo(XmlHelper.GetAttributeFromNode(feature, reNode, "MunicipalityName"));
        re.MunicipalityName = Helper.GetNormalizedString(munInfo.Name, 60);
        if (!string.IsNullOrEmpty(munInfo.Section))
        {
            re.SubunitOfLandRegister = munInfo.Section;
            re.SubunitOfLandRegisterDesignation = "Section";
        }

        re.MunicipalityCode = XmlHelper.GetAttributeFromNode(feature, reNode, "MunicipalityCode");
        re.Number = Helper.GetNormalizedString(XmlHelper.GetAttributeFromNode(feature, reNode, "Number"), 12);
        re.Type = GetRealEstateType(feature.type, XmlHelper.GetAttributeFromNode(feature, reNode, "Type"));

        // layerIndex quand on a plusieurs couches ?
        mainMap.layerIndex = "-1";
        mainMap.layerOpacity = 1.0;
        re.PlanForLandRegister = mainMap;
        printMap.layerIndex = "-1";
        printMap.layerOpacity = 1.0;
        re.PlanForLandRegisterMainPage = printMap;
        re.Limit = this.GetLimit(feature);

        re.RestrictionOnLandownership = this.GetRestrictionOnLandownership(themes, restrictions);

        return re;
    }

    private RealEstate_DPR GetRealEstate(IList<RestrictionTheme> themes, QueryResultFeature feature, RestrictionResult[] restrictions, byte[] mainImage, byte[] printImage)
    {
        Map mainMap = this.GetPlanForLandRegister(mainImage);
        Map printMap = this.GetPlanForLandRegister(printImage);

        return this.GetRealEstate(themes, feature, restrictions, mainMap, printMap);
    }

    private RealEstate_DPR GetRealEstate(IList<RestrictionTheme> themes, QueryResultFeature feature, RestrictionResult[] restrictions, string mapUrl, string printUrl)
    {
        Map mainMap = this.GetPlanForLandRegister(mapUrl);
        Map pageMap = this.GetPlanForLandRegister(printUrl);

        return this.GetRealEstate(themes, feature, restrictions, mainMap, pageMap);
    }

    private RestrictionOnLandownership[] GetRestrictionOnLandownership(IList<RestrictionTheme> themes, RestrictionResult[] restrictions)
    {
        IList<RestrictionOnLandownership> list = new List<RestrictionOnLandownership>();

        foreach (RestrictionResult restriction in restrictions)
        {
            XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", restriction.LayerName);

            if (node != null)
            {
                RestrictionResult first = restrictions.First(r => r.LayerId == restriction.LayerId && r.isFirst);
                list.Add(GetRestrictionOnLandownership(node, themes, first.AllLegends, restriction));
                foreach(int addId in this.restrWorker.GetAdditionalLayerIds(restriction.LayerId))
                {
                    foreach(RestrictionResult addResult in restrictions.Where(r => r.LayerId == addId).ToArray())
                    {
                        list.Add(GetRestrictionOnLandownership(node, themes, first.AllLegends, addResult));
                    }
                }
            }
        }

        return list.ToArray();
    }

    private RestrictionOnLandownership GetRestrictionOnLandownership(XmlNode node, IList<RestrictionTheme> themes, RestrictionLegend[] allLegends, RestrictionResult restriction)
    {
        string code = XmlHelper.GetXmlElementValue(node, "Theme/Code");
        int index = int.Parse(XmlHelper.GetXmlElementValue(node, "Theme/Index"));
        RestrictionTheme theme = themes.First(t => string.Compare(t.Code, code) == 0 && t.Index == index);

        RestrictionOnLandownership rol = new RestrictionOnLandownership();
        rol.LegendText = this.GetLocalisedMText(new string[] { restriction.Legend.Text });
        rol.Theme = this.GetTheme(theme);
        rol.TypeCode = Helper.ReduceString(restriction.Legend.TypeCode, 40);
        rol.Lawstatus = this.GetLawstatus(node, restriction);
        rol.AreaShare = restriction.Area.ToString();
        rol.PartInPercent = (decimal)restriction.PartInPercent;
        rol.LengthShare = restriction.Length.ToString();
        rol.NrOfPoints = restriction.PointNumber.ToString();

        if (this.param.withImages == true)
        {
            rol.Item = restriction.Legend.Symbol;
        }
        else
        {
            rol.Item = restriction.Legend.SymbolRef;
        }

        rol.Geometry = this.GetGeometries(node, restriction, rol.Lawstatus);
        rol.Map = this.GetMap(allLegends, restriction, rol.Theme);

        rol.ResponsibleOffice = this.GetOffice(node.SelectSingleNode("ResponsibleOffice"));
        IList<Document> documents = new List<Document>(
            this.GetLegalProvisions(node, restriction, rol.ResponsibleOffice));
        documents = documents.Concat<Document>(
            this.GetHints(node, restriction, rol.ResponsibleOffice)).ToList();
        documents = documents.Concat<Document>(
            this.GetLaws(node, rol.ResponsibleOffice)).ToList();

        rol.LegalProvisions = documents.ToArray();

        return rol;
    }

    private Map GetPlanForLandRegister(byte[] image)
    {
        Map map = this.GetMap(image);

        return map;
    }

    private Map GetPlanForLandRegister(string url)
    {
        Map map = this.GetMap(url);

        return map;
    }

    private Map GetMap(byte[] image)
    {
        Map map = new Map();

        map.Image = this.GetLocalisedBlob(image);

        return map;
    }

    private Map GetMap(string url)
    {
        Map map = new Map();

        map.ReferenceWMS = this.GetLocalisedUri(url);

        return map;
    }

    private LegendEntry[] GetOtherLegends(RestrictionLegend[] allLegends, string typeCode, Theme theme)
    {
        IList<LegendEntry> otherLegendList = new List<LegendEntry>();

        foreach (RestrictionLegend legend in allLegends.Where(l => string.Compare(l.TypeCode, typeCode) != 0))
        {
            LegendEntry entry = new LegendEntry
            {
                LegendText = GetLocalisedText(legend.Text),
                TypeCode = Helper.ReduceString(legend.TypeCode, 40),
                TypeCodelist = "-",
                Theme = theme
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

    private Map GetMap(RestrictionLegend[] allLegends, RestrictionResult restriction, Theme theme)
    {
        Map map;

        if (this.param.withImages)
        {
            map = this.GetMap(restriction.Image);
        }
        else
        {
            map = this.GetMap(restriction.MapUrl);
        }

        map.OtherLegend = this.GetOtherLegends(allLegends, restriction.Legend.TypeCode, theme);
        map.layerIndex = restriction.LayerId.ToString();
        map.layerOpacity = 1.0;

        return map;
    }

    private MultiSurfaceType GetLimit(QueryResultFeature feature)
    {
        MultiSurfaceType result = null;

        if (param.returnGeometry)
        {
            result = new MultiSurfaceType()
            {
                surface = GeometryHelper.GetPolygons(feature.geometry, GeometryHelper.GEOMETRY_SOURCE.esri)
            };
        }

        return result;
    }

    private Office GetOffice(XmlNode node)
    {
        return new Office
        {
            Name = this.GetLocalisedText(node, "Name"),
            OfficeAtWeb = this.GetLocalisedUri(node, "OfficeAtWeb")
        };
    }

    private Geometry[] GetGeometries(XmlNode node, RestrictionResult restriction, Lawstatus status)
    {
        IList<Geometry> results = new List<Geometry>();

        if (this.param.returnGeometry)
        {
            if (restriction.IdentResult.geometryType == "esriGeometryPolygon")
            {
                Geometry result = this.InitGeometry(node, status);
                result.Item = GeometryHelper.GetPolygons(restriction.IdentResult.geometry, GeometryHelper.GEOMETRY_SOURCE.esri);
                results.Add(result);
            }
            else if (restriction.IdentResult.geometryType == "esriGeometryPolyline")
            {
                foreach (PolylineType polyline in GeometryHelper.GetPolylines(restriction.IdentResult.geometry))
                {
                    Geometry result = this.InitGeometry(node, status);
                    result.Item = polyline;
                    results.Add(result);
                }
            }
            else
            {
                foreach (CoordType point in GeometryHelper.GetPoints(restriction.IdentResult.geometry))
                {
                    Geometry result = this.InitGeometry(node, status);
                    result.Item = point;
                    results.Add(result);
                }
            }
        }
        else
        {
            Geometry result = this.InitGeometry(node, status);
            results.Add(result);
        }

        return results.ToArray();
    }

    private Geometry InitGeometry(XmlNode node, Lawstatus status)
    {
        return new Geometry
        {
            Lawstatus = status,
            MetadataOfGeographicalBaseData = XmlHelper.GetXmlElementValue(node, "MetadataOfGeographicalBaseData")
        };
    }

    private Document[] GetLegalProvisions(XmlNode root, RestrictionResult restriction, Office responsibleOffice)
    {
        IList<Document> list = new List<Document>();

        int index = 1;
        foreach (XmlNode node in root.SelectNodes("LegalProvisionId"))
        {
            if (!string.IsNullOrEmpty(node.InnerText))
            {
                try
                {
                    XmlNode lpNode = XmlHelper.GetNodeByAttribute(this.docConfig, "LegalProvision", "id", node.InnerText);
                    Document doc = this.GetDocument(DocumentTypeCode.LegalProvision, lpNode, restriction, responsibleOffice, XmlHelper.GetXmlAttribute(node, "dateField", false));
                    doc.Index = index++.ToString();

                    list.Add(doc);
                }
                catch (Exception)
                {
                    throw new WsUserException(string.Format(Resources.Resource.ERROR_LEGALPROVISION, node.InnerText, restriction.IdentResult.layerName));
                }
            }
        }

        return list.ToArray();
    }

    private Document[] GetHints(XmlNode root, RestrictionResult restriction, Office responsibleOffice)
    {
        IList<Document> list = new List<Document>();

        int index = 1;
        foreach (XmlNode node in root.SelectNodes("HintId"))
        {
            if (!string.IsNullOrEmpty(node.InnerText))
            {
                try
                {
                    XmlNode hintNode = XmlHelper.GetNodeByAttribute(this.docConfig, "Hint", "id", node.InnerText);
                    Document doc = this.GetDocument(DocumentTypeCode.Hint, hintNode, restriction, responsibleOffice, string.Empty);
                    doc.Index = index++.ToString();

                    list.Add(doc);
                }
                catch (Exception)
                {
                    throw new WsUserException(string.Format(Resources.Resource.ERROR_INFORMATIONS, node.InnerText, restriction.IdentResult.layerName));
                }
            }
        }

        return list.ToArray();
    }

    private Document[] GetLaws(XmlNode root, Office responsibleOffice)
    {
        IList<Document> list = new List<Document>();

        foreach (XmlNode node in root.SelectNodes("LawId"))
        {
            if (!string.IsNullOrEmpty(node.InnerText))
            {
                XmlNode lawNode = XmlHelper.GetNodeByAttribute(this.docConfig, "Law", "id", node.InnerText);
                string id = XmlHelper.GetXmlElementValue(lawNode, "TID");

                Document doc = new Document();
                if (string.IsNullOrEmpty(id))
                {
                    doc.Title = this.GetLocalisedText(lawNode, "Title");
                    doc.Abbreviation = this.GetLocalisedText(lawNode, "Abbreviation");
                    doc.OfficialNumber = this.GetLocalisedText(lawNode, "OfficialNumber");
                    doc.TextAtWeb = this.GetLocalisedUri(lawNode, "TextAtWeb");
                    doc.Lawstatus = this.GetLawstatus(lawNode);
                    doc.Index = XmlHelper.GetXmlElementValue(lawNode, "Index");
                    doc.ResponsibleOffice = responsibleOffice;
                }
                else
                {
                    doc = oerebHelper.GetLaw(id, "fr");
                }
                doc.Type = new DocumentType
                {
                    Code = DocumentTypeCode.Law,
                    Text = GetLocalisedText(oerebHelper.GetDocumentTypeText(DocumentTypeCode.Law, "fr"))
                };
                doc.ArticleNumber = new string[] { string.Empty };


                list.Add(doc);
            }
        }

        return list.OrderBy(d => d.Index).ToArray();
    }

    private Document GetDocument(DocumentTypeCode code, XmlNode node, RestrictionResult restriction, Office responsibleOffice, string dateField)
    {
        string textAtWeb = XmlHelper.GetAttributeFromAttribute(restriction.IdentResult.attributes, node, "field");
        string date;

        if (string.IsNullOrEmpty(dateField))
        {
            date = XmlHelper.GetAttributeFromAttribute(restriction.IdentResult.attributes, node, "dateField");
        }
        else
        {
            date = restriction.IdentResult.attributes[dateField];
        }


        Document doc = new Document();
        doc.Type = new DocumentType
        {
            Code = code,
            Text = GetLocalisedText(oerebHelper.GetDocumentTypeText(code, "fr"))
        };
        doc.Title = this.GetLocalisedText(node, "Title");
        doc.TextAtWeb = this.GetLocalisedUri(textAtWeb);
        doc.Lawstatus = this.GetLawstatus(node);
        doc.ResponsibleOffice = responsibleOffice;

        DocumentExtension docExt = new DocumentExtension
        {
            Date = date
        };
        doc.extensions = new extensions
        {
            DocumentExtensions = new DocumentExtension[] { docExt }
        };

        return doc;
    }
    private Glossary[] GetGlossary()
    {
        IList<Glossary> list = new List<Glossary>();

        foreach (InformationText info in GetInformationConfig("Glossary"))
        {
            list.Add(new Glossary
            {
                Title = GetLocalisedText(info.Title),
                Content = GetLocalisedMText(info.Contents)
            });
        }

        return list.ToArray();
    }

    private Disclaimer[] GetDisclaimer()
    {
        IList<Disclaimer> list = new List<Disclaimer>();

        foreach (InformationText info in GetInformationConfig("Disclaimer"))
        {
            list.Add(new Disclaimer
            {
                Title = GetLocalisedText(info.Title),
                Content = GetLocalisedMText(info.Contents)
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
            OfficeAtWeb = this.GetLocalisedUri(node, "OfficeAtWeb"),
            Line1 = Helper.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "Line1"), 80),
            Line2 = Helper.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "Line2"), 80),
            City = Helper.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "City"), 60),
            Number = Helper.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "Number"), 7),
            PostalCode = Helper.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "PostalCode"), 4),
            Street = Helper.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "Street"), 100)
        };
    }
}

public class DocumentExtension
{
    public string Date;
}

namespace ExtractDataModel_v20
{
    public partial class extensions
    {
        private DocumentExtension[] documentExtensions;
        public DocumentExtension[] DocumentExtensions
        {
            get
            {
                return this.documentExtensions;
            }
            set
            {
                this.documentExtensions = value;
            }
        }
    }
}