/* $Rev: 29789 $ */
using System.Collections.Specialized;

public class ParamReq
{
    public enum GetEGRIDMethod { Undefined, EGRID, Coordinates, Idents, Localisation };
    public GetEGRIDMethod method;

    public ParamReq()
    {
        method = GetEGRIDMethod.Undefined;
    }

    protected string GetParameterValue(NameValueCollection parameters, string parameter)
    {
        string value = parameters[parameter];
        if (string.IsNullOrEmpty(value))
        {
            value = parameters[parameter.ToLower()];
        }
        return value;
    }
}