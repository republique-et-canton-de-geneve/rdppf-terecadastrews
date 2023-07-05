/* $Rev: 30023 $ */
using System.Collections.Specialized;
using System.Net;
using Topomat.Web.Common;

public class MapWorker
{
    public enum MapWorkerTypes { basemap, marker, restriction };

    private string mapServiceUrl;
    private MapPrintParams printParams;
    private MapWorkerTypes workerType;
    private string entityId;
    private int[] layerIds;
    private string layerDefs;

    public MapWorker(MapPrintParams printParams)
    {
        this.mapServiceUrl = WebHelper.GetConfigValue("MapServiceUrl");
        this.printParams = printParams;
    }

    public void Init(MapWorkerTypes type, string id, int[] layerIds, string layerDefs)
    {
        this.workerType = type;
        this.entityId = id;
        this.layerIds = layerIds;
        this.layerDefs = layerDefs;
    }

    public byte[] GetReportMap(Extent extent)
    {
        return GetExportImage(extent, this.layerIds, this.layerDefs);
    }

    public byte[] GetExtractMapAsImage(Extent extent)
    {
        return GetExportImage(extent, this.layerIds, this.layerDefs);
    }

    public string GetExtractMapAsUrl(Extent extent)
    {
        return GetExportUrl(extent, this.layerIds, this.layerDefs);
    }

    public string GetEntityId()
    {
        return this.entityId;
    }

    public MapWorkerTypes GetWorkerType()
    {
        return this.workerType;
    }

    private byte[] GetExportImage(Extent geomExtent, int[] layerIds, string layerDefs)
    {

        string token = TokenManager.GetToken();

        string url = string.Format("{0}/export{1}", this.mapServiceUrl, Helper.AddTokenToUrl(token));

        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values = new NameValueCollection();
            values["bbox"] = string.Format("{0},{1},{2},{3}", geomExtent.xmin, geomExtent.ymin, geomExtent.xmax, geomExtent.ymax);
            values["size"] = string.Format("{0},{1}", this.printParams.GetMapWidth(), this.printParams.GetMapHeight());
            values["dpi"] = this.printParams.GetDpi().ToString();
            values["format"] = "png32";
            values["layerDefs"] = layerDefs;
            values["layers"] = string.Format("show:{0}", string.Join(",", layerIds));
            values["transparent"] = "true";
            values["mapScale"] = this.printParams.GetScale().ToString();
            values["f"] = "image";

            byte[] raw = client.UploadValues(url, values);

            return raw;
        }
    }
    private string GetExportUrl(Extent geomExtent, int[] layerIds, string layerDefs)
    {
        string url = string.Format("{0}/export", this.mapServiceUrl);
        string[] args = new string[] {
            string.Format("?bbox={0},{1},{2},{3}", geomExtent.xmin, geomExtent.ymin, geomExtent.xmax, geomExtent.ymax),
            string.Format("&size={0},{1}", this.printParams.GetMapWidth(), this.printParams.GetMapHeight()),
            string.Format("&dpi={0}", this.printParams.GetDpi()),
            "&format=png32",
            string.Format("&layerDefs={0}", layerDefs),
            string.Format("&layers=show:{0}", string.Join(",", layerIds)),
            "&transparent=true",
            string.Format("&mapScale={0}", this.printParams.GetScale()),
            "&f=image"
        };
        foreach (string arg in args)
        {
            url += arg;
        }

        string token = TokenManager.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            url += string.Format("&token={0}", token);
        }

        return url;
    }
}