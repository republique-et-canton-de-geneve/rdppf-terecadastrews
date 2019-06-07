/* $Rev: 15751 $ */

public class GetEGRIDParamReq
{
    public double coordX;
    public double coordY;
    public bool isGNSS;

    public static GetEGRIDParamReq GetEGRIDParam(string xy, string gnss)
    {
        GetEGRIDParamReq param = new GetEGRIDParamReq();

        if (string.IsNullOrEmpty(xy) && string.IsNullOrEmpty(gnss))
        {
            return null;
        }
        bool _isGNSS = string.IsNullOrEmpty(gnss) ? false : true;
        string coords = string.IsNullOrEmpty(xy) ? gnss : xy;

        double x, y;
        if (double.TryParse(coords.Split(new char[] { ',' })[0], out x) &&
            double.TryParse(coords.Split(new char[] { ',' })[1], out y))
        {
            return new GetEGRIDParamReq
            {
                coordX = x,
                coordY = y,
                isGNSS = _isGNSS
            };
        }
        return null;
    }
}