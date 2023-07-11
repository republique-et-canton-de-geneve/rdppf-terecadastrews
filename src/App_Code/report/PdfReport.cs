/* $Rev: 22729 $ */
using iText.Html2pdf;
using iText.Html2pdf.Resolver.Font;
using iText.Kernel.Pdf;
using iText.Pdfa;
using System.IO;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

public class PdfReport
{
    private byte[] Intent;

    public PdfReport(byte[] intent)
    {
        Intent = intent;
    }

    public int GeneratePdfA(XmlDocument xmlDoc, string xsltPath, string outPath)
    {
        using (PdfWriter writer = new PdfWriter(outPath))
        {
            PdfADocument pdf = new PdfADocument(writer, PdfAConformanceLevel.PDF_A_2A,
                new PdfOutputIntent("Custom", "", "https://www.color.org", "sRGB IEC61966-2.1", new MemoryStream(Intent)));
            pdf.SetTagged();

            ConverterProperties properties = new ConverterProperties();
            properties.SetFontProvider(new DefaultFontProvider(true, true, true));

            MemoryStream htmlStream = GetHtmlStream(xmlDoc, xsltPath);

            htmlStream.Seek(0, SeekOrigin.Begin);
            HtmlConverter.ConvertToPdf(htmlStream, pdf, properties);

            pdf.Close();
        }

        using (PdfReader reader = new PdfReader(outPath))
        {
            return (new PdfDocument(reader)).GetNumberOfPages();
        }            
    }

    private MemoryStream GetHtmlStream(XmlDocument xmlDoc, string xsltPath)
    {
        MemoryStream stream = new MemoryStream();

        using (XmlNodeReader xmlReader = new XmlNodeReader(xmlDoc))
        using (XmlTextReader xsltReader = new XmlTextReader(xsltPath))
        {
            // Load XML
            XPathNavigator nav = new XPathDocument(xmlReader).CreateNavigator();

            // Load XSLT & transform
            XslCompiledTransform transformer = new XslCompiledTransform();
            transformer.Load(xsltReader);
            transformer.Transform(nav, null, stream);
        }

        return stream;
    }
    
}