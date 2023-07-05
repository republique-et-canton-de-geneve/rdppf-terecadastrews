/* $Rev: 30309 $ */
using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Kernel.Utils;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Layout;
using iText.Layout.Properties;
using iText.Layout.Renderer;
using iText.Pdfa;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Topomat.Web.Common;

public class ExtractGenerator
{

    #region Data members

    private static float[] DimensionsA4 = { 210.0f, 297.0f };
    private static int MainNumberOfPage = 3;

    // valeurs permettant de positionner blocs infos / données de base / clauses
    private static int TOC_FIXED_ELEMENTS = 117;
    private static int TOC_CONCERNED_RESTRICTION = 6;
    private static int TOC_OTHER_RESTRICTION = 4;
    private static int TOC_BLOCK = 85;

    private static int PAGE_HEIGHT = 297;

    private string workPath;
    private OeREBKRMHelper oerebHelper;

    #endregion

    #region Constructor / Destructor

    public ExtractGenerator(OeREBKRMHelper helper)
    {
        this.oerebHelper = helper;
    }

    #endregion

    #region Public methods

    public byte[] Generate(ReportData reportData)
    {
        byte[] intent = File.ReadAllBytes(System.IO.Path.Combine(WebHelper.GetConfigValue("ConfigPath"), "sRGB_CS_profile.icm"));
        this.workPath = System.IO.Path.Combine(WebHelper.GetConfigValue("WorkingPath"), reportData.section.reference);

        // get template
        ReportTemplate template = GetTemplate();

        // build restriction pages
        Stopwatch timer = Stopwatch.StartNew();

        SortedList<int, RestrictionExtract> extracts = new SortedList<int, RestrictionExtract>();
        Parallel.ForEach(reportData.section.restrictions, restr =>
        {
            if (restr.result == true)
            {
                string path = System.IO.Path.Combine(this.workPath, restr.id + ".pdf");
                int numPages = GetRestrictionExtract(intent, restr, path);
                extracts.Add(restr.order, new RestrictionExtract
                {
                    Title = restr.title,
                    Pages = numPages,
                    Path = path
                });
            }
        });

        List<toc> tables = new List<toc>();
        int pages = MainNumberOfPage + 1;
        foreach (RestrictionExtract extract in extracts.Values)
        {
            toc table = new toc();
            table.title = extract.Title;
            table.page = (pages).ToString();
            pages += extract.Pages;
            tables.Add(table);
        }

        string glossaryFilePath = System.IO.Path.Combine(this.workPath, "glossary.pdf");
        GetGlossaryExtract(intent, template, reportData.glossaries, glossaryFilePath);
        
        Helper.LogInfo(this.GetType().ToString(), "Génération des pages de restriction", timer.ElapsedMilliseconds);
        timer.Restart();

        // build main section pages
        reportData.section.tocs = tables.ToArray();
        this.TestTocBreakPage(reportData.section);

        string mainSectionFilePath = System.IO.Path.Combine(this.workPath, "reportData.pdf");
        int mainPages = this.GenerateMainSection(reportData, intent, mainSectionFilePath);

        Helper.LogInfo(this.GetType().ToString(), "Génération de la page principale", timer.ElapsedMilliseconds);
        timer.Restart();

        // correct toc page numbers if needed and rebuild main section pages
        if (mainPages != MainNumberOfPage)
        {
            foreach (toc toc in reportData.section.tocs)
            {
                int page = int.Parse(toc.page);
                page += mainPages - MainNumberOfPage;
                toc.page = page.ToString();
            }
            this.GenerateMainSection(reportData, intent, mainSectionFilePath);
        }

        List<string> pdfFiles = new List<string>();
        pdfFiles.Add(mainSectionFilePath);
        foreach (RestrictionExtract extract in extracts.Values)
        {
            pdfFiles.Add(extract.Path);
        }
        pdfFiles.Add(glossaryFilePath);

        // generate final report
        MemoryStream ms = new MemoryStream();
        PdfADocument pdf = new PdfADocument(new PdfWriter(ms), PdfAConformanceLevel.PDF_A_2A,
            new PdfOutputIntent("Custom", "", "https://www.color.org", "sRGB IEC61966-2.1", new MemoryStream(intent)));
        pdf.SetTagged();

        PdfMerger merger = new PdfMerger(pdf);
        merger.SetCloseSourceDocuments(true);
        foreach (string filePath in pdfFiles)
        {
            PdfDocument tmp = new PdfDocument(new PdfReader(filePath));
            merger.Merge(tmp, 1, tmp.GetNumberOfPages());
        }

        AddLogosAndTexts(pdf, template, reportData);

        Helper.LogInfo(this.GetType().ToString(), "Génération du rapport final", timer.ElapsedMilliseconds);
        timer.Stop();

        return ms.GetBuffer();
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

    private int GenerateMainSection(ReportData reportData, byte[] intent, string fileName)
    {
        XmlDocument xmlDataDoc = new XmlDocument();
        xmlDataDoc.LoadXml(this.BuildMainSectionXml(reportData.section));

        PdfReport report = new PdfReport(intent);

        xmlDataDoc.Save(System.IO.Path.Combine(this.workPath, "data.xml"));

        return report.GeneratePdfA(xmlDataDoc, Helper.GetReportConfigFilePath("ReportMainSection.xslt"), fileName);
    }

    private void TestTocBreakPage(mainSection section)
    {
        int marginHeight = PAGE_HEIGHT - TOC_FIXED_ELEMENTS - TOC_BLOCK;
        marginHeight -= (section.tocs.Length * TOC_CONCERNED_RESTRICTION);
        foreach (restriction r in section.restrictions)
        {
            if (r.result == false)
            {
                marginHeight -= TOC_OTHER_RESTRICTION;
            }
        }
        marginHeight -= (section.noDataThemes.Length * TOC_OTHER_RESTRICTION);

        section.marginStyle = string.Format("height:{0}mm", marginHeight);
        if (marginHeight < 10)
        {
            section.breakAfterToc = true;
        }
    }

    private int GetRestrictionExtract(byte[] intent, restriction restr, string fileName)
    {
        XmlRootAttribute xRoot = new XmlRootAttribute("restriction");

        StringBuilder sb = new StringBuilder();
        XmlSerializer serializer = new XmlSerializer(restr.GetType(), xRoot);
        serializer.Serialize(XmlWriter.Create(sb), restr);

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(sb.ToString());

        PdfReport restrReport = new PdfReport(intent);
        return restrReport.GeneratePdfA(xmlDoc, Helper.GetReportConfigFilePath("ReportRestriction.xslt"), fileName);
    }

    private void GetGlossaryExtract(byte[] intent, ReportTemplate template, InformationText[] glossaries, string fileName)
    {
        PdfDocument pdf = new PdfADocument(new PdfWriter(fileName), PdfAConformanceLevel.PDF_A_2A,
            new PdfOutputIntent("Custom", "", "https://www.color.org", "sRGB IEC61966-2.1", new MemoryStream(intent)));
        pdf.SetTagged();
        Document doc = new Document(pdf, PageSize.A4);

        doc.SetMargins(template.MarginTop, template.MarginRight, template.MarginBottom, template.MarginLeft);

        PdfFont boldFont = PdfFontFactory.CreateFont(Helper.GetReportConfigFilePath(template.BoldFontFile), PdfEncodings.CP1252, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);
        PdfFont font = PdfFontFactory.CreateFont(Helper.GetReportConfigFilePath(template.RegularFontFile), PdfEncodings.CP1252, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

        doc.Add(new Paragraph(new Text("Termes et abréviations").SetFont(boldFont).SetFontSize(15)).SetMargins(0, 0, 20, 0));
        foreach (InformationText g in glossaries)
        {
            Paragraph p = new Paragraph(new Text(string.Format("{0}: ", g.Title)).SetFont(boldFont).SetFontSize(8)).SetMargins(4, 0, 4, 0).SetFixedLeading(10);
            p.Add(new Text(g.Contents[0]).SetFont(font).SetFontSize(8));
            doc.Add(p);

            if (g.Contents.Length > 1)
            {
                for (int i = 1; i < g.Contents.Length; i++)
                {
                    Paragraph p2 = new Paragraph(new Text(g.Contents[i]).SetFont(font).SetFontSize(8)).SetMargins(2, 0, 2, 0).SetFixedLeading(10);
                    doc.Add(p2);
                }
            }

            SolidLine line = new SolidLine(0.07f);
            line.SetColor(DeviceRgb.BLACK);
            LineSeparator sep = new LineSeparator(line);
            sep.SetHorizontalAlignment(HorizontalAlignment.CENTER).SetWidth(UnitValue.CreatePercentValue(100));
            doc.Add(sep);
        }

        doc.Close();
        pdf.Close();
    }

    private void AddLogosAndTexts(PdfADocument pdf, ReportTemplate template, ReportData data)
    {
        Document doc = new Document(pdf, template.Format);

        PdfFont font = PdfFontFactory.CreateFont(Helper.GetReportConfigFilePath(template.RegularFontFile), PdfEncodings.CP1252, PdfFontFactory.EmbeddingStrategy.FORCE_EMBEDDED);

        int pages = pdf.GetNumberOfPages();
        for (int page = 1; page < pages + 1; page++)
        {
            Image chLogo = new Image(GetLogoData("ch"));
            PositionLogo(chLogo, page, template.Format, 18, 10, 44, 15);
            doc.Add(chLogo);

            Image cantLogo = new Image(ImageDataFactory.Create(string.Format("{0}/LOGORCGE_rvb300dpi_FRU.jpg", WebHelper.GetConfigValue("LogoUrl"))));
            PositionCenteredLogo(cantLogo, page, template.Format, 74, 10, 30, 13);
            doc.Add(cantLogo);

            //Image munLogo = new Image(ImageDataFactory.Create(data.municipalityLogoURL));
            //PositionCenteredLogo(munLogo, page, template.Format, 115, 9, 30, 13);
            //doc.Add(munLogo);

            Image plrLogo = new Image(GetLogoData("ch.plr"));
            PositionLogo(plrLogo, page, template.Format, 157, 10, 35, 10);
            doc.Add(plrLogo);

            doc.Add(DrawSeparator(page, template.Format, 18, 29, 174, 0.2f));
            doc.Add(DrawSeparator(page, template.Format, 18, 282, 174, 0.8f));

            float dateWidth = AddText(doc, page, font, DateTime.Now.ToString(template.Reference.DateFormat), template.Reference.FontSize,
                template.Reference.Alignment, template.Format, template.Reference.X, template.Reference.Y);
            float timeWidth = AddText(doc, page, font, DateTime.Now.ToString(template.Reference.TimeFormat), template.Reference.FontSize,
                template.Reference.Alignment, template.Format, template.Reference.X + dateWidth + template.Reference.Spacing, template.Reference.Y);
            AddText(doc, page, font, data.section.reference, template.Reference.FontSize, template.Reference.Alignment,
                template.Format, template.Reference.X + dateWidth + timeWidth + 2 * template.Reference.Spacing, template.Reference.Y);
            AddText(doc, page, font, string.Format(template.PageNumber.Format, page, pages), template.PageNumber.FontSize,
                template.PageNumber.Alignment, template.Format, template.PageNumber.X, template.PageNumber.Y);
        }

        doc.Close();
    }

    static float AddText(Document doc, int page, PdfFont font, string content, int fontSize, ReportTemplateText.ReportTemplateAlignment alignment, PageSize size, float x, float y)
    {
        float fx = size.GetWidth() / 210, fy = size.GetHeight() / 297;

        x *= fx;
        y = size.GetHeight() - (y * fy);

        Paragraph p = new Paragraph(content);
        p.SetFont(font).SetFontSize(fontSize);
        float width = GetParagraphWidth(doc, p);

        switch (alignment)
        {
            case ReportTemplateText.ReportTemplateAlignment.RIGHT:
                x -= width;
                break;
            case ReportTemplateText.ReportTemplateAlignment.CENTER:
                x -= width / 2;
                break;
            default:
                break;
        }
        p.SetFixedPosition(page, x, y, width).SetHorizontalAlignment(HorizontalAlignment.LEFT).SetVerticalAlignment(VerticalAlignment.BOTTOM);

        doc.Add(p);

        return width / fx;
    }

    static Image DrawSeparator(int page, PageSize size, float left, float top, float width, float heightInPoints)
    {
        float fx = size.GetWidth() / 210, fy = size.GetHeight() / 297;

        int w = (int)Math.Round(width * fx * (1 / heightInPoints));
        System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(w, 1);
        System.Drawing.Graphics graph = System.Drawing.Graphics.FromImage(bmp);
        graph.DrawLine(new System.Drawing.Pen(System.Drawing.Color.Black, 1), 0, 0, w, 0);

        Image img = new Image(ImageDataFactory.Create(bmp, System.Drawing.Color.Black));
        img.ScaleToFit(width * fx, heightInPoints);
        img.SetFixedPosition(page, left * fx, size.GetHeight() - (top * fy));

        return img;
    }

    static float GetParagraphWidth(Document doc, Paragraph p)
    {
        IRenderer renderer = p.CreateRendererSubTree();
        LayoutResult result = renderer.SetParent(doc.GetRenderer()).Layout(new LayoutContext(new LayoutArea(1, new Rectangle(1000, 100))));
        return ((ParagraphRenderer)renderer).GetMinMaxWidth().GetMaxWidth();
    }

    private ImageData GetLogoData(string code)
    {
        string base64 = oerebHelper.GetLogo(code, "fr");
        return ImageDataFactory.Create(Convert.FromBase64String(base64));
    }

    private void PositionLogo(Image image, int page, PageSize size, float left, float top, float width, float height)
    {
        float fx = size.GetWidth() / DimensionsA4[0], fy = size.GetHeight() / DimensionsA4[1];
        image.ScaleToFit(width * fx, height * fy);
        image.SetFixedPosition(page, left * fx, size.GetHeight() - (top * fy) - image.GetImageScaledHeight());
    }

    private void PositionCenteredLogo(Image image, int page, PageSize size, float left, float top, float width, float height)
    {
        float fx = size.GetWidth() / DimensionsA4[0], fy = size.GetHeight() / DimensionsA4[1];
        image.ScaleToFit(width * fx, height * fy);
        float xPos = ((left + (width / 2.0f)) * fx) - (image.GetImageScaledWidth() / 2.0f);
        image.SetFixedPosition(page, xPos, size.GetHeight() - (top * fy) - image.GetImageScaledHeight());
    }

    private ReportTemplate GetTemplate()
    {
        ReportTemplate template = new ReportTemplate();

        XmlDocument doc = new XmlDocument();
        doc.Load(Helper.GetReportConfigFilePath("ReportTemplate.xml"));

        template.SetMargins(doc.SelectSingleNode("/Template/Margins").InnerText);
        template.Font = doc.SelectSingleNode("/Template/Font").InnerText;
        template.RegularFontFile = doc.SelectSingleNode("/Template/RegularFontFile").InnerText;
        template.BoldFontFile = doc.SelectSingleNode("/Template/BoldFontFile").InnerText;
        template.Reference = GetTemplateText(doc.SelectSingleNode("/Template/Reference"));
        template.Reference.DateFormat = doc.SelectSingleNode("/Template/Reference/DateFormat").InnerText;
        template.Reference.TimeFormat = doc.SelectSingleNode("/Template/Reference/TimeFormat").InnerText;
        template.Reference.SetSpacing(doc.SelectSingleNode("/Template/Reference/Spacing").InnerText);
        template.PageNumber = GetTemplateText(doc.SelectSingleNode("/Template/PageNumber"));
        template.PageNumber.Format = doc.SelectSingleNode("/Template/PageNumber/Format").InnerText;

        return template;
    }

    private ReportTemplateText GetTemplateText(XmlNode node)
    {
        ReportTemplateText text = new ReportTemplateText();

        text.SetFontSize(node.SelectSingleNode("FontSize").InnerText);
        text.SetAlignment(node.SelectSingleNode("Alignement").InnerText);
        text.SetPosition(node.SelectSingleNode("Position").InnerText);

        return text;
    }

    #endregion

    private class RestrictionExtract
    {
        public string Title { get; set; }
        public int Pages { get; set; }
        public string Path { get; set; }
    }
}