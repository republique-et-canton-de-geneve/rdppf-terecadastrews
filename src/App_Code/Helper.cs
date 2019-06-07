/* $Rev: 20981 $ */
using System.IO;
using System;
using System.Text;
using Topomat.Web.Common;

public class Helper
{
    private static log4net.ILog traceLogger = null;

    #region Public methods

    public static void LogError(WsUserException ex)
    {
        Helper.GetLogger().Error(ex.ToString());
    }

    public static void LogInfo(string className, string message)
    {
        string info = string.Format("({0}) {1}", className, message);
        Helper.GetLogger().Info(info);
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

    public static double dotsToMM(int dots, int dpi)
    {
        return (dots / (double)dpi) * 25.4;
    }

    public static int mmToDots(double millimeters, int dpi)
    {
        return (int)Math.Round((millimeters / 25.4) * dpi);
    }

    #endregion

    private static log4net.ILog GetLogger()
    {
        if (traceLogger == null)
        {
            log4net.Config.XmlConfigurator.Configure();
            Helper.traceLogger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        }
        return Helper.traceLogger;
    }
}