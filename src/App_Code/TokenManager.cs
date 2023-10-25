/* $Rev: 30620 $ */
using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Caching;
using System.Web.Script.Serialization;
using Topomat.Web.Common;

public class TokenManager
{
    public TokenManager()
    {
    }

    public static string GetToken()
    {
        string token = string.Empty;
        string serverUrl = WebHelper.GetConfigValue("TokenServerUrl");
        string user = WebHelper.GetConfigValue("TokenServerUser");
        string cacheId = serverUrl + "_token";

        if (string.IsNullOrEmpty(user))
        {
            return token;
        }

        if (HttpRuntime.Cache.Get(cacheId) == null)
        {
            ServicePointManager.ServerCertificateValidationCallback = (obj, certificate, chain, errors) => true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            
            NameValueCollection data = new NameValueCollection();
            data["username"] = user;
            data["password"] = WebHelper.GetConfigValue("TokenServerPwd");
            data["f"] = "json";

            WebClient webClient = new WebClient();
            byte[] response = webClient.UploadValues(serverUrl, data);
            string responseData = Encoding.UTF8.GetString(response);

            token = new JavaScriptSerializer().Deserialize<TokenInfo>(responseData).token;

            double minutes = 10;
            double.TryParse(WebHelper.GetConfigValue("TokenExpiration"), out minutes);

            HttpRuntime.Cache.Remove(cacheId);
            HttpRuntime.Cache.Add(cacheId, token, null, DateTime.Now.AddMinutes(minutes), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
        }
        else
        {
            token = (string)HttpRuntime.Cache.Get(cacheId);
        }

        return token;
    }
}

public class TokenInfo
{
    public string token { get; set; }
    public long expires { get; set; }
}
