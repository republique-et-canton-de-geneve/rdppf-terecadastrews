/* $Rev: 14634 $ */
using System.Xml;
using System.ServiceModel.Web;
using System.Web.Script.Serialization;
using System;
using System.IO;
using System.Text;
using Topomat.Web.Common;

public class RdppfSVC : IRdppfSVC
{
    public XmlElement GetEGRID()
    {
        string xyParam = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["xy"];
        string gnssParam = WebOperationContext.Current.IncomingRequest.UriTemplateMatch.QueryParameters["gnss"];

        XmlElement elem = null;
        GetEGRIDReq req = new GetEGRIDReq();

        if(string.IsNullOrEmpty(xyParam) && string.IsNullOrEmpty(gnssParam))
        {
            return XmlHelper.GetXmlElement(new ErrorResponseType(400));
        }
        bool isGNSS = string.IsNullOrEmpty(gnssParam) ? false : true;
        string coords = string.IsNullOrEmpty(xyParam) ? gnssParam : xyParam;
        
        double x, y;
        if (double.TryParse(coords.Split(new char[] { ',' })[0], out x) &&
            double.TryParse(coords.Split(new char[] { ',' })[1], out y))
        {
            elem = req.GetEGRID(x, y, isGNSS);
        }
        else
        {
            return XmlHelper.GetXmlElement(new ErrorResponseType(400));
        }
        
        return elem;
    }

    public XmlElement GetEGRIDByID(string identdn, string number)
    {
        GetEGRIDReq req = new GetEGRIDReq();
        return req.GetEGRIDByID(identdn, number);
    }

    public XmlElement GetEGRIDByLocalisation(string postalCode, string localisation, string number)
    {
        GetEGRIDReq req = new GetEGRIDReq();
        return req.GetEGRIDByLocalisation(postalCode, localisation, number);
    }

    public XmlElement GetExtractByEGRIDAsXml(string flavour, string egrid)
    {
        try
        {
            return this.GetExtractAsXml(flavour, egrid, null, null, false);
        }
        catch (Exception ex)
        {
            Helper.AppendToLog(new WsUserException(ex));
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public XmlElement GetExtractGeometryByEGRIDAsXml(string flavour, string egrid)
    {
        try
        {
            return this.GetExtractAsXml(flavour, egrid, null, null, true);
        }
        catch (Exception ex)
        {
            Helper.AppendToLog(new WsUserException(ex));
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public XmlElement GetExtractByIDAsXml(string flavour, string identdn, string number)
    {
        try
        {
            return this.GetExtractAsXml(flavour, null, identdn, number, false);        
        }
        catch (Exception ex)
        {
            Helper.AppendToLog(new WsUserException(ex));
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public XmlElement GetExtractGeometryByIDAsXml(string flavour, string identdn, string number)
    {
        try
        {
            return this.GetExtractAsXml(flavour, null, identdn, number, true);
        }
        catch (Exception ex)
        {
            Helper.AppendToLog(new WsUserException(ex));
            return XmlHelper.GetXmlElement(new ErrorResponseType(500));
        }
    }

    public Stream GetExtractByEGRIDAsJson(string flavour, string egrid)
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        try
        {
            return this.GetExtractAsJson(flavour, egrid, null, null, false);
        }
        catch (Exception ex)
        {
            Helper.AppendToLog(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public Stream GetExtractGeometryByEGRIDAsJson(string flavour, string egrid)
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        try
        {
            return this.GetExtractAsJson(flavour, egrid, null, null, true);
        }
        catch (Exception ex)
        {
            Helper.AppendToLog(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public Stream GetExtractByIDAsJson(string flavour, string identdn, string number)
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";
        
        try
        {
            return this.GetExtractAsJson(flavour, null, identdn, number, false);
        }
        catch (Exception ex)
        {
            Helper.AppendToLog(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public Stream GetExtractGeometryByIDAsJson(string flavour, string identdn, string number)
    {
        WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

        try
        {
            return this.GetExtractAsJson(flavour, null, identdn, number, true);
        }
        catch (Exception ex)
        {
            Helper.AppendToLog(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    public Stream GetExtractByEGRIDAsPdf(string flavour, string egrid)
    {
        try
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/pdf; charset=utf-8";

            return this.GetExtractAsPdf(flavour, egrid, null, null);
        }
        catch (Exception ex)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

            Helper.AppendToLog(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }
    
    public Stream GetExtractByIDAsPdf(string flavour, string identdn, string number)
    {
        try
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/pdf; charset=utf-8";

            return this.GetExtractAsPdf(flavour, null, identdn, number);
        }
        catch (Exception ex)
        {
            WebOperationContext.Current.OutgoingResponse.ContentType = "application/json; charset=utf-8";

            Helper.AppendToLog(new WsUserException(ex));

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            string json = serializer.Serialize(new ErrorResponseType(500));

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }
    
    public XmlElement GetCapabilities()
    {
        CapabilityReq req = new CapabilityReq();
        return req.GetCapabilities();
    }

    public XmlElement GetVersions()
    {
        VersionReq req = new VersionReq();
        return req.GetVersions();
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
        GetExtractReq request = new GetExtractReq(this.GetExtractParameters(), flavour, returnGeometry);

        QueryResult qr = null;
        if (string.IsNullOrEmpty(egrid))
        {
            qr = request.ProcessID(identdn, number);
        }
        else
        {
            qr = request.ProcessEGRID(egrid);
        }

        ErrorResponseType error = request.ValidateRequest(qr);

        if (error.GetCode() == 200)
        {
            return XmlHelper.GetXmlElement(request.GetResponseAsXml(qr));
        }
        else
        {
            return XmlHelper.GetXmlElement(error);
        }
    }

    private Stream GetExtractAsJson(string flavour, string egrid, string identdn, string number, bool returnGeometry)
    {
        GetExtractReq request = new GetExtractReq(this.GetExtractParameters(), flavour, returnGeometry);

        QueryResult qr = null;
        if (string.IsNullOrEmpty(egrid))
        {
            qr = request.ProcessID(identdn, number);
        }
        else
        {
            qr = request.ProcessEGRID(egrid);
        }

        ErrorResponseType error = request.ValidateRequest(qr);
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        if (error.GetCode() == 200)
        {
            JsonExtract.JsonExtract extract = request.GetResponseAsJson(qr);
            string json = serializer.Serialize(extract);

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
        else
        {
            string json = serializer.Serialize(error);

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }

    private Stream GetExtractAsPdf(string flavour, string egrid, string identdn, string number)
    {
        GetExtractReq request = new GetExtractReq(this.GetExtractParameters(), flavour, true);

        QueryResult qr = null;
        if (string.IsNullOrEmpty(egrid))
        {
            qr = request.ProcessID(identdn, number);
        }
        else
        {
            qr = request.ProcessEGRID(egrid);
        }

        ErrorResponseType error = request.ValidateRequest(qr);
        JavaScriptSerializer serializer = new JavaScriptSerializer();
        serializer.MaxJsonLength = Int32.MaxValue;

        if (error.GetCode() == 200)
        {
            return new MemoryStream(request.GetResponseAsPdf(qr));
        }
        else
        {
            string json = serializer.Serialize(error);

            return new MemoryStream(Encoding.UTF8.GetBytes(json));
        }
    }
}
