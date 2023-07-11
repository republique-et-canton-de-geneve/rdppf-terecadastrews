/* $Rev: 25011 $ */
using ExtractDataModel_v20;
using System;
using System.Collections.Generic;
using System.Linq;

public class GeometryHelper
{
    public enum GEOMETRY_SOURCE { esri, interlis };

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

    public static SurfaceType GetPolygons(GeometryResult geometry, GeometryHelper.GEOMETRY_SOURCE source)
    {
        IList<double[][]> exteriors = new List<double[][]>();
        IList<double[][]> interiors = new List<double[][]>();

        if (source == GEOMETRY_SOURCE.esri)
        {
            foreach (double[][] ring in geometry.rings)
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
        }
        else // interlis, le premier ring est toujours l'extérieur
        {
            exteriors.Add(geometry.rings[0]);
            for (int i = 1; i < geometry.rings.Length; i++)
            {
                interiors.Add(geometry.rings[i]);
            }
        }

        return new SurfaceType()
        {
            exterior = GeometryHelper.GetBoundaries(exteriors.ToArray()),
            interior = GeometryHelper.GetBoundaries(interiors.ToArray())
        };
    }

    public static PolylineType[] GetPolylines(GeometryResult geometry)
    {
        IList<PolylineType> polylines = new List<PolylineType>();

        foreach (double[][] path in geometry.paths)
        {
            polylines.Add(GeometryHelper.GetPolyline(path));
        }

        return polylines.ToArray();
    }

    public static CoordType[] GetPoints(GeometryResult geometry)
    {
        IList<CoordType> points = new List<CoordType>();

        if (geometry.points == null)
        {
            geometry.points = new double[][] { new double[] { geometry.x, geometry.y } };
        }
        foreach(double[] coord in geometry.points)
        {
            points.Add(GeometryHelper.GetPoint(coord));
        }

        return points.ToArray();
    }

    public static double[][][] GetPolygonFromExtent(Extent ext)
    {
        double[][] coords = new double[][]
        {
            new double[] { ext.xmin, ext.ymax },
            new double[] { ext.xmax, ext.ymax },
            new double[] { ext.xmax, ext.ymin },
            new double[] { ext.xmin, ext.ymin }
        };

        return new double[][][] { coords };
    }
    #endregion

    public static int[] GetRoundedAreas(double[] areas)
    {
        // on pondère la valeur de chaque surface de manière à ce que la somme des arrondis 
        // soit indentique à la somme arrondie.
        IList<int> roundedAreas = new List<int>();

        double totArea = 0.0f;
        int roundedTotArea = 0;
        foreach (double area in areas)
        {
            int roundedArea = (int)Math.Round(area);
            totArea += area;
            roundedTotArea += roundedArea;

            roundedAreas.Add(roundedArea);
        }

        if (totArea > 0.0f)
        {
            while ((int)Math.Round(totArea) != roundedTotArea)
            {
                int max = 0;
                foreach (int roundedArea in roundedAreas)
                {
                    max = roundedArea > max ? roundedArea : max;
                }
                IList<int> adjustedAreas = new List<int>();
                foreach (int roundedArea in roundedAreas)
                {
                    int area = roundedArea;
                    if (roundedArea == max)
                    {
                        if (roundedTotArea > (int)Math.Round(totArea))
                        {
                            area = roundedArea - 1;
                            roundedTotArea = roundedTotArea - 1;
                        }
                        else
                        {
                            area = roundedArea + 1;
                            roundedTotArea = roundedTotArea + 1;
                        }
                    }
                    adjustedAreas.Add(area);
                }
                roundedAreas = new List<int>(adjustedAreas.ToArray());
            }
        }

        return roundedAreas.ToArray();
    }

    #region Private methods

    private static BoundaryType[] GetBoundaries(double[][][] rings)
    {
        IList<BoundaryType> boundaries = new List<BoundaryType>();

        foreach (double[][] coords in rings)
        {
            boundaries.Add(new BoundaryType()
            {
                polyline = GeometryHelper.GetPolyline(coords)
            });
        }

        return boundaries.ToArray();
    }

    private static PolylineType GetPolyline(double[][] coords)
    {
        IList<CoordType> coordList = new List<CoordType>();
        foreach (double[] coord in coords)
        {
            coordList.Add(GeometryHelper.GetPoint(coord));
        }

        return new PolylineType()
        {
            Items = coordList.ToArray()
        };
    }

    private static CoordType GetPoint(double[] coord)
    {
        return new CoordType()
        {
            c1 = coord[0],
            c2 = coord[1]
        };
    }

    #endregion
}