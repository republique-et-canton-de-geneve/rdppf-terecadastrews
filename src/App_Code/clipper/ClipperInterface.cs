using System.Collections.Generic;
using System.Linq;
using ClipperLib;
using System;

public class ClipperInterface
{
    private static int PRECISION_FACTOR = 1000;
    public ClipperInterface()
    {

    }

    public double[] IntersectPolygons(List<double[][][]> polygons, double[][][] testPolygon)
    {
        List<double> areas = new List<double>();
        foreach (double[][][] polygon in polygons)
        {
            areas.Add(this.IntersectPolygon(polygon, testPolygon));
        }
        return areas.ToArray();
    }

    public double IntersectPolygon(double[][][] polygon, double[][][] testPolygon)
    {
        List<List<IntPoint>> subjectPolygon = new List<List<IntPoint>>();
        foreach(double[][] coords in polygon)
        {
            subjectPolygon.Add(this.GetRing(coords));
        }
        List<List<IntPoint>> clipPolygon = new List<List<IntPoint>>();
        foreach (double[][] coords in testPolygon)
        {
            clipPolygon.Add(this.GetRing(coords));
        }
        List<List<IntPoint>> resultPolygon = new List<List<IntPoint>>();

        double area = 0;

        Clipper clipper = new Clipper();
        clipper.AddPaths(subjectPolygon, PolyType.ptSubject, true);
        clipper.AddPaths(clipPolygon, PolyType.ptClip, true);
        if (clipper.Execute(ClipType.ctIntersection, resultPolygon, PolyFillType.pftEvenOdd))
        {
            foreach (List<IntPoint> ring in resultPolygon)
            {
                area += Clipper.Area(ring);
            }
        }

        return this.GetAreaValue(area);
    }

    public double[] IntersectPolylines(List<double[][][]> polylines, double[][][] testPolygon)
    {
        List<double> lengths = new List<double>();
        foreach (double[][][] polyline in polylines)
        {
            lengths.Add(this.IntersectPolyline(polyline, testPolygon));
        }
        return lengths.ToArray();
    }

    public double IntersectPolyline(double[][][] polyline, double[][][] testPolygon)
    {
        List<List<IntPoint>> subjectPolyline = new List<List<IntPoint>>();
        foreach (double[][] coords in polyline)
        {
            subjectPolyline.Add(this.GetRing(coords));
        }
        List<List<IntPoint>> clipPolygon = new List<List<IntPoint>>();
        foreach (double[][] coords in testPolygon)
        {
            clipPolygon.Add(this.GetRing(coords));
        }

        double length = 0;

        Clipper clipper = new Clipper();
        clipper.AddPaths(subjectPolyline, PolyType.ptSubject, false);
        clipper.AddPaths(clipPolygon, PolyType.ptClip, true);

        PolyTree result = new PolyTree();
        if (clipper.Execute(ClipType.ctIntersection, result, PolyFillType.pftEvenOdd))
        {
            foreach (PolyNode node in result.Childs)
            {
                length += this.GetLength(node.Contour.ToArray());
            }
        }

        return this.GetLengthValue(length);
    }

    public bool[] IntersectPoints(List<double[]> points, double[][][] testPolygon)
    {
        List<bool> intersects = new List<bool>();
        foreach (double[] point in points)
        {
            intersects.Add(this.IntersectPoint(point, testPolygon));
        }
        return intersects.ToArray();
    }

    public bool IntersectPoint(double[] point, double[][][] testPolygon)
    {
        double offset = (double)1.0 / (double)PRECISION_FACTOR;
        
        List<double[]> coordinates = new List<double[]>();

        coordinates.Add(new double[] { point[0] - offset, point[1] + offset });
        coordinates.Add(new double[] { point[0] + offset, point[1] + offset });
        coordinates.Add(new double[] { point[0] + offset, point[1] - offset });
        coordinates.Add(new double[] { point[0] - offset, point[1] - offset });

        double area = this.IntersectPolygon(new double[][][] { coordinates.ToArray() }, testPolygon);
        return area > 0 ? true : false;        
    }

    private List<IntPoint> GetRing(double[][] coords)
    {
        List<IntPoint> ring = new List<IntPoint>();
        foreach (double[] coord in coords)
        {
            ring.Add(this.CoordToPoint(coord));
        }
        return ring;
    }

    private double GetLength(IntPoint[] segments)
    {
        double length = 0;
        for (var i = 0; i < segments.Length - 1; i++)
        {
            length += this.GetLength(segments[i], segments[i + 1]);
        }
        return length;
    }

    private double GetLength(IntPoint from, IntPoint to)
    {
        return Math.Sqrt(Math.Pow(from.X - to.X, 2) + Math.Pow(from.Y - to.Y, 2));
    }

    private IntPoint CoordToPoint(double[] coord)
    {
        return new IntPoint(DoubleToInt(coord[0]), DoubleToInt(coord[1]));
    }

    private Int64 DoubleToInt(double value)
    {
        return (Int64)Math.Round(value * PRECISION_FACTOR);
    }

    private double GetAreaValue(double area)
    {
        return (area / PRECISION_FACTOR / PRECISION_FACTOR);
    }

    private double GetLengthValue(double length)
    {
        return (length / PRECISION_FACTOR);
    }

    public void TestPolygon()
    {
        IntPoint[] outterRing = new IntPoint[] {
            new IntPoint(0, 0),
            new IntPoint(10, 0),
            new IntPoint(10, 10),
            new IntPoint(0, 10)
        };
        IntPoint[] innerRing = new IntPoint[] {
            new IntPoint(4, 4),
            new IntPoint(4, 6),
            new IntPoint(6, 6),
            new IntPoint(6, 4)
        };
        IntPoint[] clipRing = new IntPoint[] {
            new IntPoint(10, 10),
            new IntPoint(15, 10),
            new IntPoint(15, 15),
            new IntPoint(10, 15)
        };
        IntPoint[] clipLine = new IntPoint[]
        {
            new IntPoint(1, 1),
            new IntPoint(9, 2)
        };


        List<List<IntPoint>> bkgPolygon = new List<List<IntPoint>>();
        bkgPolygon.Add(outterRing.ToList());
        bkgPolygon.Add(innerRing.ToList());
        List<List<IntPoint>> clipPolygon = new List<List<IntPoint>>();
        clipPolygon.Add(clipRing.ToList());

        List<List<IntPoint>> resultPolygon = new List<List<IntPoint>>();

        Clipper clipper = new Clipper();
        clipper.AddPaths(bkgPolygon, PolyType.ptSubject, true);
        clipper.AddPaths(clipPolygon, PolyType.ptClip, true);
        if (clipper.Execute(ClipType.ctIntersection, resultPolygon, PolyFillType.pftEvenOdd))
        {
            double area = 0;
            foreach (List<IntPoint> ring in resultPolygon)
            {
                area += Clipper.Area(ring);
            }
        }
    }

    public void TestLine()
    {
        IntPoint[] outterRing = new IntPoint[] {
            new IntPoint(0, 0),
            new IntPoint(10, 0),
            new IntPoint(10, 10),
            new IntPoint(0, 10)
        };
        IntPoint[] innerRing = new IntPoint[] {
            new IntPoint(4, 4),
            new IntPoint(4, 6),
            new IntPoint(6, 6),
            new IntPoint(6, 4)
        };
        IntPoint[] clipPath = new IntPoint[]
        {
            new IntPoint(7, 11),
            new IntPoint(13, 8)
        };


        List<List<IntPoint>> bkgPolygon = new List<List<IntPoint>>();
        bkgPolygon.Add(outterRing.ToList());
        bkgPolygon.Add(innerRing.ToList());
        List<List<IntPoint>> clipPolyline = new List<List<IntPoint>>();
        clipPolyline.Add(clipPath.ToList());

        Clipper clipper = new Clipper();
        clipper.AddPaths(clipPolyline, PolyType.ptSubject, false);
        clipper.AddPaths(bkgPolygon, PolyType.ptClip, true);

        PolyTree result = new PolyTree();
        if (clipper.Execute(ClipType.ctIntersection, result, PolyFillType.pftEvenOdd))
        {
            //double area = 0;
            //foreach (List<IntPoint> ring in resultPolygon)
            //{
            //    area += Clipper.Area(ring);
            //}
        }
    }
}