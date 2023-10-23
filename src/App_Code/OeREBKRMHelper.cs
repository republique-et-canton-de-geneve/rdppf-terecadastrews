/* $Rev: 22729 $ */
using ExtractDataModel_v20;
using OeREBKRMkvs_V2_0;
using OeREBKRMtrsfr_V2_0;
using System.Collections.Generic;
using System.Linq;

public class OeREBKRMHelper
{
    private DataManager dataManager;
    private IList<TRANSFERDATASECTIONOeREBKRMkvs_V2_0ThemaOeREBKRMkvs_V2_0ThemaThema> themes;
    private IList<TRANSFERDATASECTIONOeREBKRMkvs_V2_0ThemaOeREBKRMkvs_V2_0ThemaThemaGesetz> themeLaws;
    private IList<Dokument> laws;
    private TRANSFERDATASECTIONOeREBKRM_V2_0DokumenteOeREBKRM_V2_0AmtAmt federalOffice;
    private IList<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationLogo> logos;
    private IList<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationRechtsStatusTxt> lawStatus;
    private IList<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationDokumentTypTxt> documentTypes;
    private IList<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationGrundstuecksArtTxt> realEstateTypes;
    private IList<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationGlossar> glossaries;
    private IList<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationHaftungshinweis> disclaimers;
    private IList<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationInformation> informations;

    private Dictionary<LawstatusCode, string> lawStatusDict = new Dictionary<LawstatusCode, string>()
    {
        { LawstatusCode.inForce, "inKraft" },
        { LawstatusCode.changeWithPreEffect, "AenderungMitVorwirkung" },
        { LawstatusCode.changeWithoutPreEffect, "AenderungOhneVorwirkung" }
    };

    private Dictionary<string, LawstatusCode> invLawStatusDict;

    private Dictionary<DocumentTypeCode, string> documentTypeDict = new Dictionary<DocumentTypeCode, string>()
    {
        { DocumentTypeCode.LegalProvision, "Rechtsvorschrift" },
        { DocumentTypeCode.Law, "GesetzlicheGrundlage" },
        { DocumentTypeCode.Hint, "Hinweis" }
    };

    private Dictionary<RealEstateTypeCode, string> realEstateTypeDict = new Dictionary<RealEstateTypeCode, string>()
    {
        { RealEstateTypeCode.RealEstate, "Liegenschaft" },
        { RealEstateTypeCode.Distinct_and_permanent_rightsBuildingRight, "SelbstRecht.Baurecht" },
        { RealEstateTypeCode.Distinct_and_permanent_rightsconcession, "SelbstRecht.Konzessionsrecht" },
        { RealEstateTypeCode.Distinct_and_permanent_rightsother, "SelbstRecht.weitere" },
        { RealEstateTypeCode.Distinct_and_permanent_rightsright_to_spring_water, "SelbstRecht.Quellenrecht" },
        { RealEstateTypeCode.Mineral_rights, "Bergwerk" },
    };

    public OeREBKRMHelper(DataManager manager)
    {
        dataManager = manager;

        themes = new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0ThemaOeREBKRMkvs_V2_0ThemaThema>();
        themeLaws = new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0ThemaOeREBKRMkvs_V2_0ThemaThemaGesetz>();
        foreach (object item in this.dataManager.GetOerebThemes().DATASECTION.OeREBKRMkvs_V2_0Thema.Items)
        {
            switch (item.GetType().ToString())
            {
                case "OeREBKRMkvs_V2_0.TRANSFERDATASECTIONOeREBKRMkvs_V2_0ThemaOeREBKRMkvs_V2_0ThemaThema":
                    themes.Add((TRANSFERDATASECTIONOeREBKRMkvs_V2_0ThemaOeREBKRMkvs_V2_0ThemaThema)item);
                    break;
                case "OeREBKRMkvs_V2_0.TRANSFERDATASECTIONOeREBKRMkvs_V2_0ThemaOeREBKRMkvs_V2_0ThemaThemaGesetz":
                    themeLaws.Add((TRANSFERDATASECTIONOeREBKRMkvs_V2_0ThemaOeREBKRMkvs_V2_0ThemaThemaGesetz)item);
                    break;
                default:
                    break;
            }
        }

        laws = new List<Dokument>(this.dataManager.GetOerebLaws().DATASECTION.OeREBKRM_V2_0Dokumente.OeREBKRM_V2_0DokumenteDokument);
        federalOffice = dataManager.GetOerebLaws().DATASECTION.OeREBKRM_V2_0Dokumente.OeREBKRM_V2_0AmtAmt;
        logos = new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationLogo>(
            this.dataManager.GetOerebLogos().DATASECTION.OeREBKRMkvs_V2_0Konfiguration.OeREBKRMkvs_V2_0KonfigurationLogo);
        lawStatus = new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationRechtsStatusTxt>(
            this.dataManager.GetOerebTexts().DATASECTION.OeREBKRMkvs_V2_0Konfiguration.OeREBKRMkvs_V2_0KonfigurationRechtsStatusTxt);
        documentTypes = new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationDokumentTypTxt>(
            this.dataManager.GetOerebTexts().DATASECTION.OeREBKRMkvs_V2_0Konfiguration.OeREBKRMkvs_V2_0KonfigurationDokumentTypTxt);
        realEstateTypes = new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationGrundstuecksArtTxt>(
            this.dataManager.GetOerebTexts().DATASECTION.OeREBKRMkvs_V2_0Konfiguration.OeREBKRMkvs_V2_0KonfigurationGrundstuecksArtTxt);
        glossaries = new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationGlossar>(
            this.dataManager.GetOerebTexts().DATASECTION.OeREBKRMkvs_V2_0Konfiguration.OeREBKRMkvs_V2_0KonfigurationGlossar);
        disclaimers = new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationHaftungshinweis>(
            this.dataManager.GetOerebTexts().DATASECTION.OeREBKRMkvs_V2_0Konfiguration.OeREBKRMkvs_V2_0KonfigurationHaftungshinweis);
        informations = new List<TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationInformation>(
            this.dataManager.GetOerebTexts().DATASECTION.OeREBKRMkvs_V2_0Konfiguration.OeREBKRMkvs_V2_0KonfigurationInformation);

        invLawStatusDict = new Dictionary<string, LawstatusCode>();
        foreach(LawstatusCode key in lawStatusDict.Keys)
        {
            invLawStatusDict.Add(lawStatusDict[key], key);
        }
    }

    public Dokument[] GetLaws(string theme)
    {
        IList<Dokument> documents = new List<Dokument>();

        foreach (var themeLaw in this.themeLaws.Where(tl => string.Compare(tl.Thema.REF, theme) == 0))
        {
            foreach (Dokument law in this.laws.Where(l => string.Compare(l.TID, themeLaw.Gesetz.REF) == 0))
            {
                documents.Add(law);
            }
        }

        return documents.ToArray();
    }

    public Document GetLaw(string id, string lang)
    {
        Document doc = null;
        Dokument law = laws.FirstOrDefault(l => string.Compare(l.TID, id) == 0);
        if (law != null)
        {
            LawstatusCode code = GetLawstatusCode(law.Rechtsstatus);

            doc = new Document
            {
                Title = GetLocalisedText(law.Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang),
                Abbreviation = GetLocalisedText(law.Abkuerzung.LocalisationCH_V1MultilingualText.LocalisedText, lang),
                OfficialNumber = GetLocalisedText(law.OffizielleNr.LocalisationCH_V1MultilingualText.LocalisedText, lang),
                TextAtWeb = GetLocalisedUri(law.TextImWeb.OeREBKRM_V2_0MultilingualUri.LocalisedText, lang),
                Index = law.AuszugIndex,
                Lawstatus = new Lawstatus
                {
                    Code = code,
                    Text = GetLocalisedText(this.lawStatus.First(t => string.Compare(t.Code, law.Rechtsstatus) == 0).Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang)
                },
                ResponsibleOffice = GetFederalOffice(lang)
            };
        }
        return doc;
    }

    public string GetLawPublicationDate(string id, string lang)
    {
        string date = string.Empty;
        Dokument law = laws.FirstOrDefault(l => string.Compare(l.TID, id) == 0);
        if (law != null)
        {
            date = law.publiziertAb.ToString("dd.MM.yyyy");
        }
        return date;
    }

    public Office GetFederalOffice(string lang)
    {
        return new Office
        {
            Name = GetLocalisedText(federalOffice.Name.LocalisationCH_V1MultilingualText.LocalisedText, lang),
            OfficeAtWeb = GetLocalisedUri(federalOffice.AmtImWeb.OeREBKRM_V2_0MultilingualUri.LocalisedText, lang),
            UID = federalOffice.UID
        };
    }

    public LawstatusCode GetLawstatusCode(string krmCode)
    {
        LawstatusCode code = LawstatusCode.inForce;
        foreach (LawstatusCode key in lawStatusDict.Keys)
        {
            if (string.Compare(lawStatusDict[key], krmCode) == 0)
            {
                code = key;
            }
        }
        return code;
    }

    public string GetLogo(string code, string lang)
    {
        return (this.logos.First(l => string.Compare(l.Code, code) == 0).Bild.OeREBKRM_V2_0MultilingualBlob.LocalisedBlob).First(lb => string.Compare(lb.Language, lang) == 0).Blob.BINBLBOX;
    }

    public DocumentTypeCode GetDocumentTypeCode(string krmCode)
    {
        DocumentTypeCode code = DocumentTypeCode.Hint;
        foreach (DocumentTypeCode key in documentTypeDict.Keys)
        {
            if (string.Compare(documentTypeDict[key], krmCode) == 0)
            {
                code = key;
            }
        }
        return code;
    }

    public string GetLawStatusText(LawstatusCode code, string lang)
    {
        return GetText(this.lawStatus.First(t => string.Compare(t.Code, this.lawStatusDict[code]) == 0).Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang);
    }

    public OerebLawStatus[] GetOerebLawStatuses(string lang)
    {
        IList<OerebLawStatus> list = new List<OerebLawStatus>();
        foreach(var status in lawStatus)
        {
            list.Add(new OerebLawStatus
            {
                Id = status.TID,
                Code = invLawStatusDict[status.Code],
                Text = GetText(status.Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang)
            });
        }
        return list.ToArray();
    }

    public string GetDocumentTypeText(DocumentTypeCode code, string lang)
    {
        return GetText(this.documentTypes.First(t => string.Compare(t.Code, this.documentTypeDict[code]) == 0).Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang);
    }

    public string GetRealEstateTypeText(RealEstateTypeCode code, string lang)
    {
        return GetText(this.realEstateTypes.First(t => string.Compare(t.Code, this.realEstateTypeDict[code]) == 0).Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang);
    }

    public string GetThemeText(string code, string lang)
    {
        string text = string.Empty;
        if (this.themes.Count(t => string.Compare(t.Code, code) == 0) > 0)
        {
            text = GetText(this.themes.First(t => string.Compare(t.Code, code) == 0).Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang);
        }
        return text;
    }

    public KeyValuePair<string, string> GetGlossary(string id, string lang)
    {
        TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationGlossar item = this.glossaries.First(g => string.Compare(g.TID, id) == 0);
        return new KeyValuePair<string, string>(GetText(item.Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang),
            GetText(item.Inhalt.LocalisationCH_V1MultilingualMText.LocalisedText, lang));
    }

    public KeyValuePair<string, string> GetDisclaimer(string id, string lang)
    {
        TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationHaftungshinweis item = this.disclaimers.First(g => string.Compare(g.TID, id) == 0);
        return new KeyValuePair<string, string>(GetText(item.Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang),
            GetText(item.Inhalt.LocalisationCH_V1MultilingualMText.LocalisedText, lang));
    }

    public KeyValuePair<string, string> GetInformation(string id, string lang)
    {
        TRANSFERDATASECTIONOeREBKRMkvs_V2_0KonfigurationOeREBKRMkvs_V2_0KonfigurationInformation item = this.informations.First(g => string.Compare(g.TID, id) == 0);
        return new KeyValuePair<string, string>(GetText(item.Titel.LocalisationCH_V1MultilingualText.LocalisedText, lang),
            GetText(item.Inhalt.LocalisationCH_V1MultilingualMText.LocalisedText, lang));
    }

    private string GetText(TRANSFERDATASECTIONOeREBKRMkvs_V2_0LocalisedText[] localisedText, string lang)
    {
        return localisedText.First(t => string.Compare(t.Language, lang) == 0).Text;
    }

    private string GetText(TRANSFERDATASECTIONOeREBKRMkvs_V2_0LocalisedMText[] localisedText, string lang)
    {
        return localisedText.First(t => string.Compare(t.Language, lang) == 0).Text;
    }

    private ExtractDataModel_v20.LocalisedText[] GetLocalisedText(OeREBKRMtrsfr_V2_0.LocalisedText[] localisedText, string lang)
    {
        return new ExtractDataModel_v20.LocalisedText[]
        {
            new ExtractDataModel_v20.LocalisedText
            {
                Language = SchemaHelper.GetLanguageCode(lang),
                Text = localisedText.First(t => string.Compare(t.Language, lang) == 0).Text
            }
        };
    }

    private ExtractDataModel_v20.LocalisedText[] GetLocalisedText(TRANSFERDATASECTIONOeREBKRMkvs_V2_0LocalisedText[] localisedText, string lang)
    {
        return new ExtractDataModel_v20.LocalisedText[]
        {
            new ExtractDataModel_v20.LocalisedText
            {
                Language = SchemaHelper.GetLanguageCode(lang),
                Text = localisedText.First(t => string.Compare(t.Language, lang) == 0).Text
            }
        };
    }

    private ExtractDataModel_v20.LocalisedUri[] GetLocalisedUri(OeREBKRMtrsfr_V2_0.LocalisedUri[] localisedUri, string lang)
    {
        return new ExtractDataModel_v20.LocalisedUri[]
        {
            new ExtractDataModel_v20.LocalisedUri
            {
                Language = SchemaHelper.GetLanguageCode(lang),
                Text = localisedUri.First(t => string.Compare(t.Language, lang) == 0).Text
            }
        };
    }

    private ExtractDataModel_v20.LocalisedUri[] GetLocalisedUri(TRANSFERDATASECTIONOeREBKRM_V2_0DokumenteOeREBKRM_V2_0AmtAmtAmtImWebOeREBKRM_V2_0MultilingualUriOeREBKRM_V2_0LocalisedUri[] localisedUri, string lang)
    {
        return new ExtractDataModel_v20.LocalisedUri[]
        {
            new ExtractDataModel_v20.LocalisedUri
            {
                Language = SchemaHelper.GetLanguageCode(lang),
                Text = localisedUri.First(t => string.Compare(t.Language, lang) == 0).Text
            }
        };
    }
}

public class OerebLawStatus
{
    public string Id { get; set; }
    public LawstatusCode Code { get; set; }
    public string Text { get; set; }
}