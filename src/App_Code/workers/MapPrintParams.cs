using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

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