<?xml version="1.0" encoding="utf-8"?>
<!-- $Rev: 30309 $ -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template match="mainSection">
    <head>
      <style type="text/css">
        /*
        1pt = 0.3529412mm
        */
        @page {
        size: A4 portrait;
        margin: 39mm 18mm 18mm 18mm;
        }
        html, body, table
        {
        font: 8pt Cadastra;
        }
        table
        {
        width: 100%;
        border-collapse: collapse;
        border-spacing: 0px;
        padding: 0px;
        }
        td
        {
        height: 5.5mm;
        vertical-align: middle;
        border: 0.07mm solid white;
        border-bottom-color: black;
        }
        div.mainTitle
        {
        font-size: 18pt;
        font-weight: bold;
        padding-bottom: 14mm;
        }
        div.title
        {
        font-size: 15pt;
        font-weight: bold;
        padding-bottom: 7mm;
        }
        td.label
        {
        width: 68mm;
        }
        td.value
        {
        width: 106mm;
        }
        td.page
        {
        width: 7mm;
        }
        td.simple
        {
        height: 3.6mm;
        vertical-align: middle;
        border: 0mm solid white;
        }
        .textzone
        {
        font-size: 6pt;
        padding: 3px 0px 1px 0px;
        }
        a
        {
        color: rgb(76,143,186);
        text-decoration: none;
        }
      </style>
    </head>
    <html>
      <body>
        <div class="mainTitle">
          Extrait du cadastre des restrictions de droit public à la propriété foncière (cadastre RDPPF)
        </div>
        <div>
          <img src="{mapUrl}" width="657" height="374" />
        </div>
        <div style="height:8mm;"></div>
        <table>
          <tr>
            <td class="label" style="font-weight:bold;">
              No de l'immeuble
            </td>
            <td class="value" style="font-weight:bold;">
              <xsl:value-of select="realEstate/number"/>
            </td>
          </tr>
          <tr>
            <td class="label">
              Type d'immeuble
            </td>
            <td class="value">
              <xsl:value-of select="realEstate/type"/>
            </td>
          </tr>
          <tr>
            <td class="label">
              E-GRID
            </td>
            <td class="value">
              <xsl:value-of select="realEstate/egrid"/>
            </td>
          </tr>
          <tr>
            <td class="label">
              Commune (No OFS)
            </td>
            <td class="value">
              <xsl:value-of select="realEstate/municipalityName"/>
              <xsl:text> (</xsl:text>
              <xsl:value-of select="realEstate/municipalityCode"/>
              <xsl:text>)</xsl:text>
            </td>
          </tr>
          <xsl:if test="string-length(realEstate/section)>0">
            <tr>
              <td class="label">
                Section
              </td>
              <td class="value">
                <xsl:value-of select="realEstate/section"/>
              </td>
            </tr>
          </xsl:if>
          <tr>
            <td class="label">
              Surface
            </td>
            <td class="value">
              <xsl:value-of select="realEstate/area"/>
              <xsl:text disable-output-escaping="yes"> m&#178;</xsl:text>
            </td>
          </tr>
          <tr>
            <td class="label">
              Etat de la mensuration officielle
            </td>
            <td class="value">
              <xsl:value-of select="realEstate/state"/>
            </td>
          </tr>
        </table>
        <div style="height:8mm;"></div>
        <table>
          <tr>
            <td class="label" style="font-weight:bold;">
              Identifiant de l'extrait
            </td>
            <td class="value" style="font-weight:bold;">
              <xsl:value-of select="reference"/>
            </td>
          </tr>
          <tr>
            <td class="label">
              Date de création de l'extrait
            </td>
            <td class="value">
              <xsl:value-of select="date"/>
            </td>
          </tr>
          <tr>
            <td class="label" style="vertical-align:top;padding-top:1mm;">
              Organisme responsable du cadastre
            </td>
            <td class="value" style="padding:1mm 0mm;">
              <div style="padding: 0.4mm 0mm">
                Direction de l'information du territoire
              </div>
              <div style="padding: 0.4mm 0mm">
                Quai du Rhône 12
              </div>
              <div style="padding: 0.4mm 0mm">
                1205 Genève
              </div>
              <div style="padding: 0.4mm 0mm">
                <a href="https://www.ge.ch/organisation/direction-information-du-territoire-dit">https://www.ge.ch/organisation/direction-information-du-territoire-dit</a>
              </div>
            </td>
          </tr>
        </table>
        <div style="height:12mm;"></div>
        <div class="textzone">
          L'extrait est authentifié par son numéro d'enregistrement (identifiant ci-dessus) géré par la direction de l'information du territoire.
        </div>
        <div class="title" style="page-break-before: always">
          Sommaire des thèmes RDPPF
        </div>
        <table>
          <tr>
            <td style="font-weight:bold;">
              <xsl:text>Restrictions de droit public à la propriété foncière qui touchent l’immeuble </xsl:text>
              <xsl:value-of select="realEstate/number"/>
              <xsl:text> de </xsl:text>
              <xsl:value-of select="realEstate/municipalityName"/>
            </td>
          </tr>
        </table>
        <table>
          <tr style="font-weight:bold; font-size:6pt">
            <td class="simple" colspan="2" style="font-weight:bold; font-size:6pt; padding-bottom: 1.3mm;">Page</td>
          </tr>
          <xsl:for-each select="tocs/toc">
            <tr>
              <td class="page">
                <xsl:value-of select="page"/>
              </td>
              <td>
                <xsl:value-of select="title"/>
              </td>
            </tr>
          </xsl:for-each>
        </table>
        <div style="height:8mm"></div>
        <table>
          <tr>
            <td style="font-weight:bold;">
              Restrictions de droit public à la propriété foncière qui ne touchent pas l'immeuble
            </td>
          </tr>
          <tr>
            <td class="simple" style="height:0.5mm"></td>
          </tr>
          <xsl:for-each select="restrictions/restriction">
            <xsl:if test="result='false'">
              <tr>
                <td class="simple">
                  <xsl:value-of select="title"/>
                </td>
              </tr>
            </xsl:if>
          </xsl:for-each>
        </table>
        <div style="height:8mm"></div>
        <table>
          <tr>
            <td style="font-weight:bold;">
              Restrictions de droit public à la propriété foncière pour lesquelles aucune donnée n'est disponible
            </td>
          </tr>
          <tr>
            <td class="simple" style="height:0.5mm"></td>
          </tr>
          <xsl:for-each select="noDataThemes/string">
            <tr>
              <td class="simple">
                <xsl:value-of select="."/>
              </td>
            </tr>
          </xsl:for-each>
        </table>
        <xsl:choose>
          <xsl:when test="breakAfterToc='true'">
            <div style="page-break-before: always"></div>
          </xsl:when>
          <xsl:otherwise>
            <div style="{marginStyle}"></div>
          </xsl:otherwise>
        </xsl:choose>
        <table>
          <tr>
            <td class="simple" style="width:82mm;vertical-align:top">
              <xsl:for-each select="generalInfos/InformationText">
                <div class="textzone" style="font-weight:bold">
                  <xsl:value-of select="Title"/>
                </div>
                <xsl:for-each select="Contents">
                  <div class="textzone" style="padding-bottom:2mm">
                    <xsl:value-of select="string"/>
                  </div>
                </xsl:for-each>
              </xsl:for-each>
            </td>
            <td class="simple" style="width:6mm">
              <xsl:text> </xsl:text>
            </td>
            <td class="simple" style="width:86mm;vertical-align:top">
              <div class="textzone" style="font-weight:bold">
                Clause de non-responsabilité du cadastre des sites pollués (CSP)
              </div>
              <div class="textzone">
                Le cadastre des sites pollués (CSP) est établi d'après les critères émis par l'Office fédéral de l'environnement OFEV. Il est mis à jour continuellement sur la base des nouvelles connaissances (investigations). Les surfaces des sites indiqués dans le cadastre des sites pollués peuvent ne pas correspondre à la surface effectivement polluée. Cela ne signifie pas que tout terrain non inscrit au cadastre ne soit pas pollué et qu'il soit libre de tout déchet et pollution. Les zones utilisées à des fins de transports publics, militaire et aéronautique sont de la responsabilité de la Confédération. Pour de plus amples informations veuillez vous adresser au service spécialisé cantonal des déchets: <a href="https://www.ge.ch/organisation/ocev-service-geologie-sols-dechets">https://www.ge.ch/organisation/ocev-service-geologie-sols-dechets</a> / email: gesdec@etat.ge.ch
              </div>
              <xsl:for-each select="disclaimers/InformationText">
                <div class="textzone" style="font-weight:bold;padding-top:2mm">
                  <xsl:value-of select="Title"/>
                </div>
                <xsl:for-each select="Contents">
                  <div class="textzone">
                    <xsl:value-of select="."/>
                  </div>
                </xsl:for-each>
              </xsl:for-each>
            </td>
          </tr>
        </table>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>