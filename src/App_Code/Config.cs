/* $Rev: 31768 $ */
using System.Web.Services;
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

public class MapPrintConfig
{
    public double Factor { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public string Scales { get; set; }
    public int Dpi { get; set; }
    public float RdppfOpacity { get; set; }
    public WMSService[] WMSServices { get; set; }
    public ScaleBarConfig ScaleBar { get; set; }
    public NorthArrowConfig NorthArrow { get; set; }
}

public class WMSService
{
    public string Url { get; set; }
    public int MinScale { get; set; }
    public int MaxScale { get; set; }
    public string Layers { get; set; }
}

public class ScaleBarConfig
{
    public string TopLeftPoint { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}

public class NorthArrowConfig
{
    public string TopCenterPoint { get; set; }
    public double Scale { get; set; }
    public string AlignWithScaleBar { get; set; }
}