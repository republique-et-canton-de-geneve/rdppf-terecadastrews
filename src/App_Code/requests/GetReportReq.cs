/* $Rev: 22477 $ */
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Topomat.Web.Common;
using System.Diagnostics;
using System.Threading;
using System.IO;
using System.Drawing;
using System;
using System.Drawing.Imaging;
using ExtractData_v103;
using System.Drawing.Drawing2D;

public class GetReportReq : CommonReq
{
    private static int MAP_OUTLINE_WIDTH = 2;
    private static int TITLE_MARGIN_BREAK = 55;
    private static int TITLE_MARGIN_SINGLE = 14;
    private static int TITLE_MARGIN_DOUBLE = 7;

    private string workPath;
    private string workUrl;
    private string extractId;

    public GetReportReq(GetExtractParamReq param, string flavour)
    {
        param.withImages = true;
        param.allTopics = true;
        base.Init(param, flavour, true);
        this.workPath = WebHelper.GetConfigValue("WorkingPath");
        this.workUrl = WebHelper.GetConfigValue("WorkingUrl");
    }

    public byte[] GetResponseAsPdf(QueryResultFeature feature)
    {
        Stopwatch timer = Stopwatch.StartNew();

        ReportData reportData = this.GetReportData(feature);

        Helper.LogInfo(this.GetType().ToString(), "GetResponseAsPdf - * DONNEES *", timer.ElapsedMilliseconds);
        timer.Restart();

        ExtractGenerator generator = new ExtractGenerator();
        byte[] pdfData = generator.Generate(reportData);

        Helper.LogInfo(this.GetType().ToString(), "GetResponseAsPdf - * RAPPORT *", timer.ElapsedMilliseconds);
        timer.Stop();

        return pdfData;
    }

    private ReportData GetReportData(QueryResultFeature feature)
    {
        Stopwatch timer = Stopwatch.StartNew();

        IDictionary<Thread, MapPrintWorkerThread> dictMapPrintThreads = new Dictionary<Thread, MapPrintWorkerThread>();
        IDictionary<Thread, SurfaceWorkerThread> dictSurfThreads = new Dictionary<Thread, SurfaceWorkerThread>();

        // first get maps
        Extent geomExtent = this.queryWorker.GetGeometryExtent(feature.geometry);
        this.printParams = new MapPrintParams(XmlHelper.GetMapPrintConfig(), geomExtent);

        RestrictionResult[] restrictionResults = this.restrWorker.RunAnalyse(feature, this.printParams.GetMapExtent());

        Helper.LogInfo(this.GetType().ToString(), "GetReportData - analyse", timer.ElapsedMilliseconds);
        timer.Restart();

        string marker = "markerMapLayer";
        if (feature.type == ParcelleType.DDP)
        {
            marker = "markerDDPMapLayer";
        }

        int[] ids = this.GetMapLayerIds(new string[] { marker, "addMapLayer", "mainMapLayer" });
        int markerId = this.GetMapLayerIds(new string[] { marker })[0];
        string layerDefs = string.Format("{0}:OBJECTID={1}", markerId, feature.attributes["OBJECTID"]);

        dictMapPrintThreads.Add(this.GetMapPrintWorkerThread(this.printParams, geomExtent, ids,
            MapWorkerThread.TYPE_PAGE, -1, layerDefs));

        ids = this.GetMapLayerIds(new string[] { marker, "addMapLayer", "restrictionMapLayer" });
        foreach (RestrictionResult restriction in restrictionResults)
        {
            if (restriction.isFirst)
            {
                IList<int> idList = new List<int>(ids);
                idList.Add(restriction.LayerId);

                XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", restriction.IdentResult.layerName);
                string additionalLayers = XmlHelper.GetXmlAttribute(node, "additionalLayers", false);

                if (!string.IsNullOrEmpty(additionalLayers))
                {
                    foreach (string additionalLayer in additionalLayers.Split(new char[] { ',' }))
                    {
                        idList.Add(this.mapLayerInfo.GetLayerInfo(additionalLayer).LayerID);
                    }
                }
                dictMapPrintThreads.Add(this.GetMapPrintWorkerThread(this.printParams, geomExtent, idList.ToArray(),
                    MapWorkerThread.TYPE_RESTRICTION, restriction.LayerId, layerDefs));
            }
        }

        // compute surfaces
        IEnumerable<IGrouping<int, RestrictionResult>> groups = restrictionResults.GroupBy(rr => rr.LayerId);
        foreach (IList<RestrictionResult> restrList in groups)
        {
            string layerName = restrList.First().IdentResult.layerName;
            XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig, "RestrictionOnLandownership", "layer", layerName);

            bool isComplete = bool.Parse(XmlHelper.GetXmlAttribute(node, "complete", true));
            bool isOverlap = false;
            string overlapValue = XmlHelper.GetXmlAttribute(node, "overlap", false);
            if (!string.IsNullOrEmpty(overlapValue))
            {
                bool.TryParse(overlapValue, out isOverlap);
            }

            SurfaceWorkerThread swThread = new SurfaceWorkerThread(new SurfaceWorker());
            swThread.Init(feature, isComplete, isOverlap, restrList.ToArray());

            Thread thread = new Thread(new ThreadStart(swThread.Start));
            thread.Start();

            dictSurfThreads.Add(thread, swThread);
        }

        string mainMapUrl = string.Empty;
        IDictionary<int, string> dictRestrictionMapUrls = new Dictionary<int, string>();

        this.CreateWorkingDirectory(this.GetIdentifier(feature));

        // wait for threads to finish
        foreach (KeyValuePair<Thread, SurfaceWorkerThread> pair in dictSurfThreads)
        {
            pair.Key.Join();
            if (!pair.Value.IsSuccessfull())
            {
                throw new WsUserException(pair.Value.GetErrorMessage());
            }
        }

        Helper.LogInfo(this.GetType().ToString(), "GetReportData - calcul des surfaces", timer.ElapsedMilliseconds);

        ScaleBar scaleBar = new ScaleBar(printParams);
        Bitmap bmScaleBar = scaleBar.DrawBar();
        Bitmap bmNorthArrow = new Bitmap(Path.Combine(Path.Combine(WebHelper.GetConfigValue("ApplicationPath"), "images"),
            "NorthArrow.png"));

        foreach (KeyValuePair<Thread, MapPrintWorkerThread> pair in dictMapPrintThreads)
        {
            pair.Key.Join();

            if (!pair.Value.IsSuccessfull())
            {
                throw new WsUserException(pair.Value.GetErrorMessage());
            }

            if (pair.Value.GetMapPrintType() == MapWorkerThread.TYPE_PAGE)
            {
                mainMapUrl = this.AddObjectsToMap("main", pair.Value.GetResult(), bmNorthArrow, bmScaleBar, scaleBar);
            }
            else
            {
                string name = string.Format("restr_{0}", pair.Value.GetLayerId());
                dictRestrictionMapUrls.Add(pair.Value.GetLayerId(),
                    this.AddObjectsToMap(name, pair.Value.GetResult(), bmNorthArrow, bmScaleBar, scaleBar));
            }
        }

        Helper.LogInfo(this.GetType().ToString(), "GetReportData - récupération des cartes", timer.ElapsedMilliseconds);
        timer.Restart();

        data[] attributes = this.GetMainData(feature);

        // add concerned themes
        IList<restriction> restrictions = new List<restriction>();
        IEnumerable<IGrouping<int, RestrictionResult>> restrGroups = restrictionResults.GroupBy(rr => rr.LayerId);

        foreach (IList<RestrictionResult> restrResultList in restrGroups)
        {
            restrictions.Add(this.GetRestrictionData(restrResultList.OrderBy(_l => _l.Legend.Index).ToList(), dictRestrictionMapUrls));
        }
        // add not concerned themes
        foreach (string notConcernedTheme in this.GetNotConcernedTheme(restrictionResults))
        {
            restrictions.Add(new restriction()
            {
                title = notConcernedTheme,
                result = false
            });
        }
        
        ReportData reportData = new ReportData(this.extractId);

        reportData.section.type = this.flavour.ToUpper();
        reportData.section.mapUrl = mainMapUrl;
        reportData.section.dmoDate = this.GetFormattedDMODate();
        reportData.section.datas = attributes;
        reportData.section.restrictions = restrictions.ToArray();

        IList<string> themes = new List<string>();
        foreach (Theme t in this.GetThemeWithoutData())
        {
            themes.Add(t.Text.Text);
        }
        reportData.section.noDataThemes = themes.ToArray();

        IList<string> infos = new List<string>();
        foreach (LocalisedMText t in this.GetLocalisedMText(this.infoConfig, "GeneralInformation"))
        {
            infos.Add(t.Text);
        }
        reportData.section.generalInfos = infos.ToArray();

        IList<string> data = new List<string>();
        foreach (LocalisedMText t in this.GetBaseData())
        {
            data.Add(t.Text);
        }
        reportData.section.baseData = data.ToArray();
        
        reportData.section.dmoDate = this.GetFormattedDMODate();

        IList<glossary> glossaries = new List<glossary>();
        foreach (Glossary g in this.GetGlossary())
        {
            glossaries.Add(new glossary { title = g.Title[0].Text, content = g.Content[0].Text });
        }
        reportData.glossaries = glossaries.ToArray();

        Helper.LogInfo(this.GetType().ToString(), "GetReportData - données du rapport", timer.ElapsedMilliseconds);
        timer.Stop();

        return reportData;
    }

    private KeyValuePair<Thread, MapPrintWorkerThread> GetMapPrintWorkerThread(MapPrintParams printParams, Extent extent,
        int[] layerIds, int type, int id, string layerDefs)
    {
        MapPrintWorkerThread mapPrintThread = new MapPrintWorkerThread(new MapWorker(printParams), type, id);
        mapPrintThread.Init(extent, layerIds, layerDefs);

        Thread thread = new Thread(new ThreadStart(mapPrintThread.Start));
        thread.Start();

        return new KeyValuePair<Thread, MapPrintWorkerThread>(thread, mapPrintThread);
    }

    private data[] GetMainData(QueryResultFeature feature)
    {
        XmlNode requestConfig = XmlHelper.GetConfig("request.xml", "RequestConfig");

        string xpath = "RealEstate/Parcelle";
        if (feature.type == ParcelleType.DDP)
        {
            xpath = "RealEstate/DDP";
        }
        XmlNode reNode = this.requestConfig.SelectSingleNode(xpath);

        field[] parcFields = new field[]
        {
            new field() { name = "NO_PARCELLE", value = XmlHelper.GetAttributeFromNode(feature, reNode, "Number") },
            new field() { name = "EGRID", value = XmlHelper.GetAttributeFromNode(feature, reNode, "EGRID") },
            new field() { name = "NOMFECO", value = XmlHelper.GetAttributeFromNode(feature, reNode, "Municipality") },
            new field() { name = "NUFECO", value = XmlHelper.GetAttributeFromNode(feature, reNode, "FosNr") },
            new field() { name = "SURFACE", value = XmlHelper.GetAttributeFromNode(feature, reNode, "LandRegistryArea") }
        };

        dataLayer parcDataLayer = new dataLayer
        {
            layer = "CAD_PARCELLE_MENSU",
            fields = parcFields
        };

        section parcSection = new section
        {
            id = "parcelle",
            dataLayers = new dataLayer[] { parcDataLayer }
        };

        data attrData = new data
        {
            id = "attributs",
            sections = new section[] { parcSection }
        };

        return new data[] { attrData };
    }

    private string[] GetNotConcernedTheme(RestrictionResult[] restrictions)
    {
        IList<string> list = new List<string>();
        IList<RestrictionResult> restrictionList = new List<RestrictionResult>(restrictions);
        foreach (XmlNode node in this.requestConfig.SelectNodes("RestrictionOnLandownership"))
        {
            string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
            int id = this.mapLayerInfo.GetLayerInfo(layerName).LayerID;

            int count = restrictionList.Count<RestrictionResult>(rr => rr.IdentResult.layerId == id);
            if (count == 0)
            {
                list.Add(XmlHelper.GetXmlElementValue(node, "Theme/Text"));
            }
        }
        return list.ToArray();
    }

    private restriction GetRestrictionData(IList<RestrictionResult> restrResultList,
        IDictionary<int, string> dictRestrictionMapUrls)
    {
        RestrictionResult firstResult = restrResultList.First();

        XmlNode node = XmlHelper.GetNodeByAttribute(this.requestConfig,
                "RestrictionOnLandownership", "layer", firstResult.IdentResult.layerName);

        string legendAtWeb = string.Format("{0}/{1}.htm", WebHelper.GetConfigValue("LegendUrl"),
            firstResult.IdentResult.layerName.ToLower());

        // legends
        IDictionary<string, legend> dictLegends = new Dictionary<string, legend>();
        foreach (RestrictionResult result in restrResultList)
        {
            if (dictLegends.Keys.Contains(result.Legend.TypeCode))
            {
                legend leg = dictLegends[result.Legend.TypeCode];
                leg.length += result.Length;
                leg.surface += result.Area;
                leg.surfPercent += result.PartInPercent;
            }
            else
            {
                string fileName = string.Format("leg_{0}", result.Legend.TypeCode.Replace(":", "_"));
                dictLegends.Add(result.Legend.TypeCode, new legend()
                {
                    imageUrl = this.GetSymbolUrl(fileName, result.Legend.Symbol),
                    label = result.Legend.Text,
                    length = result.Length,
                    surface = result.Area,
                    surfPercent = result.PartInPercent
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
        foreach (RestrictionResult result in restrResultList)
        {
            foreach (RestrictionLegend otherLegend in result.OtherLegends)
            {
                if (!dictLegends.Keys.Contains(otherLegend.TypeCode) && !dictOtherLegends.Keys.Contains(otherLegend.TypeCode))
                {
                    string fileName = string.Format("leg_{0}", otherLegend.TypeCode.Replace(":", "_"));
                    dictOtherLegends.Add(otherLegend.TypeCode, new legend()
                    {
                        imageUrl = this.GetSymbolUrl(fileName, otherLegend.Symbol),
                        label = otherLegend.Text,
                    });
                }

            }
        }

        // additionnal legends
        IDictionary<string, legend> dictAdditionalLegends = new Dictionary<string, legend>();
        foreach (RestrictionResult result in restrResultList)
        {
            foreach (RestrictionLegend additionalLegend in result.AdditionalLegends)
            {
                if (!dictAdditionalLegends.Keys.Contains(additionalLegend.TypeCode))
                {
                    string fileName = string.Format("leg_{0}", additionalLegend.TypeCode.Replace(":", "_"));
                    dictAdditionalLegends.Add(additionalLegend.TypeCode, new legend()
                    {
                        imageUrl = this.GetSymbolUrl(fileName, additionalLegend.Symbol),
                        label = additionalLegend.Text,
                    });
                }
            }
        }

        // regulations
        IDictionary<string, List<string>> dictRegulation = new Dictionary<string, List<string>>();
        foreach (XmlNode idNode in node.SelectNodes("regulationId"))
        {
            if (!string.IsNullOrEmpty(idNode.InnerText))
            {
                XmlNode regNode = XmlHelper.GetNodeByAttribute(this.legalConfig.SelectSingleNode("Regulations"),
                    "LegalProvisions", "id", idNode.InnerText);
                string title = XmlHelper.GetXmlElementValue(regNode, "Title");

                foreach (RestrictionResult result in restrResultList)
                {
                    string value = XmlHelper.GetAttributeFromAttribute(result.IdentResult.attributes, regNode, "field");
                    if (!string.IsNullOrEmpty(value))
                    {
                        if (dictRegulation.ContainsKey(title))
                        {
                            if (!dictRegulation[title].Contains(value))
                            {
                                dictRegulation[title].Add(value);
                            }
                        }
                        else
                        {
                            dictRegulation.Add(title, new List<string>(new string[] { value }));
                        }
                    }
                }
            }
        }
        IList<regulation> regList = new List<regulation>();
        foreach (string key in dictRegulation.Keys)
        {
            regList.Add(new regulation
            {
                label = key,
                values = dictRegulation[key].ToArray()
            });
        }

        // laws
        IList<law> lawList = new List<law>();
        foreach (XmlNode idNode in node.SelectNodes("lawId"))
        {
            if (!string.IsNullOrEmpty(idNode.InnerText))
            {
                XmlNode lawNode = XmlHelper.GetNodeByAttribute(this.legalConfig.SelectSingleNode("Laws"),
                    "LegalProvisions", "id", idNode.InnerText);

                lawList.Add(new law
                {
                    title = XmlHelper.GetXmlElementValue(lawNode, "Title"),
                    link = XmlHelper.GetXmlElementValue(lawNode, "TextAtWeb")
                });
            }
        }

        // informations
        IDictionary<string, List<string>> dictInformation = new Dictionary<string, List<string>>();
        foreach (XmlNode idNode in node.SelectNodes("informationId"))
        {
            if (!string.IsNullOrEmpty(idNode.InnerText))
            {
                XmlNode infoNode = XmlHelper.GetNodeByAttribute(this.legalConfig.SelectSingleNode("Informations"),
                    "LegalProvisions", "id", idNode.InnerText);
                string title = XmlHelper.GetXmlElementValue(infoNode, "Title");

                foreach (RestrictionResult result in restrResultList)
                {
                    string value = XmlHelper.GetAttributeFromAttribute(result.IdentResult.attributes, infoNode, "field");
                    if (!string.IsNullOrEmpty(value))
                    {
                        //value = value.Replace("&", "%26");
                        if (dictInformation.ContainsKey(title))
                        {
                            if (!dictInformation[title].Contains(value))
                            {
                                dictInformation[title].Add(value);
                            }
                        }
                        else
                        {
                            dictInformation.Add(title, new List<string>(new string[] { value }));
                        }
                    }
                }
            }
        }
        IList<information> infoList = new List<information>();
        foreach (string key in dictInformation.Keys)
        {
            infoList.Add(new information
            {
                label = key,
                values = dictInformation[key].ToArray()
            });
        }

        string restrTitle = XmlHelper.GetXmlElementValue(node, "Theme/Text");
        string titleMarginStyle = string.Format("height:{0}mm", TITLE_MARGIN_SINGLE);
        if (restrTitle.Length > TITLE_MARGIN_BREAK)
        {
            titleMarginStyle = string.Format("height:{0}mm", TITLE_MARGIN_DOUBLE);
        }
        // return object
        return new restriction()
        {
            id = XmlHelper.GetXmlElementValue(node, "SubTheme"),
            title = XmlHelper.GetXmlElementValue(node, "Theme/Text"),
            result = true,
            regulations = regList.ToArray(),
            laws = lawList.ToArray(),
            informations = infoList.ToArray(),
            service = new service()
            {
                name = XmlHelper.GetXmlElementValue(node, "ResponsibleOffice/Name"),
                link = XmlHelper.GetXmlElementValue(node, "ResponsibleOffice/OfficeAtWeb")
            },
            titleMarginStyle = titleMarginStyle,
            mapUrl = dictRestrictionMapUrls[firstResult.LayerId],
            geometryType = firstResult.IdentResult.geometryType,
            legendLink = legendAtWeb,
            legends = dictLegends.Values.ToArray(),
            otherLegends = dictOtherLegends.Values.ToArray(),
            additionnalLegends = dictAdditionalLegends.Values.ToArray()
        };
    }

    private string AddObjectsToMap(string name, byte[] data, Bitmap bmNorthArrow, Bitmap bmScaleBar, ScaleBar scaleBar)
    {
        string fileName = name + ".png";
        using (MemoryStream ms = new MemoryStream(data))
        {
            using (Bitmap bmData = new Bitmap(ms))
            using (Bitmap bm = bmData.Clone(new Rectangle(0, 0, bmData.Width, bmData.Height), PixelFormat.Format32bppArgb))
            {
                bmNorthArrow.SetResolution(bm.HorizontalResolution, bm.VerticalResolution);
                bmScaleBar.SetResolution(bm.HorizontalResolution, bm.VerticalResolution);

                Rectangle rect = this.printParams.GetScaleBarDrawRectangle();
                Point pt = this.printParams.GetNorthArrowDrawPoint();

                if (this.printParams.GetNorthArrowAlignment() == "H")
                {
                    pt.Y = rect.Y + (rect.Height / 2) - ((int)(bmNorthArrow.Height * this.printParams.GetNorthArrowScale()) / 2);
                }
                else if (this.printParams.GetNorthArrowAlignment() == "V")
                {
                    pt.X = rect.X + (bmScaleBar.Width / 2);
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
                compose.DrawImage(bmScaleBar, rect.X, rect.Y);
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

    private void CreateWorkingDirectory(string id)
    {
        Helper.CleanDirectory(this.workPath, 1);

        this.extractId = id;

        this.workPath = Path.Combine(this.workPath, this.extractId);
        Directory.CreateDirectory(this.workPath);
        this.workUrl = string.Format("{0}/{1}", this.workUrl, this.extractId);
    }
}