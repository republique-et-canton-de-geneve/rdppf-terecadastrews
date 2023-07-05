/* $Rev: 29811 $ */
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
    [WebGet(UriTemplate = "getegrid/json/", ResponseFormat = WebMessageFormat.Json)]
    Stream GetEGRIDAsJson();

    [OperationContract]
    [WebGet(UriTemplate = "extract/xml/", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetExtractAsXml();

    [OperationContract]
    [WebGet(UriTemplate = "extract/json/", ResponseFormat = WebMessageFormat.Json)]
    Stream GetExtractAsJson();

    [OperationContract]
    [WebGet(UriTemplate = "extract/pdf/")]
    Stream GetExtractAsPdf();

    [OperationContract]
    [WebGet(UriTemplate = "extract/url/", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetExtractAsUrl();

    [OperationContract]
    [WebGet(UriTemplate = "capabilities/xml/", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetCapabilitiesAsXml();

    [OperationContract]
    [WebGet(UriTemplate = "capabilities/json/", ResponseFormat = WebMessageFormat.Json)]
    Stream GetCapabilitiesAsJson();

    [OperationContract]
    [WebGet(UriTemplate = "versions/xml/", ResponseFormat = WebMessageFormat.Xml)]
    XmlElement GetVersionsAsXml();

    [OperationContract]
    [WebGet(UriTemplate = "versions/json/", ResponseFormat = WebMessageFormat.Json)]
    Stream GetVersionsAsJson();
}