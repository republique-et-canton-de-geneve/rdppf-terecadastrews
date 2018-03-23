/* $Rev: 14634 $ */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/* Nomenclature
 * SG: Surface géographique de la parcelle
 * ST: Surface technique de la parcelle
 * SGR: Surfaces géographiques des restrictions interceptant la parcelle
 * STR: Surfaces techniques des restrictions interceptant la parcelle
 * PSTR: Pourcentages des surfaces techniques des restrictions interceptant la parcelle
 * n: Nombre des restrictions interceptant la parcelle 
 */

public class ComputeSurface
{
    public static int RESULT_SURFACE = -100;
    public static int RESULT_STOP_PROCEDURE = -1;
    public static int RESULT_CONTINUE = 0;
    public static int RESULT_FINISHED = 1;

    private int[] surfaces;
    private float[] percents;

    public ComputeSurface()
    {
    }

    public int Process(bool complete, double SG, double ST, double[] SGR)
    {
        if (complete == true)
        {
            return this.ProcessComplete(SG, ST, SGR);
        }
        else
        {
            return this.ProcessIncomplete(SG, ST, SGR);
        }
    }

    public int[] GetSurfaces()
    {
        return this.surfaces;
    }

    public float[] GetPercents()
    {
        return this.percents;
    }

    private int ProcessComplete(double SG, double ST, double[] SGR)
    {
        int result = this.TestSG(SG, ST, SGR.Length);
        if (result == ComputeSurface.RESULT_CONTINUE)
        {
            double total = this.Sum(SGR);
            double[] STR = new double[SGR.Length];
            if (total != SG)
            {
                result = this.TestSumDelta(total, SG, SGR.Length, true);
                if (result == ComputeSurface.RESULT_CONTINUE)
                {
                    STR = this.ComputeSTR(total, ST, SGR);
                }
            }
            else
            {
                STR = this.ComputeSTR(SG, ST, SGR);
            }
            if (result != ComputeSurface.RESULT_STOP_PROCEDURE)
            {
                this.surfaces = this.AdjustCompleteSTR(ST, STR);
            }
        }
        if (result != ComputeSurface.RESULT_STOP_PROCEDURE)
        {
            double[] PSTR = this.ComputePSTR(ST);
            this.percents = this.AdjustCompletePSTR(PSTR);

            result = ComputeSurface.RESULT_FINISHED;
        }
        return result;
    }

    private int ProcessIncomplete(double SG, double ST, double[] SGR)
    {
        int result = this.TestSG(SG, ST, SGR.Length);
        if (result == ComputeSurface.RESULT_CONTINUE)
        {
            double[] STR = this.ComputeSTR(SG, ST, SGR);
            this.surfaces = this.AdjustIncompleteSTR(ST, STR);
        }
        if (result != ComputeSurface.RESULT_STOP_PROCEDURE)
        {
            double[] PSTR = this.ComputePSTR(ST);
            this.percents = this.AdjustIncompletePSTR(PSTR);

            result = ComputeSurface.RESULT_FINISHED;
        }
        return result;
    }

    private int TestSG(double SG, double ST, int n)
    {
        if (SG < 5) 
        {
            if (n == 1) {
                this.surfaces = new int[] { (int)Math.Round(ST) };
                return ComputeSurface.RESULT_SURFACE;
            } else {
                return ComputeSurface.RESULT_STOP_PROCEDURE;
            }
        }
        else
        {
            return ComputeSurface.RESULT_CONTINUE;
        }
    }

    private int TestSumDelta(double total, double surface, int n, bool complete)
    {
        double delta = Math.Abs(total - surface);
        bool check = (delta < n) & (delta < 5.0);

        if (complete == true)
        {
            if ((delta / surface) < 0.05 && check == true)
            {
                return ComputeSurface.RESULT_CONTINUE;
            }
        }
        else if (check == true)
        {
            return ComputeSurface.RESULT_CONTINUE;
        }

        return ComputeSurface.RESULT_STOP_PROCEDURE;
    }

    private double[] ComputeSTR(double SG, double ST, double[] SGR)
    {
        IList<double> STR = new List<double>();

        foreach (double sgri in SGR)
        {
            double stri = (sgri / SG) * Math.Round(ST);
            if (stri < 0.5)
            {
                STR.Add(-1);
            }
            else
            {
                STR.Add(stri);
            }
        }

        return STR.ToArray();
    }

    private int[] AdjustCompleteSTR(double ST, double[] STR)
    {
        IList<int> roundSTR = new List<int>();

        foreach (double stri in STR)
        {
            roundSTR.Add((int)Math.Round(stri));
        }

        int total = this.Sum(roundSTR.ToArray()), countLimit = 0;
        while (total != Math.Round(ST) && countLimit < 100)
        {
            double Rmini = double.MaxValue;
            int index = -1;
            if (total < Math.Round(ST))
            {
                for (int i = 0; i < STR.Length; i++)
                {
                    if (STR[i] != -1)
                    {
                        double strifi = roundSTR[i] + 0.5;
                        double R = Math.Abs(strifi - STR[i]) / strifi;
                        if (R < Rmini)
                        {
                            index = i;
                            Rmini = R;
                        }
                    }
                }
                roundSTR[index] = roundSTR[index] + 1;
            }
            else
            {
                for (int i = 0; i < STR.Length; i++)
                {
                    if (STR[i] != -1)
                    {
                        double strifi = roundSTR[i] - 0.5;
                        double R = Math.Abs(strifi - STR[i]) / strifi;
                        if (R < Rmini)
                        {
                            index = i;
                            Rmini = R;
                        }
                    }
                }
                roundSTR[index] = roundSTR[index] - 1;
            }
            total = this.Sum(roundSTR.ToArray());
            countLimit++;
        }

        return roundSTR.ToArray();
    }

    private int[] AdjustIncompleteSTR(double ST, double[] STR)
    {
        IList<int> roundSTR = new List<int>();

        foreach (double stri in STR)
        {
            roundSTR.Add((int)Math.Round(stri));
        }

        int total = this.Sum(roundSTR.ToArray()), countLimit = 0;
        while (total > Math.Round(ST) && countLimit < 100)
        {
            double Rmini = double.MaxValue;
            int index = -1;
            for (int i = 0; i < STR.Length; i++)
            {
                if (STR[i] != -1)
                {
                    double strifi = roundSTR[i] - 0.5;
                    double R = Math.Abs(strifi - STR[i]) / strifi;
                    if (R < Rmini)
                    {
                        index = i;
                        Rmini = R;
                    }
                }
            }
            roundSTR[index] = roundSTR[index] - 1;

            total = this.Sum(roundSTR.ToArray());
            countLimit++;
        }

        return roundSTR.ToArray();
    }

    private double[] ComputePSTR(double ST)
    {
        IList<double> PSTR = new List<double>();

        foreach (int stri in this.surfaces)
        {
            if (stri == -1)
            {
                PSTR.Add(-1);
            }
            else
            {
                double p = (stri * 100 / ST) < 0.01 ? 0.01 : (stri * 100 / ST);
                PSTR.Add(p);
             }
        }

        return PSTR.ToArray();
    }

    private float[] AdjustCompletePSTR(double[] PSTR)
    {
        IList<float> roundPSTR = new List<float>();

        foreach (double pstri in PSTR)
        {
            roundPSTR.Add(this.PRound(pstri));
        }

        float total = this.Sum(roundPSTR.ToArray()), countLimit = 0;
        while (total != 100 && countLimit < 100)
        {
            double Rmini = double.MaxValue;
            int index = -1;
            if (total < 100)
            {
                for (int i = 0; i < PSTR.Length; i++)
                {
                    if (PSTR[i] != -1)
                    {
                        double pstrifi = roundPSTR[i] + 0.005;
                        double R = Math.Abs(pstrifi - roundPSTR[i]) / pstrifi;
                        if (R < Rmini)
                        {
                            index = i;
                            Rmini = R;
                        }
                    }
                }
                roundPSTR[index] = this.PRound(roundPSTR[index] + 0.01);
            }
            else
            {
                for (int i = 0; i < PSTR.Length; i++)
                {
                    if (PSTR[i] != -1)
                    {
                        double pstrifi = roundPSTR[i] - 0.005;
                        double R = Math.Abs(pstrifi - roundPSTR[i]) / pstrifi;
                        if (R < Rmini)
                        {
                            index = i;
                            Rmini = R;
                        }
                    }
                }
                roundPSTR[index] = this.PRound(roundPSTR[index] - 0.01);
            }
            total = this.Sum(roundPSTR.ToArray());
            countLimit++;
        }

        return roundPSTR.ToArray();
    }

    private float[] AdjustIncompletePSTR(double[] PSTR)
    {
        IList<float> roundPSTR = new List<float>();

        foreach (double pstri in PSTR)
        {
            roundPSTR.Add(this.PRound(pstri));
        }

        float total = this.Sum(roundPSTR.ToArray()), countLimit = 0;
        while (total > 100 && countLimit < 100)
        {
            double Rmini = double.MaxValue;
            int index = -1;
            for (int i = 0; i < PSTR.Length; i++)
            {
                if (PSTR[i] != -1)
                {
                    double pstrifi = roundPSTR[i] - 0.005;
                    double R = Math.Abs(pstrifi - roundPSTR[i]) / pstrifi;
                    if (R < Rmini)
                    {
                        index = i;
                        Rmini = R;
                    }
                }
            }
            roundPSTR[index] = this.PRound(roundPSTR[index] - 0.01);

            total = this.Sum(roundPSTR.ToArray());
            countLimit++;
        }

        return roundPSTR.ToArray();
    }

    private double Sum(double[] values)
    {
        double total = 0.0;
        foreach (double value in values)
        {
            if (value != -1)
            {
                total += value;
            }
        }
        // round to 1/1'000'000
        return (Math.Round(total * 1000000) / 1000000);
    }

    private int Sum(int[] values)
    {
        int total = 0;
        foreach (int value in values)
        {
            if (value != -1)
            {
                total += value;
            }
        }
        return total;
    }

    private float Sum(float[] values)
    {
        float total = 0.0f;
        foreach (float value in values)
        {
            if (value != -1)
            {
                total += value;
            }
        }
        // round to 1/100
        return (float)(Math.Round(total * 100) / 100);
    }

    private float PRound(double value)
    {
        return (float)(Math.Round(value * 100) / 100);
    }
}