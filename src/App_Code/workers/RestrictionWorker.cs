/* $Rev: 21379 $ */
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using ESRI.ArcGIS.SOAP;
using Topomat.Web.Common;

public class RestrictionWorker
{
    private XmlNode requestConfig;
    private LayerInfo layerInfo;
    private LegendInfo legendInfo;
    private QueryWorker queryWorker;
    private GetExtractParamReq extractParams;

    public RestrictionWorker(LayerInfo layInfo, LegendInfo legInfo, QueryWorker worker)
    {
        GetExtractParamReq param = new GetExtractParamReq
        {
            allTopics = true,
            lang = "fr",
            withImages = true
        };
        this.Init(layInfo, legInfo, worker, param);
    }

    public RestrictionWorker(LayerInfo layInfo, LegendInfo legInfo, QueryWorker worker, GetExtractParamReq param)
    {
        this.Init(layInfo, legInfo, worker, param);
    }

    private void Init(LayerInfo layInfo, LegendInfo legInfo, QueryWorker worker, GetExtractParamReq param)
    {
        this.requestConfig = XmlHelper.GetConfig("request.xml", "RequestConfig");
        this.layerInfo = layInfo;
        this.legendInfo = legInfo;
        this.queryWorker = worker;
        this.extractParams = param;
    }

    public RestrictionResult[] RunAnalyse(QueryResult parcelle, Extent mapExtent)
    {
        // request intersected entities
        string ids = this.GetIdentifyLayerIds();

        string jsonGeometry = this.queryWorker.BufferPolygonRequest(parcelle.features,
                    QueryWorker.WKID_MN95, QueryWorker.WKID_MN95, -0.1);

        IdentifyResult[] irs = this.queryWorker.IdentifyRequestFromPolygon(ids, jsonGeometry, true);
        IList<RestrictionResult> intersectList = this.AddInfos(irs);

        if (irs.Length > 0)
        {
            // request other entities on map for the layers with positive results
            IList<int> intersectLayerIds = this.GetIdentifyLayerIds(irs);

            irs = this.queryWorker.IdentifyRequestFromExtent(string.Join(",", intersectLayerIds), mapExtent, false);
            IList<RestrictionResult> mapList = this.AddInfos(irs);

            this.MergeResults(intersectList, mapList, intersectLayerIds);

            // request legends for additional layers
            IdentifyResult[] additionalIdentifyResults =
                this.queryWorker.IdentifyRequestFromExtent(string.Join(",", this.GetAdditionalIdentifyLayerIds(irs)), 
                mapExtent, false);

            this.MergeAdditionalLegends(intersectList, additionalIdentifyResults);
        }

        return intersectList.ToArray();
    }

    private string GetIdentifyLayerIds()
    {
        IList<int> list = new List<int>();

        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            string theme = XmlHelper.GetXmlElementValue(node, "Theme/Code");
            if (this.extractParams.allTopics || this.extractParams.topics.Contains(theme))
            {
                string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
                list.Add(this.layerInfo.GetLayerInfo(layerName).LayerID);
            }
        }

        return string.Join(",", list);
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
        }

        return list;
    }

    private IList<int> GetAdditionalIdentifyLayerIds(IdentifyResult[] irs)
    {
        IList<int> list = new List<int>();

        foreach (IdentifyResult ir in irs)
        {
            XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", ir.layerName);
            string additionalLayers = XmlHelper.GetXmlAttribute(node, "additionalLayers", false);

            if (!string.IsNullOrEmpty(additionalLayers))
            {
                foreach (string additionalLayer in additionalLayers.Split(new char[] { ',' }))
                {
                    int id = this.layerInfo.GetLayerInfo(additionalLayer).LayerID;
                    if (!list.Contains(id))
                    {
                        list.Add(id);
                    }
                }
            }            
        }

        return list;
    }

    private IList<RestrictionResult> AddInfos(IdentifyResult[] irs)
    {
        IList<RestrictionResult> results = new List<RestrictionResult>();

        foreach (IdentifyResult ir in irs)
        {
            RestrictionResult result = new RestrictionResult(ir);

            MapLayerInfo layInfo = this.layerInfo.GetLayerInfo(ir.layerName);
            result.LayerId = ir.layerId;
            result.OIDFieldName = layInfo.IDField;
            result.OID = long.Parse(ir.attributes[layInfo.IDField]);
            result.UniqueId = string.Format("{0}_{1}", ir.layerId, ir.attributes[layInfo.IDField]);

            result.Legend = this.GetLegend(layInfo, ir.attributes);

            results.Add(result);
        }

        return results;
    }
    
    private RestrictionLegend GetLegend(MapLayerInfo layerInfo, IDictionary<string, string> attributes)
    {
        MapServerLegendInfo legInfo = this.legendInfo.GetLegendInfo(layerInfo.LayerID);
        LayerInfoJson.Renderer renderer = this.layerInfo.GetLayerInfoAsJson(layerInfo.LayerID).drawingInfo.renderer;

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

        RestrictionLegend legend = new RestrictionLegend();
        if (legendClass != null)
        {
            legend = new RestrictionLegend
            {
                Index = legendIndex, 
                Text = label,
                TypeCode = string.Format("{0}:{1}", layerInfo.LayerID, Helper.ToHexString(label))
            };
        }
        else
        {
            throw new WsUserException(string.Format(Resources.Resource.LEGEND_NOT_FOUND, label, layerInfo.Name));
        }

        if (string.IsNullOrEmpty(legendClass.SymbolImage.ImageURL))
        {
            legend.Symbol = legendClass.SymbolImage.ImageData;
        }
        else
        {
            legend.SymbolRef = legendClass.SymbolImage.ImageURL;
        }

        return legend;
    }

    private void MergeResults(IList<RestrictionResult> intersects, IList<RestrictionResult> others, IList<int> ids)
    {
        foreach (int id in ids)
        {
            bool first = true;
            IList<long> intersectOids = new List<long>();
            IList<string> intersectTypeCodes = new List<string>();

            foreach (RestrictionResult intersect in intersects.Where<RestrictionResult>(ir => ir.LayerId == id))
            {
                intersectOids.Add(intersect.OID);
                if (!intersectTypeCodes.Contains(intersect.Legend.TypeCode))
                {
                    intersectTypeCodes.Add(intersect.Legend.TypeCode);
                }
            }

            foreach (RestrictionResult intersect in intersects.Where<RestrictionResult>(ir => ir.LayerId == id))
            {
                intersect.isFirst = first;
                if (first)
                {
                    // Build layerDefs with all layer entities on map and add other legends
                    IList<long> oids = new List<long>();
                    oids.Add(intersect.OID);

                    IList<RestrictionLegend> otherLegends = new List<RestrictionLegend>();
                    otherLegends.Add(intersect.Legend);

                    foreach (RestrictionResult other in others.Where<RestrictionResult>(or => or.LayerId == id))
                    {
                        if (!oids.Contains(other.OID) && !intersectOids.Contains(other.OID))
                        {
                            oids.Add(other.OID);
                        }
                        if (otherLegends.Count(rl => string.Compare(rl.TypeCode, other.Legend.TypeCode) == 0) == 0 &&
                            !intersectTypeCodes.Contains(other.Legend.TypeCode))
                        {
                            otherLegends.Add(other.Legend);
                        }
                    }

                    intersect.LayerDefs = string.Format("{0}:{1} IN ({2})", id, intersect.OIDFieldName, string.Join(",", oids));
                    intersect.OtherLegends = otherLegends.OrderBy(_l => _l.Index).ToArray();

                    first = false;
                }
                else
                {
                    intersect.LayerDefs = string.Format("{0}:{1}={2}", id, intersect.OIDFieldName, intersect.OID);
                    intersect.OtherLegends = new RestrictionLegend[] { intersect.Legend };
                }
            }
        }
    }

    private void MergeAdditionalLegends(IList<RestrictionResult> results, IdentifyResult[] irs)
    {
        foreach (RestrictionResult result in results)
        {
            IList<RestrictionLegend> list = new List<RestrictionLegend>();

            if (result.isFirst)
            {
                XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer",
                    result.IdentResult.layerName);
                string additionalLayers = XmlHelper.GetXmlAttribute(node, "additionalLayers", false);

                if (!string.IsNullOrEmpty(additionalLayers))
                {
                    foreach (string additionalLayer in additionalLayers.Split(new char[] { ',' }))
                    {
                        MapLayerInfo layInfo = this.layerInfo.GetLayerInfo(additionalLayer);
                        foreach (IdentifyResult ir in irs.Where<IdentifyResult>(_ir => _ir.layerId == layInfo.LayerID))
                        {
                            list.Add(this.GetLegend(layInfo, ir.attributes));
                        }
                    }
                    
                }
            }

            result.AdditionalLegends = list.ToArray();
        }
    }
}

public class RestrictionResult
{
    public IdentifyResult IdentResult;
    public int LayerId;
    public string OIDFieldName;
    public long OID;
    public string UniqueId;
    public bool isFirst;
    public string LayerDefs;
    public byte[] Image;
    public string MapUrl;
    public int Area;
    public int Length;
    public double PartInPercent;
    public RestrictionLegend Legend;
    public RestrictionLegend[] OtherLegends;
    public RestrictionLegend[] AdditionalLegends;

    public RestrictionResult(IdentifyResult ir)
    {
        this.IdentResult = ir;
    }
}

public class RestrictionLegend
{
    public int Index;
    public string Text;
    public byte[] Symbol;
    public string SymbolRef;
    public string TypeCode;
}