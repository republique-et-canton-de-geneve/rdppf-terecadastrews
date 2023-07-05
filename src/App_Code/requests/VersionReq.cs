/* $Rev: 30107 $ */
using ExtractDataModel_v20;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Topomat.Web.Common;

public class VersionReq
{
    private static IDictionary<string, string> versions = new Dictionary<string, string>
    {
        { "2.0", "RdppfSVC.svc"}
    };
    private string baseUrl;

	public VersionReq()
	{
        baseUrl = WebHelper.GetConfigValue("WebServiceUrl");
	}

    public XmlElement GetVersionsAsXml()
    {
        XmlElement root = XmlHelper.GetXmlElement(GetVersions());
        root.SetAttribute("schemaLocation", SchemaHelper.Namespaces["xsi"].Value, SchemaHelper.GetSchemaLocation(new string[] { "versioning" }));

        return root;
    }

    public GetVersionsResponseType GetVersions()
    {
        GetVersionsResponseType GetVersionsResponse = new GetVersionsResponseType();

        IList<VersionType> versionList = new List<VersionType>();
        foreach(string key in versions.Keys)
        {
            versionList.Add(new VersionType
            {
                version = key,
                serviceEndpointBase = string.Format("{0}/{1}", this.baseUrl, versions[key])
            });
        }
        GetVersionsResponse.supportedVersion = versionList.ToArray();

        return GetVersionsResponse;
    }
}