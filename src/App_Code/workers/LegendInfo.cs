/* $Rev: 23677 $ */
using System.Collections.Generic;
using ESRI.ArcGIS.SOAP;
using Topomat.Web.Common;

public class LegendInfo
{
    private IDictionary<int, MapServerLegendInfo> legendInfos = new Dictionary<int, MapServerLegendInfo>();
    private static int[] GENERIC_SIZE = { 40, 26 };
    private static int[] REPORT_SIZE = { 24, 12 };

    public LegendInfo(string token, string mapServiceUrl, int[] layerIds, bool withImages, bool forReport)
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
        imgType.ImageFormat = esriImageFormat.esriImagePNG32;
        if (withImages == true)
        {
            imgType.ImageReturnType = esriImageReturnType.esriImageReturnMimeData;
        }
        else
        {
            imgType.ImageReturnType = esriImageReturnType.esriImageReturnURL;
        }

        MapServerLegendPatch legendPatch = new MapServerLegendPatch();
        legendPatch.ImageDPI = 72;
        legendPatch.Width = forReport ? LegendInfo.REPORT_SIZE[0] : LegendInfo.GENERIC_SIZE[0];
        legendPatch.Height = forReport ? LegendInfo.REPORT_SIZE[1] : LegendInfo.GENERIC_SIZE[1];       

        foreach (MapServerLegendInfo info in mapService.GetLegendInfo(defaultMapName, layerIds, legendPatch, imgType))
        {
            this.legendInfos.Add(info.LayerID, info);
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
}