/* $Rev: 30621 $ */
using ESRI.ArcGIS.SOAP;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Topomat.Web.Common;

public class LayerInfo
{
    private IDictionary<string, MapLayerInfo> layerInfos = new Dictionary<string, MapLayerInfo>();
    private IDictionary<int, LayerInfoJson.Layer> jsonLayerInfos = new Dictionary<int, LayerInfoJson.Layer>();

    public LayerInfo(string token, string mapServiceUrl)
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
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

        MapServerInfo serverInfo = mapService.GetServerInfo(defaultMapName);

        foreach (MapLayerInfo mlInfo in serverInfo.MapLayerInfos)
        {
            this.layerInfos.Add(mlInfo.Name, mlInfo);
        }

        LayerInfoJson.LayerInfo info = this.RequestLayerInfoAsJson(token, mapServiceUrl);

        foreach (LayerInfoJson.Layer layer in info.layers)
        {
            this.jsonLayerInfos.Add(layer.id, layer);
        }
    }

    public MapLayerInfo GetLayerInfo(string name, bool required)
    {
        MapLayerInfo info = null;
        if (this.layerInfos.TryGetValue(name, out info))
        {
            return info;
        }
        else
        {
            if (required)
            {
                throw new WsUserException(string.Format(Resources.Resource.LAYER_NOT_FOUND, name));
            }
            else
            {
                return null;
            }
        }
    }

    public Field GetFieldInfo(MapLayerInfo info, string fieldName)
    {
        return info.Fields.FieldArray.First(f => f.Name == fieldName);
    }

    public LayerInfoJson.Layer GetLayerInfoAsJson(int id)
    {
        LayerInfoJson.Layer info = null;
        if (this.jsonLayerInfos.TryGetValue(id, out info))
        {
            return info;
        }
        else
        {
            throw new WsUserException(string.Format(Resources.Resource.LAYER_NOT_FOUND, id));
        }
    }

    private LayerInfoJson.LayerInfo RequestLayerInfoAsJson(string token, string mapServiceUrl)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values = new NameValueCollection();
            values["f"] = "json";

            string url = string.Format("{0}/layers{1}", mapServiceUrl, Helper.AddTokenToUrl(token));
            byte[] response = client.UploadValues(url, values);
            string resp = Encoding.UTF8.GetString(response);

            LayerInfoJson.LayerInfo result = serializer.Deserialize<LayerInfoJson.LayerInfo>(resp);

            return result;
        }
    }
}
