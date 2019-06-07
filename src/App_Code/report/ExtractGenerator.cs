/* $Rev: 21372 $ */
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Topomat.Web.Common;
using Topomat.Pdf.Report;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Collections.Generic;
using System.Net;
using System.Diagnostics;
using System.Threading;

public class ExtractGenerator
{

    #region Data members

    private static int LINES_BEFORE_BREAK = 24;
    private string workPath;
    private List<ReportAnnex> Annexes;
    private int NextAnnex;

    #endregion

    #region Constructor / Destructor

    public ExtractGenerator()
    {
        this.Annexes = new List<ReportAnnex>();
        this.NextAnnex = 1;
    }

    #endregion

    #region Public methods

    public byte[] Generate(ReportData reportData)
    {
        this.workPath = Path.Combine(WebHelper.GetConfigValue("WorkingPath"), reportData.section.reference);

        // get template
        ReportTemplate template = GetTemplate();

        // build restriction pages
        Stopwatch timer = Stopwatch.StartNew();

        string glossaryFileName = Path.Combine(this.workPath, "glossary.pdf");
        IDictionary<Thread, ExtractWorkerThread> dictExtractThreads = new Dictionary<Thread, ExtractWorkerThread>();
        foreach (restriction restr in reportData.section.restrictions)
        {
            if (restr.result == true)
            {
                restr.annexes = this.GetAnnexes(reportData.section.type, restr);
                //this.TestBreakPage(restr);

                ExtractWorkerThread ewThread = new ExtractWorkerThread(new ExtractWorker(workPath, template));
                ewThread.InitRestriction(restr, Path.Combine(this.workPath, restr.id + ".pdf"));

                Thread thread = new Thread(new ThreadStart(ewThread.Start));
                thread.Start();

                dictExtractThreads.Add(thread, ewThread);
            }
        }

        ExtractWorkerThread gewThread = new ExtractWorkerThread(new ExtractWorker(workPath, template));
        gewThread.InitGlossary(reportData.glossaries, glossaryFileName);

        Thread gThread = new Thread(new ThreadStart(gewThread.Start));
        gThread.Start();

        dictExtractThreads.Add(gThread, gewThread);
        
        List<string> tmpFiles = new List<string>();
        List<toc> tables = new List<toc>();
        int pages = 1;

        // wait for extract threads to finish
        foreach (KeyValuePair<Thread, ExtractWorkerThread> pair in dictExtractThreads)
        {
            pair.Key.Join();
            if (!pair.Value.IsSuccessfull())
            {
                throw new WsUserException(pair.Value.GetErrorMessage());
            }

            if (pair.Value.GetWorkerType() == (int)ExtractWorkerThread.workerTypes.Restriction)
            {
                tmpFiles.Add(pair.Value.GetFileName());

                toc table = new toc();
                table.title = pair.Value.GetRestriction().title;
                using (PdfReader input = new PdfReader(pair.Value.GetFileName()))
                {
                    table.page = (pages).ToString();
                    pages += input.NumberOfPages;
                }
                table.annexes = pair.Value.GetRestriction().annexes;
                tables.Add(table);
            }            
        }

        
        Helper.LogInfo(this.GetType().ToString(), "Generate - pages de restriction + abréviations", timer.ElapsedMilliseconds);
        timer.Restart();

        // build main section pages
        reportData.section.tocs = tables.ToArray();
        this.TestTocBreakPage(reportData.section);

        string tmpFileName = this.GenerateMainSection(template, reportData);

        Helper.LogInfo(this.GetType().ToString(), "Generate - page de garde", timer.ElapsedMilliseconds);
        timer.Restart();

        // correct toc page numbers
        int mainPages = 0;
        using (PdfReader input = new PdfReader(tmpFileName))
        {
            mainPages = input.NumberOfPages;
        }
        foreach (toc toc in reportData.section.tocs)
        {
            int page = int.Parse(toc.page);
            page += mainPages;
            toc.page = page.ToString();
        }

        // rebuild main section pages
        string fname = this.GenerateMainSection(template, reportData);

        Helper.LogInfo(this.GetType().ToString(), "Generate - page de garde + sommaire", timer.ElapsedMilliseconds);
        timer.Restart();

        List<string> pdfFiles = new List<string>();
        pdfFiles.Add(fname);
        foreach (string tf in tmpFiles)
        {
            pdfFiles.Add(tf);
        }
        pdfFiles.Add(glossaryFileName);

        // generate final report
        PdfReport report = new PdfReport(template.Format, template.MarginLeft, template.MarginRight,
                    template.MarginTop, template.MarginBottom);

        if (template.UseModel == true)
        {
            report.SetTemplate(Helper.GetReportConfigFilePath("Template.pdf"));
        }

        FontFactory.RegisterDirectory(template.Font.FontDirectory);
        BaseFont bf = FontFactory.GetFont(template.Font.FontName, BaseFont.CP1252, BaseFont.EMBEDDED).BaseFont;

        foreach (ReportTemplateZone dz in template.TextDateZones)
        {
            TextZone zone = new TextZone(DateTime.Now.ToString(dz.Text), bf, dz.FontSize);
            zone.SetPosition(dz.Alignement, dz.PositionX, dz.PositionY, dz.Rotation);
            zone.SetRGBColorFill(dz.FontColor[0], dz.FontColor[1], dz.FontColor[2]);
            report.AddTextZone(zone);
        }
        foreach (ReportTemplateZone pz in template.TextPageZones)
        {
            TextZone zone = new TextZone(pz.Text, bf, pz.FontSize);
            zone.SetPosition(pz.Alignement, pz.PositionX, pz.PositionY, pz.Rotation);
            zone.SetRGBColorFill(pz.FontColor[0], pz.FontColor[1], pz.FontColor[2]);
            report.AddTextPageZone(zone);
        }
        foreach (ReportTemplateZone z in template.TextZones)
        {
            string content = string.Empty;
            switch (z.Text)
            {
                case "#REF#":
                    content = reportData.section.reference;
                    break;
                default:
                    break;
            }
            TextZone zone = new TextZone(content, bf, z.FontSize);
            zone.SetPosition(z.Alignement, z.PositionX, z.PositionY, z.Rotation);
            zone.SetRGBColorFill(z.FontColor[0], z.FontColor[1], z.FontColor[2]);
            report.AddTextZone(zone);
        }

        Helper.LogInfo(this.GetType().ToString(), "Generate - rapport final", timer.ElapsedMilliseconds);
        timer.Restart();

        // add legal documents
        if (this.Annexes.Count == 0)
        {
            fname = Path.Combine(this.workPath, "staticExtract.pdf");
            report.MergePdfFiles(pdfFiles, this.workPath, fname);
        }
        else
        {
            fname = Path.Combine(this.workPath, "staticExtract_temp.pdf");
            report.MergePdfFiles(pdfFiles, this.workPath, fname);
            pdfFiles.Add(fname);

            Dictionary<int, string> annexFiles = new Dictionary<int, string>();
            annexFiles.Add(-1, fname);
            foreach (ReportAnnex annex in this.Annexes)
            {
                if (annex.isIncluded == true)
                {
                    annexFiles.Add(annex.number, annex.value);
                }
            }
            fname = Path.Combine(this.workPath, "staticExtract.pdf");
            MergeLegalFiles(reportData.section.reference, annexFiles, fname);
        }

        Helper.LogInfo(this.GetType().ToString(), "Generate - ajout des dispositions juridiques", timer.ElapsedMilliseconds);
        timer.Stop();

        return File.ReadAllBytes(fname);
    }

    #endregion

    #region private methods

    private string BuildMainSectionXml(mainSection section)
    {
        XmlRootAttribute xRoot = new XmlRootAttribute("mainSection");

        StringBuilder sb = new StringBuilder();
        XmlSerializer serializer = new XmlSerializer(section.GetType(), xRoot);
        serializer.Serialize(XmlWriter.Create(sb), section);

        return sb.ToString();
    }

    //private string BuildGlossaryXml(glossary[] glossaries)
    //{
    //    XmlRootAttribute xRoot = new XmlRootAttribute("glossaryRoot");

    //    StringBuilder sb = new StringBuilder();
    //    XmlSerializer serializer = new XmlSerializer(glossaries.GetType(), xRoot);
    //    serializer.Serialize(XmlWriter.Create(sb), glossaries);

    //    return sb.ToString();
    //}

    private string BuildAnnexXml(string node)
    {
        XmlRootAttribute xRoot = new XmlRootAttribute("annex");

        StringBuilder sb = new StringBuilder();
        XmlSerializer serializer = new XmlSerializer(node.GetType(), xRoot);
        serializer.Serialize(XmlWriter.Create(sb), node);

        return sb.ToString();
    }

    private string GenerateMainSection(ReportTemplate template, ReportData reportData)
    {
        XmlDocument xmlDataDoc = new XmlDocument();
        xmlDataDoc.LoadXml(this.BuildMainSectionXml(reportData.section));

        PdfReport dataReport = new PdfReport(template.Format, template.MarginLeft, template.MarginRight,
            template.MarginTop, template.MarginBottom);

        // generate pdf
        string filename = Path.Combine(this.workPath, "reportData.pdf");
        using (XmlNodeReader xmlReader = new XmlNodeReader(xmlDataDoc))
        using (XmlTextReader xsltReader = new XmlTextReader(Helper.GetReportConfigFilePath("ReportMainSection.xslt")))
        {
            dataReport.GeneratePdf(xmlReader, xsltReader, this.workPath, filename);
        }

        return filename;
    }

    private void TestBreakPage(restriction restr)
    {
        // approximate length of page from collections of restriction
        int length = restr.legends.Length + restr.otherLegends.Length + restr.additionnalLegends.Length;
        // legends count double;
        length = length * 2 + 1;
        foreach (regulation reg in restr.regulations)
        {
            length++;
            length += reg.values.Length;
        }

        if (length > LINES_BEFORE_BREAK)
        {
            restr.breakAfterLegend = true;
        }
        else
        {
            length += restr.laws.Length * 2;
            foreach (information info in restr.informations)
            {
                length++;
                length += info.values.Length;
            }
            length++;
            length += restr.annexes.Length;

            if (length > LINES_BEFORE_BREAK)
            {
                restr.breakAfterRegulation = true;
            }
        }
    }

    private void TestTocBreakPage(mainSection section)
    {
        int test = 0;
        foreach (toc toc in section.tocs)
        {
            if (toc.annexes.Length > 0)
            {
                test += toc.annexes.Length;
            }
            else
            {
                test += 2;
            }
        }
        if (test > LINES_BEFORE_BREAK)
        {
            section.breakAfterToc = true;
        }
        test += section.restrictions.Length;
        if (test > LINES_BEFORE_BREAK)
        {
            section.breakAfterOther = true;
        }
    }

    private annex[] GetAnnexes(string type, restriction restr)
    {
        List<annex> annexes = new List<annex>();

        if (string.Compare(type, "REDUCED") != 0)
        {
            foreach (regulation reg in restr.regulations)
            {
                foreach (string value in reg.values)
                {
                    ReportAnnex repAnnex = this.Annexes.Find(a => string.Compare(a.value, value) == 0);
                    if (repAnnex == null)
                    {
                        repAnnex = new ReportAnnex(value);
                        if (repAnnex.isIncluded)
                        {
                            repAnnex.number = this.NextAnnex;
                            this.NextAnnex++;
                        }
                        this.Annexes.Add(repAnnex);
                    }
                    if (repAnnex.isIncluded)
                    {
                        annex annex = new annex();
                        annex.title = reg.label;
                        annex.number = repAnnex.number;
                        annexes.Add(annex);
                    }
                }
            }
        }

        return annexes.ToArray();
    }



    private ReportTemplate GetTemplate()
    {
        ReportTemplate template = new ReportTemplate();

        XmlDocument doc = new XmlDocument();
        doc.Load(Helper.GetReportConfigFilePath("ReportTemplate.xml"));

        template.SetFormat(doc.SelectSingleNode("/Template/Format").InnerText);
        template.SetMargins(doc.SelectSingleNode("/Template/Margins").InnerText);
        template.SetUseModel(doc.SelectSingleNode("/Template/UseModel").InnerText);

        XmlNode fNode = doc.SelectSingleNode("Template/Font");
        template.Font.SetFontDirectory(fNode.SelectSingleNode("Directory").InnerText);
        template.Font.SetFontName(fNode.SelectSingleNode("Name").InnerText);

        foreach (XmlNode node in doc.SelectNodes("/Template/TextDateZones/TextDateZone"))
        {
            template.TextDateZones.Add(GetTemplateZone(template, node));
        }

        foreach (XmlNode node in doc.SelectNodes("/Template/TextPageZones/TextPageZone"))
        {
            template.TextPageZones.Add(GetTemplateZone(template, node));
        }

        foreach (XmlNode node in doc.SelectNodes("/Template/TextZones/TextZone"))
        {
            template.TextZones.Add(GetTemplateZone(template, node));
        }

        foreach (XmlNode node in doc.SelectNodes("/Template/TextAnnexZones/TextAnnexZone"))
        {
            template.TextAnnexZones.Add(GetTemplateZone(template, node));
        }

        return template;
    }

    private ReportTemplateZone GetTemplateZone(ReportTemplate template, XmlNode node)
    {
        ReportTemplateZone zone = new ReportTemplateZone(template.Format, template.Dimensions);

        zone.Text = node.SelectSingleNode("Text").InnerText;
        zone.SetFontSize(node.SelectSingleNode("FontSize").InnerText);
        zone.SetAlignement(node.SelectSingleNode("Alignement").InnerText);
        XmlNode hpNode = node.SelectSingleNode("HorizontalPosition");
        zone.SetPositionX(XmlHelper.GetXmlAttribute(hpNode, "from", true), hpNode.InnerText);
        XmlNode vpNode = node.SelectSingleNode("VerticalPosition");
        zone.SetPositionY(XmlHelper.GetXmlAttribute(vpNode, "from", true), vpNode.InnerText);
        zone.SetRotation(node.SelectSingleNode("Rotation").InnerText);
        zone.SetFontColor(node.SelectSingleNode("Color").InnerText);

        return zone;
    }

    private void MergeLegalFiles(string reference, Dictionary<int, string> inputFiles, string outputFileName)
    {
        // get template
        ReportTemplate template = GetTemplate();

        IList<string> files = new List<string>();
        IList<string> titleFiles = new List<string>();
        foreach (KeyValuePair<int, string> input in inputFiles)
        {
            if (input.Key > 0)
            {
                string titleFile = BuildAnnexPage(reference, template, input.Key);
                files.Add(titleFile);
                titleFiles.Add(titleFile);
            }
            files.Add(input.Value);
        }

        using (FileStream fs = new FileStream(outputFileName, FileMode.Create, FileAccess.Write))
        {
            Document document = new Document();
            PdfCopy pdf = new PdfCopy(document, fs);
            try
            {
                document.Open();

                Stopwatch timer = Stopwatch.StartNew();

                foreach (string file in files)
                {
                    using (PdfReader reader = new PdfReader(file))
                    {
                        pdf.AddDocument(reader);
                    }

                    string msg = String.Format("MergeLegalFiles - Fichier: {0}", file);
                    Helper.LogInfo(this.GetType().ToString(), msg, timer.ElapsedMilliseconds);
                    timer.Restart();
                }

                timer.Stop();
            }
            finally
            {
                document.Close();
            }
        }
        // delete temporary file
        foreach (string tf in titleFiles)
        {
            File.Delete(tf);
        }
    }

    private string BuildAnnexPage(string reference, ReportTemplate template, int number)
    {
        PdfReport report = new PdfReport(template.Format, template.MarginLeft, template.MarginRight,
                    template.MarginTop, template.MarginBottom);

        if (template.UseModel == true)
        {
            report.SetTemplate(Helper.GetReportConfigFilePath("Template.pdf"));
        }

        FontFactory.RegisterDirectory(template.Font.FontDirectory);
        BaseFont bf = FontFactory.GetFont(template.Font.FontName, BaseFont.CP1252, BaseFont.EMBEDDED).BaseFont;

        foreach (ReportTemplateZone dz in template.TextDateZones)
        {
            TextZone zone = new TextZone(DateTime.Now.ToString(dz.Text), bf, dz.FontSize);
            zone.SetPosition(dz.Alignement, dz.PositionX, dz.PositionY, dz.Rotation);
            zone.SetRGBColorFill(dz.FontColor[0], dz.FontColor[1], dz.FontColor[2]);
            report.AddTextZone(zone);
        }
        foreach (ReportTemplateZone z in template.TextZones)
        {
            string content = string.Empty;
            switch (z.Text)
            {
                case "#REF#":
                    content = reference;
                    break;
                default:
                    break;
            }
            TextZone zone = new TextZone(content, bf, z.FontSize);
            zone.SetPosition(z.Alignement, z.PositionX, z.PositionY, z.Rotation);
            zone.SetRGBColorFill(z.FontColor[0], z.FontColor[1], z.FontColor[2]);
            report.AddTextZone(zone);
        }
        foreach (ReportTemplateZone pz in template.TextAnnexZones)
        {
            string content = string.Format(pz.Text, number);
            TextZone zone = new TextZone(content, bf, pz.FontSize);
            zone.SetPosition(pz.Alignement, pz.PositionX, pz.PositionY, pz.Rotation);
            zone.SetRGBColorFill(pz.FontColor[0], pz.FontColor[1], pz.FontColor[2]);
            report.AddTextPageZone(zone);
        }

        XmlDocument xmlEmptyDoc = new XmlDocument();
        xmlEmptyDoc.LoadXml(this.BuildAnnexXml(string.Empty));

        // generate pdf
        string fname = Path.Combine(this.workPath, "_annex_" + number.ToString() + ".pdf");
        using (XmlNodeReader xmlReader = new XmlNodeReader(xmlEmptyDoc))
        using (XmlTextReader xsltReader = new XmlTextReader(Helper.GetReportConfigFilePath("ReportAnnex.xslt")))
        {
            report.GeneratePdf(xmlReader, xsltReader, this.workPath, fname);
        }
        return fname;
    }

    #endregion

    #region private Classes

    private class ReportAnnex
    {
        public int number;
        public string value;
        public bool isLink;
        public bool isIncluded = false;

        public ReportAnnex(string value)
        {
            this.value = value;
            this.isLink = this.TestLink();
            if (this.isLink)
            {
                this.isIncluded = this.TestAccess();
            }
        }

        private bool TestLink()
        {
            if (this.value.StartsWith("http"))
            {
                return true;
            }
            return false;
        }

        private bool TestAccess()
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(this.value) as HttpWebRequest;
                request.Method = "HEAD";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                using (PdfReader reader = new PdfReader(this.value))
                {
                    return (response.StatusCode == HttpStatusCode.OK);
                }
            }
            catch
            {
                Helper.LogError(new WsUserException(string.Format(Resources.Resource.DOC_NOT_APPENDED, this.value)));
                return false;
            }
        }
    }

    #endregion
}