/* $Rev: 30309 $ */
using ESRI.ArcGIS.SOAP;
using ExtractDataModel_v20;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Topomat.Web.Common;

public class CommonReq
{
    protected OeREBKRMHelper oerebHelper;
    protected LayerInfo mapLayerInfo;
    protected LegendInfo mapLegendInfo;
    protected QueryWorker queryWorker;
    protected RestrictionWorker restrWorker;
    protected GetExtractParamReq param;
    protected XmlNode requestConfig;
    protected XmlNode infoConfig;
    protected XmlNode docConfig;
    protected MapPrintParams printParams;

    public CommonReq()
    {
    }

    public QueryResultFeature ProcessEGRID(string egrid)
    {
        MapLayerInfo info = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName"), true);
        Field field = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("ParcelleEGRIDFieldName"));

        QueryResult qr = this.queryWorker.QueryAttrRequest(info.LayerID, QueryWorker.GetWhereClause(field, egrid), ParcelleType.BienFonds, true);
        if (qr.features.Length == 0)
        {
            info = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DDPLayerName"), true);
            field = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("DDPEGRIDFieldName"));

            qr = this.queryWorker.QueryAttrRequest(info.LayerID, QueryWorker.GetWhereClause(field, egrid), ParcelleType.DDP, true);
        }

        return qr.features.Length > 0 ? qr.features[0] : null;
    }

    public QueryResultFeature ProcessID(string identdn, string number)
    {
        MapLayerInfo info = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName"), true);
        Field noComField = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("ParcelleNoCommFieldName"));
        Field noParcField = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("ParcelleNoFieldName"));

        string clause = string.Format("{0} AND {1}", QueryWorker.GetWhereClause(noComField, identdn),
            QueryWorker.GetWhereClause(noParcField, number));
        QueryResult qr = this.queryWorker.QueryAttrRequest(info.LayerID, clause, ParcelleType.BienFonds, true);

        if (qr.features.Length == 0)
        {
            info = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DDPLayerName"), true);
            noComField = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("DDPNoCommFieldName"));
            noParcField = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("DDPNoFieldName"));

            clause = string.Format("{0} AND {1}", QueryWorker.GetWhereClause(noComField, identdn),
                QueryWorker.GetWhereClause(noParcField, number));
            qr = this.queryWorker.QueryAttrRequest(info.LayerID, clause, ParcelleType.DDP, true);
        }

        return qr.features.Length > 0 ? qr.features[0] : null;
    }

    protected void Init(GetExtractParamReq param, bool forReport)
    {
        Stopwatch timer = Stopwatch.StartNew();

        string token = TokenManager.GetToken();

        this.oerebHelper = new OeREBKRMHelper(new DataManager());
        this.mapLayerInfo = new LayerInfo(token, WebHelper.GetConfigValue("MapServiceUrl"));
        this.queryWorker = new QueryWorker(token);

        Helper.LogInfo(this.GetType().ToString(), "Récupération des layer infos", timer.ElapsedMilliseconds);
        timer.Restart();

        this.param = param;

        this.requestConfig = XmlHelper.GetConfig("request.xml", "RequestConfig");
        this.infoConfig = XmlHelper.GetConfig("information.xml", "InformationConfig");
        this.docConfig = XmlHelper.GetConfig("document.xml", "DocumentConfig");

        // call after mapLayerInfo and requestConfig has been instantiated
        this.mapLegendInfo = this.GetLegendInfo(token, WebHelper.GetConfigValue("MapServiceUrl"), forReport);
        this.restrWorker = new RestrictionWorker(this.mapLayerInfo, this.mapLegendInfo, this.queryWorker, param, !forReport);

        Helper.LogInfo(this.GetType().ToString(), "Récupération des legend infos", timer.ElapsedMilliseconds);
        timer.Stop();
    }

    protected LegendInfo GetLegendInfo(string token, string url, bool forReport)
    {
        IList<int> layerIds = new List<int>();
        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
            layerIds.Add(this.mapLayerInfo.GetLayerInfo(layerName, true).LayerID);

            string additionalLayers = XmlHelper.GetXmlAttribute(node, "additionalLayers", false);
            if (!string.IsNullOrEmpty(additionalLayers))
            {
                foreach (string additionalLayer in additionalLayers.Split(new char[] { ',' }))
                {
                    int id = this.mapLayerInfo.GetLayerInfo(additionalLayer, true).LayerID;
                    if (!layerIds.Contains(id))
                    {
                        layerIds.Add(id);
                    }
                }
            }

            string additionalLegends = XmlHelper.GetXmlAttribute(node, "additionalLegends", false);
            if (!string.IsNullOrEmpty(additionalLegends))
            {
                foreach (string additionalLegend in additionalLegends.Split(new char[] { ',' }))
                {
                    int id = this.mapLayerInfo.GetLayerInfo(additionalLegend, true).LayerID;
                    if (!layerIds.Contains(id))
                    {
                        layerIds.Add(id);
                    }
                }
            }
        }

        return new LegendInfo(token, url, layerIds.ToArray(), forReport);
    }

    protected int[] GetMapLayerIds(string[] layerNodeNames)
    {
        IList<int> layerIds = new List<int>();
        foreach (string layerNodeName in layerNodeNames)
        {
            foreach (XmlNode layerNode in this.requestConfig.SelectNodes(layerNodeName))
            {
                if (layerNode != null && !string.IsNullOrEmpty(layerNode.InnerText))
                {
                    layerIds.Add(this.mapLayerInfo.GetLayerInfo(layerNode.InnerText, true).LayerID);
                }
            }
        }
        return layerIds.ToArray();
    }

    protected string GetIdentifier(QueryResultFeature feature)
    {
        string guid = Guid.NewGuid().ToString("N").ToUpper();
        guid = guid.Replace("-", "");
        guid = guid.Insert(4, "-");
        guid = guid.Insert(9, "-");
        guid = guid.Insert(14, "-");
        guid = guid.Substring(0, 18);

        string noCom = string.Empty, noParc = string.Empty;
        if (feature.type == ParcelleType.BienFonds)
        {
            noCom = feature.attributes[WebHelper.GetConfigValue("ParcelleNoCommFieldName")];
            noParc = feature.attributes[WebHelper.GetConfigValue("ParcelleNoFieldName")];
        }
        else if (feature.type == ParcelleType.DDP)
        {
            noCom = feature.attributes[WebHelper.GetConfigValue("DDPNoCommFieldName")];
            noParc = feature.attributes[WebHelper.GetConfigValue("DDPNoFieldName")];
        }

        return string.Format("{0}-{1}-{2}", guid, noCom, noParc);
    }

    protected string GetFormattedDMODate()
    {
        DateTime? date = this.GetDMODate();
        if (date != null)
        {
            return ((DateTime)date).ToString("dd.MM.yyyy");
        }
        else
        {
            return string.Empty;
        }
    }

    protected DateTime? GetDMODate()
    {
        int layerId = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DateDmoLayerName"), true).LayerID;
        string field = WebHelper.GetConfigValue("DateDmoFieldName");

        QueryResult qr = this.queryWorker.QueryAttrRequest(layerId, "1=1", ParcelleType.Undefined, false);
        if (qr.features[0] != null && qr.features[0].attributes[field] != null)
        {
            string value = qr.features[0].attributes[field];
            double millisecs;
            if (double.TryParse(value, out millisecs))
            {
                DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                return epoch.AddMilliseconds(millisecs);
            }
        }

        return null;
    }

    protected IList<RestrictionTheme> GetThemes()
    {
        IList<RestrictionTheme> themes = new List<RestrictionTheme>();

        foreach (XmlNode rNode in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            XmlNode tNode = rNode.SelectSingleNode("Theme");
            themes.Add(new RestrictionTheme
            {
                Code = XmlHelper.GetXmlElementValue(tNode, "Code"),
                SubText = XmlHelper.GetXmlElementValue(tNode, "Text"),
                Index = int.Parse(XmlHelper.GetXmlElementValue(tNode, "Index")),
                Layer = XmlHelper.GetXmlAttribute(rNode, "layer", true)
            });
        }

        foreach (IList<RestrictionTheme> group in themes.GroupBy(t => t.Code))
        {
            foreach (RestrictionTheme theme in group)
            {
                theme.Text = this.oerebHelper.GetThemeText(theme.Code, "fr");
                theme.SubCode = string.Empty;
                theme.IsSubTheme = false;
                if (group.Count > 1)
                {
                    if (string.IsNullOrEmpty(theme.SubText))
                    {
                        string info = string.Format("Noeud <RestrictionOnLandownership> avec attribut layer={0}", theme.Layer);
                        throw new WsUserException(string.Format(Resources.Resource.ERROR_REQUESTS, info));
                    }
                    theme.SubCode = string.Format("ch.GE.{0}", Regex.Replace(theme.SubText, @"[^a-zA-Z]", string.Empty));
                    theme.IsSubTheme = true;
                }
            }
        }

        return themes.OrderBy(t => t.Index).ToList();
    }

    protected Theme[] GetThemeWithoutData()
    {
        IList<RestrictionTheme> themes = new List<RestrictionTheme>();
        foreach (XmlNode node in infoConfig.SelectNodes("ThemeWithoutData"))
        {
            string code = XmlHelper.GetXmlElementValue(node, "Code");
            themes.Add(new RestrictionTheme
            {
                Code = code,
                Text = oerebHelper.GetThemeText(code, "fr"),
                Index = int.Parse(XmlHelper.GetXmlElementValue(node, "Index"))
            });
        }

        IList<Theme> list = new List<Theme>();
        foreach (RestrictionTheme theme in themes.OrderBy(t => t.Index))
        {
            list.Add(this.GetTheme(theme));
        }
        return list.ToArray();
    }

    protected Theme GetTheme(RestrictionTheme theme)
    {
        string text = theme.IsSubTheme ? string.Format("{0}: {1}", theme.Text, theme.SubText) : theme.Text;
        return new Theme
        {
            Code = theme.Code,
            SubCode = theme.IsSubTheme ? theme.SubCode : null,
            Text = GetLocalisedText(text)
        };
    }

    protected RealEstateType GetRealEstateType(ParcelleType type, string value)
    {
        RealEstateTypeCode code = GetRealEstateTypeCode(type, value);
        return new RealEstateType
        {
            Code = code,
            Text = GetLocalisedText(oerebHelper.GetRealEstateTypeText(code, "fr"))
        };
    }

    protected RealEstateTypeCode GetRealEstateTypeCode(ParcelleType type, string value)
    {
        RealEstateTypeCode code = RealEstateTypeCode.RealEstate;
        switch (type)
        {
            case ParcelleType.BienFonds:
                code = RealEstateTypeCode.RealEstate;
                break;
            case ParcelleType.DDP:
                switch (value)
                {
                    case "1":
                    case "2":
                        code = RealEstateTypeCode.Distinct_and_permanent_rightsBuildingRight;
                        break;
                    case "3":
                        code = RealEstateTypeCode.Distinct_and_permanent_rightsright_to_spring_water;
                        break;
                    default:
                        code = RealEstateTypeCode.Distinct_and_permanent_rightsother;
                        break;
                }
                break;
        }
        return code;
    }

    protected LocalisedMText[] GetLocalisedMText(XmlNode root, string name)
    {
        IList<LocalisedMText> list = new List<LocalisedMText>();

        foreach (XmlNode node in root.SelectNodes(name))
        {
            list.Add(new LocalisedMText
            {
                Language = LanguageCode.fr,
                Text = node.InnerText
            });
        }

        return list.ToArray();
    }

    protected LocalisedMText[] GetLocalisedMText(string[] texts)
    {
        IList<LocalisedMText> list = new List<LocalisedMText>();

        foreach (string text in texts)
        {
            list.Add(new LocalisedMText
            {
                Language = LanguageCode.fr,
                Text = text
            });
        }
            

        return list.ToArray();
    }

    protected LocalisedText[] GetLocalisedText(XmlNode root, string name)
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

    protected LocalisedText[] GetLocalisedText(string text)
    {
        LocalisedText localisedText = new LocalisedText
        {
            Language = LanguageCode.fr,
            Text = text == null ? string.Empty : text
        };

        return new LocalisedText[] { localisedText };
    }

    protected LocalisedUri[] GetLocalisedUri(XmlNode root, string name)
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

    protected LocalisedUri[] GetLocalisedUri(string text)
    {
        LocalisedUri uri = new LocalisedUri
        {
            Language = SchemaHelper.GetLanguageCode(this.param.lang),
            Text = text == null ? string.Empty : text
        };

        return new LocalisedUri[] { uri };
    }

    protected LocalisedBlob[] GetLocalisedBlob(byte[] data)
    {
        LocalisedBlob blob = new LocalisedBlob
        {
            Language = SchemaHelper.GetLanguageCode(this.param.lang),
            Blob = data
        };

        return new LocalisedBlob[] { blob };
    }

    protected IList<InformationText> GetInformationConfig(string nodeId)
    {
        IList<InformationText> infos = new List<InformationText>();
        foreach (XmlNode node in this.infoConfig.SelectNodes(nodeId))
        {
            string id = XmlHelper.GetXmlElementValue(node, "TID");
            if (!string.IsNullOrEmpty(id))
            {
                KeyValuePair<string, string> info = new KeyValuePair<string, string>(string.Empty, string.Empty);
                switch (nodeId)
                {
                    case "Disclaimer":
                        info = oerebHelper.GetDisclaimer(id, param.lang);
                        break;
                    case "Glossary":
                        info = oerebHelper.GetGlossary(id, param.lang);
                        break;
                }
                infos.Add(new InformationText
                {
                    Title = info.Key,
                    Contents = new string[] { info.Value }
                });
            }
            else
            {
                IList<string> contents = new List<string>();
                foreach (XmlNode n in node.SelectNodes("Content"))
                {
                    contents.Add(n.InnerText);
                }
                string use = XmlHelper.GetXmlAttribute(node, "useInStaticExtract", false);
                infos.Add(new InformationText
                {
                    UseInStaticExtract = !string.IsNullOrEmpty(use) && string.Compare(use, "false") == 0 ? false : true,
                    Title = XmlHelper.GetXmlElementValue(node, "Title"),
                    Contents = contents.ToArray()
                });
            }            
        }
        return infos;
    }
}

public class RestrictionTheme
{
    public string Code { get; set; }
    public string SubCode { get; set; }
    public string Text { get; set; }
    public string SubText { get; set; }
    public int Index { get; set; }
    public string Layer { get; set; }
    public bool IsSubTheme { get; set; }
}

public class InformationText
{
    public bool UseInStaticExtract { get; set; }
    public string Title { get; set; }
    public string[] Contents { get; set; }
}