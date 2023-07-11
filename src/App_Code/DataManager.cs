/* $Rev: 20981 $ */
using OeREBKRMkvs_V2_0;
using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Caching;
using System.Xml;
using System.Xml.Serialization;
using Topomat.Web.Common;

public class DataManager
{
    private string OerebDataCacheId = "oerebData_";

    public DataManager()
    {
    }

    public TRANSFER GetOerebTexts()
    {
        string cacheId = OerebDataCacheId + "texts";
        if (HttpRuntime.Cache.Get(cacheId) == null)
        {
            ResetOerebData();
        }
        return (TRANSFER)HttpRuntime.Cache.Get(cacheId);
    }

    public TRANSFER GetOerebLogos()
    {
        string cacheId = OerebDataCacheId + "logos";
        if (HttpRuntime.Cache.Get(cacheId) == null)
        {
            ResetOerebData();
        }
        return (TRANSFER)HttpRuntime.Cache.Get(cacheId);
    }

    public TRANSFER GetOerebLaws()
    {
        string cacheId = OerebDataCacheId + "laws";
        if (HttpRuntime.Cache.Get(cacheId) == null)
        {
            ResetOerebData();
        }
        return (TRANSFER)HttpRuntime.Cache.Get(cacheId);
    }

    public TRANSFER GetOerebThemes()
    {
        string cacheId = OerebDataCacheId + "themes";
        if (HttpRuntime.Cache.Get(cacheId) == null)
        {
            ResetOerebData();
        }
        return (TRANSFER)HttpRuntime.Cache.Get(cacheId);
    }

    public void ResetOerebData()
    {
        string cacheId = OerebDataCacheId + "texts";
        HttpRuntime.Cache.Remove(cacheId);
        HttpRuntime.Cache.Add(cacheId, Deserialize(WebHelper.GetConfigValue("OerebDataTextsFileUrl")), null,
            DateTime.Now.AddDays(1), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);

        cacheId = OerebDataCacheId + "logos";
        HttpRuntime.Cache.Remove(cacheId);
        TRANSFER transfer = Deserialize(WebHelper.GetConfigValue("OerebDataLogosFileUrl"));
        HttpRuntime.Cache.Add(cacheId, transfer, null, DateTime.Now.AddDays(1), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null); ;
        StoreLogos(transfer);

        cacheId = OerebDataCacheId + "laws";
        HttpRuntime.Cache.Remove(cacheId);
        HttpRuntime.Cache.Add(cacheId, Deserialize(WebHelper.GetConfigValue("OerebDataLawsFileUrl")), null,
            DateTime.Now.AddDays(1), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);

        cacheId = OerebDataCacheId + "themes";
        HttpRuntime.Cache.Remove(cacheId);

        HttpRuntime.Cache.Add(cacheId, Deserialize(WebHelper.GetConfigValue("OerebDataThemesFileUrl")), null,
            DateTime.Now.AddDays(1), Cache.NoSlidingExpiration, CacheItemPriority.Normal, null);
    }

    private void StoreLogos(TRANSFER transfer)
    {
        List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationLogo> logos =
            new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationLogo>(
            transfer.DATASECTION.OeREBKRMkvs_V2_0Konfiguration.OeREBKRMkvs_V2_0KonfigurationLogo);
        foreach (var logo in logos)
        {
            StoreLogos(logo.Code, logo.Bild.OeREBKRM_V2_0MultilingualBlob.LocalisedBlob);
        }
    }

    private void StoreLogos(string code, TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationLogoBildOeREBKRM_V2_0MultilingualBlobOeREBKRM_V2_0LocalisedBlob[] blobs)
    {
        string logoPath = WebHelper.GetConfigValue("LogoPath");
        foreach (var blob in blobs)
        {
            switch (blob.Language)
            {
                case "fr":
                case "de":
                    string path = System.IO.Path.Combine(logoPath, string.Format("{0}.{1}.png", code, blob.Language));
                    System.IO.File.WriteAllBytes(path, Convert.FromBase64String(blob.Blob.BINBLBOX));
                    break;
                default:
                    break;
            }
        }
    }

    private TRANSFER Deserialize(string url)
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(url);

        XmlSerializer serializer = new XmlSerializer(typeof(TRANSFER));

        using (XmlNodeReader reader = new XmlNodeReader(doc.DocumentElement))
        {
            return (TRANSFER)serializer.Deserialize(reader);
        }
    }
}