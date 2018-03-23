/* $Rev: 14634 $ */
using System;
using System.Web;
using Topomat.Web.Common;
using System.Net;
using System.IO;
using System.Web.Caching;

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
            // get token
            string request = string.Format("{0}?request=getToken&username={1}&password={2}", serverUrl, user, 
                WebHelper.GetConfigValue("TokenServerPwd"));
            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(request);
            HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

            Stream dataStream = resp.GetResponseStream();
            StreamReader reader = new StreamReader(dataStream);
            token = reader.ReadToEnd();
            resp.Close();

            double minutes = 10;
            double.TryParse(WebHelper.GetConfigValue("TokenExpiration"), out minutes);
            HttpRuntime.Cache.Add(cacheId, token, null, DateTime.Now.AddMinutes(minutes), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
        }
        else
        {
            token = (string)HttpRuntime.Cache.Get(cacheId);
        }

        return token;
    }
}