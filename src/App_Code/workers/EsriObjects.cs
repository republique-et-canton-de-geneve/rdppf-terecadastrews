/* $Rev: 30309 $ */
using System.Collections.Generic;

public enum ParcelleType { BienFonds, DDP, Undefined };

public class QueryResult
{
    public string geometryType;
    public SpatialReference spatialReference;
    public QueryResultFeature[] features;
}

public class QueryResultFeature
{
    public IDictionary<string, string> attributes;
    public GeometryResult geometry;
    public ParcelleType type;
}

public class IdentifyResp
{
    public IdentifyResult[] results;
}

public class IdentifyResult
{
    public int layerId;
    public string layerName;
    public IDictionary<string, string> attributes;
    public string geometryType;
    public GeometryResult geometry;
}

public class GeometryResult
{
    public double x;
    public double y;
    public double[][][] rings;
    public double[][][] paths;
    public double[][] points;
}

public class SpatialReference
{
    public int wkid;
    public int latestWkid;
}

public class Extent
{
    public double xmin;
    public double xmax;
    public double ymin;
    public double ymax;
}

public class Position
{
    public double x;
    public double y;
}

public class BufferResp
{
    public BufferGeometry[] geometries;
}

public class BufferGeometry
{
    public double[][][] rings;
}

public class RequestGeometry
{
    public string geometryType;
    public RequestPolygon geometry;
}

public class RequestGeometries
{
    public string geometryType;
    public object[] geometries;
}

public class RequestPolygon
{
    public double[][][] rings;
}

namespace LayerInfoJson
{
    public class LayerInfo
    {
        public Layer[] layers;
    }

    public class Layer
    {
        public int id;
        public string name;
        public string geometryType;
        public DrawingInfo drawingInfo;
    }

    public class DrawingInfo
    {
        public Renderer renderer;
    }

    public class Renderer
    {
        public string type;
        public string label;
        public string field1;
        public string field2;
        public string field3;
        public string fieldDelimiter;
        public UniqueValueInfo[] uniqueValueInfos;
    }

    public class UniqueValueInfo
    {
        public string value;
        public string label;
    }
}

namespace LegendInfoJson
{
    public class LegendInfo
    {
        public Layer[] layers;
    }

    public class Layer
    {
        public int layerId;
        public Legend[] legend;
    }

    public class Legend
    {
        public string label;
        public string url;
    }
}