/* $Rev: 29811 $ */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Topomat.Web.Common;

public class XmlHelper
{
    public XmlHelper()
    {
    }

    public static CommuneConfig GetCommuneConfig()
    {
        string communeCfgFile = Path.Combine(WebHelper.GetConfigValue("ConfigPath"), "commune.xml");

        XmlSerializer serializer = new XmlSerializer(typeof(CommuneConfig));
        using (StreamReader reader = new StreamReader(communeCfgFile))
        {
            return (CommuneConfig)serializer.Deserialize(reader);
        }
    }

    public static MapPrintConfig GetMapPrintConfig()
    {
        string cfgFile = Path.Combine(WebHelper.GetConfigValue("ConfigPath"), "mapPrintConfig.xml");

        XmlSerializer serializer = new XmlSerializer(typeof(MapPrintConfig));
        using (StreamReader reader = new StreamReader(cfgFile))
        {
            return (MapPrintConfig)serializer.Deserialize(reader);
        }
    }

    public static XmlNode GetConfig(string fileName, string name)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(Path.Combine(WebHelper.GetConfigValue("ConfigPath"), fileName));

        return doc.SelectSingleNode(name);
    }

    public static string GetXmlAttribute(XmlNode node, string name, bool required)
    {
        if (node.Attributes[name] == null)
        {
            if (required)
            {
                throw new Exception(string.Format(Resources.Resource.MISSING_XML_ATTRIBUTE, name, node.OuterXml));
            }
            else
            {
                return null;
            }
        }
        return node.Attributes[name].Value;
    }

    public static XmlElement GetXmlElement(object obj)
    {
        XmlDocument xmlDoc = new XmlDocument();

        StringBuilder sb = new StringBuilder();
        XmlSerializer xmlSerializer = new XmlSerializer(obj.GetType());
        xmlSerializer.Serialize(XmlWriter.Create(sb), obj);

        xmlDoc.LoadXml(sb.ToString());

        return xmlDoc.DocumentElement;
    }

    public static XmlElement GetXmlElement(object obj, XmlSerializerNamespaces ns)
    {
        XmlDocument doc = new XmlDocument();

        using (XmlWriter writer = doc.CreateNavigator().AppendChild())
        {
            new XmlSerializer(obj.GetType()).Serialize(writer, obj, ns);
        }

        return doc.DocumentElement;
    }

    public static XmlElement GetXmlElement(XmlDocument doc, string prefix, string name, string ns, string text)
    {
        XmlElement el = doc.CreateElement(prefix, name, ns);
        if (!string.IsNullOrEmpty(text))
        {
            el.InnerText = text;
        }
        return el;
    }

    public static string GetXmlElementValue(XmlNode root, string name)
    {
        string value = string.Empty;
        XmlNode node = root.SelectSingleNode(name);
        if (node != null)
        {
            value = node.InnerText;
        }
        return value;
    }

    public static XmlNode GetNodeByAttribute(XmlNode root, string name, string attribute, string value)
    {
        foreach (XmlNode node in root.SelectNodes(name))
        {
            if (XmlHelper.GetXmlAttribute(node, attribute, true) == value)
            {
                return node;
            }
        }
        return null;
    }

    public static string GetAttributeFromNode(QueryResultFeature feature, XmlNode parent, string name)
    {
        string result = string.Empty;

        XmlNode node = parent.SelectSingleNode(name);
        if (node != null && !string.IsNullOrEmpty(node.InnerText))
        {
            result = feature.attributes[node.InnerText];
        }

        return result;
    }

    public static string GetAttributeFromAttribute(IDictionary<string, string> attributes, XmlNode node, string attribute)
    {
        string result = string.Empty;

        string xmlAttribute = XmlHelper.GetXmlAttribute(node, attribute, false);
        if (!string.IsNullOrEmpty(xmlAttribute))
        {
            if(attributes.ContainsKey(xmlAttribute))
            {
                string value = attributes[xmlAttribute];
                if (!(string.IsNullOrEmpty(value) || string.Compare(value, "Null") == 0))
                {
                    result = value;
                }
            }
            else
            {
                foreach(string key in attributes.Keys)
                {
                    Helper.LogDebug("XmlHelper.GetAttributeFromAttribute()", string.Format("attributes[{0}]={1}", key, attributes[key]));
                }
                throw new WsUserException(string.Format("GetAttributeFromAttribute - Erreur: attribute={0}, xmlAttribute={1}", attribute, xmlAttribute));
            }            
        }

        return result;
    }
}