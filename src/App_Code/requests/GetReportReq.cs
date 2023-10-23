/* $Rev: 30621 $ */
using ExtractDataModel_v20;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Topomat.Web.Common;

public class GetReportReq : CommonReq
{
    private static int MAP_OUTLINE_WIDTH = 2;

    private string workPath;
    private string workUrl;
    private string extractId;

    public GetReportReq(GetExtractParamReq param)
    {
        param.withImages = true;
        param.allTopics = true;
        base.Init(param, true);
        this.workPath = WebHelper.GetConfigValue("WorkingPath");
        this.workUrl = WebHelper.GetConfigValue("WorkingUrl");
    }

    public byte[] GetResponseAsPdf(QueryResultFeature feature)
    {
        ReportData reportData = this.GetReportData(feature);

        ExtractGenerator generator = new ExtractGenerator(this.oerebHelper);
        byte[] pdfData = generator.Generate(reportData);

        return pdfData;
    }

    private ReportData GetReportData(QueryResultFeature feature)
    {
        Stopwatch timer = Stopwatch.StartNew();

        IList<MapWorker> mapWorkers = new List<MapWorker>();
        Extent geomExtent = this.queryWorker.GetGeometryExtent(feature.geometry);
        this.printParams = new MapPrintParams(XmlHelper.GetMapPrintConfig(), geomExtent);

        RestrictionResult[] restrictionResults = this.restrWorker.RunAnalyse(feature, this.printParams.GetMapExtent());

        Helper.LogInfo(this.GetType().ToString(), "Données du rapport, analyse", timer.ElapsedMilliseconds);
        timer.Restart();

        string marker = "markerMapLayer";
        if (feature.type == ParcelleType.DDP)
        {
            marker = "markerDDPMapLayer";
        }

        int[] ids = this.GetMapLayerIds(new string[] { marker, "addMapLayer", "mainMapLayer" });
        int markerId = this.GetMapLayerIds(new string[] { marker })[0];
        string markerlayerDefs = string.Format("\"{0}\":\"OBJECTID={1}\"", markerId, feature.attributes["OBJECTID"]);
        mapWorkers.Add(InitMapWorker(this.printParams, MapWorker.MapWorkerTypes.marker, string.Empty, ids, new string[] { markerlayerDefs }));

        ids = this.GetMapLayerIds(new string[] { marker, "addMapLayer", "restrictionMapLayer" });
        IList<string> addedLayerIds = new List<string>();
        foreach (RestrictionResult restriction in restrictionResults.Where(rr => rr.isFirst))
        {
            IList<int> idList = new List<int>(ids);

            int id = restriction.isAdditionalResult ? this.restrWorker.GetOriginalLayerId(restriction.LayerId) : restriction.LayerId;
            string uniqueId = GetMapWorkerId(id, restriction.Lawstatus);

            if (!addedLayerIds.Contains(uniqueId))
            {
                idList.Add(id);
                idList = idList.Concat(this.restrWorker.GetAdditionalLayerIds(id)).ToList();
                idList = idList.Concat(this.restrWorker.GetAdditionalLegendIds(id)).ToList();

                XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer",
                    restriction.isAdditionalResult ? this.restrWorker.GetOriginalLayerName(restriction.LayerId) : restriction.LayerName);
                string statusFieldName = XmlHelper.GetXmlAttribute(node.SelectSingleNode("Lawstatus"), "field", true);


                if (!string.IsNullOrEmpty(statusFieldName))
                {
                    IList<string> layerDefs = new List<string>(new string[] { markerlayerDefs });
                    layerDefs.Add(string.Format("\"{0}\":\"{1}='{2}'\"", id, statusFieldName, restriction.Lawstatus));
                    mapWorkers.Add(InitMapWorker(this.printParams, MapWorker.MapWorkerTypes.restriction, uniqueId, idList.ToArray(), layerDefs.ToArray()));
                }
                else
                {
                    mapWorkers.Add(InitMapWorker(this.printParams, MapWorker.MapWorkerTypes.restriction, uniqueId, idList.ToArray(), new string[] { markerlayerDefs }));
                }
                addedLayerIds.Add(uniqueId);
            }
        }

        // calcul des surfaces
        Parallel.ForEach(restrictionResults.GroupBy(rr => new { rr.LayerId, rr.Lawstatus }), restrList =>
        {
            bool isComplete = false;
            bool isOverlap = false;

            if (!restrList.First().isAdditionalResult)
            {
                XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", restrList.First().LayerName);

                isComplete = bool.Parse(XmlHelper.GetXmlAttribute(node, "complete", true));
                string overlapValue = XmlHelper.GetXmlAttribute(node, "overlap", false);
                if (!string.IsNullOrEmpty(overlapValue))
                {
                    bool.TryParse(overlapValue, out isOverlap);
                }
            }

            SurfaceWorker worker = new SurfaceWorker();
            worker.GetSurfaces(feature, isComplete, isOverlap, restrList.ToArray());
        });

        Helper.LogInfo(this.GetType().ToString(), "Données du rapport, calcul des surfaces", timer.ElapsedMilliseconds);
        timer.Restart();

        // récupération des cartes
        string mainMapUrl = string.Empty;
        IDictionary<string, string> dictRestrictionMapUrls = new Dictionary<string, string>();

        this.CreateWorkingDirectory(this.GetIdentifier(feature));

        Bitmap bmScaleBar = new ScaleBar(printParams).DrawBar(GetReportFontName());

        Parallel.ForEach(mapWorkers, worker =>
        {
            byte[] imageData = worker.GetExtractMapAsImage(geomExtent);
            switch (worker.GetWorkerType())
            {
                case MapWorker.MapWorkerTypes.marker:
                    mainMapUrl = this.AddObjectsToMap("main", imageData, bmScaleBar);
                    break;
                default:
                    string uniqueId = worker.GetEntityId();
                    dictRestrictionMapUrls.Add(uniqueId, this.AddObjectsToMap(string.Format("restr_{0}", uniqueId), imageData, bmScaleBar));
                    break;
            }
        });

        Helper.LogInfo(this.GetType().ToString(), "Données du rapport, récupération des cartes", timer.ElapsedMilliseconds);
        timer.Restart();

        // add concerned themes
        IList<RestrictionTheme> themes = GetThemes();
        IList<restriction> restrictions = GetRestrictionData(themes, restrictionResults, dictRestrictionMapUrls);

        // add not concerned themes
        foreach (string notConcernedTheme in this.GetNotConcernedTheme(themes, restrictionResults))
        {
            restrictions.Add(new restriction()
            {
                title = notConcernedTheme,
                result = false
            });
        }

        RealEstateData re = GetMainData(feature);
        ReportData reportData = new ReportData(this.extractId);

        reportData.section.mapUrl = mainMapUrl;
        reportData.section.dmoDate = this.GetFormattedDMODate();
        reportData.section.realEstate = re;
        reportData.section.restrictions = restrictions.ToArray();

        IList<string> themeWithoutData = new List<string>();
        foreach (Theme t in this.GetThemeWithoutData())
        {
            themeWithoutData.Add(t.Text[0].Text);
        }
        reportData.section.noDataThemes = themeWithoutData.ToArray();

        IList<string> infos = new List<string>();
        foreach (LocalisedMText t in this.GetLocalisedMText(this.infoConfig, "Information"))
        {
            infos.Add(t.Text);
        }
        reportData.section.generalInfos = GetInformationConfig("Information").First();

        InformationText baseData = GetInformationConfig("BaseData").First();
        IList<string> contents = new List<string>();
        foreach (string value in baseData.Contents)
        {
            string content = value.Contains("###DMODATE###") ? value.Replace("###DMODATE###", this.GetFormattedDMODate()) : value;
            contents.Add(content);
        }
        reportData.section.baseData = new InformationText
        {
            Title = baseData.Title,
            Contents = contents.ToArray()
        };
        reportData.section.dmoDate = this.GetFormattedDMODate();

        reportData.section.disclaimers = GetInformationConfig("Disclaimer").Where(d => d.UseInStaticExtract).ToArray();

        reportData.glossaries = GetInformationConfig("Glossary").ToArray();

        Helper.LogInfo(this.GetType().ToString(), "Données du rapport, informations", timer.ElapsedMilliseconds);
        timer.Stop();

        return reportData;
    }

    private MapWorker InitMapWorker(MapPrintParams printParams, MapWorker.MapWorkerTypes type, string id, int[] layerIds, string[] layerDefs)
    {
        MapWorker worker = new MapWorker(printParams);
        worker.Init(type, id, layerIds, layerDefs);
        return worker;
    }

    private string GetMapWorkerId(int layerId, string lawStatus)
    {
        if (string.IsNullOrEmpty(lawStatus))
        {
            return string.Format("{0}_{1}", layerId, GetOerebLawStatus(LawstatusCode.inForce).Id);
        }
        else
        {
            OerebLawStatus status = GetOerebLawStatus(lawStatus);
            if(status == null)
            {
                throw new WsUserException(string.Format(Resources.Resource.ERROR_LAWSTATUS, lawStatus, layerId));
            }
            return string.Format("{0}_{1}", layerId, status.Id);
        }
    }

    private RealEstateData GetMainData(QueryResultFeature feature)
    {
        XmlNode requestConfig = XmlHelper.GetConfig("request.xml", "RequestConfig");

        string xpath = "RealEstate/Parcelle";
        if (feature.type == ParcelleType.DDP)
        {
            xpath = "RealEstate/DDP";
        }
        XmlNode reNode = this.requestConfig.SelectSingleNode(xpath);

        RealEstateTypeCode code = GetRealEstateTypeCode(feature.type, XmlHelper.GetAttributeFromNode(feature, reNode, "Type"));

        return new RealEstateData
        {
            number = XmlHelper.GetAttributeFromNode(feature, reNode, "Number"),
            type = oerebHelper.GetRealEstateTypeText(code, "fr"),
            egrid = XmlHelper.GetAttributeFromNode(feature, reNode, "EGRID"),
            municipalityName = XmlHelper.GetAttributeFromNode(feature, reNode, "MunicipalityName"),
            municipalityCode = XmlHelper.GetAttributeFromNode(feature, reNode, "MunicipalityCode"),
            area = XmlHelper.GetAttributeFromNode(feature, reNode, "LandRegistryArea"),
            state = GetFormattedDMODate()
        };


    }

    private IList<restriction> GetRestrictionData(IList<RestrictionTheme> themes, RestrictionResult[] results, IDictionary<string, string> dictRestrictionMapUrls)
    {
        IList<restriction> restrictions = new List<restriction>();

        IDictionary<int, IList<RestrictionResult>> resultsByLayerId = new Dictionary<int, IList<RestrictionResult>>();
        foreach (RestrictionResult rr in results.OrderBy(r => r.LayerId))
        {
            int id = rr.isAdditionalResult ? this.restrWorker.GetOriginalLayerId(rr.LayerId) : rr.LayerId;
            if (!resultsByLayerId.ContainsKey(id))
            {
                resultsByLayerId.Add(id, new List<RestrictionResult>());

            }
            resultsByLayerId[id].Add(rr);
        }

        foreach (int layerId in resultsByLayerId.Keys)
        {
            foreach (IList<RestrictionResult> restrByStatus in resultsByLayerId[layerId].GroupBy(r => r.Lawstatus))
            {
                IList<RestrictionResult> subResults = restrByStatus.OrderBy(r => r.Legend.Index).ToList();

                RestrictionResult first = subResults.First(r => r.isFirst);
                string name = first.isAdditionalResult ? this.mapLayerInfo.GetLayerInfoAsJson(layerId).name : first.LayerName;
                XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", name);

                OerebLawStatus status = GetOerebLawStatus(first.Lawstatus);
                if (status == null)
                {
                    status = GetOerebLawStatus(LawstatusCode.inForce);
                }

                // legends
                IDictionary<string, legend> dictLegends = new Dictionary<string, legend>();
                foreach (RestrictionResult result in subResults)
                {
                    if (dictLegends.Keys.Contains(result.Legend.TypeCode))
                    {
                        legend leg = dictLegends[result.Legend.TypeCode];
                        leg.length += result.Length;
                        leg.surface += result.Area;
                        leg.surfPercent += result.PartInPercent;
                        leg.points += result.PointNumber;
                    }
                    else
                    {
                        string fileName = string.Format("leg_{0}", result.Legend.TypeCode.Replace(":", "_"));
                        dictLegends.Add(result.Legend.TypeCode, new legend()
                        {
                            imageUrl = this.GetSymbolUrl(fileName, result.Legend.Symbol),
                            label = result.Legend.Text,
                            geometryType = result.Legend.GeometryType,
                            geometryOrder = result.Legend.Order,
                            length = result.Length,
                            surface = result.Area,
                            surfPercent = result.PartInPercent,
                            points = result.PointNumber
                        });
                    }
                }
                foreach (string key in dictLegends.Keys)
                {
                    dictLegends[key].surfPercent = Math.Round(dictLegends[key].surfPercent, 1);
                    dictLegends[key].surfPercentFormatted = string.Format("{0:0.0}", dictLegends[key].surfPercent);
                }

                // other legends
                IDictionary<string, legend> dictOtherLegends = new Dictionary<string, legend>();
                foreach (RestrictionLegend otherLegend in first.AllLegends)
                {
                    if (!dictLegends.Keys.Contains(otherLegend.TypeCode) && !dictOtherLegends.Keys.Contains(otherLegend.TypeCode))
                    {
                        string fileName = string.Format("leg_{0}", otherLegend.TypeCode.Replace(":", "_"));
                        dictOtherLegends.Add(otherLegend.TypeCode, new legend()
                        {
                            imageUrl = this.GetSymbolUrl(fileName, otherLegend.Symbol),
                            label = otherLegend.Text,
                            geometryOrder = otherLegend.Order
                        });
                    }

                }

                // additional legends
                IDictionary<string, legend> dictAdditionalLegends = new Dictionary<string, legend>();
                foreach (RestrictionLegend addLegend in first.AdditionalLegends)
                {
                    if (!dictAdditionalLegends.ContainsKey(addLegend.TypeCode))
                    {
                        string fileName = string.Format("leg_{0}", addLegend.TypeCode.Replace(":", "_"));
                        dictAdditionalLegends.Add(addLegend.TypeCode, new legend()
                        {
                            imageUrl = this.GetSymbolUrl(fileName, addLegend.Symbol),
                            label = addLegend.Text,
                            geometryOrder = addLegend.Order
                        });
                    }
                }

                // LegalProvisions
                IDictionary<string, List<string>> dictLegalProvision = new Dictionary<string, List<string>>();
                foreach (XmlNode idNode in node.SelectNodes("LegalProvisionId"))
                {
                    if (!string.IsNullOrEmpty(idNode.InnerText))
                    {
                        XmlNode lpNode = XmlHelper.GetNodeByAttribute(this.docConfig, "LegalProvision", "id", idNode.InnerText);

                        string lpTitle = XmlHelper.GetXmlElementValue(lpNode, "Title");

                        foreach (RestrictionResult result in subResults)
                        {
                            string value = XmlHelper.GetAttributeFromAttribute(result.IdentResult.attributes, lpNode, "field");
                            if (!string.IsNullOrEmpty(value))
                            {
                                string date = XmlHelper.GetAttributeFromAttribute(result.IdentResult.attributes, idNode, "dateField");
                                if (string.IsNullOrEmpty(date))
                                {
                                    date = XmlHelper.GetAttributeFromAttribute(result.IdentResult.attributes, lpNode, "dateField");
                                }
                                string fullTitle = string.IsNullOrEmpty(date) ? lpTitle : lpTitle + " (" + date + ")";

                                if (dictLegalProvision.ContainsKey(fullTitle))
                                {
                                    if (!dictLegalProvision[fullTitle].Contains(value))
                                    {
                                        dictLegalProvision[fullTitle].Add(value);
                                    }
                                }
                                else
                                {
                                    dictLegalProvision.Add(fullTitle, new List<string>(new string[] { value }));
                                }
                            }
                        }
                    }
                }
                IList<legalProvision> lpList = new List<legalProvision>();
                foreach (string key in dictLegalProvision.Keys)
                {
                    lpList.Add(new legalProvision
                    {
                        label = key,
                        values = dictLegalProvision[key].ToArray()
                    });
                }

                // Laws
                IList<law> lawList = new List<law>();
                foreach (XmlNode idNode in node.SelectNodes("LawId"))
                {
                    if (!string.IsNullOrEmpty(idNode.InnerText))
                    {
                        XmlNode lawNode = XmlHelper.GetNodeByAttribute(this.docConfig, "Law", "id", idNode.InnerText);
                        string id = XmlHelper.GetXmlElementValue(lawNode, "TID");

                        if (string.IsNullOrEmpty(id))
                        {
                            string lawTitle = string.Format("{0} ({1}), {2}", XmlHelper.GetXmlElementValue(lawNode, "Title"),
                                XmlHelper.GetXmlElementValue(lawNode, "Abbreviation"), XmlHelper.GetXmlElementValue(lawNode, "OfficialNumber"));
                            lawList.Add(new law
                            {
                                index = XmlHelper.GetXmlElementValue(lawNode, "Index"),
                                title = lawTitle,
                                link = XmlHelper.GetXmlElementValue(lawNode, "TextAtWeb")
                            });
                        }
                        else
                        {
                            Document law = oerebHelper.GetLaw(id, "fr");
                            string lawTitle = string.Format("{0} ({1}), {2}", law.Title[0].Text, law.Abbreviation[0].Text, law.OfficialNumber[0].Text);
                            lawList.Add(new law
                            {
                                index = law.Index,
                                title = lawTitle,
                                link = law.TextAtWeb[0].Text
                            });
                        }
                    }
                }
                lawList.OrderBy(l => l.index);

                // Hints
                IDictionary<string, List<string>> dictHint = new Dictionary<string, List<string>>();
                foreach (XmlNode idNode in node.SelectNodes("HintId"))
                {
                    if (!string.IsNullOrEmpty(idNode.InnerText))
                    {
                        XmlNode hintNode = XmlHelper.GetNodeByAttribute(this.docConfig, "Hint", "id", idNode.InnerText);
                        string hintTitle = XmlHelper.GetXmlElementValue(hintNode, "Title");

                        foreach (RestrictionResult result in subResults)
                        {
                            string value = XmlHelper.GetAttributeFromAttribute(result.IdentResult.attributes, hintNode, "field");
                            if (!string.IsNullOrEmpty(value))
                            {
                                //value = value.Replace("&", "%26");
                                if (dictHint.ContainsKey(hintTitle))
                                {
                                    if (!dictHint[hintTitle].Contains(value))
                                    {
                                        dictHint[hintTitle].Add(value);
                                    }
                                }
                                else
                                {
                                    dictHint.Add(hintTitle, new List<string>(new string[] { value }));
                                }
                            }
                        }
                    }
                }
                IList<hint> hintList = new List<hint>();
                foreach (string key in dictHint.Keys)
                {
                    hintList.Add(new hint
                    {
                        label = key,
                        values = dictHint[key].ToArray()
                    });
                }

                string code = XmlHelper.GetXmlElementValue(node, "Theme/Code");
                int index = int.Parse(XmlHelper.GetXmlElementValue(node, "Theme/Index"));
                RestrictionTheme theme = themes.First(t => string.Compare(t.Code, code) == 0 && t.Index == index);
                string uniqueId = GetMapWorkerId(layerId, first.Lawstatus);

                string title = theme.IsSubTheme ? string.Format("{0}: {1}", theme.Text, theme.SubText) : theme.Text;
                restrictions.Add(new restriction()
                {
                    id = string.Format("{0}_{1}", theme.IsSubTheme ? theme.SubCode : theme.Code, status.Id),
                    tocTitle = status.Code == LawstatusCode.inForce ? title : string.Format("{0} ({1})", title, status.Text),
                    title = title,
                    order = theme.Index + double.Parse(status.Id) / 10,
                    result = true,
                    lawStatus = status.Text,
                    legalProvisions = lpList.ToArray(),
                    laws = lawList.ToArray(),
                    hints = hintList.ToArray(),
                    service = new service()
                    {
                        name = XmlHelper.GetXmlElementValue(node, "ResponsibleOffice/Name"),
                        link = XmlHelper.GetXmlElementValue(node, "ResponsibleOffice/OfficeAtWeb")
                    },
                    mapUrl = dictRestrictionMapUrls[uniqueId],
                    legends = dictLegends.Values.OrderBy(l => l.geometryOrder).ToArray(),
                    otherLegends = dictOtherLegends.Values.OrderBy(l => l.geometryOrder).ToArray(),
                    additionalLegends = dictAdditionalLegends.Values.OrderBy(l => l.geometryOrder).ToArray(),
                });
            }
        }

        return restrictions;
    }

    private string[] GetNotConcernedTheme(IList<RestrictionTheme> themes, RestrictionResult[] restrictions)
    {
        IList<string> list = new List<string>();
        foreach (RestrictionTheme theme in themes)
        {
            int id = mapLayerInfo.GetLayerInfo(theme.Layer, true).LayerID;
            int count = restrictions.Count(rr => rr.LayerId == id);
            foreach (int addId in this.restrWorker.GetAdditionalLayerIds(id))
            {
                count += restrictions.Count(rr => rr.LayerId == addId);
            }
            if (count == 0)
            {
                string title = theme.IsSubTheme ? string.Format("{0}: {1}", theme.Text, theme.SubText) : theme.Text;
                if (!list.Contains(title))
                {
                    list.Add(title);
                }
            }
        }
        return list.ToArray();
    }

    private string AddObjectsToMap(string name, byte[] data, Bitmap bmScaleBar)
    {
        string fileName = name + ".png";
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (Bitmap bmData = new Bitmap(ms))
            using (Bitmap bm = bmData.Clone(new Rectangle(0, 0, bmData.Width, bmData.Height), PixelFormat.Format32bppArgb))
            {
                Bitmap bmNorthArrow = new Bitmap(Path.Combine(Path.Combine(WebHelper.GetConfigValue("ApplicationPath"), "images"), "NorthArrow.png"));
                Bitmap scClone = (Bitmap)bmScaleBar.Clone();

                bmNorthArrow.SetResolution(bm.HorizontalResolution, bm.VerticalResolution);
                scClone.SetResolution(bm.HorizontalResolution, bm.VerticalResolution);

                Rectangle rect = this.printParams.GetScaleBarDrawRectangle();
                Point pt = this.printParams.GetNorthArrowDrawPoint();

                if (this.printParams.GetNorthArrowAlignment() == "H")
                {
                    pt.Y = rect.Y + (rect.Height / 2) - ((int)(bmNorthArrow.Height * this.printParams.GetNorthArrowScale()) / 2);
                }
                else if (this.printParams.GetNorthArrowAlignment() == "V")
                {
                    pt.X = rect.X + (scClone.Width / 2);
                }

                Graphics compose = Graphics.FromImage(bm);
                compose.InterpolationMode = InterpolationMode.High;
                compose.CompositingQuality = CompositingQuality.HighQuality;
                compose.SmoothingMode = SmoothingMode.AntiAlias;

                // North arrow
                compose.DrawImage(bmNorthArrow, pt.X - (bmNorthArrow.Width / 2), pt.Y,
                    (int)(bmNorthArrow.Width * this.printParams.GetNorthArrowScale()),
                    (int)(bmNorthArrow.Height * this.printParams.GetNorthArrowScale()));
                // scale bar
                compose.DrawImage(scClone, rect.X, rect.Y);
                // map outline
                compose.DrawRectangle(new Pen(Color.Black, GetReportReq.MAP_OUTLINE_WIDTH),
                    1, 1, bm.Width - GetReportReq.MAP_OUTLINE_WIDTH, bm.Height - GetReportReq.MAP_OUTLINE_WIDTH);

                bm.Save(Path.Combine(this.workPath, fileName));
            }
        }
        return string.Format("{0}/{1}", this.workUrl, fileName);
    }

    private string GetSymbolUrl(string name, byte[] data)
    {
        using (MemoryStream ms = new MemoryStream(data))
        {
            Bitmap bm = new Bitmap(ms);

            string fileName = name + ".png";
            bm.Save(Path.Combine(this.workPath, fileName));

            return string.Format("{0}/{1}", this.workUrl, fileName);
        }
    }

    private string GetReportFontName()
    {
        XmlDocument doc = new XmlDocument();
        doc.Load(Helper.GetReportConfigFilePath("ReportTemplate.xml"));
        return doc.SelectSingleNode("/Template/Font").InnerText;
    }

    private void CreateWorkingDirectory(string id)
    {
        Helper.CleanDirectory(this.workPath, 1);

        this.extractId = id;

        this.workPath = Path.Combine(this.workPath, this.extractId);
        Directory.CreateDirectory(this.workPath);
        this.workUrl = string.Format("{0}/{1}", this.workUrl, this.extractId);
    }
}