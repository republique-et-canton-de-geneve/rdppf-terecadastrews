/* $Rev: 29811 $ */
using ESRI.ArcGIS.SOAP;
using ExtractDataModel_v20;
using System.Collections.Generic;
using System.Linq;
using Topomat.Web.Common;

public class GetEGRIDReq : CommonReq
{
    private bool returnGeometry;

    public GetEGRIDReq(bool retGeom)
    {
        string token = TokenManager.GetToken();

        this.mapLayerInfo = new LayerInfo(token, WebHelper.GetConfigValue("MapServiceUrl"));
        this.queryWorker = new QueryWorker(token);
        this.oerebHelper = new OeREBKRMHelper(new DataManager());
        this.returnGeometry = retGeom;
    }

    public QueryResult GetEGRIDByCoordinates(double x, double y, bool isGNSS)
    {
        string jsonGeometry = string.Empty;

        if (isGNSS == true)
        {
            string geometries = string.Format("{0},{1}", y, x);
            jsonGeometry = this.queryWorker.BufferRequest(geometries, QueryWorker.WKID_WGS84, QueryWorker.WKID_MN95, 1.0);
        }
        else
        {
            if (x < 1000000 && y < 1000000)
            {
                x += 2000000;
                y += 1000000;
            }
            string geometries = string.Format("{0},{1}", x, y);
            jsonGeometry = this.queryWorker.BufferRequest(geometries, QueryWorker.WKID_MN95, QueryWorker.WKID_MN95, 1.0);
        }

        QueryResult qrResults = this.queryWorker.QueryGeomRequest(this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName"), true).LayerID,
            "esriGeometryPolygon", jsonGeometry, ParcelleType.BienFonds, this.returnGeometry);
        QueryResult qrDdps = this.queryWorker.QueryGeomRequest(this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DDPLayerName"), true).LayerID,
            "esriGeometryPolygon", jsonGeometry, ParcelleType.DDP, this.returnGeometry);

        ConcatQueryResults(qrResults, qrDdps.features);

        return qrResults;
    }

    public QueryResult GetEGRIDByID(string identdn, string number)
    {
        MapLayerInfo info = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName"), true);
        Field noComField = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("ParcelleNoCommFieldName"));
        Field noParcField = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("ParcelleNoFieldName"));

        string clause = string.Format("{0} AND {1}", QueryWorker.GetWhereClause(noComField, identdn), QueryWorker.GetWhereClause(noParcField, number));
        QueryResult qrResults = this.queryWorker.QueryAttrRequest(info.LayerID, clause, ParcelleType.BienFonds, this.returnGeometry);

        info = this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DDPLayerName"), true);
        noComField = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("DDPNoCommFieldName"));
        noParcField = this.mapLayerInfo.GetFieldInfo(info, WebHelper.GetConfigValue("DDPNoFieldName"));

        clause = string.Format("{0} AND {1}", QueryWorker.GetWhereClause(noComField, identdn), QueryWorker.GetWhereClause(noParcField, number));
        QueryResult qrDdps = this.queryWorker.QueryAttrRequest(info.LayerID, clause, ParcelleType.DDP, this.returnGeometry);

        ConcatQueryResults(qrResults, qrDdps.features);

        return qrResults;        
    }

    public QueryResult GetEGRIDByLocalisation(string postalCode, string localisation, string number)
    {
        QueryResult qrResults = new QueryResult()
        {
            features = new QueryResultFeature[] { }
        };

        // attribute request on adresses
        string clause = string.Format("{0}={1} AND UPPER({2}) LIKE '%{3}%'",
            WebHelper.GetConfigValue("AdresseNPAFieldName"), postalCode,
            WebHelper.GetConfigValue("AdresseVoieFieldName"), localisation.ToUpper());
        if (!string.IsNullOrEmpty(number))
        {
            clause += string.Format(" AND {0}='{1}'", WebHelper.GetConfigValue("AdresseNumFieldName"), number);
        }

        QueryResult qrAdresses = this.queryWorker.QueryAttrRequest(
            this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("AdresseLayerName"), true).LayerID, clause, ParcelleType.Undefined, false);

        if (qrAdresses.features.Length > 0)
        {
            IList<string> egids = new List<string>();
            foreach (QueryResultFeature adrFeat in qrAdresses.features)
            {
                egids.Add(adrFeat.attributes[WebHelper.GetConfigValue("AdresseEGIDFieldName")]);
            }

            // attribute requests on batiments
            clause = string.Format("{0} IN ({1})", WebHelper.GetConfigValue("BatimentHSEGIDFieldName"), string.Join(",", egids));
            QueryResult qrBatHS = this.queryWorker.QueryAttrRequest(
                this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("BatimentHSLayerName"), true).LayerID, clause, ParcelleType.Undefined, true);

            IList<QueryResultFeature> batFeatures = new List<QueryResultFeature>(qrBatHS.features);

            clause = string.Format("{0} IN ({1})", WebHelper.GetConfigValue("BatimentSSEGIDFieldName"), string.Join(",", egids));
            QueryResult qrBatSS = this.queryWorker.QueryAttrRequest(
                this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("BatimentSSLayerName"), true).LayerID, clause, ParcelleType.Undefined, true);

            batFeatures = batFeatures.Concat<QueryResultFeature>(qrBatSS.features).ToList<QueryResultFeature>();

            // geographic requests on parcelles
            if (batFeatures.Count > 0)
            {
                string jsonGeometry = this.queryWorker.BufferPolygonRequest(batFeatures.ToArray(),
                    QueryWorker.WKID_MN95, QueryWorker.WKID_MN95, -0.1);

                qrResults = this.queryWorker.QueryGeomRequest(
                    this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName"), true).LayerID,
                    "esriGeometryPolygon", jsonGeometry, ParcelleType.BienFonds, this.returnGeometry);
                QueryResult qrDdps = this.queryWorker.QueryGeomRequest(
                    this.mapLayerInfo.GetLayerInfo(WebHelper.GetConfigValue("DDPLayerName"), true).LayerID,
                    "esriGeometryPolygon", jsonGeometry, ParcelleType.DDP, this.returnGeometry);

                ConcatQueryResults(qrResults, qrDdps.features);
            }
        }

        return qrResults;
    }

    public GetEGRIDResponseType[] GetEGRIDResponse(QueryResult qResult)
    {
        List<GetEGRIDResponseType> results = new List<GetEGRIDResponseType>();

        foreach (QueryResultFeature feature in qResult.features)
        {
            MultiSurfaceType limit = null;
            if (this.returnGeometry)
            {
                limit = new MultiSurfaceType()
                {
                    surface = GeometryHelper.GetPolygons(feature.geometry, GeometryHelper.GEOMETRY_SOURCE.esri)
                };
            }

            if (feature.type == ParcelleType.BienFonds)
            {
                results.Add(new GetEGRIDResponseType
                {
                    egrid = feature.attributes[WebHelper.GetConfigValue("ParcelleEGRIDFieldName")],
                    number = feature.attributes[WebHelper.GetConfigValue("ParcelleNoFieldName")],
                    identDN = feature.attributes[WebHelper.GetConfigValue("ParcelleNoCommFieldName")],
                    type = GetRealEstateType(feature.type, feature.attributes[WebHelper.GetConfigValue("ParcelleTypeFieldName")]),
                    limit = limit
                });
            }
            else if (feature.type == ParcelleType.DDP)
            {
                results.Add(new GetEGRIDResponseType
                {
                    egrid = feature.attributes[WebHelper.GetConfigValue("DDPEGRIDFieldName")],
                    number = feature.attributes[WebHelper.GetConfigValue("DDPNoFieldName")],
                    identDN = feature.attributes[WebHelper.GetConfigValue("DDPNoCommFieldName")],
                    type = GetRealEstateType(feature.type, feature.attributes[WebHelper.GetConfigValue("DDPTypeFieldName")]),
                    limit = limit
                });
            }            
        }

        return results.ToArray();
    }

    private void ConcatQueryResults(QueryResult qr, QueryResultFeature[] features)
    {
        IList<QueryResultFeature> list = new List<QueryResultFeature>(qr.features);
        qr.features = list.Concat(features).ToArray();
    }
}