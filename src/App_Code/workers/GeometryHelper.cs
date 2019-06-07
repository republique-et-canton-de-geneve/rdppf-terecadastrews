/* $Rev: 19053 $ */
using System;
using System.Collections.Generic;
using System.Linq;

public class GeometryHelper
{

    #region Public methods    

    public static bool IsOrientedClockwise(double[][] coords)
    {
        double area = 0;
        for (int i = 0; i < coords.Length - 1; i++)
        {
            area += (coords[i + 1][0] - coords[i][0]) * (coords[i + 1][1] + coords[i][1]) / 2;
        }
        return area > 0;
    }

    public static string StringifyCoords(double[][] coords)
    {
        string result = string.Empty;
        foreach (double[] coord in coords)
        {
            double roundX = Math.Round(coord[0] * 1000) / 1000;
            double roundY = Math.Round(coord[1] * 1000) / 1000;
            if (!string.IsNullOrEmpty(result))
            {
                result += " ";
            }
            result += string.Format("{0} {1}", roundX, roundY);
        }
        return result;
    }

    public static EsriToGmlPolygon[] GetPolygons(double[][][] rings)
    {
        IList<double[][]> exteriors = new List<double[][]>();
        IList<double[][]> interiors = new List<double[][]>();

        foreach (double[][] ring in rings)
        {
            if (GeometryHelper.IsOrientedClockwise(ring))
            {
                exteriors.Add(ring);
            }
            else
            {
                interiors.Add(ring);
            }
        }

        IList<EsriToGmlPolygon> polygons = new List<EsriToGmlPolygon>();
        foreach (double[][] exterior in exteriors)
        {
            Extent extExtent = GeometryHelper.GetExtent(exterior);

            EsriToGmlPolygon polygon = new EsriToGmlPolygon();
            polygon.ExteriorCoords = exterior;
            polygon.InteriorRings = new List<double[][]>();            

            foreach (double[][] interior in interiors)
            {
                if (GeometryHelper.IsContained(GeometryHelper.GetExtent(interior), extExtent))
                {
                    polygon.InteriorRings.Add(interior);
                }
            }

            polygons.Add(polygon);
        }

        return polygons.ToArray();
    }
    
    #endregion

    #region Private methods
    
    private static Extent GetExtent(double[][] coords)
    {
        double xmin = double.MaxValue, ymin = double.MaxValue, xmax = double.MinValue, ymax = double.MinValue;

        foreach (double[] coord in coords)
        {
            xmin = Math.Min(xmin, coord[0]);
            ymin = Math.Min(ymin, coord[1]);
            xmax = Math.Max(xmax, coord[0]);
            ymax = Math.Max(ymax, coord[1]);
        }

        return new Extent
        {
            xmin = xmin,
            xmax = xmax,
            ymin = ymin,
            ymax = ymax
        };
    }

    private static bool IsContained(Extent inner, Extent outer)
    {
        return inner.xmin >= outer.xmin &&
            inner.xmax <= outer.xmax &&
            inner.ymin >= outer.ymin &&
            inner.ymax <= outer.ymax ? true : false;
    }

    #endregion
}

public class EsriToGmlPolygon
{
    public double[][] ExteriorCoords;
    public IList<double[][]> InteriorRings;
}