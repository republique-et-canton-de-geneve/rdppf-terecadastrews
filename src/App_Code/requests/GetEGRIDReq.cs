/* $Rev: 25011 $ */
using System.Linq;
using Topomat.Web.Common;
using System.Collections.Generic;
using ExtractData_v103;
using System;

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

        QueryResult qrResults = this.queryWorker.QueryGeomRequest(this.parcelleLayerId, "esriGeometryPolygon", jsonGeometry, ParcelleType.BienFonds, false);
        QueryResult qrDdps = this.queryWorker.QueryGeomRequest(this.ddpLayerId, "esriGeometryPolygon", jsonGeometry, ParcelleType.DDP, false);

        ConcatQueryResults(qrResults, qrDdps.features);

        return qrResults;
    }

    public QueryResult GetEGRIDByID(string identdn, string number)
    {
        int num;
        if (int.TryParse(number, out num))
        {
            string clause = string.Format("{0}={1} AND {2}={3}", WebHelper.GetConfigValue("ParcelleNoCommFieldName"), identdn,
            WebHelper.GetConfigValue("ParcelleNoFieldName"), number);
            QueryResult qrResults = this.queryWorker.QueryAttrRequest(this.parcelleLayerId, clause, ParcelleType.BienFonds, false);

            clause = string.Format("{0}={1} AND {2}={3}", WebHelper.GetConfigValue("DDPNoCommFieldName"), identdn,
                    WebHelper.GetConfigValue("DDPNoFieldName"), number);
            QueryResult qrDdps = this.queryWorker.QueryAttrRequest(this.ddpLayerId, clause, ParcelleType.DDP, false);

            ConcatQueryResults(qrResults, qrDdps.features);

            return qrResults;
        }
        else
        {
            return GetEGRIDByLocalisation(identdn, number, string.Empty);
        }
        
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

        QueryResult qrAdresses = this.queryWorker.QueryAttrRequest(this.adresseLayerId, clause, ParcelleType.Undefined, false);

        if (qrAdresses.features.Length > 0)
        {
            IList<string> egids = new List<string>();
            foreach (QueryResultFeature adrFeat in qrAdresses.features)
            {
                egids.Add(adrFeat.attributes[WebHelper.GetConfigValue("AdresseEGIDFieldName")]);
            }

            // attribute requests on batiments
            clause = string.Format("{0} IN ({1})", WebHelper.GetConfigValue("BatimentHSEGIDFieldName"), string.Join(",", egids));
            QueryResult qrBatHS = this.queryWorker.QueryAttrRequest(this.batimentHSLayerId, clause, ParcelleType.Undefined, true);

            IList<QueryResultFeature> batFeatures = new List<QueryResultFeature>(qrBatHS.features);

            clause = string.Format("{0} IN ({1})", WebHelper.GetConfigValue("BatimentSSEGIDFieldName"), string.Join(",", egids));
            QueryResult qrBatSS = this.queryWorker.QueryAttrRequest(this.batimentSSLayerId, clause, ParcelleType.Undefined, true);

            batFeatures = batFeatures.Concat<QueryResultFeature>(qrBatSS.features).ToList<QueryResultFeature>();

            // geographic requests on parcelles
            if (batFeatures.Count > 0)
            {
                string jsonGeometry = this.queryWorker.BufferPolygonRequest(batFeatures.ToArray(),
                    QueryWorker.WKID_MN95, QueryWorker.WKID_MN95, -0.1);

                qrResults = this.queryWorker.QueryGeomRequest(this.parcelleLayerId, "esriGeometryPolygon", jsonGeometry, ParcelleType.BienFonds, false);
                QueryResult qrDdps = this.queryWorker.QueryGeomRequest(this.ddpLayerId, "esriGeometryPolygon", jsonGeometry, ParcelleType.DDP, false);

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
            if (feature.type == ParcelleType.BienFonds)
            {
                results.Add(new GetEGRIDResponseType
                {
                    egrid = feature.attributes[WebHelper.GetConfigValue("ParcelleEGRIDFieldName")],
                    number = feature.attributes[WebHelper.GetConfigValue("ParcelleNoFieldName")],
                    identDN = feature.attributes[WebHelper.GetConfigValue("ParcelleNoCommFieldName")]
                });
            }
            else if (feature.type == ParcelleType.DDP)
            {
                results.Add(new GetEGRIDResponseType
                {
                    egrid = feature.attributes[WebHelper.GetConfigValue("DDPEGRIDFieldName")],
                    number = feature.attributes[WebHelper.GetConfigValue("DDPNoFieldName")],
                    identDN = feature.attributes[WebHelper.GetConfigValue("DDPNoCommFieldName")]
                });
            }            
        }

        return results.ToArray();
    }

    public List<KeyValuePair<string, string>> GetGenericEGRIDRResponse(QueryResult qResult)
    {
        List<KeyValuePair<string, string>> results = new List<KeyValuePair<string, string>>();

        foreach (QueryResultFeature feature in qResult.features)
        {
            if (feature.type == ParcelleType.BienFonds)
            {
                results.Add(new KeyValuePair<string, string>("egrid", feature.attributes[WebHelper.GetConfigValue("ParcelleEGRIDFieldName")]));
                results.Add(new KeyValuePair<string, string>("number", feature.attributes[WebHelper.GetConfigValue("ParcelleNoFieldName")]));
                results.Add(new KeyValuePair<string, string>("identDN", feature.attributes[WebHelper.GetConfigValue("ParcelleNoCommFieldName")]));
            }
            else if (feature.type == ParcelleType.DDP)
            {
                results.Add(new KeyValuePair<string, string>("egrid", feature.attributes[WebHelper.GetConfigValue("DDPEGRIDFieldName")]));
                results.Add(new KeyValuePair<string, string>("number", feature.attributes[WebHelper.GetConfigValue("DDPNoFieldName")]));
                results.Add(new KeyValuePair<string, string>("identDN", feature.attributes[WebHelper.GetConfigValue("DDPNoCommFieldName")]));
            }
        }

        return results;
    }

    public IDictionary<string, string> GetEGRIDResponseTypeInfo()
    {
        IDictionary<string, string> info = new Dictionary<string, string>();

        Type type = new GetEGRIDResponseType().GetType();
        if (type.GetCustomAttributesData().Any(d => d.Constructor.DeclaringType == typeof(System.Xml.Serialization.XmlRootAttribute)))
        {
            var data = type.GetCustomAttributesData().First(d => d.Constructor.DeclaringType == typeof(System.Xml.Serialization.XmlRootAttribute));
            if (data.NamedArguments.Any(a => a.MemberInfo.Name == "Namespace"))
            {
                info.Add("name", data.ConstructorArguments[0].Value.ToString());
                info.Add("namespace", data.NamedArguments.First(a => a.MemberInfo.Name == "Namespace").TypedValue.Value.ToString());
            }
        }

        return info;
    }

    private void ConcatQueryResults(QueryResult qr, QueryResultFeature[] features)
    {
        IList<QueryResultFeature> list = new List<QueryResultFeature>(qr.features);
        qr.features = list.Concat(features).ToArray();
    }
}