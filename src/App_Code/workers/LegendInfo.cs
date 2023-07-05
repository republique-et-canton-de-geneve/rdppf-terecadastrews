/* $Rev: 30107 $ */
using ESRI.ArcGIS.SOAP;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Topomat.Web.Common;

public class LegendInfo
{    
    public static int[] GENERIC_SIZE = { 32, 20 };
    public static int[] REPORT_SIZE = { 30, 15 };

    private IDictionary<int, MapServerLegendInfo> legendInfos = new Dictionary<int, MapServerLegendInfo>();
    private IDictionary<int, LegendInfoJson.Legend[]> jsonLegendInfos = new Dictionary<int, LegendInfoJson.Legend[]>();

    public LegendInfo(string token, string mapServiceUrl, int[] layerIds, bool forReport)
    {
        MapServerProxy mapService = new MapServerProxy();
        if (string.IsNullOrEmpty(token))
        {
            mapService.Url = mapServiceUrl.Replace("/rest/", "/");
        }
        else
        {
            mapService.Url = string.Format("{0}?token={1}", mapServiceUrl.Replace("/rest/", "/"), token);
        }

        string defaultMapName = mapService.GetDefaultMapName();

        ImageType imgType = new ImageType();
        imgType.ImageFormat = esriImageFormat.esriImagePNG;
        imgType.ImageReturnType = esriImageReturnType.esriImageReturnMimeData;

        MapServerLegendPatch legendPatch = new MapServerLegendPatch();
        legendPatch.ImageDPI = 72;
        legendPatch.Width = forReport ? LegendInfo.REPORT_SIZE[0] : LegendInfo.GENERIC_SIZE[0];
        legendPatch.Height = forReport ? LegendInfo.REPORT_SIZE[1] : LegendInfo.GENERIC_SIZE[1];       

        foreach (MapServerLegendInfo info in mapService.GetLegendInfo(defaultMapName, layerIds, legendPatch, imgType))
        {
            this.legendInfos.Add(info.LayerID, info);
        }

        foreach (LegendInfoJson.Layer layer in RequestLegendInfoAsJson(token, mapServiceUrl).layers)
        {
            this.jsonLegendInfos.Add(layer.layerId, layer.legend);
        }
    }

    public MapServerLegendInfo GetLegendInfo(int id)
    {
        MapServerLegendInfo info = null;
        if (this.legendInfos.TryGetValue(id, out info))
        {
            return info;
        }
        else
        {
            throw new WsUserException(string.Format(Resources.Resource.LAYER_NOT_FOUND, id));
        }
    }

    public LegendInfoJson.Legend[] GetLegendInfoAsJson(int id)
    {
        LegendInfoJson.Legend[] legends = null;
        if (this.jsonLegendInfos.TryGetValue(id, out legends))
        {
            return legends;
        }
        else
        {
            throw new WsUserException(string.Format(Resources.Resource.LEGEND_NOT_FOUND, "*", id));
        }
    }

    private LegendInfoJson.LegendInfo RequestLegendInfoAsJson(string token, string mapServiceUrl)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values["f"] = "json";

            string url = string.Format("{0}/legend{1}", mapServiceUrl, Helper.AddTokenToUrl(token));
            byte[] response = client.UploadValues(url, values);
            string resp = Encoding.UTF8.GetString(response);

            LegendInfoJson.LegendInfo result = serializer.Deserialize<LegendInfoJson.LegendInfo>(resp);

            return result;
        }
    }
}