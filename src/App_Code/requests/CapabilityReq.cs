/* $Rev: 14634 $ */
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using DataExtract.Extract;

[XmlRoot("CommuneConfig")]
public class CapabilityReq
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
    private XmlNode requestConfig;

    public CapabilityReq()
	{
        this.communeCfg = XmlHelper.GetCommuneConfig();
        this.requestConfig = XmlHelper.GetConfig("request.xml", "RequestConfig");
	}

    public XmlElement GetCapabilities()
    {
        GetCapabilitiesResponseType GetCapabilitiesResponse = new GetCapabilitiesResponseType();

        IList<Theme> themes = new List<Theme>();
        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            Theme theme = new Theme();
            theme.Code = XmlHelper.GetXmlElementValue(node, "Theme/Code");
            theme.Text = new LocalisedText();
            theme.Text.Language = LanguageCode.fr;
            theme.Text.Text = XmlHelper.GetXmlElementValue(node, "SubTheme");
            themes.Add(theme);
        }

        IList<string> municipalities = new List<string>();
        foreach (CommuneCfg com in this.communeCfg.Items)
        {
            if (com.hasData == true)
            {
                municipalities.Add(com.name);
            }
        }

        GetCapabilitiesResponse.topic = themes.ToArray();
        GetCapabilitiesResponse.municipality = municipalities.ToArray();
        GetCapabilitiesResponse.flavour = flavours.ToArray();
        GetCapabilitiesResponse.language = languages.ToArray();
        GetCapabilitiesResponse.crs = crs.ToArray();

        return XmlHelper.GetXmlElement(GetCapabilitiesResponse);
    }
}