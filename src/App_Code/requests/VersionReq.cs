/* $Rev: 14634 $ */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Topomat.Web.Common;
using System.Xml.Serialization;
using System.Xml;
using System.Text;

public class VersionReq
{
    private static IDictionary<string, string> versions = new Dictionary<string, string>
    {
        { "1.0", "RdppfSVC.svc"}
    };
    private string baseUrl;

	public VersionReq()
	{
        baseUrl = WebHelper.GetConfigValue("WebsiteUrl");
	}

    public XmlElement GetVersions()
    {
        GetVersionsResponseType GetVersionsResponse = new GetVersionsResponseType();

        IList<VersionType> versionList = new List<VersionType>();
        foreach(string key in versions.Keys)
        {
            VersionType v = new VersionType();
            v.version = string.Format("Version {0}", key);
            v.serviceEndpointBase = string.Format("{0}/{1}", this.baseUrl, versions[key]);
            versionList.Add(v);
        }
        GetVersionsResponse.supportedVersion = versionList.ToArray();

        return XmlHelper.GetXmlElement(GetVersionsResponse);
    }
}