/* $Rev: 21379 $ */
using System;

public class ReportData
{
    public mainSection section;
    public glossary[] glossaries;

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
    public string type;
    public string mapUrl;
    public string date = string.Empty;
    public string dmoDate;
    public data[] datas;
    public toc[] tocs;
    public restriction[] restrictions;
    public string[] noDataThemes;
    public string[] generalInfos;
    public string[] baseData;
    public office office;
    public bool breakAfterToc = false;
    public bool breakAfterOther = false;
}

public class data
{
    public string id;
    public section[] sections;
}

public class section
{
    public string id;
    public dataLayer[] dataLayers;
}

public class dataLayer
{
    public string layer;
    public field[] fields;
    public externalData[] externalDatas;
    public dataLayer[] dataLayers;
}

public class externalData
{
    public field[] fields;
}

public class field
{
    public string name;
    public string value;
}

public class restriction
{
    public string id;
    public string title;
    public string layer;
    public bool result;
    public regulation[] regulations;
    public law[] laws;
    public information[] informations;
    public service service;
    public string mapUrl;
    public string geometryType = string.Empty;
    public string legendLink = string.Empty;    
    public legend[] legends;
    public legend[] otherLegends = null;
    public legend[] additionnalLegends = null;
    public annex[] annexes;
    public bool breakAfterLegend = false;
    public bool breakAfterRegulation = false;
}

public class regulation
{
    public string label;
    public string[] values;
}

public class law
{
    public string title;
    public string link;
}

public class information
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
    public int length;
    public int surface;
    public double surfPercent;
}

public class toc
{
    public string title;
    public string page;
    public annex[] annexes = null;
}

public class clause
{
    public string title;
    public string content;
}

public class glossary
{
    public string title;
    public string content;
}

public class office
{
    public string name;
    public string rue;
    public string localite;
    public string link;
}

public class annex
{
    public int number;
    public string title;
}