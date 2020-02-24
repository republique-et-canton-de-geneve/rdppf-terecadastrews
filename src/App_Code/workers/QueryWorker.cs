/* $Rev: 22885 $ */
using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using Topomat.Web.Common;

public class QueryWorker
{
    public static int WKID_WGS84 = 4326;
    public static int WKID_MN95 = 2056;

    private JavaScriptSerializer serializer;
    private string mapServiceUrl;
    private string geometryServiceUrl;
    private string token;

	public QueryWorker(string token)
	{
        this.serializer = new JavaScriptSerializer();
        this.serializer.MaxJsonLength = Int32.MaxValue;

        this.mapServiceUrl = WebHelper.GetConfigValue("MapServiceUrl");
        this.geometryServiceUrl = WebHelper.GetConfigValue("GeometryServiceUrl");
        this.token = token;
	}

    public QueryResult QueryAttrRequest(int id, string whereClause, ParcelleType type, bool returnGeometry)
    {
        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values = new NameValueCollection();
            values["outFields"] = "*";
            values["where"] = whereClause;
            values["returnGeometry"] = returnGeometry.ToString();
            values["f"] = "json";
            
            string url = string.Format("{0}/{1}/query{2}", this.mapServiceUrl, id, Helper.AddTokenToUrl(this.token));
            byte[] response = client.UploadValues(url, values);
            string resp = Encoding.UTF8.GetString(response);

            QueryResult result = serializer.Deserialize<QueryResult>(resp);            
            foreach (QueryResultFeature feat in result.features)
            {
                feat.type = type;
            }            

            return result;
        }
    }

    public QueryResult QueryGeomRequest(int id, string geometryType, string jsonGeometry, ParcelleType type, bool returnGeometry)
    {
        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values = new NameValueCollection();
            values["outFields"] = "*";
            values["geometry"] = jsonGeometry;
            values["geometryType"] = geometryType;
            values["returnGeometry"] = returnGeometry.ToString();
            values["f"] = "json";

            string url = string.Format("{0}/{1}/query{2}", this.mapServiceUrl, id, Helper.AddTokenToUrl(this.token));
            byte[] response = client.UploadValues(url, values);
            string resp = Encoding.UTF8.GetString(response);

            QueryResult result = serializer.Deserialize<QueryResult>(resp);
            foreach (QueryResultFeature feat in result.features)
            {
                feat.type = type;
            }
            
            return result;
        }
    }

    public IdentifyResult[] IdentifyRequestFromPolygon(string ids, string jsonGeometry, bool returnGeometry)
    {
        Extent extent = this.GetIdentifyExtent(jsonGeometry);

        return this.IdentifyRequest(ids, jsonGeometry, "esriGeometryPolygon", extent, returnGeometry);
    }

    public IdentifyResult[] IdentifyRequestFromExtent(string ids, Extent extent, bool returnGeometry)
    {
        string jsonGeometry = this.serializer.Serialize(extent);

        return this.IdentifyRequest(ids, jsonGeometry, "esriGeometryEnvelope", extent, returnGeometry);
    }

    private IdentifyResult[] IdentifyRequest(string ids, string jsonGeometry, string geometryType, Extent ext, bool returnGeometry)
    {
        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();
            values["geometry"] = jsonGeometry;
            values["geometryType"] = geometryType;
            values["layers"] = string.Format("all:{0}", ids);
            values["tolerance"] = "0";
            values["mapExtent"] = string.Format("{0},{1},{2},{3}", ext.xmin, ext.ymin, ext.xmax, ext.ymax);
            values["imageDisplay"] = "500,500";
            values["returnGeometry"] = returnGeometry.ToString();
            values["f"] = "json";

            string url = string.Format("{0}/identify{1}", this.mapServiceUrl, Helper.AddTokenToUrl(this.token));
            byte[] response = client.UploadValues(url, values);
            string identResult = Encoding.UTF8.GetString(response);

            IdentifyResp identResp = serializer.Deserialize<IdentifyResp>(identResult);

            return identResp.results;
        }
    }
    
    public string BufferRequest(string geometries, int inSR, int outSR, double buffer)
    {
        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values["geometries"] = geometries;
            values["inSR"] = inSR.ToString();
            values["outSR"] = outSR.ToString();
            values["distances"] = buffer.ToString();
            values["unionResults"] = "true";
            values["geodesic"] = "false";
            values["f"] = "json";

            string url = string.Format("{0}/buffer{1}", this.geometryServiceUrl, Helper.AddTokenToUrl(this.token));
            byte[] response = client.UploadValues(url, values);
            string bufferResult = Encoding.UTF8.GetString(response);

            // modify response to format a Geometry
            BufferResp bufferResp = serializer.Deserialize<BufferResp>(bufferResult);
            
            return serializer.Serialize(bufferResp.geometries[0]);
        }
    }

    public string BufferPolygonRequest(QueryResultFeature[] features, int inSR, int outSR, double buffer)
    {
        IList<BufferGeometry> geometries = new List<BufferGeometry>();
        foreach (QueryResultFeature feature in features)
        {
            BufferGeometry geom = new BufferGeometry();
            geom.rings = feature.geometry.rings;
            geometries.Add(geom);
        }
        
        string jsonGeometries = string.Format("{{\"geometryType\":\"esriGeometryPolygon\",\"geometries\":{0}}}",
            this.serializer.Serialize(geometries));

        return this.BufferRequest(jsonGeometries, inSR, outSR, buffer);
    }

    public Extent GetGeometryExtent(GeometryResult geometry)
    {
        return this.GetExtent(geometry.rings, 0.0);
    }

    private Extent GetIdentifyExtent(string jsonGeometry)
    {
        BufferGeometry geometry = this.serializer.Deserialize<BufferGeometry>(jsonGeometry);
        return this.GetExtent(geometry.rings, 50.0);
    }

    private Extent GetExtent(double[][][] rings, double buffer)
    {
        double xmin = double.MaxValue, ymin = double.MaxValue, xmax = double.MinValue, ymax = double.MinValue;

        foreach (double[][] ring in rings)
        {
            foreach (double[] coord in ring)
            {
                xmin = Math.Min(xmin, coord[0]);
                ymin = Math.Min(ymin, coord[1]);
                xmax = Math.Max(xmax, coord[0]);
                ymax = Math.Max(ymax, coord[1]);
            }
        }

        return new Extent
        {
            xmin = xmin - buffer,
            xmax = xmax + buffer,
            ymin = ymin - buffer,            
            ymax = ymax + buffer
        };
    }    
}