/* $Rev: 14634 $ */
using System.Xml.Serialization;

[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class CommuneConfig
{
    [System.Xml.Serialization.XmlElementAttribute("CommuneCfg", Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
    public CommuneCfg[] Items { get; set; }
}

[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class CommuneCfg
{
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string name { get; set; }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string num { get; set; }

    [System.Xml.Serialization.XmlAttributeAttribute()]
    public bool hasData { get; set; }
}