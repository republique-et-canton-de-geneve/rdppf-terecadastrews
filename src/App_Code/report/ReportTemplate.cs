/* $Rev: 19053 $ */
using System.Collections.Generic;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.IO;
using System;

public class ReportTemplate
{
    public Rectangle Format = PageSize.A4;
    public float MarginLeft;
    public float MarginRight;
    public float MarginTop;
    public float MarginBottom;
    public ReportTemplateFont Font;
    public IList<ReportTemplateZone> TextZones;
    public IList<ReportTemplateZone> TextDateZones;
    public IList<ReportTemplateZone> TextPageZones;
    public IList<ReportTemplateZone> TextAnnexZones;

    public float[] Dimensions;

    private static float[] DimensionsA3 = { 297.0f, 420.0f };
    private static float[] DimensionsA4 = { 210.0f, 297.0f };
    private static float[] DimensionsA5 = { 148.0f, 210.0f };

    public bool UseModel = false;

	public ReportTemplate()
	{
        // set default values
        MarginLeft = 25 * Format.Width / DimensionsA4[0];
        MarginRight = 25 * Format.Width / DimensionsA4[0];
        MarginTop = 25 * Format.Height / DimensionsA4[1];
        MarginBottom = 25 * Format.Height / DimensionsA4[1];

        Dimensions = DimensionsA4;

        Font = new ReportTemplateFont();
        TextZones = new List<ReportTemplateZone>();
        TextDateZones = new List<ReportTemplateZone>();
        TextPageZones = new List<ReportTemplateZone>();
        TextAnnexZones = new List<ReportTemplateZone>();
	}

    public void SetFormat(string format)
    {
        switch (format)
        {
            case "A3":
                Dimensions = DimensionsA3;
                Format = PageSize.A3;
                break;
            case "A4":
                Dimensions = DimensionsA4;
                Format = PageSize.A4;
                break;
            case "A5":
                Dimensions = DimensionsA5;
                Format = PageSize.A5;
                break;
        }
    }

    public void SetMargins(string margin)
    {
        string[] margins = margin.Split(new char[] { ',' });
        if (margins.Length == 4)
        {
            float m = 25f;
            float.TryParse(margins[0], out m);
            MarginLeft = m * Format.Width / Dimensions[0];
            float.TryParse(margins[1], out m);
            MarginRight = m * Format.Width / Dimensions[0];
            float.TryParse(margins[2], out m);
            MarginTop = m * Format.Height / Dimensions[1];
            float.TryParse(margins[3], out m);
            MarginBottom = m * Format.Height / Dimensions[1];
        }        
    }

    public void SetUseModel(string use)
    {
        bool.TryParse(use, out UseModel);
    }
}

public class ReportTemplateFont
{
    public string FontDirectory;
    public string FontName;

    public ReportTemplateFont()
    {
        FontDirectory = Path.Combine(Directory.GetParent(
            Environment.GetFolderPath(Environment.SpecialFolder.System)).FullName, "Fonts");
        FontName = BaseFont.HELVETICA;
    }

    public void SetFontDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            FontDirectory = path;
        }
    }

    public void SetFontName(string name)
    {
        FontFactory.RegisterDirectory(FontDirectory);
        if (FontFactory.Contains(name))
        {
            FontName = name;
        }
    }
}

public class ReportTemplateZone
{
    public string Text;
    public int FontSize = 11;
    public int Alignement = PdfContentByte.ALIGN_LEFT;
    public float PositionX;
    public float PositionY;
    public float Rotation = 0.0f;
    public int[] FontColor = { 0, 0, 0};

    private Rectangle Format = PageSize.A4;
    private float[] Dimensions;

    public ReportTemplateZone(Rectangle format, float[] dimensions)
    {
        Format = format;
        Dimensions = dimensions;
    }

    public void SetFontSize(string size)
    {
        int.TryParse(size, out FontSize);
    }

    public void SetAlignement(string align)
    {
        switch (align)
        {
            case "left":
                Alignement = PdfContentByte.ALIGN_LEFT;
                break;
            case "center":
                Alignement = PdfContentByte.ALIGN_CENTER;
                break;
            case "right":
                Alignement = PdfContentByte.ALIGN_RIGHT;
                break;
        }
    }

    public void SetPositionX(string from, string offset)
    {
        float value = 25f;        

        switch (from)
        {
            case "left":
                float.TryParse(offset, out value);
                PositionX = Format.GetLeft(value * Format.Width / Dimensions[0]);
                break;
            case "right":
                float.TryParse(offset, out value);
                PositionX = Format.GetRight(value * Format.Width / Dimensions[0]);
                break;
        }
    }

    public void SetPositionY(string from, string offset)
    {
        float value = 25f;

        switch (from)
        {
            case "top":
                float.TryParse(offset, out value);
                PositionY = Format.GetTop(value * Format.Height / Dimensions[1]);
                break;
            case "bottom":
                float.TryParse(offset, out value);
                PositionY = Format.GetBottom(value * Format.Height / Dimensions[1]);
                break;
        }
    }

    public void SetRotation(string rotation)
    {
        float.TryParse(rotation, out Rotation);
    }

    public void SetFontColor(string color)
    {
        string[] colors = color.Split(new char[] { ',' });
        if (colors.Length == 3)
        {
            int.TryParse(colors[0], out FontColor[0]);
            int.TryParse(colors[1], out FontColor[1]);
            int.TryParse(colors[2], out FontColor[2]);
        }
    }
}