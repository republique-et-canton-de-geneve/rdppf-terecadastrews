/* $Rev: 14634 $ */
using Topomat.Web.Common;
using System.Xml;
using System.IO;
using System;
using System.Text;

public class Helper
{

    #region Public methods

    public static void AppendToLog(WsUserException ex)
    {
        AppendToLog(ex.ToString());
    }

    public static void AppendToLog(string txt)
    {
        string path = WebHelper.GetConfigValue("ErrorLog");
        LogWriter writer = new LogWriter(path, 100000);
        writer.Append(txt);
    }

    public static void AppendToTrace(string message)
    {
        string useTrace = WebHelper.GetConfigValue("UseTrace");
        if (useTrace == "true")
        {
            string path = WebHelper.GetConfigValue("ProcessTraceLog");
            LogWriter writer = new LogWriter(path, 100000);

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(message);

            writer.Append(sb.ToString());
        }
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
        if (string.IsNullOrEmpty(token))
        {
            return string.Empty;
        }
        else
        {
            return string.Format("?token={0}", token);
        }
    }

    public static string ToHexString(string input)
    {
        var sb = new StringBuilder();

        var bytes = Encoding.Unicode.GetBytes(input);
        foreach (var t in bytes)
        {
            sb.Append(t.ToString("X"));
        }

        return sb.ToString();
    }

    #endregion
}