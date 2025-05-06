/* $Rev: 31340 $ */
using System;

public class ReportData
{
    public mainSection section;
    public InformationText[] glossaries;

    public ReportData()
    {
    }

    public ReportData(string uid)
    {
        this.section = new mainSection();
        this.section.reference = uid;
        this.section.date = DateTime.Now.ToString("dd.MM.yyyy");
    }
}

public class mainSection
{
    public string reference;
    public string mapUrl;
    public string date = string.Empty;
    public string dmoDate;
    public RealEstateData realEstate;
    public toc[] tocs;
    public restriction[] restrictions;
    public string[] noDataThemes;
    public InformationText[] generalInfos;
    public InformationText[] disclaimers;
    public office office;
    public string marginStyle = "height:10mm";
    public bool breakAfterToc = false;
}

public class RealEstateData
{
    public string number;
    public string type;
    public string egrid;
    public string municipalityName;
    public string section;
    public string municipalityCode;
    public string municipalityEcussonUrl;
    public string area;
    public string state;
}

public class restriction
{
    public string id;
    public string tocTitle;
    public string title;
    public double order;
    public bool result;
    public string lawStatus;
    public string lawStatusId;
    public legalProvision[] legalProvisions;
    public law[] laws;
    public hint[] hints;
    public service service;
    public string mapUrl;
    public legend[] legends;
    public legend[] otherLegends = null;
    public legend[] additionalLegends = null;
    public legend[] additionalLegendsOnMap = null;
}

public class legalProvision
{
    public string label;
    public string[] values;
}

public class law
{
    public string index;
    public string title;
    public string link;
}

public class hint
{
    public string label;
    public string[] values;
}

public class service
{
    public string name;
    public string link;
}

public class legend
{
    public string imageUrl;
    public string label;
    public string geometryType;
    public int geometryOrder;
    public double length;
    public int surface;
    public double surfPercent;
    public string surfPercentFormatted;
    public int points;
}

public class toc
{
    public string title;
    public string page;
}

public class office
{
    public string name;
    public string rue;
    public string localite;
    public string link;
}