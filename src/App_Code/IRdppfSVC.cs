/* $Rev: 21372 $ */
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Xml;
using System.IO;

[ServiceContract]
public interface IRdppfSVC
{
    [OperationContract]
    [WebGet(UriTemplate = "getegrid/xml/", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEGRIDAsXml();
    
    [OperationContract]
    [WebGet(UriTemplate = "getegrid/xml/{identdn}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEGRIDByIDAsXml(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "getegrid/xml/{postalcode}/{localisation}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEGRIDByLocalisationAsXml(string postalCode, string localisation, string number);

    [OperationContract]
    [WebGet(UriTemplate = "getegrid/json/", ResponseFormat = WebMessageFormat.Json)]
    Stream GetEGRIDAsJson();

    [OperationContract]
    [WebGet(UriTemplate = "getegrid/json/{identdn}/{number}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetEGRIDByIDAsJson(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "getegrid/json/{postalcode}/{localisation}/{number}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetEGRIDByLocalisationAsJson(string postalCode, string localisation, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/xml/{egrid}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetReducedExtractByEGRIDAsXml(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/xml/geometry/{egrid}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetReducedExtractGeometryByEGRIDAsXml(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/xml/{identdn}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetReducedExtractByIDAsXml(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/xml/geometry/{identdn}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetReducedExtractGeometryByIDAsXml(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/embeddable/xml/{egrid}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEmbeddableExtractByEGRIDAsXml(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/embeddable/xml/geometry/{egrid}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEmbeddableExtractGeometryByEGRIDAsXml(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/embeddable/xml/{identdn}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEmbeddableExtractByIDAsXml(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/embeddable/xml/geometry/{identdn}/{number}", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetEmbeddableExtractGeometryByIDAsXml(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/json/{egrid}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetReducedExtractByEGRIDAsJson(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/json/geometry/{egrid}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetReducedExtractGeometryByEGRIDAsJson(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/json/{identdn}/{number}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetReducedExtractByIDAsJson(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/json/geometry/{identdn}/{number}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetReducedExtractGeometryByIDAsJson(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/embeddable/json/{egrid}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetEmbeddableExtractByEGRIDAsJson(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/embeddable/json/geometry/{egrid}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetEmbeddableExtractGeometryByEGRIDAsJson(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/embeddable/json/{identdn}/{number}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetEmbeddableExtractByIDAsJson(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/embeddable/json/geometry/{identdn}/{number}", ResponseFormat = WebMessageFormat.Json)]
    Stream GetEmbeddableExtractGeometryByIDAsJson(string identdn, string number);
    
    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/pdf/{egrid}")]
    Stream GetReducedExtractByEGRIDAsPdf(string egrid);
    
    [OperationContract]
    [WebGet(UriTemplate = "extract/reduced/pdf/{identdn}/{number}")]
    Stream GetReducedExtractByIDAsPdf(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/full/pdf/{egrid}")]
    Stream GetFullExtractByEGRIDAsPdf(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/full/pdf/{identdn}/{number}")]
    Stream GetFullExtractByIDAsPdf(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "extract/signed/pdf/{egrid}")]
    Stream GetSignedExtractByEGRIDAsPdf(string egrid);

    [OperationContract]
    [WebGet(UriTemplate = "extract/signed/pdf/{identdn}/{number}")]
    Stream GetSignedExtractByIDAsPdf(string identdn, string number);

    [OperationContract]
    [WebGet(UriTemplate = "capabilities/xml", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetCapabilitiesAsXml();

    [OperationContract]
    [WebGet(UriTemplate = "capabilities/json", ResponseFormat = WebMessageFormat.Json)]
    Stream GetCapabilitiesAsJson();

    [OperationContract]
    [WebGet(UriTemplate = "versions/xml", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetVersionsAsXml();

    [OperationContract]
    [WebGet(UriTemplate = "versions/json", ResponseFormat = WebMessageFormat.Json)]
    Stream GetVersionsAsJson();
}