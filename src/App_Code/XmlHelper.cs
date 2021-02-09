/* $Rev: 25011 $ */
using System;
using System.Xml;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using Topomat.Web.Common;
using System.Collections.Generic;
using System.Xml.Linq;

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

    public static XmlElement GetXmlElement(IDictionary<string, string> info, List<KeyValuePair<string, string>> items)
    {
        XmlDocument doc = new XmlDocument();

        if (info.ContainsKey("name") && info.ContainsKey("namespace"))
        {
            XmlElement rootElement = doc.CreateElement(info["name"], info["namespace"]);
            doc.AppendChild(rootElement);

            foreach (KeyValuePair<string, string> item in items)
            {
                XmlElement el = doc.CreateElement(item.Key, info["namespace"]);
                el.AppendChild(doc.CreateTextNode(item.Value));
                rootElement.AppendChild(el);
            }
        }

        return doc.DocumentElement;
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
            string value = attributes[xmlAttribute];
            if (!(string.IsNullOrEmpty(value) || string.Compare(value, "Null") == 0))
            {
                result = value;
            }
        }

        return result;
    }

    public static XmlElement AppendElement(XmlDocument doc, XmlElement parent, string name)
    {
        return XmlHelper.AppendElement(doc, parent, name, null, null);
    }

    public static XmlElement AppendElement(XmlDocument doc, XmlElement parent, string name, string content)
    {
        return XmlHelper.AppendElement(doc, parent, name, content, null);
    }

    public static XmlElement AppendElement(XmlDocument doc, XmlElement parent, string name, IDictionary<string, string> attributes)
    {
        return XmlHelper.AppendElement(doc, parent, name, null, attributes);
    }

    public static XmlElement AppendElement(XmlDocument doc, XmlElement parent, string name, string content, IDictionary<string, string> attributes)
    {
        XmlElement element = doc.CreateElement(name);

        if (!string.IsNullOrEmpty(content))
        {
            element.InnerText = content;
        }

        if (attributes != null)
        {
            foreach (string key in attributes.Keys)
            {
                element.SetAttribute(key, attributes[key]);
            }
        }

        parent.AppendChild(element);

        return element;
    }

    public static XmlElement ToXmlElement(XElement element)
    {
        using (XmlReader xmlReader = element.CreateReader())
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(xmlReader);
            return xmlDoc.DocumentElement;
        }
    }

    public static XElement ToXElement(Type inputType, object input)
    {
        XDocument doc = new XDocument();
        XmlSerializer xmlSerializer = new XmlSerializer(inputType);
        using (XmlWriter writer = doc.CreateWriter())
        {
            xmlSerializer.Serialize(writer, input);
        }
        return doc.Root;
    }

    public static string SerializeToString(Type inputType, object input)
    {
        XmlSerializer xmlSerializer = new XmlSerializer(inputType);
        using (StringWriter writer = new StringWriter())
        {
            xmlSerializer.Serialize(writer, input);
            return writer.ToString();
        }
    }
}