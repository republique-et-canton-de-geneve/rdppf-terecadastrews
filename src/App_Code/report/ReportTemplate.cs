/* $Rev: 30309 $ */
using iText.IO.Font.Constants;
using iText.Kernel.Geom;

public class ReportTemplate
{
    public PageSize Format = PageSize.A4;
    public float MarginLeft;
    public float MarginRight;
    public float MarginTop;
    public float MarginBottom;
    public string Font;
    public string RegularFontFile;
    public string BoldFontFile;
    public ReportTemplateText Reference;
    public ReportTemplateText PageNumber;

    public float[] Dimensions = { 210.0f, 297.0f };

    public ReportTemplate()
    {
        // set default values
        MarginLeft = 25 * Format.GetWidth() / Dimensions[0];
        MarginRight = 25 * Format.GetWidth() / Dimensions[0];
        MarginTop = 25 * Format.GetHeight() / Dimensions[1];
        MarginBottom = 25 * Format.GetHeight() / Dimensions[1];

        Font = StandardFonts.HELVETICA;
    }

    public void SetMargins(string margin)
    {
        string[] margins = margin.Split(new char[] { ',' });
        if (margins.Length == 4)
        {
            float m = 25f;
            float.TryParse(margins[0], out m);
            MarginTop = m * Format.GetHeight() / Dimensions[1];
            float.TryParse(margins[1], out m);
            MarginRight = m * Format.GetWidth() / Dimensions[0];
            float.TryParse(margins[2], out m);
            MarginBottom = m * Format.GetHeight() / Dimensions[1];
            float.TryParse(margins[3], out m);
            MarginLeft = m * Format.GetWidth() / Dimensions[0];
        }
    }
}

public class ReportTemplateText
{
    public enum ReportTemplateAlignment { LEFT, CENTER, RIGHT };

    public int FontSize = 11;
    public ReportTemplateAlignment Alignment;
    public float X = 25;
    public float Y = 25;

    public string DateFormat;
    public string TimeFormat;
    public float Spacing = 1;

    public string Format;

    public ReportTemplateText()
    {
    }

    public void SetFontSize(string value)
    {
        int.TryParse(value, out FontSize);
    }

    public void SetAlignment(string value)
    {
        switch (value)
        {
            case "center":
                Alignment = ReportTemplateAlignment.CENTER;
                break;
            case "right":
                Alignment = ReportTemplateAlignment.RIGHT;
                break;
            default:
                Alignment = ReportTemplateAlignment.LEFT;
                break;
        }
    }

    public void SetPosition(string value)
    {
        string[] coords = value.Split(new char[] { ',' });
        if (coords.Length == 2)
        {
            float.TryParse(coords[0], out X);
            float.TryParse(coords[1], out Y);
        }
    }

    public void SetSpacing(string value)
    {
        float.TryParse(value, out Spacing);
    }
}