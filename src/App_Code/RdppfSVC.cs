/* $Rev: 21372 $ */
using System.Xml;
using System.ServiceModel.Web;
using System.Web.Script.Serialization;
using System;
using System.IO;
using System.Text;
using Topomat.Web.Common;
using System.Diagnostics;
using System.Net;

public class RdppfSVC : IRdppfSVC
{
    public XmlElement GetEGRIDAsXml()
    {
        try
        {
            GetEGRIDParamReq paramReq = GetEGRIDParamReq.GetEGRIDParam(
                WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["xy"],
                WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["gnss"]
            );

            if (paramReq != null)
            {
                GetEGRIDReq req = new GetEGRIDReq();
                QueryResult qr = req.GetEGRID(paramReq);

                if (qr.features.Length > 0)
                {
                    return XmlHelper.GetXmlElement(req.GetEGRIDResponse(qr));
                }
                else
                {
                    WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                    return XmlHelper.GetXmlElement(new ErrorResponseType(204));
                }
            }
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                return XmlHelper.GetXmlElement(new ErrorResponseType(400));
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

            GetEGRIDParamReq paramReq = GetEGRIDParamReq.GetEGRIDParam(
                WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["xy"],
                WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["gnss"]
            );

            if (paramReq != null)
            {
                GetEGRIDReq req = new GetEGRIDReq();
                QueryResult qr = req.GetEGRID(paramReq);

                if (qr.features.Length > 0)
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
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.BadRequest;
                json = serializer.Serialize(new ErrorResponseType(400));
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

    public XmlElement GetEGRIDByIDAsXml(string identdn, string number)
    {
        try
        {
            GetEGRIDReq req = new GetEGRIDReq();
            QueryResult qr = req.GetEGRIDByID(identdn, number);

            if (qr.features.Length > 0)
            {
                return XmlHelper.GetXmlElement(req.GetEGRIDResponse(qr));
            }
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                return XmlHelper.GetXmlElement(new ErrorResponseType(204));
            }
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public Stream GetEGRIDByIDAsJson(string identdn, string number)
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        try
        {
            string json = string.Empty;

            GetEGRIDReq req = new GetEGRIDReq();
            QueryResult qr = req.GetEGRIDByID(identdn, number);

            if (qr.features.Length > 0)
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

    public XmlElement GetEGRIDByLocalisationAsXml(string postalCode, string localisation, string number)
    {
        try
        {
            GetEGRIDReq req = new GetEGRIDReq();
            QueryResult qr = req.GetEGRIDByLocalisation(postalCode, localisation, number);

            if (qr.features.Length > 0)
            {
                return XmlHelper.GetXmlElement(req.GetEGRIDResponse(qr));
            }
            else
            {
                WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
                return XmlHelper.GetXmlElement(new ErrorResponseType(204));
            }
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public Stream GetEGRIDByLocalisationAsJson(string postalCode, string localisation, string number)
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        try
        {
            string json = string.Empty;

            GetEGRIDReq req = new GetEGRIDReq();
            QueryResult qr = req.GetEGRIDByLocalisation(postalCode, localisation, number);

            if (qr.features.Length > 0)
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

    public XmlElement GetReducedExtractByEGRIDAsXml(string egrid)
    {
        return this.GetExtractByEGRIDAsXml("reduced", egrid, false);
    }

    public XmlElement GetEmbeddableExtractByEGRIDAsXml(string egrid)
    {
        return this.GetExtractByEGRIDAsXml("embeddable", egrid, false);
    }

    public XmlElement GetReducedExtractGeometryByEGRIDAsXml(string egrid)
    {
        return this.GetExtractByEGRIDAsXml("reduced", egrid, true);
    }

    public XmlElement GetEmbeddableExtractGeometryByEGRIDAsXml(string egrid)
    {
        return this.GetExtractByEGRIDAsXml("embeddable", egrid, true);
    }

    private XmlElement GetExtractByEGRIDAsXml(string flavour, string egrid, bool returnGeometry)
    {
        try
        {
            return this.GetExtractAsXml(flavour, egrid, null, null, returnGeometry);
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));
            Helper.LogError(new WsUserException(ex));
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public XmlElement GetReducedExtractByIDAsXml(string identdn, string number)
    {
        return this.GetExtractByIDAsXml("reduced", identdn, number, false);
    }

    public XmlElement GetEmbeddableExtractByIDAsXml(string identdn, string number)
    {
        return this.GetExtractByIDAsXml("embeddable", identdn, number, false);
    }

    public XmlElement GetReducedExtractGeometryByIDAsXml(string identdn, string number)
    {
        return this.GetExtractByIDAsXml("reduced", identdn, number, true);
    }

    public XmlElement GetEmbeddableExtractGeometryByIDAsXml(string identdn, string number)
    {
        return this.GetExtractByIDAsXml("embeddable", identdn, number, true);
    }

    private XmlElement GetExtractByIDAsXml(string flavour, string identdn, string number, bool returnGeometry)
    {
        try
        {
            return this.GetExtractAsXml(flavour, null, identdn, number, returnGeometry);
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public Stream GetReducedExtractByEGRIDAsJson(string egrid)
    {
        return this.GetExtractByEGRIDAsJson("reduced", egrid, false);
    }

    public Stream GetEmbeddableExtractByEGRIDAsJson(string egrid)
    {
        return this.GetExtractByEGRIDAsJson("embeddable", egrid, false);
    }

    public Stream GetReducedExtractGeometryByEGRIDAsJson(string egrid)
    {
        return this.GetExtractByEGRIDAsJson("reduced", egrid, true);
    }

    public Stream GetEmbeddableExtractGeometryByEGRIDAsJson(string egrid)
    {
        return this.GetExtractByEGRIDAsJson("embeddable", egrid, true);
    }

    private Stream GetExtractByEGRIDAsJson(string flavour, string egrid, bool returnGeometry)
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        try
        {
            return this.GetExtractAsJson(flavour, egrid, null, null, returnGeometry);
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public Stream GetReducedExtractByIDAsJson(string identdn, string number)
    {
        return this.GetExtractByIDAsJson("reduced", identdn, number, false);
    }

    public Stream GetEmbeddableExtractByIDAsJson(string identdn, string number)
    {
        return this.GetExtractByIDAsJson("embeddable", identdn, number, false);
    }

    public Stream GetReducedExtractGeometryByIDAsJson(string identdn, string number)
    {
        return this.GetExtractByIDAsJson("reduced", identdn, number, true);
    }

    public Stream GetEmbeddableExtractGeometryByIDAsJson(string identdn, string number)
    {
        return this.GetExtractByIDAsJson("embeddable", identdn, number, true);
    }

    private Stream GetExtractByIDAsJson(string flavour, string identdn, string number, bool returnGeometry)
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        try
        {
            return this.GetExtractAsJson(flavour, null, identdn, number, returnGeometry);
        }
        catch (Exception ex)
        {
            Helper.LogError(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public Stream GetReducedExtractByEGRIDAsPdf(string egrid)
    {
        return this.GetExtractByEGRIDAsPdf("reduced", egrid);
    }

    public Stream GetFullExtractByEGRIDAsPdf(string egrid)
    {
        return this.GetExtractByEGRIDAsPdf("full", egrid);
    }

    public Stream GetSignedExtractByEGRIDAsPdf(string egrid)
    {
        return this.GetExtractByEGRIDAsPdf("signed", egrid);
    }

    private Stream GetExtractByEGRIDAsPdf(string flavour, string egrid)
    {
        try
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/pdf; charset=utf-8";

            return this.GetExtractAsPdf(flavour, egrid, null, null);
        }
        catch (Exception ex)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

            Helper.LogError(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.InternalServerError;
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public Stream GetReducedExtractByIDAsPdf(string identdn, string number)
    {
        return this.GetExtractByIDAsPdf("reduced", identdn, number);
    }

    public Stream GetFullExtractByIDAsPdf(string identdn, string number)
    {
        return this.GetExtractByIDAsPdf("full", identdn, number);
    }

    public Stream GetSignedExtractByIDAsPdf(string identdn, string number)
    {
        return this.GetExtractByIDAsPdf("signed", identdn, number);
    }

    private Stream GetExtractByIDAsPdf(string flavour, string identdn, string number)
    {
        try
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/pdf; charset=utf-8";

            return this.GetExtractAsPdf(flavour, null, identdn, number);
        }
        catch (Exception ex)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

            Helper.LogError(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
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

    private GetExtractParamReq GetExtractParameters()
    {
        string langParam = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["lang"];
        string topicsParam = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["topics"];
        string query = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.RequestUri.Query;
        bool withImages = query.Contains("withimages");

        return GetExtractParamReq.GetExtractParam(langParam, topicsParam, withImages);
    }

    private XmlElement GetExtractAsXml(string flavour, string egrid, string identdn, string number, bool returnGeometry)
    {
        GetExtractParamReq paramRequest = this.GetExtractParameters();
        GetExtractReq extractRequest = new GetExtractReq(paramRequest, flavour, returnGeometry);

        QueryResult qr = null;
        if (string.IsNullOrEmpty(egrid))
        {
            qr = extractRequest.ProcessID(identdn, number);
        }
        else
        {
            qr = extractRequest.ProcessEGRID(egrid);
        }

        if (qr.features.Length != 1)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
            return XmlHelper.GetXmlElement(new ErrorResponseType(204));
        }

        if (extractRequest.IsEmbeddable())
        {
            GetReportReq reportRequest = new GetReportReq(paramRequest, flavour);

            return XmlHelper.GetXmlElement(extractRequest.GetEmbeddableResponseAsXml(reportRequest.GetResponseAsPdf(qr)));
        }
        else
        {
            return XmlHelper.GetXmlElement(extractRequest.GetResponseAsXml(qr));
        }
    }

    private Stream GetExtractAsJson(string flavour, string egrid, string identdn, string number, bool returnGeometry)
    {
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        GetExtractParamReq paramRequest = this.GetExtractParameters();
        GetExtractReq extractRequest = new GetExtractReq(paramRequest, flavour, returnGeometry);

        QueryResult qr = null;
        if (string.IsNullOrEmpty(egrid))
        {
            qr = extractRequest.ProcessID(identdn, number);
        }
        else
        {
            qr = extractRequest.ProcessEGRID(egrid);
        }

        if (qr.features.Length != 1)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
            string json = serializer.Serialize(new ErrorResponseType(204));
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }

        if (extractRequest.IsEmbeddable())
        {
            GetReportReq reportRequest = new GetReportReq(paramRequest, flavour);

            JsonExtract.JsonEmbeddableExtract extract =
                extractRequest.GetEmbeddableResponseAsJson(reportRequest.GetResponseAsPdf(qr));

            string json = serializer.Serialize(extract);

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
        else
        {
            JsonExtract.JsonExtract extract = extractRequest.GetResponseAsJson(qr);
            string json = serializer.Serialize(extract);

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    private Stream GetExtractAsPdf(string flavour, string egrid, string identdn, string number)
    {
        Stopwatch timer = Stopwatch.StartNew();
        Stopwatch global = Stopwatch.StartNew();

        Helper.LogInfo(this.GetType().ToString(), "GetExtractAsPdf - *** START ***", timer.ElapsedMilliseconds);

        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        GetExtractParamReq paramRequest = this.GetExtractParameters();
        GetReportReq reportRequest = new GetReportReq(paramRequest, flavour);

        Helper.LogInfo(this.GetType().ToString(), "GetExtractAsPdf - *** PARAMETRES ***", timer.ElapsedMilliseconds);
        timer.Restart();

        QueryResult qr = null;
        if (string.IsNullOrEmpty(egrid))
        {
            qr = reportRequest.ProcessID(identdn, number);

            Helper.LogInfo(this.GetType().ToString(), "GetExtractAsPdf - *** PROCESS commune + parcelle ***", timer.ElapsedMilliseconds);
            timer.Restart();
        }
        else
        {
            qr = reportRequest.ProcessEGRID(egrid);

            Helper.LogInfo(this.GetType().ToString(), "GetExtractAsPdf - *** PROCESS EGRID ***", timer.ElapsedMilliseconds);
            timer.Restart();
        }

        if (qr.features.Length != 1)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = HttpStatusCode.NoContent;
            string json = serializer.Serialize(new ErrorResponseType(204));
            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }

        byte[] buffer = reportRequest.GetResponseAsPdf(qr);

        Helper.LogInfo(this.GetType().ToString(), "GetExtractAsPdf - *** ANALYSE + PDF ***", timer.ElapsedMilliseconds);
        timer.Stop();

        Helper.LogInfo(this.GetType().ToString(), "GetExtractAsPdf - *** END ***", global.ElapsedMilliseconds);
        global.Stop();

        return new MemoryStream(buffer);
    }
}
