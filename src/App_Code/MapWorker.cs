/* $Rev: 14634 $ */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Topomat.Web.Common;
using System.Net;
using System.Collections.Specialized;
using System.IO;
using System.Drawing;
using System.Diagnostics;

public class MapWorker
{
    private string mapServiceUrl;
    private MapPrintParams printParams;

    public MapWorker(MapPrintParams printParams)
    {
        this.mapServiceUrl = WebHelper.GetConfigValue("MapServiceUrl");
        this.printParams = printParams;
    }

    public byte[] GetExportImage(Extent geomExtent, int[] layerIds, string layerDefs)
    {
        
        string token = TokenManager.GetToken();

        string url = string.Format("{0}/export{1}", this.mapServiceUrl, Helper.AddTokenToUrl(token));

        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values = new NameValueCollection();
            values["bbox"] = string.Format("{0},{1},{2},{3}", geomExtent.xmin, geomExtent.ymin, geomExtent.xmax, geomExtent.ymax);
            values["size"] = string.Format("{0},{1}", this.printParams.GetSizeWidth(), this.printParams.GetSizeHeight());
            values["dpi"] = MapPrintParams.printDpi.ToString();
            values["format"] = "png";
            values["layerDefs"] = layerDefs;
            values["layers"] = string.Format("show:{0}", string.Join(",", layerIds));
            values["transparent"] = "true";
            values["mapScale"] = this.printParams.GetScale().ToString();

            /*** for test purpose comment the following */
            //values["f"] = "json";
            //byte[] response = client.UploadValues(url, values);
            //string resp = System.Text.Encoding.UTF8.GetString(response);

            //Helper.AppendToTrace(string.Format("MapWorker:GetExportImage, layerDefs: {0}, result: {1}.", layerDefs, resp));
            /*** end of test ***/

            values["f"] = "image";
            
            byte[] raw = client.UploadValues(url, values);
            
            return raw;
        }
    }
}

public class MapPrintParams
{
    public static double printFactor = 1.5;
    public static double printWidth = 174.0;
    public static double printHeight = 99.0;
    public static int[] printScales =
        new int[] { 500, 1000, 1500, 2000, 2500, 3000, 3500, 4000, 4500, 5000, 7500, 10000, 12500, 15000 };
    public static int printDpi = 300;

    private int scale;
    private double sizeWidth;
    private double sizeHeight;
    private Position center;

    public MapPrintParams(Extent geomExtent)
    {
        this.scale = this.ComputeScale(geomExtent);
        this.sizeWidth = this.GetImageSize(MapPrintParams.printWidth);
        this.sizeHeight = this.GetImageSize(MapPrintParams.printHeight);
        this.center = this.GetCenter(geomExtent);
    }

    public int GetScale()
    {
        return this.scale;
    }

    public int GetSizeWidth()
    {
        return (int)Math.Round(this.sizeWidth);
    }

    public int GetSizeHeight()
    {
        return (int)Math.Round(this.sizeHeight);
    }

    public Extent GetMapExtent()
    {
        Extent extent = new Extent();

        double w = (MapPrintParams.printWidth / 1000) * this.scale / 2;
        double h = (MapPrintParams.printHeight / 1000) * this.scale / 2;

        return new Extent
        {
            xmin = this.center.x - w,
            xmax = this.center.x + w,
            ymin = this.center.y - h,
            ymax = this.center.y + h
        };
    }

    private int ComputeScale(Extent geomExtent)
    {
        // expand extent
        double width = (geomExtent.xmax - geomExtent.xmin) * MapPrintParams.printFactor;
        double height = (geomExtent.ymax - geomExtent.ymin) * MapPrintParams.printFactor;
        // compute scale on width or height ?
        double geomRatio = width / height;
        double printRatio = MapPrintParams.printWidth / MapPrintParams.printHeight;
        double geomSize = geomRatio >= printRatio ? width : height;
        double printSize = geomRatio >= printRatio ? MapPrintParams.printWidth : MapPrintParams.printHeight;
        // compute scale
        int nearestScale = (int)Math.Round((geomSize * 1000) / printSize);
        if (MapPrintParams.printScales.Count<int>(ps => ps > nearestScale) > 0)
        {
            nearestScale = MapPrintParams.printScales.First<int>(ps => ps > nearestScale);
        }
        else
        {
            nearestScale = MapPrintParams.printScales[MapPrintParams.printScales.Length - 1];
        }

        return nearestScale;
    }

    private double GetImageSize(double size)
    {
        // convert size(mm) in inches
        double inches = size / 25.4;
        return inches * MapPrintParams.printDpi;
    }

    private Position GetCenter(Extent extent)
    {
        return new Position
        {
            x = (extent.xmin + extent.xmax) / 2,
            y = (extent.ymin + extent.ymax) / 2
        };
    }
}

public class MapWorkerThread : CommonThread
{
    public static int TYPE_MAIN = 1;
    public static int TYPE_ROL = 2;
    public static int TYPE_RESTRICTION = 3;

    private MapWorker worker;
    private int mapType;
    private string entityId;
    private Extent extent;
    private int[] layerIds;
    private string layerDefs;
    private byte[] result;

    public MapWorkerThread(MapWorker worker, int type, string id)
    {
        this.worker = worker;
        this.mapType = type;
        this.entityId = id;
    }

    public void Init(Extent extent, int[] layerIds, string layerDefs)
    {
        this.extent = extent;
        this.layerIds = layerIds;
        this.layerDefs = layerDefs;
    }

    public int GetMapType()
    {
        return this.mapType;
    }

    public string GetEntityId()
    {
        return this.entityId;
    }

    public void Start()
    {
        try
        {
            this.result = this.worker.GetExportImage(this.extent, this.layerIds, this.layerDefs);
            this.Success = true;
        }
        catch (WsUserException ex)
        {
            this.ErrorMessage = ex.Message;
            this.Success = false;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = ex.Message;
            this.Success = false;
        }
    }

    public byte[] GetResult()
    {
        return this.result;
    }
}