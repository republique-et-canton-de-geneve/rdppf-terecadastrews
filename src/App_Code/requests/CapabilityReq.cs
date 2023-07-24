/* $Rev: 30386 $ */
using ExtractDataModel_v20;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

public class CapabilityReq : CommonReq
{
    private static IList<string> flavours = new List<string>
    {
        { "REDUCED" }
    };
    public static IList<string> languages = new List<string>
    {
        { "fr" }
    };
    private static IList<string> crs = new List<string>
    {
        { "EPSG:2056" }
    };

    private CommuneConfig communeCfg;

    public CapabilityReq()
    {
        this.communeCfg = XmlHelper.GetCommuneConfig();
        this.Init(new GetExtractParamReq(), false);
    }

    public XmlElement GetCapabilitiesAsXml()
    {
        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
        ns.Add(string.Empty, SchemaHelper.Namespaces["extract"].Value);
        ns.Add(SchemaHelper.Namespaces["data"].Key, SchemaHelper.Namespaces["data"].Value);
        ns.Add(SchemaHelper.Namespaces["xsd"].Key, SchemaHelper.Namespaces["xsd"].Value);
        ns.Add(SchemaHelper.Namespaces["xsi"].Key, SchemaHelper.Namespaces["xsi"].Value);

        XmlElement root = XmlHelper.GetXmlElement(GetCapabilities(), ns);
        root.SetAttribute("schemaLocation", SchemaHelper.Namespaces["xsi"].Value, SchemaHelper.GetSchemaLocation(new string[] { "extract", "data" }));

        return root;
    }

    public GetCapabilitiesResponseType GetCapabilities()
    {
        GetCapabilitiesResponseType GetCapabilitiesResponse = new GetCapabilitiesResponseType();

        IDictionary<string, Theme> themes = new Dictionary<string, Theme>();
        foreach (RestrictionTheme theme in GetThemes())
        {
            if (!themes.ContainsKey(theme.Code))
            {
                themes.Add(theme.Code, new Theme
                {
                    Code = theme.Code,
                    Text = GetLocalisedText(theme.Text)
                });
            }
        }

        IList<string> municipalities = new List<string>();
        foreach (CommuneCfg com in this.communeCfg.Items)
        {
            if (com.hasData == true)
            {
                municipalities.Add(com.num);
            }
        }

        GetCapabilitiesResponse.topic = themes.Values.ToArray();
        GetCapabilitiesResponse.municipality = municipalities.ToArray();
        GetCapabilitiesResponse.flavour = flavours.ToArray();
        GetCapabilitiesResponse.language = languages.ToArray();
        GetCapabilitiesResponse.crs = crs.ToArray();

        return GetCapabilitiesResponse;
    }
}