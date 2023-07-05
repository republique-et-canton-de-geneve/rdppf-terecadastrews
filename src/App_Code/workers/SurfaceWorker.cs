/* $Rev: 30023 $ */
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using Topomat.Web.Common;

public class SurfaceWorker
{
    private JavaScriptSerializer serializer;
    private string geometryServiceUrl;
    private string token;

    public SurfaceWorker()
    {
        this.serializer = new JavaScriptSerializer();
        this.serializer.MaxJsonLength = Int32.MaxValue;

        this.geometryServiceUrl = WebHelper.GetConfigValue("GeometryServiceUrl");
        this.token = TokenManager.GetToken();
    }

    public void GetSurfaces(QueryResultFeature feature, bool isComplete, bool isOverlap, RestrictionResult[] restrictions)
    {
        ClipperInterface clipper = new ClipperInterface();

        if (string.Compare(restrictions[0].IdentResult.geometryType, "esriGeometryPoint") == 0)
        {
            foreach (RestrictionResult restriction in restrictions)
            {
                restriction.Length = 0;
                restriction.Area = 0;
                restriction.PartInPercent = 0.0f;
                restriction.PointNumber = 1;
            }
            return;
        }
        else if (string.Compare(restrictions[0].IdentResult.geometryType, "esriGeometryPolyline") == 0)
        {
            List<double[][][]> polylines = new List<double[][][]>();
            for (int i = 0; i < restrictions.Length; i++)
            {
                polylines.Add(restrictions[i].IdentResult.geometry.paths);
            }
            double[] lengths = clipper.IntersectPolylines(polylines, feature.geometry.rings);

            for (int i = 0; i < restrictions.Length; i++)
            {
                restrictions[i].Length = (int)Math.Round(lengths[i]);
                restrictions[i].Area = 0;
                restrictions[i].PartInPercent = 0.0f;
                restrictions[i].PointNumber = 0;
            }
        }
        else
        {
            IList<double> areaResults = new List<double>();

            if (isOverlap) // cas particulier, les entités peuvent être superposées
            {
                // Calculer la surface intersectée de l'union des géométries
                IList<GeometryResult> geometries = new List<GeometryResult>();
                for (int i = 0; i < restrictions.Length; i++)
                {
                    geometries.Add(restrictions[i].IdentResult.geometry);
                }
                RequestGeometry unionGeometry = this.UnionRequest(geometries.ToArray());
                List<double[][][]> unionPolygons = new List<double[][][]>() { unionGeometry.geometry.rings };
                double[] unionAreas = clipper.IntersectPolygons(unionPolygons, feature.geometry.rings);           

                // Calculer les surfaces intersectées pour chaque entité
                List<double[][][]> polygons = new List<double[][][]>();
                for (int i = 0; i < restrictions.Length; i++)
                {
                    polygons.Add(restrictions[i].IdentResult.geometry.rings);
                }
                double[] areas = clipper.IntersectPolygons(polygons, feature.geometry.rings);

                // Pondérer les surfaces avec la surface intersectée de l'union
                double sumArea = 0.0;
                foreach (double area in areas)
                {
                    sumArea += area;
                }
                for (int i = 0; i < areas.Length; i++)
                {
                    areaResults.Add(areas[i] / sumArea * unionAreas[0]);
                }
            }
            else
            {
                List<double[][][]> polygons = new List<double[][][]>();
                for (int i = 0; i < restrictions.Length; i++)
                {
                    polygons.Add(restrictions[i].IdentResult.geometry.rings);
                }
                areaResults = new List<double>(clipper.IntersectPolygons(polygons, feature.geometry.rings));
            }

            double SG = 0, ST = 0;
            string errorId = string.Empty;
            if (feature.type == ParcelleType.BienFonds)
            {
                SG = double.Parse(feature.attributes[WebHelper.GetConfigValue("ParcelleAreaFieldName")]);
                ST = double.Parse(feature.attributes[WebHelper.GetConfigValue("ParcelleSurfaceFieldName")]);
                errorId = feature.attributes[WebHelper.GetConfigValue("ParcelleEGRIDFieldName")];
            }
            else if (feature.type == ParcelleType.DDP)
            {
                SG = double.Parse(feature.attributes[WebHelper.GetConfigValue("DDPAreaFieldName")]);
                ST = double.Parse(feature.attributes[WebHelper.GetConfigValue("DDPSurfaceFieldName")]);
                errorId = feature.attributes[WebHelper.GetConfigValue("DDPEGRIDFieldName")];
            }            

            ComputeSurface cs = new ComputeSurface();
            if (cs.Process(isComplete, SG, ST, areaResults.ToArray()) == ComputeSurface.RESULT_FINISHED)
            {
                for (int i = 0; i < restrictions.Length; i++)
                {
                    restrictions[i].Length = 0;
                    restrictions[i].Area = cs.GetSurfaces()[i];
                    restrictions[i].PartInPercent = Math.Round(cs.GetPercents()[i], 2);
                    restrictions[i].PointNumber = 0;
                }
            }
            else
            {
                throw new WsUserException(string.Format(Resources.Resource.WRONG_COMPUTE_SURFACE, errorId));
            }
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

    private string GetRequestGeometries(GeometryResult[] geometries)
    {
        IList<object> objList = new List<object>();
        string geometryType = string.Empty;

        foreach (GeometryResult geometry in geometries)
        {
            KeyValuePair<string, object> pair = this.GetGenericGeometry(geometry);
            objList.Add(pair.Value);
            geometryType = pair.Key;
        }

        RequestGeometries obj = new RequestGeometries
        {
            geometryType = geometryType,
            geometries = objList.ToArray()
        };

        return this.serializer.Serialize(obj);
    }

    private KeyValuePair<string, object> GetGenericGeometry(GeometryResult geometry)
    {
        if (geometry.rings != null)
        {
            string json = "{\"rings\":" + this.serializer.Serialize(geometry.rings) + "}";
            return new KeyValuePair<string, object>("esriGeometryPolygon", this.serializer.Deserialize<object>(json));
        }
        else if (geometry.paths != null)
        {
            string json = "{\"paths\":" + this.serializer.Serialize(geometry.paths) + "}";
            return new KeyValuePair<string, object>("esriGeometryPolyline", this.serializer.Deserialize<object>(json));
        }
        else if (geometry.paths != null)
        {
            string json = "{\"points\":" + this.serializer.Serialize(geometry.points) + "}";
            return new KeyValuePair<string, object>("esriGeometryMultipoint", this.serializer.Deserialize<object>(json));
        }
        else
        {
            string json = "{\"x\":" + this.serializer.Serialize(geometry.x) +
                ", \"y\":" + this.serializer.Serialize(geometry.y) + "}";
            return new KeyValuePair<string, object>("esriGeometryPoint", this.serializer.Deserialize<object>(json));
        }
    }
}

