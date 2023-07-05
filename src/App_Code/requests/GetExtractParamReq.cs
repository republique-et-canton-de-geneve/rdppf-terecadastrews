/* $Rev: 30309 $ */
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

public class GetExtractParamReq : ParamReq
{
    private static string[] federalTopics = { "ch.ProjektierungszonenNationalstrassen", "ch.BaulinienNationalstrassen", 
        "ch.ProjektierungszonenEisenbahnanlagen", "ch.BaulinienEisenbahnanlagen", 
        "ch.ProjektierungszonenFlughafenanlagen", "ch.BaulinienFlughafenanlagen", "ch.Sicherheitszonenplan", 
        "ch.BelasteteStandorteMilitaer", "ch.BelasteteStandorteZivileFlugplaetze", "ch.BelasteteStandorteOeffentlicherVerkehr", 
        "ch.ProjektierungszonenStarkstromanlagen", "ch.BaulinienStarkstromanlagen" };

    public string EGRID;
    public string identDN;
    public string number;

    public bool returnGeometry;
    public string lang;
    public bool allTopics;
    public IList<string> partialTopics;
    public bool withImages;

    public GetExtractParamReq()
    {
        lang = "fr";
        allTopics = true;
    }

    public bool Parse(NameValueCollection parameters)
    {
        EGRID = GetParameterValue(parameters, "EGRID");
        if (!string.IsNullOrEmpty(EGRID))
        {
            method = GetEGRIDMethod.EGRID;
        }

        identDN = GetParameterValue(parameters, "IDENTDN");
        number = GetParameterValue(parameters, "NUMBER");
        if (!string.IsNullOrEmpty(identDN) && !string.IsNullOrEmpty(number))
        {
            method = GetEGRIDMethod.Idents;
        }

        bool.TryParse(GetParameterValue(parameters, "GEOMETRY"), out returnGeometry);
        bool.TryParse(GetParameterValue(parameters, "WITHIMAGES"), out withImages);

        string language = GetParameterValue(parameters, "LANG");
        if (!string.IsNullOrEmpty(language))
        {
            if (!CapabilityReq.languages.Contains(language.ToLower()))
            {
                return false;
            }
            else
            {
                lang = language.ToLower();
            }
        }

        string topics = GetParameterValue(parameters, "TOPICS");
        if (!string.IsNullOrEmpty(topics) && string.Compare(topics, "ALL") != 0)
        {
            if (string.Compare(topics, "ALL_FEDERAL") == 0)
            {
                allTopics = false;
                partialTopics = GetExtractParamReq.federalTopics.ToList();
            }
            else
            {
                partialTopics = topics.Split(new char[] { ',' }).ToList();
            }
        }

        return method == GetEGRIDMethod.Undefined ? false : true;
    }
}