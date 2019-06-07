<%@ WebHandler Language="C#" Class="BuildLegends" %>
/* $Rev: 15622 $ */
using System;
using System.Text;
using System.Web;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;
using ESRI.ArcGIS.SOAP;
using Topomat.Web.Common;

public class BuildLegends : IHttpHandler
{
    public void ProcessRequest(HttpContext context)
    {
        IList<int> layerIds = new List<int>();
        string token = TokenManager.GetToken();
        string mapServiceUrl = WebHelper.GetConfigValue("MapServiceUrl");

        LayerInfo layInfo = new LayerInfo(token, mapServiceUrl);
        XmlNode root = XmlHelper.GetConfig("request.xml", "RequestConfig");
        foreach (XmlNode node in root.SelectNodes("addMapLayer"))
        {
            string layerName = node.InnerText;
            layerIds.Add(layInfo.GetLayerInfo(layerName).LayerID);
        }
        foreach (XmlNode node in root.SelectNodes("mainMapLayer"))
        {
            string layerName = node.InnerText;
            layerIds.Add(layInfo.GetLayerInfo(layerName).LayerID);
        }
        foreach (XmlNode node in root.SelectNodes("restrictionMapLayer"))
        {
            string layerName = node.InnerText;
            layerIds.Add(layInfo.GetLayerInfo(layerName).LayerID);
        }
        foreach (XmlNode node in root.SelectNodes("RestrictionOnLandownership"))
        {
            string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
            layerIds.Add(layInfo.GetLayerInfo(layerName).LayerID);

            string additionalLayers = XmlHelper.GetXmlAttribute(node, "additionalLayers", false);
            if (!string.IsNullOrEmpty(additionalLayers))
            {
                foreach (string addLayer in additionalLayers.Split(new char[] { ',' }))
                {
                    int id = layInfo.GetLayerInfo(addLayer).LayerID;
                    if (!layerIds.Contains(id))
                    {
                        layerIds.Add(id);
                    }
                }
            }
        }

        LegendInfo legInfo = new LegendInfo(token, mapServiceUrl, layerIds.ToArray(), true, false);

        // get main legend
        StringBuilder sb = new StringBuilder();
        this.GetHeader(sb);
        foreach (XmlNode node in root.SelectNodes("addMapLayer"))
        {
            string layerName = node.InnerText;
            int id = layInfo.GetLayerInfo(layerName).LayerID;
            this.GetBody(sb, layerName, legInfo.GetLegendInfo(id));
        }
        foreach (XmlNode node in root.SelectNodes("mainMapLayer"))
        {
            string layerName = node.InnerText;
            int id = layInfo.GetLayerInfo(layerName).LayerID;
            this.GetBody(sb, layerName, legInfo.GetLegendInfo(id));
        }
        this.GetFooter(sb);

        string path = System.IO.Path.Combine(WebHelper.GetConfigValue("LegendPath"),
            WebHelper.GetConfigValue("MainLegendName") + ".htm");
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

        // get restriction legends
        foreach (XmlNode node in root.SelectNodes("RestrictionOnLandownership"))
        {
            string layerName = XmlHelper.GetXmlAttribute(node, "layer", true);
            int id = layInfo.GetLayerInfo(layerName).LayerID;

            sb = new StringBuilder();
            this.GetHeader(sb);
            this.GetBody(sb, layerName, legInfo.GetLegendInfo(id));

            string additionalLayers = XmlHelper.GetXmlAttribute(node, "additionalLayers", false);
            if (!string.IsNullOrEmpty(additionalLayers))
            {
                foreach (string addLayer in additionalLayers.Split(new char[] { ',' }))
                {
                    int addId = layInfo.GetLayerInfo(addLayer).LayerID;
                    this.GetBody(sb, addLayer, legInfo.GetLegendInfo(addId));
                }
            }

            foreach (XmlNode rmlNode in root.SelectNodes("restrictionMapLayer"))
            {
                string rmlLayerName = rmlNode.InnerText;
                int rmlId = layInfo.GetLayerInfo(rmlLayerName).LayerID;
                this.GetBody(sb, rmlLayerName, legInfo.GetLegendInfo(rmlId));
            }
            this.GetFooter(sb);

            path = System.IO.Path.Combine(WebHelper.GetConfigValue("LegendPath"), layerName.ToLower() + ".htm");
            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        context.Response.ContentType = "text/plain";
        context.Response.Write("Finished.");
    }

    public bool IsReusable
    {
        get
        {
            return false;
        }
    }

    private void GetHeader(StringBuilder sb)
    {
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<title>Cadastre RDPPF - République et canton de Genève</title>");
        sb.AppendLine("<link href=\"arcgis_rest.css\" rel=\"stylesheet\" type=\"text/css\" />");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div>");
        sb.AppendLine("<img style=\"float: left;\" src=\"../logos/LOGORCGE_rvb300dpi_FRU.jpg\" alt=\"Logo\" width=\"80\" />");
        sb.AppendLine("<h1><div>Cadastre RDPPF</div><div>République et canton de Genève</div></h1>");
        sb.AppendLine("</div>");
        sb.AppendLine("<br />");
        sb.AppendLine(string.Format("<h2>Légende</h2>"));
        sb.AppendLine("<div class=\"rbody\">");
        sb.AppendLine("<table class=\"formTable\">");
    }

    private void GetBody(StringBuilder sb, string layer, MapServerLegendInfo info)
    {
        sb.AppendLine("<tr><td>");
        sb.AppendLine(string.Format("<b>{0}</b>", layer));
        sb.AppendLine("<table>");

        foreach (MapServerLegendGroup group in info.LegendGroups)
        {
            foreach (MapServerLegendClass lc in group.LegendClasses)
            {
                string image = Convert.ToBase64String(lc.SymbolImage.ImageData);

                sb.AppendLine("<tr valign=\"middle\">");
                sb.AppendLine(string.Format("<td><img src=\"data:image/png;base64,{0}\" /></td>", image));
                sb.AppendLine(string.Format("<td>{0}</td>", lc.Label));
                sb.AppendLine("</tr>");
            }
        }

        sb.AppendLine("</table>");
        sb.AppendLine("</tr></td>");
    }

    private void GetFooter(StringBuilder sb)
    {
        sb.AppendLine("</table>");
        sb.AppendLine("<br />");
        sb.AppendLine("</div>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
    }
}