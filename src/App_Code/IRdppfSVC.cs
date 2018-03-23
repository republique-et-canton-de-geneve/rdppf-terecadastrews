/* $Rev: 14634 $ */
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Xml;
using System.IO;

[ServiceContract]
public interface IRdppfSVC
{
    [OperationContract]
    [WebGet(UriTemplate = "getegrid", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEGRID();

    [OperationContract]
    [WebGet(UriTemplate = "getegrid/{identdn}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEGRIDByID(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "getegrid/{postalcode}/{localisation}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEGRIDByLocalisation(string postalCode, string localisation, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/xml/{egrid}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetExtractByEGRIDAsXml(string flavour, string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/xml/geometry/{egrid}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetExtractGeometryByEGRIDAsXml(string flavour, string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/xml/{identdn}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetExtractByIDAsXml(string flavour, string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/xml/geometry/{identdn}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetExtractGeometryByIDAsXml(string flavour, string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/json/{egrid}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetExtractByEGRIDAsJson(string flavour, string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/json/geometry/{egrid}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetExtractGeometryByEGRIDAsJson(string flavour, string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/json/{identdn}/{number}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetExtractByIDAsJson(string flavour, string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/json/geometry/{identdn}/{number}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetExtractGeometryByIDAsJson(string flavour, string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/pdf/{egrid}")]
    Stream GetExtractByEGRIDAsPdf(string flavour, string egrid);
    
    [OperationContract]
    [WebGet(UriTemplate = "extract/{flavour}/pdf/{identdn}/{number}")]
    Stream GetExtractByIDAsPdf(string flavour, string identdn, string number);
    
    [OperationContract]
    [WebGet(UriTemplate = "capabilities", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetCapabilities();

    [OperationContract]
    [WebGet(UriTemplate = "versions", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetVersions();
}