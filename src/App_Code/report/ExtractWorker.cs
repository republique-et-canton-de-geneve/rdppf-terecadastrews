/* $Rev: 19636 $ */
using System;
using Topomat.Web.Common;
using System.Xml;
using Topomat.Pdf.Report;
using System.IO;
using System.Xml.Serialization;
using System.Text;

public class ExtractWorker
{
    private ReportTemplate reportTemplate;
    private string workingPath;

    public ExtractWorker(string path, ReportTemplate template)
    {
        this.reportTemplate = template;
        this.workingPath = path;
    }

    public void GetRestrictionExtract(restriction restr, string fileName)
    {
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(this.BuildRestrictionXml(restr));

        PdfReport restrReport = new PdfReport(this.reportTemplate.Format, this.reportTemplate.MarginLeft,
            this.reportTemplate.MarginRight, this.reportTemplate.MarginTop, this.reportTemplate.MarginBottom);

        // generate pdf
        using (XmlNodeReader xmlReader = new XmlNodeReader(xmlDoc))
        using (XmlTextReader xsltReader = new XmlTextReader(Helper.GetReportConfigFilePath("ReportRestriction.xslt")))
        {
            restrReport.GeneratePdf(xmlReader, xsltReader, this.workingPath, fileName);
        }
    }

    public void GetGlossaryExtract(glossary[] glossaries, string fileName)
    {
        // build glossary page
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(this.BuildGlossaryXml(glossaries));

        xmlDoc.Save(Path.Combine(this.workingPath, "glossary.xml"));

        PdfReport glossaryReport = new PdfReport(this.reportTemplate.Format, this.reportTemplate.MarginLeft,
            this.reportTemplate.MarginRight, this.reportTemplate.MarginTop, this.reportTemplate.MarginBottom);

        // generate pdf
        using (XmlNodeReader xmlReader = new XmlNodeReader(xmlDoc))
        using (XmlTextReader xsltReader = new XmlTextReader(Helper.GetReportConfigFilePath("ReportGlossary.xslt")))
        {
            glossaryReport.GeneratePdf(xmlReader, xsltReader, this.workingPath, fileName);
        }
    }

    private string BuildRestrictionXml(restriction restr)
    {
        XmlRootAttribute xRoot = new XmlRootAttribute("restriction");

        StringBuilder sb = new StringBuilder();
        XmlSerializer serializer = new XmlSerializer(restr.GetType(), xRoot);
        serializer.Serialize(XmlWriter.Create(sb), restr);

        return sb.ToString();
    }

    private string BuildGlossaryXml(glossary[] glossaries)
    {
        XmlRootAttribute xRoot = new XmlRootAttribute("glossaryRoot");

        StringBuilder sb = new StringBuilder();
        XmlSerializer serializer = new XmlSerializer(glossaries.GetType(), xRoot);
        serializer.Serialize(XmlWriter.Create(sb), glossaries);

        return sb.ToString();
    }
}

public class ExtractWorkerThread : CommonThread
{
    public enum workerTypes { Restriction, Glossary };
    private ExtractWorker worker;
    private int workerType;
    private restriction restriction;
    private glossary[] glossaries;
    private string fileName;

    public ExtractWorkerThread(ExtractWorker worker)
    {
        this.worker = worker;
    }

    public void InitRestriction(restriction r, string name)
    {
        this.workerType = (int)workerTypes.Restriction;
        this.restriction = r;
        this.fileName = name;
    }

    public void InitGlossary(glossary[] glossaries, string name)
    {
        this.workerType = (int)workerTypes.Glossary;
        this.glossaries = glossaries;
        this.fileName = name;
    }

    public void Start()
    {
        try
        {
            switch (this.workerType)
            {
                case (int)workerTypes.Restriction:
                    this.worker.GetRestrictionExtract(this.restriction, this.fileName);
                    this.Success = true;
                    break;
                case (int)workerTypes.Glossary:
                    this.worker.GetGlossaryExtract(this.glossaries, this.fileName);
                    this.Success = true;
                    break;
                default:
                    this.Success = false;
                    break;
            }
            
        }
        catch (WsUserException ex)
        {
            this.ErrorMessage = ex.Message;
            this.Success = false;
        }
        catch (Exception ex)
        {
            this.ErrorMessage = ex.Message;
            this.Success = false;
        }
    }

    public string GetFileName()
    {
        return this.fileName;
    }

    public restriction GetRestriction()
    {
        return this.restriction;
    }

    public int GetWorkerType()
    {
        return this.workerType;
    }
}


