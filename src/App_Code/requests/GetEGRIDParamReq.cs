/* $Rev: 29811 $ */
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;

public class GetEGRIDParamReq : ParamReq
{
    public double coordX;
    public double coordY;
    public bool isGNSS;
    public bool returnGeometry;
    public string identDN;
    public string number;
    public string postalCode;
    public string localisation;

    public GetEGRIDParamReq() : base()
    {
        number = string.Empty;
    }

    public bool Parse(NameValueCollection parameters)
    {
        bool.TryParse(GetParameterValue(parameters, "GEOMETRY"), out returnGeometry);

        string coordsEN = GetParameterValue(parameters, "EN");
        string coordsGNSS = GetParameterValue(parameters, "GNSS");

        Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
        if (!string.IsNullOrEmpty(coordsEN))
        {
            if (!GetCoords(coordsEN))
            {
                return false;
            }
            method = GetEGRIDMethod.Coordinates;
            isGNSS = false;
        }
        else if (!string.IsNullOrEmpty(coordsGNSS))
        {
            if (!GetCoords(coordsGNSS))
            {
                return false;
            }
            method = GetEGRIDMethod.Coordinates;
            isGNSS = true;
        }

        identDN = GetParameterValue(parameters, "IDENTDN");
        number = GetParameterValue(parameters, "NUMBER");
        if (!string.IsNullOrEmpty(identDN) && !string.IsNullOrEmpty(number))
        {
            method = GetEGRIDMethod.Idents;
        }

        postalCode = GetParameterValue(parameters, "POSTALCODE");
        localisation = GetParameterValue(parameters, "LOCALISATION");
        if (!string.IsNullOrEmpty(postalCode) && !string.IsNullOrEmpty(localisation))
        {
            method = GetEGRIDMethod.Localisation;
        }

        return method == GetEGRIDMethod.Undefined ? false : true;
    }

    private bool GetCoords(string value)
    {
        if (double.TryParse(value.Split(new char[] { ',' })[0], out coordX) &&
            double.TryParse(value.Split(new char[] { ',' })[1], out coordY))
        {
            return true;
        }
        return false;
    }
}