/* $Rev: 14634 $ */
using System;
using System.Linq;
using System.Web.Script.Serialization;
using System.Net;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using Topomat.Web.Common;

public class SurfaceWorker
{
    private JavaScriptSerializer serializer;
    private string geometryServiceUrl;
    private string token;
    private ComputeSurface result;

    public SurfaceWorker()
    {
        this.serializer = new JavaScriptSerializer();
        this.serializer.MaxJsonLength = Int32.MaxValue;

        this.geometryServiceUrl = WebHelper.GetConfigValue("GeometryServiceUrl");
        this.token = TokenManager.GetToken();

        this.result = new ComputeSurface();
    }

    public void GetSurfaces(QueryResultFeature feature, bool isComplete, bool isOverlap, RestrictionResult[] restrictions)
    {
        // Ne pas calculer de surfaces pour les couches de type point ou ligne
        if (restrictions[0].IdentResult.geometryType != "esriGeometryPolygon")
        {
            foreach (RestrictionResult restriction in restrictions)
            {
                restriction.Area = 0;
                restriction.PartInPercent = 0.0f;
            }
            return;
        }

        AreasAndLengthsResult areaResult = null;

        if (isOverlap) // particular case, the entities are overlaping
        {
            // calculate area of the result of union on entities
            IList<GeometryResult> geometries = new List<GeometryResult>();
            for (int i = 0; i < restrictions.Length; i++)
            {
                geometries.Add(restrictions[i].IdentResult.geometry);
            }
            RequestGeometry unionGeometry = this.UnionRequest(geometries.ToArray());
            RequestGeometries unionIntersectResult = this.IntersectRequest(feature.geometry, unionGeometry);
            AreasAndLengthsResult unionAreaResult = this.AreasAndLengthsRequest(unionIntersectResult);

            // calculate areas for each entities
            RequestGeometries intersectResult = this.IntersectRequest(feature.geometry, geometries.ToArray());
            areaResult = this.AreasAndLengthsRequest(intersectResult);

            // ponderate real areas with union area
            double sumArea = 0.0;
            foreach(double area in areaResult.areas)
            {
                sumArea += area;
            }
            for (int i = 0; i < areaResult.areas.Length; i++)
            {
                areaResult.areas[i] = areaResult.areas[i] / sumArea * unionAreaResult.areas[0];
            }
        }
        else
        {
            IList<GeometryResult> geometries = new List<GeometryResult>();
            for (int i = 0; i < restrictions.Length; i++)
            {
                geometries.Add(restrictions[i].IdentResult.geometry);
            }
            RequestGeometries intersectResult = this.IntersectRequest(feature.geometry, geometries.ToArray());
            areaResult = this.AreasAndLengthsRequest(intersectResult);
        }

        double SG = double.Parse(feature.attributes[WebHelper.GetConfigValue("ParcelleAreaFieldName")]);
        double ST = double.Parse(feature.attributes[WebHelper.GetConfigValue("ParcelleSurfaceFieldName")]);

        ComputeSurface cs = new ComputeSurface();
        if (cs.Process(isComplete, SG, ST, areaResult.areas) == ComputeSurface.RESULT_FINISHED)
        {
            for (int i = 0; i < restrictions.Length; i++)
            {
                restrictions[i].Area = cs.GetSurfaces()[i];
                restrictions[i].PartInPercent = cs.GetPercents()[i];
            }
        }
        else
        {
            string id = feature.attributes[WebHelper.GetConfigValue("EGRID")];
            throw new WsUserException(string.Format(Resources.Resource.WRONG_COMPUTE_SURFACE, id));
        }
    }

    private RequestGeometry UnionRequest(GeometryResult[] geometries)
    {
        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values["geometries"] = this.GetRequestGeometries(geometries);
            values["sr"] = QueryWorker.WKID_MN95.ToString();
            values["f"] = "json";

            byte[] response = client.UploadValues(string.Format("{0}/union?token={1}", this.geometryServiceUrl, this.token), values);
            string json = Encoding.UTF8.GetString(response);

            return serializer.Deserialize<RequestGeometry>(json);
        }
    }

    private RequestGeometries IntersectRequest(GeometryResult geometry, RequestGeometry geom)
    {
        RequestGeometries geometries = new RequestGeometries
        {
            geometryType = geom.geometryType,
            geometries = new RequestPolygon[] { geom.geometry }
        };
        return this.IntersectRequest(geometry, this.serializer.Serialize(geometries));
    }

    private RequestGeometries IntersectRequest(GeometryResult geometry, GeometryResult[] geometries)
    {
        return this.IntersectRequest(geometry, this.GetRequestGeometries(geometries));
    }

    private RequestGeometries IntersectRequest(GeometryResult geometry, string geometries)
    {
        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values["geometries"] = geometries;
            values["geometry"] = this.GetRequestGeometry(geometry);
            values["sr"] = QueryWorker.WKID_MN95.ToString();
            values["f"] = "json";

            byte[] response = client.UploadValues(string.Format("{0}/intersect?token={1}", this.geometryServiceUrl, this.token), values);
            string json = Encoding.UTF8.GetString(response);

            return serializer.Deserialize<RequestGeometries>(json); ;
        }
    }

    private AreasAndLengthsResult AreasAndLengthsRequest(RequestGeometries reqGeometries)
    {
        using (WebClient client = new WebClient())
        {
            NameValueCollection values = new NameValueCollection();

            values["polygons"] = this.serializer.Serialize(reqGeometries.geometries);
            values["sr"] = QueryWorker.WKID_MN95.ToString();
            values["f"] = "json";

            byte[] response = client.UploadValues(string.Format("{0}/areasAndLengths?token={1}", this.geometryServiceUrl, this.token), values);
            string json = Encoding.UTF8.GetString(response);

            return serializer.Deserialize<AreasAndLengthsResult>(json);
        }
    }

    private string GetRequestGeometry(GeometryResult geometry)
    {
        RequestGeometry obj = new RequestGeometry
        {
            geometryType = "esriGeometryPolygon",
            geometry = new RequestPolygon
                {
                    rings = geometry.rings
                }
        };

        return this.serializer.Serialize(obj);
    }

    private string GetRequestGeometries(GeometryResult[] geometries)
    {
        IList<RequestPolygon> polygonList = new List<RequestPolygon>();
        foreach (GeometryResult geometry in geometries)
        {
            polygonList.Add(new RequestPolygon
            {
                rings = geometry.rings
            });
        }

        RequestGeometries obj = new RequestGeometries
        {
            geometryType = "esriGeometryPolygon",
            geometries = polygonList.ToArray()
        };

        return this.serializer.Serialize(obj);
    }

    private class RequestGeometry
    {
        public string geometryType;
        public RequestPolygon geometry;
    }

    private class RequestGeometries
    {
        public string geometryType;
        public RequestPolygon[] geometries;
    }

    private class RequestPolygon
    {
        public double[][][] rings;
    }

    private class AreasAndLengthsResult
    {
        public double[] areas;
        public double[] lengths;
    }
}

public class SurfaceWorkerThread : CommonThread
{
    private SurfaceWorker worker;
    private QueryResultFeature feature;
    private bool isComplete;
    private bool isOverlap;
    private RestrictionResult[] restrictions;

    public SurfaceWorkerThread(SurfaceWorker worker)
    {
        this.worker = worker;
    }

    public void Init(QueryResultFeature feature, bool complete, bool overlap, RestrictionResult[] restrictions)
    {
        this.feature = feature;
        this.isComplete = complete;
        this.isOverlap = overlap;
        this.restrictions = restrictions;
    }
    
    public void Start()
    {
        try
        {
            this.worker.GetSurfaces(this.feature, this.isComplete, this.isOverlap, this.restrictions);
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
}

