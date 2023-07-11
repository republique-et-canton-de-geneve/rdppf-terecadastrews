/* $Rev: 25011 $ */
using ExtractDataModel_v20;
using System.Collections.Generic;
using System.Linq;

public class SchemaHelper
{
    public static Dictionary<string, KeyValuePair<string, string>> Namespaces = new Dictionary<string, KeyValuePair<string, string>>()
    {
        { "extract", new KeyValuePair<string, string>("extract", "http://schemas.geo.admin.ch/V_D/OeREB/2.0/Extract") },
        { "data", new KeyValuePair<string, string>("data", "http://schemas.geo.admin.ch/V_D/OeREB/2.0/ExtractData") },
        { "geometry", new KeyValuePair<string, string>("geometry", "http://www.interlis.ch/geometry/1.0") },
        { "xsd", new KeyValuePair<string, string>("xsd", "http://www.w3.org/2001/XMLSchema") },
        { "xsi", new KeyValuePair<string, string>("xsi", "http://www.w3.org/2001/XMLSchema-instance") }
    };

    public static Dictionary<string, string> SchemaLocations = new Dictionary<string, string>()
    {
        { "extract", "http://schemas.geo.admin.ch/V_D/OeREB/2.0/Extract http://schemas.geo.admin.ch/V_D/OeREB/2.0/Extract.xsd" },
        { "data", "http://schemas.geo.admin.ch/V_D/OeREB/2.0/ExtractData http://schemas.geo.admin.ch/V_D/OeREB/2.0/ExtractData.xsd" },
        { "geometry", "http://www.interlis.ch/geometry/1.0 http://models.interlis.ch/refhb24/geometry.xsd" },
        { "versioning", "http://schemas.geo.admin.ch/V_D/OeREB/1.0/Versioning http://schemas.geo.admin.ch/V_D/OeREB/1.0/Versioning.xsd" }
    };

    public static string GetSchemaLocation(string[] keys)
    {
        string location = string.Empty;
        foreach(string key in keys)
        {
            location += string.IsNullOrEmpty(location) ? SchemaLocations[key] : " " + SchemaLocations[key];
        }
        return location;
    }

    public static LanguageCode GetLanguageCode(string language)
    {
        switch (language)
        {
            case "fr":
                return LanguageCode.fr;
            case "de":
                return LanguageCode.de;
            case "it":
                return LanguageCode.it;
            case "rm":
                return LanguageCode.rm;
            case "en":
                return LanguageCode.en;
            default:
                return LanguageCode.fr;
        }
    }
}