/* $Rev: 30107 $ */
using log4net;
using System;
using System.IO;
using System.Text;
using Topomat.Web.Common;

public class Helper
{
    private static ILog traceLogger = null;

    #region Public methods

    public static void LogError(WsUserException ex)
    {
        Helper.GetLogger().Error(ex.ToString());
    }

    public static void LogDebug(string className, string message)
    {
        string info = string.Format("({0}) {1}", className, message);
        Helper.GetLogger().Debug(info);
    }

    public static void LogInfo(string className, string message, long ms)
    {
        string info = string.Format("({0}) {1} : {2} ms.", className, message, ms);
        Helper.GetLogger().Info(info);
    }

    public static string GetReportConfigFilePath(string fileName)
    {
        string path = Path.Combine(WebHelper.GetConfigValue("ConfigPath"), "report");
        return Path.Combine(path, fileName);
    }

    public static void CleanDirectory(string path, int hours)
    {
        foreach (string dir in Directory.GetDirectories(path))
        {
            if (Directory.GetCreationTime(dir) < DateTime.Now.AddHours(-hours))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch { }
            }
        }
        foreach (string file in Directory.GetFiles(path))
        {
            if (File.GetCreationTime(file) < DateTime.Now.AddHours(-hours))
            {
                try
                {
                    File.Delete(file);
                }
                catch { }
            }
        }
    }

    public static string AddTokenToUrl(string token)
    {
        return Helper.AddTokenToUrl(token, "?");
    }

    public static string AddTokenToUrl(string token, string delimiter)
    {
        if (string.IsNullOrEmpty(token))
        {
            return string.Empty;
        }
        else
        {
            return string.Format("{0}token={1}", delimiter, token);
        }
    }

    public static string ToHexString(string input)
    {
        var sb = new StringBuilder();

        var bytes = Encoding.Unicode.GetBytes(input.Replace(" ", string.Empty));
        foreach (var t in bytes)
        {
            string tStr = t.ToString("X");
            if (tStr.Length == 2)
            {
                sb.Append(tStr);
            }
        }

        return sb.ToString();
    }

    public static string GetSymbolUrl(string code)
    {
        return string.Format("{0}/{1}.png", WebHelper.GetConfigValue("SymbolUrl"), code);
    }

    public static string GetSymbolPath(string code)
    {
        return System.IO.Path.Combine(WebHelper.GetConfigValue("SymbolPath"), string.Format("{0}.png", code));
    }

    public static string GetNormalizedString(string input, int length)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        else
        {
            return input.Length > length ? input.Substring(0, length) : input;
        }
    }

    public static string ReduceString(string input, int length)
    {
        if (input.Length <= length)
        {
            return input;
        }
        else
        {
            double factor = Math.Ceiling(((double)(input.Length - 4) / (double)length));
            StringBuilder sb = new StringBuilder(input.Substring(0, 4));
            for (int i = 4; i < (input.Length - 4); i += (int)factor)
            {
                sb.Append(input[i]);
            }
            return sb.ToString();
        }
    }

    public static double dotsToMM(int dots, int dpi)
    {
        return (dots / (double)dpi) * 25.4;
    }

    public static int mmToDots(double millimeters, int dpi)
    {
        return (int)Math.Round((millimeters / 25.4) * dpi);
    }

    #endregion

    private static ILog GetLogger()
    {
        if (traceLogger == null)
        {
            log4net.Config.XmlConfigurator.Configure();
            Helper.traceLogger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }
        return Helper.traceLogger;
    }
}