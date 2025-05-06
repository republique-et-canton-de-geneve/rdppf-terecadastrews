/* $Rev: 31340 $ */
using ESRI.ArcGIS.SOAP;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Topomat.Web.Common;

public class RestrictionWorker
{
    private XmlNode requestConfig;
    private LayerInfo layerInfo;
    private LegendInfo legendInfo;
    private QueryWorker queryWorker;
    private GetExtractParamReq extractParams;
    private IList<AdditionalLayer> additionalLayers;
    private IList<AdditionalLayer> additionalLegends;
    private bool storeSymbols;

    public RestrictionWorker(LayerInfo layInfo, LegendInfo legInfo, QueryWorker worker, GetExtractParamReq param, bool storeSymbols)
    {
        this.requestConfig = XmlHelper.GetConfig("request.xml", "RequestConfig");
        this.layerInfo = layInfo;
        this.legendInfo = legInfo;
        this.queryWorker = worker;
        this.extractParams = param;
        this.additionalLayers = new List<AdditionalLayer>();
        this.additionalLegends = new List<AdditionalLayer>();

        this.storeSymbols = storeSymbols;
    }

    public RestrictionResult[] RunAnalyse(QueryResultFeature feature, Extent mapExtent)
    {
        this.GetAdditionalLayers();

        // request intersected entities
        string ids = this.GetIdentifyLayerIds();

        string jsonGeometry = this.queryWorker.BufferPolygonRequest(new QueryResultFeature[] { feature },
                    QueryWorker.WKID_MN95, QueryWorker.WKID_MN95, -0.1);

        IdentifyResult[] irs = this.queryWorker.IdentifyRequestFromPolygon(ids, jsonGeometry, true);
        IList<RestrictionResult> intersectList = this.AddInfos(irs);

        if (irs.Length > 0)
        {
            // request other entities on map for the layers with positive results
            IList<int> intersectLayerIds = this.GetIdentifyLayerIds(irs);

            irs = this.queryWorker.IdentifyRequestFromExtent(string.Join(",", intersectLayerIds), mapExtent, false);
            IList<RestrictionResult> mapList = this.AddInfos(irs);

            // request intersected entities for additional legends
            IList<int> addList = new List<int>();
            foreach (AdditionalLayer al in this.additionalLegends)
            {
                if (!addList.Contains(al.AdditionalInfo.LayerID))
                {
                    addList.Add(al.AdditionalInfo.LayerID);
                }
            }
            string addIds = string.Join(",", addList.OrderBy(n => n));

            IdentifyResult[] addResults = this.queryWorker.IdentifyRequestFromPolygon(addIds, jsonGeometry, false);
            IList<RestrictionResult> additionalLegendList = this.AddInfos(addResults);

            this.MergeResults(intersectList, mapList, additionalLegendList, intersectLayerIds);
        }
        this.StoreLegendSymbols(intersectList);

        return intersectList.ToArray();
    }

    public IList<int> GetAdditionalLayerIds(int id)
    {
        IList<int> ids = new List<int>();
        foreach (AdditionalLayer al in this.additionalLayers.Where(l => l.LayerInfo.LayerID == id))
        {
            ids.Add(al.AdditionalInfo.LayerID);
        }
        return ids;
    }

    public int GetOriginalLayerId(int id, bool forLayer)
    {
        return forLayer ? this.additionalLayers.First(l => l.AdditionalInfo.LayerID == id).LayerInfo.LayerID :
            this.additionalLegends.First(leg => leg.AdditionalInfo.LayerID == id).LayerInfo.LayerID;
    }

    public string GetOriginalLayerName(int id, bool forLayer)
    {
        return forLayer ? this.additionalLayers.First(l => l.AdditionalInfo.LayerID == id).LayerInfo.Name :
             this.additionalLegends.First(leg => leg.AdditionalInfo.LayerID == id).LayerInfo.Name;
    }

    public IList<int> GetAdditionalLegendIds(int id)
    {
        IList<int> ids = new List<int>();
        foreach (AdditionalLayer al in this.additionalLegends.Where(l => l.LayerInfo.LayerID == id))
        {
            ids.Add(al.AdditionalInfo.LayerID);
        }
        return ids;
    }

    private void GetAdditionalLayers()
    {
        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
            string addLayers = XmlHelper.GetXmlAttribute(node, "additionalLayers", false);
            string addLegends = XmlHelper.GetXmlAttribute(node, "additionalLegends", false);

            MapLayerInfo info = this.layerInfo.GetLayerInfo(layerName, true);

            if (!string.IsNullOrEmpty(addLayers))
            {
                foreach (string addLayer in addLayers.Split(new char[] { ',' }))
                {
                    this.additionalLayers.Add(new AdditionalLayer
                    {
                        LayerInfo = info,
                        AdditionalInfo = this.layerInfo.GetLayerInfo(addLayer, true)
                    });
                }
            }

            if (!string.IsNullOrEmpty(addLegends))
            {
                foreach (string addLegend in addLegends.Split(new char[] { ',' }))
                {
                    this.additionalLegends.Add(new AdditionalLayer
                    {
                        LayerInfo = info,
                        AdditionalInfo = this.layerInfo.GetLayerInfo(addLegend, true)
                    });
                }
            }
        }
    }

    private string GetIdentifyLayerIds()
    {
        IList<int> list = new List<int>();

        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            string code = XmlHelper.GetXmlElementValue(node, "Theme/Code");
            if (this.extractParams.allTopics || this.extractParams.partialTopics.Contains(code))
            {
                string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
                int id = this.layerInfo.GetLayerInfo(layerName, true).LayerID;
                list.Add(id);

                foreach (AdditionalLayer al in this.additionalLayers.Where(l => l.LayerInfo.LayerID == id))
                {
                    if (!list.Contains(al.AdditionalInfo.LayerID))
                    {
                        list.Add(al.AdditionalInfo.LayerID);
                    }
                }
            }
        }

        return string.Join(",", list.OrderBy(n => n));
    }

    private IList<int> GetIdentifyLayerIds(IdentifyResult[] irs)
    {
        IList<int> list = new List<int>();

        foreach (IdentifyResult ir in irs)
        {
            if (!list.Contains(ir.layerId))
            {
                list.Add(ir.layerId);
            }
            foreach (AdditionalLayer al in this.additionalLayers.Where(l => l.LayerInfo.LayerID == ir.layerId))
            {
                if (!list.Contains(al.AdditionalInfo.LayerID))
                {
                    list.Add(al.AdditionalInfo.LayerID);
                }
            }
            foreach (AdditionalLayer al in this.additionalLegends.Where(l => l.LayerInfo.LayerID == ir.layerId))
            {
                if (!list.Contains(al.AdditionalInfo.LayerID))
                {
                    list.Add(al.AdditionalInfo.LayerID);
                }
            }
        }

        return list.OrderBy(n => n).ToList();
    }

    private IList<RestrictionResult> AddInfos(IdentifyResult[] irs)
    {
        IList<RestrictionResult> results = new List<RestrictionResult>();

        foreach (IdentifyResult ir in irs)
        {
            RestrictionResult result = new RestrictionResult(ir);

            bool isAdditionalLayer = this.additionalLayers.Count(al => al.AdditionalInfo.LayerID == ir.layerId) > 0;
            bool isAdditionalLegend = this.additionalLegends.Count(al => al.AdditionalInfo.LayerID == ir.layerId) > 0;

            int id = ir.layerId; string name = ir.layerName;
            MapLayerInfo layInfo = this.layerInfo.GetLayerInfo(ir.layerName, true);

            string lawstatus = "En vigueur";
            if (!isAdditionalLegend)
            {
                XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", isAdditionalLayer ? GetOriginalLayerName(ir.layerId, true) : ir.layerName);
                string lawstatusFieldName = XmlHelper.GetXmlAttribute(node.SelectSingleNode("Lawstatus"), "field", false);

                if (!string.IsNullOrEmpty(lawstatusFieldName))
                {
                    string alias = layerInfo.GetFieldInfo(layInfo, lawstatusFieldName).AliasName;
                    if (ir.attributes.ContainsKey(alias))
                    {
                        lawstatus = ir.attributes[alias];
                    }
                }
            }

            result.LayerId = ir.layerId;
            result.LayerName = ir.layerName;
            result.Lawstatus = lawstatus;
            result.OIDFieldName = layInfo.IDField;
            result.OID = long.Parse(ir.attributes[layInfo.IDField]);
            result.UniqueId = string.Format("{0}_{1}", ir.layerId, ir.attributes[layInfo.IDField]);
            result.Legend = this.GetLegend(layInfo, ir.attributes);
            result.isAdditionalResult = isAdditionalLayer;
            result.isAdditionalLegend = isAdditionalLegend;

            results.Add(result);
        }

        return results;
    }

    private RestrictionLegend GetLegend(MapLayerInfo layerInfo, IDictionary<string, string> attributes)
    {
        MapServerLegendInfo legInfo = this.legendInfo.GetLegendInfo(layerInfo.LayerID);
        LayerInfoJson.Layer layInfo = this.layerInfo.GetLayerInfoAsJson(layerInfo.LayerID);
        LayerInfoJson.Renderer renderer = layInfo.drawingInfo.renderer;

        string label = string.Empty;
        switch (renderer.type)
        {
            case "simple":
                label = renderer.label;
                break;
            case "uniqueValue":
                Field fieldInfo = this.layerInfo.GetFieldInfo(layerInfo, renderer.field1);

                string value = attributes[fieldInfo.AliasName];
                if (fieldInfo.Domain != null)
                {
                    CodedValueDomain domain = (CodedValueDomain)fieldInfo.Domain;
                    value = (string)domain.CodedValues.First<CodedValue>(cv => cv.Name == value).Code;
                }
                if (renderer.uniqueValueInfos.Count(uvi => uvi.value == value) > 0)
                {
                    label = renderer.uniqueValueInfos.First(uvi => uvi.value == value).label;
                }
                else
                {
                    throw new WsUserException(string.Format(Resources.Resource.ERROR_RENDERER,
                        fieldInfo.AliasName, value, layerInfo.Name));
                }

                break;
            default:
                throw new WsUserException(string.Format(Resources.Resource.UNKNOW_RENDERER_TYPE,
                    renderer.type, layerInfo.Name));
        }

        int index = 0, legendIndex = 0;
        MapServerLegendClass legendClass = null;
        foreach (MapServerLegendGroup group in legInfo.LegendGroups)
        {
            foreach (MapServerLegendClass lc in group.LegendClasses)
            {
                if (string.Compare(lc.Label, label) == 0)
                {
                    legendClass = lc;
                    legendIndex = index;
                }
                index++;
            }
        }

        LegendInfoJson.Legend jsonLegendInfo = this.legendInfo.GetLegendInfoAsJson(layerInfo.LayerID).FirstOrDefault(jli => string.Compare(jli.label, label) == 0);
        if (jsonLegendInfo == null)
        {
            throw new WsUserException(string.Format(Resources.Resource.LEGEND_NOT_FOUND, label, layerInfo.Name));
        }

        RestrictionLegend legend = new RestrictionLegend();
        if (legendClass != null)
        {
            legend = new RestrictionLegend
            {
                Index = legendIndex,
                Text = label,
                TypeCode = string.Format("{0}:{1}", layerInfo.LayerID, jsonLegendInfo.url),
                GeometryType = layInfo.geometryType,
                Order = GetGeometryOrder(layInfo.geometryType)
            };
        }
        else
        {
            throw new WsUserException(string.Format(Resources.Resource.LEGEND_NOT_FOUND, label, layerInfo.Name));
        }

        legend.Symbol = legendClass.SymbolImage.ImageData;

        return legend;
    }

    private void MergeResults(IList<RestrictionResult> intersectResults, IList<RestrictionResult> mapResults, IList<RestrictionResult> addLegendResults, IList<int> ids)
    {
        foreach (int id in ids)
        {
            foreach (IList<RestrictionResult> restrByStatus in intersectResults.Where(ir => ir.LayerId == id).GroupBy(ir => ir.Lawstatus))
            {
                bool first = true;
                foreach (RestrictionResult intersect in restrByStatus)
                {
                    intersect.isFirst = first;
                    if (first)
                    {
                        int originId = intersect.isAdditionalResult ? GetOriginalLayerId(id, true) : id;

                        IList<RestrictionLegend> allLegends = new List<RestrictionLegend>();
                        foreach (RestrictionResult other in mapResults.Where(or => or.LayerId == originId))
                        {
                            if (allLegends.Count(rl => string.Compare(rl.TypeCode, other.Legend.TypeCode) == 0) == 0)
                            {
                                allLegends.Add(other.Legend);
                            }
                        }

                        foreach (AdditionalLayer al in this.additionalLayers.Where(l => l.LayerInfo.LayerID == originId))
                        {
                            foreach (RestrictionResult other in mapResults.Where(or => or.LayerId == al.AdditionalInfo.LayerID))
                            {
                                if (allLegends.Count(rl => string.Compare(rl.TypeCode, other.Legend.TypeCode) == 0) == 0)
                                {
                                    allLegends.Add(other.Legend);
                                }
                            }
                        }
                        intersect.AllLegends = allLegends.OrderBy(_l => _l.Index).ToArray();

                        IList<RestrictionLegend> addLegends = new List<RestrictionLegend>();
                        IList<RestrictionLegend> addLegendsOnMap = new List<RestrictionLegend>();
                        foreach (AdditionalLayer al in this.additionalLegends.Where(l => l.LayerInfo.LayerID == originId))
                        {
                            foreach (RestrictionResult other in addLegendResults.Where(or => or.LayerId == al.AdditionalInfo.LayerID))
                            {
                                if (addLegends.Count(rl => string.Compare(rl.TypeCode, other.Legend.TypeCode) == 0) == 0)
                                {
                                    addLegends.Add(other.Legend);
                                }
                            }
                            foreach (RestrictionResult other in mapResults.Where(or => or.LayerId == al.AdditionalInfo.LayerID))
                            {
                                if (addLegends.Count(rl => string.Compare(rl.TypeCode, other.Legend.TypeCode) == 0) == 0 &&
                                    addLegendsOnMap.Count(rl => string.Compare(rl.TypeCode, other.Legend.TypeCode) == 0) == 0)
                                {
                                    addLegendsOnMap.Add(other.Legend);
                                }
                            }
                        }
                        intersect.AdditionalLegends = addLegends.OrderBy(_l => _l.Index).ToArray();
                        intersect.AdditionalLegendsOnMap = addLegendsOnMap.OrderBy(_l => _l.Index).ToArray();

                        first = false;
                    }
                    else
                    {
                        intersect.AllLegends = new RestrictionLegend[] { };
                        intersect.AdditionalLegends = new RestrictionLegend[] { };
                        intersect.AdditionalLegendsOnMap = new RestrictionLegend[] { };
                    }
                }
            }
        }
    }

    private void StoreLegendSymbols(IList<RestrictionResult> results)
    {
        Dictionary<string, byte[]> symbols = new Dictionary<string, byte[]>();
        foreach (RestrictionResult result in results)
        {
            RestrictionLegend[] allLegends = result.AllLegends.Concat(result.AdditionalLegends.Concat(result.AdditionalLegendsOnMap)).ToArray();
            foreach (RestrictionLegend legend in allLegends)
            {
                string _code = legend.TypeCode.Replace(":", "_");
                if (!symbols.ContainsKey(_code))
                {
                    symbols.Add(_code, legend.Symbol);
                }
                if (!this.extractParams.withImages)
                {
                    legend.SymbolRef = Helper.GetSymbolUrl(_code);
                    legend.Symbol = null;
                }
            }
            string code = result.Legend.TypeCode.Replace(":", "_");
            if (!symbols.ContainsKey(code))
            {
                symbols.Add(code, result.Legend.Symbol);
            }
            result.Legend.SymbolRef = Helper.GetSymbolUrl(code);
        }

        if (this.storeSymbols)
        {
            foreach (string key in symbols.Keys)
            {
                string path = Helper.GetSymbolPath(key);
                if (!File.Exists(path))
                {
                    File.WriteAllBytes(path, symbols[key]);
                }
            }
        }
    }

    private int GetGeometryOrder(string type)
    {
        int order = 10;
        switch (type)
        {
            case "esriGeometryPolygon":
                order = 1;
                break;
            case "esriGeometryPolyline":
                order = 2;
                break;
            default:
                order = 3;
                break;
        }
        return order;
    }

    private class AdditionalLayer
    {
        public MapLayerInfo LayerInfo { get; set; }
        public MapLayerInfo AdditionalInfo { get; set; }
    }
}

public class RestrictionResult
{
    public IdentifyResult IdentResult { get; set; }
    public int LayerId { get; set; }
    public string LayerName { get; set; }
    public string Lawstatus { get; set; }
    public string OIDFieldName { get; set; }
    public long OID { get; set; }
    public string UniqueId { get; set; }
    public bool isFirst { get; set; }
    public byte[] Image { get; set; }
    public string MapUrl { get; set; }
    public int Area { get; set; }
    public int Length { get; set; }
    public double PartInPercent { get; set; }
    public int PointNumber { get; set; }
    public RestrictionLegend Legend { get; set; }
    public RestrictionLegend[] AllLegends { get; set; }
    public RestrictionLegend[] AdditionalLegends { get; set; }
    public RestrictionLegend[] AdditionalLegendsOnMap { get; set; }
    public bool isAdditionalResult { get; set; }
    public bool isAdditionalLegend { get; set; }

    public RestrictionResult(IdentifyResult ir)
    {
        this.IdentResult = ir;
    }
}

public class RestrictionLegend
{
    public int Index { get; set; }
    public string Text { get; set; }
    public byte[] Symbol { get; set; }
    public string SymbolRef { get; set; }
    public string TypeCode { get; set; }
    public string GeometryType { get; set; }
    public int Order { get; set; }
}