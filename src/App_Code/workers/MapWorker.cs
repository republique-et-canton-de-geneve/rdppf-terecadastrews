/* $Rev: 31768 $ */
using System.Collections.Specialized;
using System.Net;
using Topomat.Web.Common;

public class MapWorker
{
    public enum MapWorkerTypes { mainBasemap, restrBasemap, marker, restriction };

    private string mapServiceUrl;
    private MapPrintParams printParams;
    private MapWorkerTypes workerType;
    private string entityId;
    private int[] layerIds;
    private string[] layerDefs;

    public MapWorker(MapPrintParams printParams)
    {
        this.mapServiceUrl = WebHelper.GetConfigValue("MapServiceUrl");
        this.printParams = printParams;
    }

    public void Init(MapWorkerTypes type, string id, int[] layerIds, string[] layerDefs)
    {
        this.workerType = type;
        this.entityId = id;
        this.layerIds = layerIds;
        this.layerDefs = layerDefs;
    }

    public byte[] GetReportMap(Extent extent, WMSService wmsService)
    {
        if (this.workerType == MapWorkerTypes.mainBasemap || this.workerType == MapWorkerTypes.restrBasemap)
        {
            string url = GetWMSExportUrl(wmsService);
            if (string.IsNullOrEmpty(url))
            {
                return GetExportImage(extent, this.layerIds, this.layerDefs);
            }
            else
            {
                return GetImage(url);
            }

        }
        else
        {
            return GetExportImage(extent, this.layerIds, this.layerDefs);
        }
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

    public byte[] GetImage(string url)
    {
        using (WebClient client = new WebClient())
        {
            return client.DownloadData(url);
        }
    }

    private byte[] GetExportImage(Extent geomExtent, int[] layerIds, string[] layerDefs)
    {
        SetDecimalSeparator();

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
            values["layerDefs"] = "{" + string.Join(",", layerDefs) + "}";
            values["layers"] = string.Format("show:{0}", string.Join(",", layerIds));
            values["transparent"] = "true";
            values["mapScale"] = this.printParams.GetScale().ToString();
            values["f"] = "image";

            byte[] raw = client.UploadValues(url, values);

            return raw;
        }
    }
    private string GetExportUrl(Extent geomExtent, int[] layerIds, string[] layerDefs)
    {
        SetDecimalSeparator();

        string url = string.Format("{0}/export", this.mapServiceUrl);
        string[] args = new string[] {
            string.Format("?bbox={0},{1},{2},{3}", geomExtent.xmin, geomExtent.ymin, geomExtent.xmax, geomExtent.ymax),
            string.Format("&size={0},{1}", this.printParams.GetMapWidth(), this.printParams.GetMapHeight()),
            string.Format("&dpi={0}", this.printParams.GetDpi()),
            "&format=png32",
            "&layerDefs={" + string.Join(",", layerDefs) + "}",
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

    private string GetWMSExportUrl(WMSService cfg)
    {
        SetDecimalSeparator();

        Extent mapExtent = this.printParams.GetMapExtent();

        string url = string.Empty;
        if (cfg != null)
        {
            url = cfg.Url;
            string[] args = new string[] {
            "?SERVICE=WMS&REQUEST=GetMap&FORMAT=image/png&TRANSPARENT=TRUE&STYLES=&VERSION=1.3.0&CRS=EPSG:2056",
            string.Format("&LAYERS={0}", cfg.Layers),
            string.Format("&WIDTH={0}", this.printParams.GetMapWidth()),
            string.Format("&HEIGHT={0}", this.printParams.GetMapHeight()),
            string.Format("&BBOX={0},{1},{2},{3}", mapExtent.xmin, mapExtent.ymin, mapExtent.xmax, mapExtent.ymax),
        };
            foreach (string arg in args)
            {
                url += arg;
            }
        }

        return url;
    }

    private void SetDecimalSeparator()
    {
        System.Globalization.CultureInfo ci = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        ci.NumberFormat.NumberDecimalSeparator = ".";
        System.Threading.Thread.CurrentThread.CurrentCulture = ci;
    }
}