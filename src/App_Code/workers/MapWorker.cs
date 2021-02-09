/* $Rev: 23677 $ */
using System;
using System.Linq;
using System.Net;
using System.Collections.Specialized;
using Topomat.Web.Common;
using System.Collections.Generic;
using System.Drawing;

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
    public string GetExportUrl(Extent geomExtent, int[] layerIds, string layerDefs)
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

public class MapPrintParams
{
    private int scale;
    private Rectangle scaleBarRect;
    private Point northArrowPt;
    private double northArrowScale;
    private string northArrowAlignment;
    private Position center;
    private MapPrintConfig printConfig;

    public MapPrintParams(MapPrintConfig cfg, Extent geomExtent)
    {
        this.printConfig = cfg;
        this.scale = this.ComputeScale(geomExtent);
        this.center = this.GetCenter(geomExtent);

        // compute draw rectangle for scale bar
        int width = Helper.mmToDots(cfg.ScaleBar.Width, cfg.Dpi);
        int height = Helper.mmToDots(cfg.ScaleBar.Height, cfg.Dpi);

        Point pt = this.GetPointFromConfig(cfg.ScaleBar.TopLeftPoint, cfg.Dpi);
        this.scaleBarRect = new Rectangle(pt.X, pt.Y, width, height);

        // compute draw point for arrow
        this.northArrowPt = this.GetPointFromConfig(cfg.NorthArrow.TopCenterPoint, cfg.Dpi);
        this.northArrowScale = cfg.NorthArrow.Scale;
        this.northArrowAlignment = cfg.NorthArrow.AlignWithScaleBar;
    }

    public int GetScale()
    {
        return this.scale;
    }

    public int GetMapWidth()
    {
        return Helper.mmToDots(this.printConfig.Width, this.printConfig.Dpi);
    }

    public int GetMapHeight()
    {
        return Helper.mmToDots(this.printConfig.Height, this.printConfig.Dpi);
    }

    public Rectangle GetScaleBarDrawRectangle()
    {
        return this.scaleBarRect;
    }

    public Point GetNorthArrowDrawPoint()
    {
        return this.northArrowPt;
    }

    public double GetNorthArrowScale()
    {
        return this.northArrowScale;
    }

    public string GetNorthArrowAlignment()
    {
        return this.northArrowAlignment;
    }

    public int GetDpi()
    {
        return this.printConfig.Dpi;
    }

    public Extent GetMapExtent()
    {
        Extent extent = new Extent();

        double w = (this.printConfig.Width / 1000) * this.scale / 2;
        double h = (this.printConfig.Height / 1000) * this.scale / 2;

        return new Extent
        {
            xmin = this.center.x - w,
            xmax = this.center.x + w,
            ymin = this.center.y - h,
            ymax = this.center.y + h
        };
    }

    private Point GetPointFromConfig(string cfg, int dpi)
    {
        double ptX, ptY;
        string[] cfgPos = cfg.Split(new char[] { ',' });
        if (double.TryParse(cfgPos[0], out ptX) && double.TryParse(cfgPos[1], out ptY))
        {
            return new Point(Helper.mmToDots(ptX, dpi), Helper.mmToDots(ptY, dpi));
        }
        return new Point(0, 0);
    }

    private int ComputeScale(Extent geomExtent)
    {
        // expand extent
        double width = (geomExtent.xmax - geomExtent.xmin) * this.printConfig.Factor;
        double height = (geomExtent.ymax - geomExtent.ymin) * this.printConfig.Factor;
        // compute scale on width or height ?
        double geomRatio = width / height;
        double printRatio = this.printConfig.Width / this.printConfig.Height;
        double geomSize = geomRatio >= printRatio ? width : height;
        double printSize = geomRatio >= printRatio ? this.printConfig.Width : this.printConfig.Height;
        // compute scale
        int nearestScale = (int)Math.Round((geomSize * 1000) / printSize);
        int[] scales = this.GetScales(this.printConfig.Scales);
        if (scales.Count<int>(ps => ps > nearestScale) > 0)
        {
            nearestScale = scales.First<int>(ps => ps > nearestScale);
        }
        else
        {
            nearestScale = scales[scales.Length - 1];
        }

        return nearestScale;
    }

    private int[] GetScales(string list)
    {
        IList<int> scales = new List<int>();
        foreach (string l in list.Split(new char[] { ',' }))
        {
            int value;
            if (int.TryParse(l, out value))
            {
                scales.Add(value);
            }
        }
        return scales.ToArray();
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
    public static int TYPE_PAGE = 2;
    public static int TYPE_RESTRICTION = 3;

    private MapWorker worker;
    private int mapType;
    private string entityId;
    private bool withImages;
    private Extent extent;
    private int[] layerIds;
    private string layerDefs;
    private byte[] resultAsImage;
    private string resultAsUrl;

    public MapWorkerThread(MapWorker worker, int type, string id, bool withImages)
    {
        this.worker = worker;
        this.mapType = type;
        this.entityId = id;
        this.withImages = withImages;
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
            if (this.withImages)
            {
                this.resultAsImage = this.worker.GetExportImage(this.extent, this.layerIds, this.layerDefs);
            }
            else
            {
                this.resultAsUrl = this.worker.GetExportUrl(this.extent, this.layerIds, this.layerDefs);
            }
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

    public byte[] GetResultAsImage()
    {
        return this.resultAsImage;
    }

    public string GetResultAsUrl()
    {
        return this.resultAsUrl;
    }
}

public class MapPrintWorkerThread : CommonThread
{
    private MapWorker worker;
    private int mapPrintType;
    private int layerId;
    private Extent extent;
    private int[] layerIds;
    private string layerDefs;
    private byte[] result;

    public MapPrintWorkerThread(MapWorker worker, int type, int id)
    {
        this.worker = worker;
        this.mapPrintType = type;
        this.layerId = id;
    }

    public void Init(Extent extent, int[] layerIds, string layerDefs)
    {
        this.extent = extent;
        this.layerIds = layerIds;
        this.layerDefs = layerDefs;
    }

    public int GetMapPrintType()
    {
        return this.mapPrintType;
    }

    public int GetLayerId()
    {
        return this.layerId;
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