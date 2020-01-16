/* $Rev: 22459 $ */
using System.Linq;
using Topomat.Web.Common;
using System.Collections.Generic;
using ExtractData_v103;

public class GetEGRIDReq
{
    private int parcelleLayerId;
    private int ddpLayerId;
    private int adresseLayerId;
    private int batimentHSLayerId;
    private int batimentSSLayerId;
    private QueryWorker queryWorker;

    public GetEGRIDReq()
    {
        string token = TokenManager.GetToken();

        LayerInfo layerInfo = new LayerInfo(token, WebHelper.GetConfigValue("MapServiceUrl"));
        this.parcelleLayerId = layerInfo.GetLayerInfo(WebHelper.GetConfigValue("ParcelleLayerName")).LayerID;
        this.ddpLayerId = layerInfo.GetLayerInfo(WebHelper.GetConfigValue("DDPLayerName")).LayerID;
        this.adresseLayerId = layerInfo.GetLayerInfo(WebHelper.GetConfigValue("AdresseLayerName")).LayerID;
        this.batimentHSLayerId = layerInfo.GetLayerInfo(WebHelper.GetConfigValue("BatimentHSLayerName")).LayerID;
        this.batimentSSLayerId = layerInfo.GetLayerInfo(WebHelper.GetConfigValue("BatimentSSLayerName")).LayerID;

        this.queryWorker = new QueryWorker(token);
    }

    public QueryResult GetEGRID(GetEGRIDParamReq paramReq)
    {
        string jsonGeometry = string.Empty;

        if (paramReq.isGNSS == true)
        {
            string geometries = string.Format("{0},{1}", paramReq.coordY, paramReq.coordX);
            jsonGeometry = this.queryWorker.BufferRequest(geometries, QueryWorker.WKID_WGS84, QueryWorker.WKID_MN95, 1.0);
        }
        else
        {
            if (paramReq.coordX < 1000000 && paramReq.coordY < 1000000)
            {
                paramReq.coordX += 2000000;
                paramReq.coordY += 1000000;
            }
            string geometries = string.Format("{0},{1}", paramReq.coordX, paramReq.coordY);
            jsonGeometry = this.queryWorker.BufferRequest(geometries, QueryWorker.WKID_MN95, QueryWorker.WKID_MN95, 1.0);
        }

        return this.SendGeomRequest(jsonGeometry);
    }

    public QueryResult GetEGRIDByID(string identdn, string number)
    {
        string clause = string.Format("{0}={1} AND {2}={3}", WebHelper.GetConfigValue("ParcelleNoCommFieldName"), identdn,
            WebHelper.GetConfigValue("ParcelleNoFieldName"), number);

        return this.queryWorker.QueryAttrRequest(this.parcelleLayerId, clause, false);
    }

    public QueryResult GetEGRIDByLocalisation(string postalCode, string localisation, string number)
    {
        QueryResult qrResults = new QueryResult()
        {
            features = new QueryResultFeature[] { }
        };
        // attribute request on adresses
        string clause = string.Format("{0}={1} AND UPPER({2}) LIKE '%{3}%' AND {4}='{5}'",
            WebHelper.GetConfigValue("AdresseNPAFieldName"), postalCode,
            WebHelper.GetConfigValue("AdresseVoieFieldName"), localisation.ToUpper(),
            WebHelper.GetConfigValue("AdresseNumFieldName"), number);

        QueryResult qrAdresses = this.queryWorker.QueryAttrRequest(this.adresseLayerId, clause, false);

        // attribute requests on batiments
        foreach (QueryResultFeature adrFeat in qrAdresses.features)
        {
            string egid = adrFeat.attributes[WebHelper.GetConfigValue("AdresseEGIDFieldName")];

            clause = string.Format("{0}={1}", WebHelper.GetConfigValue("BatimentHSEGIDFieldName"), egid);
            QueryResult qrBatHS = this.queryWorker.QueryAttrRequest(this.batimentHSLayerId, clause, true);

            IList<QueryResultFeature> batFeatures = new List<QueryResultFeature>(qrBatHS.features);

            clause = string.Format("{0}={1}", WebHelper.GetConfigValue("BatimentSSEGIDFieldName"), egid);
            QueryResult qrBatSS = this.queryWorker.QueryAttrRequest(this.batimentSSLayerId, clause, true);

            batFeatures = batFeatures.Concat<QueryResultFeature>(qrBatSS.features).ToList<QueryResultFeature>();

            // geographic requests on parcelles
            if (batFeatures.Count > 0)
            {
                string jsonGeometry = this.queryWorker.BufferPolygonRequest(batFeatures.ToArray(),
                    QueryWorker.WKID_MN95, QueryWorker.WKID_MN95, -0.1);

                qrResults = this.queryWorker.QueryGeomRequest(this.parcelleLayerId, "esriGeometryPolygon", jsonGeometry, false);
                QueryResult qrDdps = this.queryWorker.QueryGeomRequest(this.ddpLayerId, "esriGeometryPolygon", jsonGeometry, false);

                ConcatQueryResults(qrResults, qrDdps.features);
            }
        }

        return qrResults;
    }

    public GetEGRIDResponseType GetEGRIDResponse(QueryResult qResult)
    {
        List<string> egrids = new List<string>();
        List<string> numbers = new List<string>();
        List<string> identdns = new List<string>();
        foreach (QueryResultFeature feature in qResult.features)
        {
            if (feature.type == ParcelleType.BienFonds)
            {
                egrids.Add(feature.attributes[WebHelper.GetConfigValue("ParcelleEGRIDFieldName")]);
                numbers.Add(feature.attributes[WebHelper.GetConfigValue("ParcelleNoFieldName")]);
                identdns.Add(feature.attributes[WebHelper.GetConfigValue("ParcelleNoCommFieldName")]);
            }
            else if (feature.type == ParcelleType.DDP)
            {
                egrids.Add(feature.attributes[WebHelper.GetConfigValue("DDPEGRIDFieldName")]);
                numbers.Add(feature.attributes[WebHelper.GetConfigValue("DDPNoFieldName")]);
                identdns.Add(feature.attributes[WebHelper.GetConfigValue("DDPNoCommFieldName")]);
            }            
        }

        return new GetEGRIDResponseType
        {
            egrid = egrids.ToArray(),
            number = numbers.ToArray(),
            identDN = identdns.ToArray()
        };
    }

    private QueryResult GetEGRIDFromGNSS(double x, double y)
    {
        string geometries = string.Format("{0},{1}", y, x);
        string jsonGeometry = this.queryWorker.BufferRequest(geometries, QueryWorker.WKID_WGS84, QueryWorker.WKID_MN95, 1.0);

        return this.SendGeomRequest(jsonGeometry);
    }

    private QueryResult GetEGRIDFromMN(double x, double y)
    {
        if (x < 1000000 && y < 1000000)
        {
            x += 2000000;
            y += 1000000;
        }
        string geometries = string.Format("{0},{1}", x, y);
        string jsonGeometry = this.queryWorker.BufferRequest(geometries, QueryWorker.WKID_MN95, QueryWorker.WKID_MN95, 1.0);

        return this.SendGeomRequest(jsonGeometry);
    }

    private QueryResult SendGeomRequest(string jsonGeometry)
    {
        return this.queryWorker.QueryGeomRequest(this.parcelleLayerId, "esriGeometryPolygon", jsonGeometry, false);
    }

    private void ConcatQueryResults(QueryResult qr, QueryResultFeature[] features)
    {
        IList<QueryResultFeature> list = new List<QueryResultFeature>(qr.features);
        qr.features = list.Concat(features).ToArray();
    }
}