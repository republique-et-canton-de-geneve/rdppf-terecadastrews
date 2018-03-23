/* $Rev: 14634 $ */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

public class GetExtractParamReq
{
    private static string[] federalTopics = { "LandUsePlans",  "MotorwaysProjectPlaningZones", "MotorwaysBuildingLines", "RailwaysProjectPlanningZones", "RailwaysBuildingLines", "AirportsProjectPlanningZones", "AirportsBuildingLines", "AirportsSecurityZonePlans", "ContaminatedSites", "ContaminatedMilitarySites", "ContaminatedCivilAviationSites", "ContaminatedPublicTransportSites", "GroundwaterProtectionZones", "GroundwaterProtectionSites", "NoiseSensitivityLevels", "ForestPerimeters", "ForestDistanceLines" };
    public string lang;
    public bool allTopics;
    public IList<string> topics;
    public bool withImages;
    public ErrorResponseType error;

    public GetExtractParamReq()
    {
        this.allTopics = false;
        this.topics = new List<string>();
        this.withImages = false;
        this.error = null;
    }

    public static GetExtractParamReq GetExtractParam(string lang, string topics, bool withImages)
    {
        GetExtractParamReq param = new GetExtractParamReq();

        if(string.IsNullOrEmpty(lang))
        {
            lang = "fr";    
        }
        if (!CapabilityReq.languages.Contains(lang))
        {
            param.error = new ErrorResponseType(401);
        }
        else
        {
            param.lang = lang;
        }

        if (string.IsNullOrEmpty(topics))
        {
            param.allTopics = true;
        }
        else
        {
            string[] topicArray = topics.Split(new char[] { ',' });
            if(topicArray.Contains("ALL"))
            {
                param.allTopics = true;
            }
            else 
            {
                if (topicArray.Contains("ALL_FEDERAL"))
                {
                    param.topics = GetExtractParamReq.federalTopics.ToList();
                }
                foreach(string topic in topicArray)
                {
                    if (string.Compare(topic, "ALL_FEDERAL") != 0)
                    {
                        param.topics.Add(topic);
                    }
                }
            }
        }

        param.withImages = withImages;


        return param;
    }
}