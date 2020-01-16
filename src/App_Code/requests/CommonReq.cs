/* $Rev: 22461 $ */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Text.RegularExpressions;
using Topomat.Web.Common;
using ExtractData_v103;
using System.Diagnostics;

public class CommonReq
{
    private static string regexThemeCode = @"LandUsePlans|MotorwaysProjectPlaningZones|MotorwaysBuildingLines|RailwaysProjectPlanningZones|RailwaysBuildingLines|AirportsProjectPlanningZones|AirportsBuildingLines|AirportsSecurityZonePlans|ContaminatedSites|ContaminatedMilitarySites|ContaminatedCivilAviationSites|ContaminatedPublicTransportSites|GroundwaterProtectionZones|GroundwaterProtectionSites|NoiseSensitivityLevels|ForestPerimeters|ForestDistanceLines|(ch\.[A-Z]{2}\.[a-zA-Z][a-zA-Z0-9]*)|(ch\.[0-9]{4}\.[a-zA-Z][a-zA-Z0-9]*)|(fl\.[a-zA-Z][a-zA-Z0-9]*)";

    protected LayerInfo mapLayerInfo;
    protected LegendInfo mapLegendInfo;
    protected QueryWorker queryWorker;
    protected RestrictionWorker restrWorker;
    protected string flavour;
    protected GetExtractParamReq param;
    protected XmlNode requestConfig;
    protected XmlNode infoConfig;
    protected XmlNode legalConfig;
    protected MapPrintParams printParams;

    public CommonReq()
    {
    }

    public bool IsReduced()
    {
        return string.Compare(this.flavour.ToUpper(), "REDUCED") == 0 ? true : false;
    }
    
    public bool IsEmbeddable()
    {
        return string.Compare(this.flavour.ToUpper(), "EMBEDDABLE") == 0 ? true : false;
    }

    public QueryResultFeature ProcessEGRID(string egrid)
    {
        ParcelleType type = ParcelleType.BienFonds;

        int id = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName")).LayerID;
        string clause = string.Format("{0}='{1}'", WebHelper.GetConfigValue("ParcelleEGRIDFieldName"), egrid);

        QueryResult qr = this.queryWorker.QueryAttrRequest(id, clause, true);

        if (qr.features.Length == 0)
        {
            id = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DDPLayerName")).LayerID;
            clause = string.Format("{0}='{1}'", WebHelper.GetConfigValue("DDPEGRIDFieldName"), egrid);

            qr = this.queryWorker.QueryAttrRequest(id, clause, true);
            type = ParcelleType.DDP;
        }

        QueryResultFeature feature = null;
        if (qr.features.Length == 1)
        {
            feature = qr.features[0];
            feature.type = type;
        }

        return feature;
    }

    public QueryResultFeature ProcessID(string identdn, string number)
    {
        ParcelleType type = ParcelleType.BienFonds;

        int id = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName")).LayerID;
        string clause = string.Format("{0}={1} AND {2}={3}", WebHelper.GetConfigValue("ParcelleNoCommFieldName"), identdn,
            WebHelper.GetConfigValue("ParcelleNoFieldName"), number);

        QueryResult qr = this.queryWorker.QueryAttrRequest(id, clause, true);

        if (qr.features.Length == 0)
        {
            id = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DDPLayerName")).LayerID;
            clause = string.Format("{0}={1} AND {2}={3}", WebHelper.GetConfigValue("DDPNoCommFieldName"), identdn,
                WebHelper.GetConfigValue("DDPNoFieldName"), number);

            qr = this.queryWorker.QueryAttrRequest(id, clause, true);
            type = ParcelleType.DDP;
        }

        QueryResultFeature feature = null;
        if (qr.features.Length == 1)
        {
            feature = qr.features[0];
            feature.type = type;
        }

        return feature;
    }

    protected void Init(GetExtractParamReq param, string flavour, bool forReport)
	{
        Stopwatch timer = Stopwatch.StartNew();

        string token = TokenManager.GetToken();

        Helper.LogInfo(this.GetType().ToString(), "Init - récupération du token", timer.ElapsedMilliseconds);
        timer.Restart();

        this.mapLayerInfo = new LayerInfo(token, WebHelper.GetConfigValue("MapServiceUrl"));
        this.queryWorker = new QueryWorker(token);

        Helper.LogInfo(this.GetType().ToString(), "Init - récupération des layer infos", timer.ElapsedMilliseconds);
        timer.Restart();

        this.flavour = flavour;
        this.param = param;

        this.requestConfig = XmlHelper.GetConfig("request.xml", "RequestConfig");
        this.infoConfig = XmlHelper.GetConfig("information.xml", "InformationConfig");
        this.legalConfig = XmlHelper.GetConfig("legalProvision.xml", "LegalProvisionConfig");

        Helper.LogInfo(this.GetType().ToString(), "Init - récupération de la configuration", timer.ElapsedMilliseconds);
        timer.Restart();

        // call after mapLayerInfo and requestConfig has been instantiated
        this.mapLegendInfo = this.GetLegendInfo(token, WebHelper.GetConfigValue("MapServiceUrl"), forReport);
        this.restrWorker = new RestrictionWorker(this.mapLayerInfo, this.mapLegendInfo, this.queryWorker, param);

        Helper.LogInfo(this.GetType().ToString(), "Init - récupération des legend infos", timer.ElapsedMilliseconds);
        timer.Stop();
	}

    protected LegendInfo GetLegendInfo(string token, string url, bool forReport)
    {
        IList<int> layerIds = new List<int>();
        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
            layerIds.Add(this.mapLayerInfo.GetLayerInfo(layerName).LayerID);

            string additionalLayers = XmlHelper.GetXmlAttribute(node, "additionalLayers", false);
            if (!string.IsNullOrEmpty(additionalLayers))
            {
                foreach (string additionalLayer in additionalLayers.Split(new char[] { ',' }))
                {
                    int id = this.mapLayerInfo.GetLayerInfo(additionalLayer).LayerID;
                    if(!layerIds.Contains(id))
                    {
                        layerIds.Add(id);
                    }                    
                }
            }
        }

        return new LegendInfo(token, url, layerIds.ToArray(), this.param.withImages, forReport);
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
                    layerIds.Add(this.mapLayerInfo.GetLayerInfo(layerNode.InnerText).LayerID);
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
        int layerId = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DateDmoLayerName")).LayerID;
        string field = WebHelper.GetConfigValue("DateDmoFieldName");

        QueryResult qr = this.queryWorker.QueryAttrRequest(layerId, "1=1", false);
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

    protected Theme[] GetThemeWithoutData()
    {
        IList<Theme> list = new List<Theme>();

        foreach (XmlNode node in this.infoConfig.SelectNodes("ThemeWithoutData"))
        {
            list.Add(this.GetTheme(node));
        }

        return list.ToArray();
    }

    protected Theme GetTheme(XmlNode node)
    {
        Regex r = new Regex(CommonReq.regexThemeCode);

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
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = node.SelectSingleNode("Text").InnerText
            }
        };
    }

    protected LocalisedMText[] GetBaseData()
    {
        IList<LocalisedMText> list = new List<LocalisedMText>();

        foreach (XmlNode node in this.infoConfig.SelectNodes("BaseData"))
        {
            string text = node.InnerText;
            if (text.Contains("###DMODATE###"))
            {
                text = text.Replace("###DMODATE###", this.GetFormattedDMODate());
            }
            list.Add(new LocalisedMText
            {
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = text
            });
        }

        return list.ToArray();
    }

    protected Glossary[] GetGlossary()
    {
        IList<Glossary> list = new List<Glossary>();

        foreach (XmlNode node in this.infoConfig.SelectNodes("Glossary"))
        {
            LocalisedText title = new LocalisedText
            {
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = XmlHelper.GetXmlElementValue(node, "Title")
            };
            LocalisedMText content = new LocalisedMText
            {
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = XmlHelper.GetXmlElementValue(node, "Content")
            };
            list.Add(new Glossary
            {
                Title = new LocalisedText[] { title },
                Content = new LocalisedMText[] { content }
            });
        }

        return list.ToArray();
    }

    protected ExclusionOfLiability[] GetExclusionOfLiability()
    {
        IList<ExclusionOfLiability> list = new List<ExclusionOfLiability>();

        foreach (XmlNode node in this.infoConfig.SelectNodes("ExclusionOfLiability"))
        {
            LocalisedText title = new LocalisedText
            {
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = XmlHelper.GetXmlElementValue(node, "Title")
            };
            LocalisedMText content = new LocalisedMText
            {
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = XmlHelper.GetXmlElementValue(node, "Content")
            };
            list.Add(new ExclusionOfLiability
            {
                Title = new LocalisedText[] { title },
                Content = new LocalisedMText[] { content }
            });
        }

        return list.ToArray();
    }

    protected Office GetPLRCadastreAuthority(XmlNode root, string name)
    {
        XmlNode node = root.SelectSingleNode(name);

        return new Office
        {
            Name = this.GetLocalisedText(node, "Name"),
            OfficeAtWeb = new WebReference
            {
                Value = XmlHelper.GetXmlElementValue(node, "OfficeAtWeb")
            },
            Line1 = this.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "Line1"), 80),
            Line2 = this.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "Line2"), 80),
            City = this.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "City"), 60),
            Number = this.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "Number"), 7),
            PostalCode = this.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "PostalCode"), 4),
            Street = this.GetNormalizedString(XmlHelper.GetXmlElementValue(node, "Street"), 100)
        };
    }

    protected LocalisedMText[] GetLocalisedMText(XmlNode root, string name)
    {
        IList<LocalisedMText> list = new List<LocalisedMText>();

        foreach (XmlNode node in root.SelectNodes(name))
        {
            list.Add(new LocalisedMText
            {
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = node.InnerText
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
                LanguageSpecified = true,
                Language = LanguageCode.fr,
                Text = node.InnerText
            });
        }

        return list.ToArray();
    }

    protected string GetNormalizedString(string input, int length)
    {
        return input.Length > length ?
            input.Substring(0, length) : input;
    }
}