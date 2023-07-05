/* $Rev: 30309 $ */
using ExtractDataModel_v20;
using System;
using System.IO;
using System.Net;
using System.ServiceModel.Web;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml;
using System.Xml.Serialization;
using Topomat.Web.Common;

public class RdppfSVC : IRdppfSVC
{
    public XmlElement GetEGRIDAsXml()
    {
        try
        {
            GetEGRIDParamReq param = new GetEGRIDParamReq();
            if (param.Parse(WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters))
            {
                QueryResult qr = null;
                GetEGRIDReq req = new GetEGRIDReq(param.returnGeometry);
                switch (param.method)
                {
                    case GetEGRIDParamReq.GetEGRIDMethod.Coordinates:
                        qr = req.GetEGRIDByCoordinates(param.coordX, param.coordY, param.isGNSS);
                        break;
                    case GetEGRIDParamReq.GetEGRIDMethod.Idents:
                        qr = req.GetEGRIDByID(param.identDN, param.number);
                        break;
                    case GetEGRIDParamReq.GetEGRIDMethod.Localisation:
                        qr = req.GetEGRIDByLocalisation(param.postalCode, param.localisation, param.number);
                        break;
                }

                if (qr.features != null && qr.features.Length > 0)
                {
                    XmlDocument doc = new XmlDocument();

                    XmlElement rootElement = doc.CreateElement("GetEGRIDResponse", SchemaHelper.Namespaces["extract"].Value);
                    rootElement.SetAttribute(string.Format("xmlns:{0}", SchemaHelper.Namespaces["data"].Key), SchemaHelper.Namespaces["data"].Value);
                    if (param.returnGeometry)
                    {
                        rootElement.SetAttribute(string.Format("xmlns:{0}", SchemaHelper.Namespaces["geometry"].Key), SchemaHelper.Namespaces["geometry"].Value);
                    }
                    rootElement.SetAttribute(string.Format("xmlns:{0}", SchemaHelper.Namespaces["xsd"].Key), SchemaHelper.Namespaces["xsd"].Value);
                    rootElement.SetAttribute(string.Format("xmlns:{0}", SchemaHelper.Namespaces["xsi"].Key), SchemaHelper.Namespaces["xsi"].Value);
                    rootElement.SetAttribute("schemaLocation", SchemaHelper.Namespaces["xsi"].Value, SchemaHelper.GetSchemaLocation(new string[] { "extract", "data" }));

                    doc.AppendChild(rootElement);

                    foreach (GetEGRIDResponseType data in req.GetEGRIDResponse(qr))
                    {
                        rootElement.AppendChild(XmlHelper.GetXmlElement(doc, string.Empty, "egrid", SchemaHelper.Namespaces["extract"].Value, data.egrid));
                        rootElement.AppendChild(XmlHelper.GetXmlElement(doc, string.Empty, "number", SchemaHelper.Namespaces["extract"].Value, data.number));
                        rootElement.AppendChild(XmlHelper.GetXmlElement(doc, string.Empty, "identDN", SchemaHelper.Namespaces["extract"].Value, data.identDN));

                        XmlElement elType = XmlHelper.GetXmlElement(doc, string.Empty, "type", SchemaHelper.Namespaces["extract"].Value, string.Empty);
                        rootElement.AppendChild(elType);
                        elType.AppendChild(XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["data"].Key, "Code", SchemaHelper.Namespaces["data"].Value, data.type.Code.ToString()));

                        XmlElement elText = XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["data"].Key, "Text", SchemaHelper.Namespaces["data"].Value, string.Empty);
                        elType.AppendChild(elText);

                        foreach (LocalisedText text in data.type.Text)
                        {
                            XmlElement elLocalisedText = XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["data"].Key, "LocalisedText", SchemaHelper.Namespaces["data"].Value, string.Empty);
                            elText.AppendChild(elLocalisedText);
                            elLocalisedText.AppendChild(XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["data"].Key, "Language", SchemaHelper.Namespaces["data"].Value, text.Language.ToString()));
                            elLocalisedText.AppendChild(XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["data"].Key, "Text", SchemaHelper.Namespaces["data"].Value, text.Text));
                        }

                        if (param.returnGeometry)
                        {
                            XmlElement elLimit = XmlHelper.GetXmlElement(doc, string.Empty, "limit", SchemaHelper.Namespaces["extract"].Value, string.Empty);
                            rootElement.AppendChild(elLimit);
                            XmlElement elSurface = XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["geometry"].Key, "surface", SchemaHelper.Namespaces["geometry"].Value, string.Empty);
                            elLimit.AppendChild(elSurface);

                            foreach (BoundaryType ext in data.limit.surface.exterior)
                            {
                                OutputBoundaryXml(doc, elSurface, ext, "exterior");
                            }
                            foreach (BoundaryType ext in data.limit.surface.interior)
                            {
                                OutputBoundaryXml(doc, elSurface, ext, "interior");
                            }
                        }
                    }

                    return doc.DocumentElement;
                }
                else
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                    return XmlHelper.GetXmlElement(new ErrorResponseType(204));
                }
            }
            else
            {
                throw new WsUserException(HttpStatusCode.BadRequest.ToString());
            }
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public Stream GetEGRIDAsJson()
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        try
        {
            string json = string.Empty;

            GetEGRIDParamReq param = new GetEGRIDParamReq();
            if (param.Parse(WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters))
            {
                QueryResult qr = null;
                GetEGRIDReq req = new GetEGRIDReq(param.returnGeometry);
                switch (param.method)
                {
                    case GetEGRIDParamReq.GetEGRIDMethod.Coordinates:
                        qr = req.GetEGRIDByCoordinates(param.coordX, param.coordY, param.isGNSS);
                        break;
                    case GetEGRIDParamReq.GetEGRIDMethod.Idents:
                        qr = req.GetEGRIDByID(param.identDN, param.number);
                        break;
                    case GetEGRIDParamReq.GetEGRIDMethod.Localisation:
                        qr = req.GetEGRIDByLocalisation(param.postalCode, param.localisation, param.number);
                        break;
                }

                if (qr.features != null && qr.features.Length > 0)
                {
                    JsonExtract.JsonEGRID response = new JsonExtract.JsonEGRID();
                    response.Item = req.GetEGRIDResponse(qr);

                    json = serializer.Serialize(response);
                }
                else
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                    json = serializer.Serialize(new ErrorResponseType(204));
                }
            }
            else
            {
                throw new WsUserException(HttpStatusCode.BadRequest.ToString());
            }

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));

            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            string json = serializer.Serialize(new ErrorResponseType(500));
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public XmlElement GetExtractAsXml()
    {
        try
        {
            GetExtractParamReq param = new GetExtractParamReq();
            if (param.Parse(WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters))
            {
                GetExtractReq extractRequest = new GetExtractReq(param);

                QueryResultFeature feature = null;
                switch (param.method)
                {
                    case GetEGRIDParamReq.GetEGRIDMethod.EGRID:
                        feature = extractRequest.ProcessEGRID(param.EGRID);
                        break;
                    case GetEGRIDParamReq.GetEGRIDMethod.Idents:
                        feature = extractRequest.ProcessID(param.identDN, param.number);
                        break;
                }

                if (feature == null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                    return XmlHelper.GetXmlElement(new ErrorResponseType(204));
                }

                GetExtractByIdResponseType resp = extractRequest.GetResponseAsXml(feature);

                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add(string.Empty, SchemaHelper.Namespaces["extract"].Value);
                ns.Add(SchemaHelper.Namespaces["data"].Key, SchemaHelper.Namespaces["data"].Value);
                ns.Add(SchemaHelper.Namespaces["geometry"].Key, SchemaHelper.Namespaces["geometry"].Value);
                ns.Add(SchemaHelper.Namespaces["xsd"].Key, SchemaHelper.Namespaces["xsd"].Value);
                ns.Add(SchemaHelper.Namespaces["xsi"].Key, SchemaHelper.Namespaces["xsi"].Value);

                XmlElement root = XmlHelper.GetXmlElement(resp, ns);
                root.SetAttribute("schemaLocation", SchemaHelper.Namespaces["xsi"].Value, SchemaHelper.GetSchemaLocation(new string[] { "extract", "data", "geometry" }));

                return root;
            }
            else
            {
                throw new WsUserException(HttpStatusCode.BadRequest.ToString());
            }
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public XmlElement GetExtractAsUrl()
    {
        try
        {

            GetExtractParamReq param = new GetExtractParamReq();
            if (param.Parse(WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters))
            {
                GetExtractReq extractRequest = new GetExtractReq(param);

                QueryResultFeature feature = null;
                switch (param.method)
                {
                    case GetEGRIDParamReq.GetEGRIDMethod.EGRID:
                        feature = extractRequest.ProcessEGRID(param.EGRID);
                        break;
                    case GetEGRIDParamReq.GetEGRIDMethod.Idents:
                        feature = extractRequest.ProcessID(param.identDN, param.number);
                        break;
                }

                if (feature == null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                    return XmlHelper.GetXmlElement(new ErrorResponseType(204));
                }

                string resp = extractRequest.GetResponseAsUrl(feature);

                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.SeeOther;
                WebOperationContext.Current.OutgoingResponse.Location = resp;
                return XmlHelper.GetXmlElement(new ErrorResponseType(303));
            }
            else
            {
                throw new WsUserException(HttpStatusCode.BadRequest.ToString());
            }
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public Stream GetExtractAsJson()
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        try
        {
            GetExtractParamReq param = new GetExtractParamReq();
            if (param.Parse(WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters))
            {
                GetExtractReq extractRequest = new GetExtractReq(param);

                QueryResultFeature feature = null;
                switch (param.method)
                {
                    case GetEGRIDParamReq.GetEGRIDMethod.EGRID:
                        feature = extractRequest.ProcessEGRID(param.EGRID);
                        break;
                    case GetEGRIDParamReq.GetEGRIDMethod.Idents:
                        feature = extractRequest.ProcessID(param.identDN, param.number);
                        break;
                }

                if (feature == null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                    return new MemoryStream(Encoding.UTF8.GetBytes(serializer.Serialize(new ErrorResponseType(204))));
                }

                JsonExtract.JsonExtract extract = extractRequest.GetResponseAsJson(feature);
                string json = serializer.Serialize(extract);
                return new MemoryStream(Encoding.UTF8.GetBytes(json));
            }
            else
            {
                throw new WsUserException(HttpStatusCode.BadRequest.ToString());
            }
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));

            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public Stream GetExtractAsPdf()
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        try
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/pdf; charset=utf-8";

            GetExtractParamReq param = new GetExtractParamReq();
            if (param.Parse(WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters))
            {
                GetReportReq reportRequest = new GetReportReq(param);

                QueryResultFeature feature = null;
                switch (param.method)
                {
                    case GetEGRIDParamReq.GetEGRIDMethod.EGRID:
                        feature = reportRequest.ProcessEGRID(param.EGRID);
                        break;
                    case GetEGRIDParamReq.GetEGRIDMethod.Idents:
                        feature = reportRequest.ProcessID(param.identDN, param.number);
                        break;
                }

                if (feature == null)
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                    string json = serializer.Serialize(new ErrorResponseType(204));
                    return new MemoryStream(Encoding.UTF8.GetBytes(json));
                }

                byte[] resp = reportRequest.GetResponseAsPdf(feature);
                return new MemoryStream(resp);
            }
            else
            {
                throw new WsUserException(HttpStatusCode.BadRequest.ToString());
            }
        }
        catch (Exception ex)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

            Helper.LogError(new WsUserException(ex));

            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public XmlElement GetCapabilitiesAsXml()
    {
        try
        {
            CapabilityReq req = new CapabilityReq();
            return req.GetCapabilitiesAsXml();
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public Stream GetCapabilitiesAsJson()
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        try
        {
            CapabilityReq req = new CapabilityReq();
            string json = serializer.Serialize(req.GetCapabilities());

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));

            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            string json = serializer.Serialize(new ErrorResponseType(500));
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }
    
    public XmlElement GetVersionsAsXml()
    {
        try
        {
            VersionReq req = new VersionReq();
            return req.GetVersionsAsXml();
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public Stream GetVersionsAsJson()
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        try
        {
            VersionReq req = new VersionReq();
            string json = serializer.Serialize(req.GetVersions());

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));

            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            string json = serializer.Serialize(new ErrorResponseType(500));
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    private void OutputBoundaryXml(XmlDocument doc, XmlElement root, BoundaryType boundary, string name)
    {
        XmlElement el = XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["geometry"].Key, name, SchemaHelper.Namespaces["geometry"].Value, string.Empty);
        root.AppendChild(el);
        XmlElement elPolyline = XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["geometry"].Key, "polyline", SchemaHelper.Namespaces["geometry"].Value, string.Empty);
        el.AppendChild(elPolyline);

        foreach (CoordType ct in boundary.polyline.Items)
        {
            XmlElement elCoord = XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["geometry"].Key, "coord", SchemaHelper.Namespaces["geometry"].Value, string.Empty);
            elPolyline.AppendChild(elCoord);

            elCoord.AppendChild(XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["geometry"].Key, "c1", SchemaHelper.Namespaces["geometry"].Value, ct.c1.ToString()));
            elCoord.AppendChild(XmlHelper.GetXmlElement(doc, SchemaHelper.Namespaces["geometry"].Key, "c2", SchemaHelper.Namespaces["geometry"].Value, ct.c2.ToString()));
        }
    }
}
