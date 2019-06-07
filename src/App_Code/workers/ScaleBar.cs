/* $Rev: 19264 $ */
using System;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;

public class ScaleBar
{
    public static int LINE_WIDTH = 2;
    public static int HALO_WIDTH = 2;
    public static int MARGIN_LEFT = 10;
    public static int MARGIN_RIGHT = 60;
    public static int MARGIN_BOTTOM = 40;

    private int barWidth;
    private int barHeight;
    private int rightBarValue;
    private MapPrintParams printParams;

    public ScaleBar(MapPrintParams parameters)
    {
        this.printParams = parameters;
        Rectangle rect = parameters.GetScaleBarDrawRectangle();
        this.InitBarParams(rect.Width, rect.Height);
    }

    public Bitmap DrawBar()
    {
        int centerX = (int)Math.Floor(this.barWidth / 2.0);
        int centerY = (int)Math.Floor(this.barHeight / 2.0);

        Bitmap bmp = new Bitmap(this.barWidth + ScaleBar.MARGIN_LEFT + ScaleBar.MARGIN_RIGHT,
            this.barHeight + ScaleBar.MARGIN_BOTTOM);

        Graphics graph = Graphics.FromImage(bmp);
        graph.InterpolationMode = InterpolationMode.High;
        graph.CompositingQuality = CompositingQuality.HighQuality;
        graph.SmoothingMode = SmoothingMode.AntiAlias;

        // fill rectangle with halo
        graph.FillRectangle(new SolidBrush(Color.White),
            ScaleBar.MARGIN_LEFT - ScaleBar.HALO_WIDTH,
            0,
            barWidth + (2 * ScaleBar.HALO_WIDTH),
            barHeight + (2 * ScaleBar.HALO_WIDTH));
        // draw outer rectangle
        graph.DrawRectangle(new Pen(Color.Black, ScaleBar.LINE_WIDTH),
            ScaleBar.MARGIN_LEFT + (ScaleBar.LINE_WIDTH / 2),
            ScaleBar.HALO_WIDTH + (ScaleBar.LINE_WIDTH / 2),
            barWidth - ScaleBar.LINE_WIDTH,
            barHeight - ScaleBar.LINE_WIDTH);

        // fill black rectangles
        graph.FillRectangle(new SolidBrush(Color.Black),
            ScaleBar.MARGIN_LEFT,
            ScaleBar.HALO_WIDTH,
            centerX,
            centerY);
        graph.FillRectangle(new SolidBrush(Color.Black),
            ScaleBar.MARGIN_LEFT + centerX,
            ScaleBar.HALO_WIDTH + centerY,
            centerX,
            centerY);
        // add lines
        graph.DrawLine(new Pen(Color.Black, ScaleBar.LINE_WIDTH),
            ScaleBar.MARGIN_LEFT,
            ScaleBar.HALO_WIDTH + centerY,
            ScaleBar.MARGIN_LEFT + barWidth,
            ScaleBar.HALO_WIDTH + centerY);
        graph.DrawLine(new Pen(Color.Black, ScaleBar.LINE_WIDTH),
            ScaleBar.MARGIN_LEFT + centerX,
            ScaleBar.HALO_WIDTH,
            ScaleBar.MARGIN_LEFT + centerX,
            ScaleBar.HALO_WIDTH + barHeight);

        // draw values
        int middleBarValue = rightBarValue / 2;
        this.DrawTextWithHalo(graph, "0", new Point(ScaleBar.MARGIN_LEFT, barHeight + 5));
        this.DrawTextWithHalo(graph, middleBarValue.ToString(), new Point(ScaleBar.MARGIN_LEFT + centerX, barHeight + 5));
        this.DrawTextWithHalo(graph, rightBarValue.ToString(), new Point(ScaleBar.MARGIN_LEFT + barWidth, barHeight + 5));
        this.DrawTextWithHalo(graph, "m", new Point(ScaleBar.MARGIN_LEFT + barWidth + ScaleBar.MARGIN_RIGHT - 16, barHeight + 5));

        return bmp;
    }

    public int GetBarWidth()
    {
        return this.barWidth;
    }

    private void InitBarParams(int width, int height)
    {
        double value = (Helper.dotsToMM(width, this.printParams.GetDpi()) / 1000.0) * this.printParams.GetScale();
        this.rightBarValue = this.GetRightBarValue(value);
        this.barWidth = (int)Math.Round((Helper.mmToDots(this.rightBarValue, this.printParams.GetDpi()) * 1000.0) / this.printParams.GetScale());
        this.barHeight = height;
    }

    private void DrawTextWithHalo(Graphics graph, string text, Point point)
    {
        StringFormat sf = new StringFormat(StringFormatFlags.NoClip)
        {
            Alignment = StringAlignment.Center
        };

        Font font = new Font("Arial", 32.0f, FontStyle.Bold);
        Pen pen = new Pen(Color.White, ScaleBar.HALO_WIDTH + 1);
        pen.LineJoin = LineJoin.Round;

        GraphicsPath path = new GraphicsPath();
        path.AddString(text, font.FontFamily, (int)font.Style, font.SizeInPoints, point, sf);

        graph.DrawPath(pen, path);
        graph.FillPath(new SolidBrush(Color.Black), path);
    }

    private int GetRightBarValue(double input)
    {
        // si <= 10, on arrondit à chiffre pair précédent
        if (input <= 10)
        {
            return this.GetNearestValue(input, 2);
        }
        // si <= 50, on arrondit à 4aine précédente
        else if (input <= 50)
        {
            return this.GetNearestValue(input, 4);
        }
        // si <= 100, on arrondit à dizaine précédente
        else if (input <= 100)
        {
            return this.GetNearestValue(input, 10);
        }
        // si <= 500, on arrondit à 50aine précédente
        else if (input <= 500)
        {
            return this.GetNearestValue(input, 50);
        }
        // si <= 1000, on arrondit à 100aine précédente
        else if (input <= 1000)
        {
            return this.GetNearestValue(input, 100);
        }
        // si <= 5000, on arrondit à 500aine précédente
        else if (input <= 5000)
        {
            return this.GetNearestValue(input, 500);
        }
        // si <= 10000, on arrondit à millier précédent
        else
        {
            return this.GetNearestValue(input, 1000);
        }
    }

    private int GetNearestValue(double value, int factor)
    {
        int floor = (int)Math.Floor(value);
        return (floor - (floor % factor));
    }
}