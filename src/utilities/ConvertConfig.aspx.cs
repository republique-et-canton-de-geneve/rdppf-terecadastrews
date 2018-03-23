/* $Rev: 14634 $ */
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Topomat.Web.Common;

public partial class ConvertConfig : System.Web.UI.Page
{
    private static string xmlStaticFile = @"D:\exploitation\9888\utilities\StaticConfig.xml";

    private static IDictionary<int, string> dictLaws = new Dictionary<int, string>
    {
        { 1, "LAT" },
        { 2, "OAT" },
        { 3, "LALAT" },
        { 4, "LGZD" },
        { 5, "LZIAM" },
        { 6, "LCI" },
        { 7, "Lext" },
        { 8, "LEaux" },
        { 9, "OEaux" },
        { 10, "LEaux-GE" },
        { 11, "REaux-GE" },
        { 12, "LPE" },
        { 13, "OPB" },
        { 14, "LALPE" },
        { 15, "RPBV" },
        { 16, "LFo" },
        { 17, "OFo" },
        { 18, "LForêt" },
        { 19, "RForêt" },
        { 20, "RAZIDI" },
        { 21, "LPMNS" },
        { 22, "LPRLac" },
        { 23, "LPRRhône" },
        { 24, "LPRArve" },
        { 25, "LGEA" },
        { 26, "RGEA" },
        { 27, "LPRVers" },
        { 28, "Osites" },
        { 29, "LaLSC" },
        { 30, "LRM" },
        { 31, "LCdF" },
        { 32, "LA" },
        { 33, "OSIA" }
    };

    protected void Page_Load(object sender, EventArgs e)
    {

    }

    protected void ConvertBtn_Click(object sender, EventArgs e)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(OutputRequestCfg(SiteUrl.Text));
        sb.AppendLine(OutputCommuneCfg(SiteUrl.Text));
        sb.AppendLine("Terminé.");

        Result.Text = sb.ToString();
    }

    private string OutputRequestCfg(string url)
    {
        string jsonRequest, jsMainMapLayers, jsRestrictionMapLayers;
        using (WebClient client = new WebClient())
        {
            jsonRequest = client.DownloadString(url + "/js/config/requests.json");
            string jsRdppf = client.DownloadString(url + "/js/config/rdppf.js");

            jsMainMapLayers = this.GetLayerList(jsRdppf, "mainMapLayers");
            jsRestrictionMapLayers = this.GetLayerList(jsRdppf, "restrictionMapLayers");
        }

        JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
        RequestCfgJson jsonRequestConfig = jsSerializer.Deserialize<RequestCfgJson>(jsonRequest);

        LayerInfo layerInfos = new LayerInfo(TokenManager.GetToken(), WebHelper.GetConfigValue("MapServiceUrl"));

        IDictionary<string, string> dictLegalFields = new Dictionary<string, string>();
        IDictionary<string, string> dictInfoFields = new Dictionary<string, string>();
        XmlNode legalConfig = XmlHelper.GetConfig("legalProvision.xml", "LegalProvisionConfig");

        foreach (XmlNode node in legalConfig.SelectNodes("Regulations/LegalProvisions"))
        {
            dictLegalFields.Add(XmlHelper.GetXmlAttribute(node, "field", false),
                XmlHelper.GetXmlAttribute(node, "id", false));
        }
        foreach (XmlNode node in legalConfig.SelectNodes("Informations/LegalProvisions"))
        {
            dictInfoFields.Add(XmlHelper.GetXmlAttribute(node, "field", false),
                XmlHelper.GetXmlAttribute(node, "id", false));
        }

        XmlDocument staticDoc = new XmlDocument();
        staticDoc.Load(xmlStaticFile);
        XmlNode staticConfig = staticDoc.SelectSingleNode("StaticConfig");

        XmlDocument doc = new XmlDocument();

        doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", "yes"));
        XmlElement root = doc.CreateElement("RequestConfig");

        // get mapLayers
        IList<string> mainMapLayerList = jsSerializer.Deserialize<string[]>(jsMainMapLayers);
        IList<string> restrMapLayerList = jsSerializer.Deserialize<string[]>(jsRestrictionMapLayers);
        IList<string> addMapLayers = new List<string>();
        IList<string> mainMapLayers = new List<string>();
        IList<string> restrictionMapLayers = new List<string>();
        foreach (string layer in mainMapLayerList)
        {
            if (string.Compare(layer, "marker") != 0)
            {
                if (restrMapLayerList.Contains(layer))
                {
                    addMapLayers.Add(layer);
                }
                else
                {
                    mainMapLayers.Add(layer);
                }
            }
        }
        foreach (string layer in restrMapLayerList)
        {
            if (!mainMapLayerList.Contains(layer))
            {
                restrictionMapLayers.Add(layer);
            }
        }
        foreach (string layer in addMapLayers)
        {
            XmlHelper.AppendElement(doc, root, "addMapLayer", layer);
        }
        foreach (string layer in mainMapLayers)
        {
            XmlHelper.AppendElement(doc, root, "mainMapLayer", layer);
        }
        foreach (string layer in restrictionMapLayers)
        {
            XmlHelper.AppendElement(doc, root, "restrictionMapLayer", layer);
        }

        // get RealEstate
        XmlNode reNode = doc.ImportNode(staticConfig.SelectSingleNode("RealEstate"), true);
        root.AppendChild(reNode);

        // get restrictions
        string defaultLawstatusField = XmlHelper.GetXmlElementValue(staticConfig, "DefaultLawstatusField");
        foreach (RestrictionCfgJson jsonRestr in jsonRequestConfig.restrictions)
        {
            int parentId = layerInfos.GetLayerInfo(jsonRestr.test.layer).ParentLayerID;

            XmlNode staticNode = XmlHelper.GetNodeByAttribute(staticConfig, "RestrictionOnLandownership", "layer", jsonRestr.test.layer);
            
            IDictionary<string, string> attributes = new Dictionary<string, string> {
                { "layer", jsonRestr.test.layer },
                { "complete", jsonRestr.test.complete.ToString().ToLower() }
            };
            if (jsonRestr.test.overlap == true)
            {
                attributes.Add("overlap", "true");
            }
            if (jsonRestr.additionnalLayers != null && jsonRestr.additionnalLayers.Length > 0)
            {
                IList<string> addLayers = new List<string>();
                foreach (AdditionalLayerCfgJson al in jsonRestr.additionnalLayers)
                {
                    addLayers.Add(al.layer);
                }
                attributes.Add("additionalLayers", string.Join(",", addLayers));
            }
            XmlElement elRestr = XmlHelper.AppendElement(doc, root, "RestrictionOnLandownership", attributes);
            XmlHelper.AppendElement(doc, elRestr, "Information", layerInfos.GetLayerInfoAsJson(parentId).name);
            XmlElement elTheme = XmlHelper.AppendElement(doc, elRestr, "Theme");
            XmlHelper.AppendElement(doc, elTheme, "Code", XmlHelper.GetXmlElementValue(staticNode, "ThemeCode"));
            XmlHelper.AppendElement(doc, elTheme, "Text", jsonRestr.title);
            XmlHelper.AppendElement(doc, elRestr, "SubTheme", jsonRestr.id);

            string field = defaultLawstatusField;
            if (staticNode.SelectSingleNode("LawstatusField") != null)
            {
                field = XmlHelper.GetXmlElementValue(staticNode, "LawstatusField");
            }
            attributes = new Dictionary<string, string> {
                    { "field", field }
                };
            XmlHelper.AppendElement(doc, elRestr, "Lawstatus", attributes);

            XmlHelper.AppendElement(doc, elRestr, "MetadataOfGeographicalBaseData", string.Empty);
            if (jsonRestr.regulations != null)
            {
                foreach (FieldCfgJson regulation in jsonRestr.regulations)
                {
                    if (dictLegalFields.ContainsKey(regulation.field))
                    {
                        XmlHelper.AppendElement(doc, elRestr, "regulationId", dictLegalFields[regulation.field]);
                    }
                    else
                    {
                        XmlHelper.AppendElement(doc, elRestr, "regulationId", "ERROR");
                    }
                }
            }
            if (jsonRestr.lawIds != null)
            {
                foreach (int id in jsonRestr.lawIds)
                {
                    if (ConvertConfig.dictLaws.ContainsKey(id))
                    {
                        XmlHelper.AppendElement(doc, elRestr, "lawId", ConvertConfig.dictLaws[id]);
                    }
                    else
                    {
                        XmlHelper.AppendElement(doc, elRestr, "lawId", "ERROR");
                    }
                }
            }
            if (jsonRestr.informations != null)
            {
                foreach (FieldCfgJson information in jsonRestr.informations)
                {
                    if (dictInfoFields.ContainsKey(information.field))
                    {
                        XmlHelper.AppendElement(doc, elRestr, "informationId", dictInfoFields[information.field]);
                    }
                    else
                    {
                        XmlHelper.AppendElement(doc, elRestr, "informationId", "ERROR");
                    }
                }
            }
            XmlElement elOffice = XmlHelper.AppendElement(doc, elRestr, "ResponsibleOffice");
            XmlHelper.AppendElement(doc, elOffice, "Name", jsonRestr.service.name);
            XmlHelper.AppendElement(doc, elOffice, "OfficeAtWeb", jsonRestr.service.link);
        }

        doc.AppendChild(root);

        string path = System.IO.Path.Combine(WebHelper.GetConfigValue("ConfigPath"), "converted");
        path = System.IO.Path.Combine(path, "request.xml");

        doc.Save(path);

        return path;
    }

    private string OutputCommuneCfg(string url)
    {
        string json;
        using (WebClient client = new WebClient())
        {
            json = client.DownloadString(url + "/js/config/communes.json");
        }

        JavaScriptSerializer jsSerializer = new JavaScriptSerializer();
        CommuneConfig config = new CommuneConfig();
        config.Items = jsSerializer.Deserialize<CommuneCfg[]>(json);

        XmlDocument xmlDoc = new XmlDocument();

        XmlRootAttribute xRoot = new XmlRootAttribute("CommuneConfig");

        StringBuilder sb = new StringBuilder();
        XmlSerializer xmlSerializer = new XmlSerializer(config.GetType(), xRoot);
        xmlSerializer.Serialize(XmlWriter.Create(sb), config);

        xmlDoc.LoadXml(sb.ToString());

        string path = System.IO.Path.Combine(WebHelper.GetConfigValue("ConfigPath"), "converted");
        path = System.IO.Path.Combine(path, "commune.xml");

        xmlDoc.Save(path);

        return path;
    }

    private string GetLayerList(string input, string name)
    {
        int markerPos = input.IndexOf(name);
        int startPos = input.IndexOf("[", markerPos);
        int endPos = input.IndexOf("]", markerPos);
        return input.Substring(startPos, endPos - startPos + 1);
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

    private class DataCfgJson
    {
        public string id;
        public SectionCfgJson[] sections;
    }

    private class SectionCfgJson
    {
        public string id;
        public string layer;
        public double buffer;
    }

    private class LawCfgJson
    {
        public string id;
        public string title;
        public string link;
    }

    private class TestCfgJson
    {
        public string layer;
        public bool complete;
        public bool overlap;
        public double buffer;
    }

    private class AdditionalLayerCfgJson
    {
        public string layer;
        public bool legend;
    }

    private class FieldCfgJson
    {
        public string field;
        public string label;
    }

    private class ServiceCfgJson
    {
        public string name;
        public string link;
    }

    private class RequestCfgJson
    {
        public DataCfgJson[] data;
        public RestrictionCfgJson[] restrictions;
        public LawCfgJson[] laws;
    }

    private class RestrictionCfgJson
    {
        public string id;
        public string title;
        public string legendLink;
        public TestCfgJson test;
        public AdditionalLayerCfgJson[] additionnalLayers;
        public FieldCfgJson[] regulations;
        public int[] lawIds;
        public FieldCfgJson[] informations;
        public ServiceCfgJson service;
    }
}