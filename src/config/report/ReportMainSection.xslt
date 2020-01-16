<?xml version="1.0" encoding="utf-8"?>
<!-- $Rev: 22677 $ -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template match="mainSection">
    <head>
      <style type="text/css">
        /*
        testé sur impression: conversion pixels / mm: 1000/265
        7pt -> 9px -> 2.47mm
        9pt -> 12px -> 3.18mm
        15pt -> 20px -> 5.29mm
        18pt -> 24px -> 6.35mm
        */
        html, body, table
        {
        font: 8.5pt Cadastra;
        }
        table
        {
        width: 100%;
        border: 0px solid #c0c0c0;
        border-collapse: collapse;
        border-spacing: 0px;
        padding: 0px;
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
        td.separator
        {
        border: 0.07mm solid white;
        border-bottom-color: black;
        height: 2px;
        }
        td.label
        {
        padding-top: 7px;
        padding-bottom: 3px;
        width: 68mm;
        }
        td.value
        {
        padding-top: 7px;
        padding-bottom: 3px;
        width: 106mm;
        }
        td.page
        {
        padding-top: 7px;
        padding-bottom: 3px;
        width: 7mm;
        }
        td.pageTitle
        {
        padding-top: 7px;
        padding-bottom: 3px;
        width: 75mm;
        }
        td.annex
        {
        padding-top: 7px;
        padding-bottom: 3px;
        width: 8mm;
        }
        td.annexTitle
        {
        padding-top: 7px;
        padding-bottom: 3px;
        width: 84mm;
        }
        .textzone
        {
        font-size: 6.5pt;
        padding: 3px 0px 1px 0px;
        }
        a
        {
        color: rgb(76,143,186);
        text-decoration: none;
        }
        .link
        {
        color: rgb(76,143,186);
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
        <div style="height:8.5mm;"></div>
        <xsl:for-each select="datas/data">
          <xsl:choose>
            <xsl:when test="id='attributs'">
              <xsl:call-template name="Attributs"/>
            </xsl:when>
          </xsl:choose>
        </xsl:for-each>
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
            <td class="separator" colspan="2"></td>
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
            <td class="separator" colspan="2"></td>
          </tr>
          <tr>
            <td class="label" style="vertical-align:top;padding-top:0px">
              Organisme responsable du cadastre
            </td>
            <td class="value">
              <div style="padding: 0.5mm 0mm">Direction de l'information du territoire</div>
              <div style="padding: 0.5mm 0mm">Quai du Rhône 12, 1205 Genève</div>
            </td>
          </tr>
          <tr>
            <td class="separator" colspan="2"></td>
          </tr>
          <xsl:if test="type='SIGNED'">
            <tr>
              <td class="label">
                Emoluments: 50.-
              </td>
              <td class="value">
                Reproduction réservée
              </td>
            </tr>
            <tr>
              <td class="separator" colspan="2"></td>
            </tr>
          </xsl:if>
        </table>
        <xsl:if test="type='SIGNED'">
          <div style="height:32mm;"></div>
        </xsl:if>
        <xsl:if test="not(type='SIGNED')">
          <div style="height:38mm;"></div>
        </xsl:if>
        <div class="textzone">
          L'extrait est authentifié par son numéro d'enregistrement (identifiant ci-dessus) géré par la direction de l'information du territoire.
        </div>
        <p style="page-break-before: always">
          <div class="title">
            Sommaire des thèmes RDPPF
          </div>
          <xsl:for-each select="datas/data">
            <xsl:choose>
              <xsl:when test="id='attributs'">
                <xsl:for-each select="sections/section">
                  <xsl:choose>
                    <xsl:when test="id='parcelle'">
                      <xsl:for-each select="dataLayers/dataLayer[layer='CAD_PARCELLE_MENSU']">
                        <table>
                          <tr>
                            <td style="font-weight:bold;">
                              <xsl:text>Restrictions de droit public à la propriété foncière qui touchent l'immeuble </xsl:text>
                              <xsl:value-of select="fields/field[name='NO_PARCELLE']/value"/>
                              <xsl:text> de </xsl:text>
                              <xsl:value-of select="fields/field[name='NOMFECO']/value"/>
                            </td>
                          </tr>
                          <tr>
                            <td class="separator"></td>
                          </tr>
                          <tr>
                            <td style="height:3px"></td>
                          </tr>
                        </table>
                      </xsl:for-each>
                    </xsl:when>
                  </xsl:choose>
                </xsl:for-each>
              </xsl:when>
            </xsl:choose>
          </xsl:for-each>
          <table>
            <tr>
              <td class="page"></td>
              <td class="pageTitle"></td>
              <td class="annex"></td>
              <td class="annexTitle"></td>
            </tr>
            <xsl:choose>
              <xsl:when test="type='REDUCED'">
                <tr style="font-weight:bold; font-size:6.5pt/8.5pt">
                  <td colspan="4">Page</td>
                </tr>
                <tr>
                  <td colspan="4" style="height:2mm"></td>
                </tr>
                <xsl:for-each select="tocs/toc">
                  <tr>
                    <td class="page">
                      <xsl:value-of select="page"/>
                    </td>
                    <td colspan="3" class="pageTitle">
                      <xsl:value-of select="title"/>
                    </td>
                  </tr>
                  <tr>
                    <td class="separator" colspan="4"></td>
                  </tr>
                </xsl:for-each>
              </xsl:when>
              <xsl:otherwise>
                <tr>
                  <td colspan="2" style="font-weight:bold; font-size:6.5pt/8.5pt;">Page</td>
                  <td colspan="2" style="font-weight:bold; font-size:6.5pt/8.5pt;">Annexes</td>
                </tr>
                <tr>
                  <td colspan="4" style="height:2mm"></td>
                </tr>
                <xsl:for-each select="tocs/toc">
                  <tr>
                    <td class="page">
                      <xsl:value-of select="page"/>
                    </td>
                    <td class="pageTitle">
                      <xsl:value-of select="title"/>
                    </td>
                    <td colspan="2" style="padding:8px 0px 3px 0px;">
                      <xsl:if test="count(annexes/annex)>0">
                        <table>
                          <xsl:for-each select="annexes/annex">
                            <tr>
                              <td class="annex">
                                <xsl:text>A</xsl:text>
                                <xsl:value-of select="number"/>
                              </td>
                              <td class="annexTitle">
                                <xsl:value-of select="title"/>
                              </td>
                            </tr>
                          </xsl:for-each>
                        </table>
                      </xsl:if>
                    </td>
                  </tr>
                  <tr>
                    <td class="separator" colspan="4"></td>
                  </tr>
                </xsl:for-each>
              </xsl:otherwise>
            </xsl:choose>
          </table>
          <div style="height:10mm"></div>
          <table>
            <tr>
              <td style="font-weight:bold;">
                Restrictions de droit public à la propriété foncière qui ne touchent pas l'immeuble
              </td>
            </tr>
            <tr>
              <td class="separator"></td>
            </tr>
            <tr>
              <td style="height:4px"></td>
            </tr>
            <xsl:for-each select="restrictions/restriction">
              <xsl:if test="result='false'">
                <tr>
                  <td>
                    <xsl:value-of select="title"/>
                  </td>
                </tr>
              </xsl:if>
            </xsl:for-each>
          </table>
          <div style="height:9mm"></div>
          <table>
            <tr>
              <td style="font-weight:bold;">
                Restrictions de droit public à la propriété foncière pour lesquelles aucune donnée n'est disponible
              </td>
            </tr>
            <tr>
              <td class="separator"></td>
            </tr>
            <tr>
              <td style="height:4px"></td>
            </tr>
            <xsl:for-each select="noDataThemes/string">
              <tr>
                <td>
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
              <td style="width:82mm;vertical-align:top">
                <div class="textzone" style="font-weight:bold">
                  Informations générales
                </div>
                <xsl:for-each select="generalInfos/string">
                  <div class="textzone">
                    <xsl:value-of select="."/>
                  </div>
                </xsl:for-each>
                <div class="textzone" style="font-weight:bold;padding-top:2mm">
                  Données de base
                </div>
                <xsl:for-each select="baseData/string">
                  <div class="textzone">
                    <xsl:value-of select="."/>
                  </div>
                </xsl:for-each>
              </td>
              <td style="width:6mm">
                <xsl:text> </xsl:text>
              </td>
              <td style="width:86mm;vertical-align:top">
                <div class="textzone" style="font-weight:bold">
                  Clause de non-responsabilité du cadastre des sites pollués (CSP)
                </div>
                <div class="textzone">
                  Le cadastre des sites pollués (CSP) est établi d’après les critères émis par
                  l’Office fédéral de l’environnement OFEV. Il est mis à jour continuellement
                  sur la base des nouvelles connaissances (investigations).
                  Les surfaces des sites indiqués dans le cadastre des sites pollués peut
                  ne pas correspondre à la surface effectivement polluée. Cela ne signifie
                  pas que tout terrain non inscrit au cadastre ne soit pas pollué et libre
                  de tout déchet et pollution. Les zones utilisées à des fins de transports
                  publics, militaire et aéronautique sont de la responsabilité de la
                  Confédération. Pour de plus amples informations veuillez vous adresser
                  au service spécialisé cantonal des déchets: <span class="link">
                    https://www.ge.ch/organisation/ocev-service-geologie-sols-dechets
                  </span> / email: gesdec@etat.ge.ch
                </div>
                <div class="textzone" style="font-weight:bold;padding-top:2mm">
                  Clause de non-responsabilité – distance par rapport à la forêt
                </div>
                <div class="textzone">
                  La distance par rapport à la forêt est fixée sur la base de constatations
                  de nature forestière établies en vertu de l'article 4 de la loi cantonale
                  sur les forêts (LForêts, M 5 10). Ces décisions ponctuelles permettent de
                  compléter et mettre à jour le cadastre forestier, mais elles ne revêtent
                  pas de caractère systématique, ni exhaustif.
                  L'existence de la forêt étant dynamique et reliée uniquement à un état de
                  fait, indépendamment de l'origine et du mode d'exploitation des boisés,
                  cela implique qu'un boisé n'ayant pas fait l'objet d'un constat de nature
                  forestière peut être de la forêt au sens de la législation sur les forêts,
                  même si son contour géométrique n'a pas été déterminé précisément.
                  Dès lors, il convient de prendre en compte que l'absence d'inscription
                  d'une restriction de droit public relative à la distance par rapport à la
                  forêt, portée sur un bien-fonds, ne signifie pas que ce bien-fonds ne soit
                  pas concerné par cette restriction. Pour de plus amples informations,
                  veuillez consulter le service cantonal des forêts et vous référer, à titre
                  indicatif, au cadastre forestier
                </div>
              </td>
            </tr>
          </table>
        </p>
      </body>
    </html>
  </xsl:template>
  <xsl:template name="Attributs">
    <xsl:for-each select="sections/section">
      <xsl:choose>
        <xsl:when test="id='parcelle'">
          <xsl:for-each select="dataLayers/dataLayer[layer='CAD_PARCELLE_MENSU']">
            <table>
              <tr>
                <td class="label" style="font-weight:bold;">
                  No de l'immeuble
                </td>
                <td class="value" style="font-weight:bold;">
                  <xsl:value-of select="fields/field[name='NO_PARCELLE']/value"/>
                </td>
              </tr>
              <tr>
                <td class="separator" colspan="2"></td>
              </tr>
              <tr>
                <td class="label">
                  E-GRID
                </td>
                <td class="value">
                  <xsl:value-of select="fields/field[name='EGRID']/value"/>
                </td>
              </tr>
              <tr>
                <td class="separator" colspan="2"></td>
              </tr>
              <tr>
                <td class="label">
                  Commune (No OFS)
                </td>
                <td class="value">
                  <xsl:value-of select="fields/field[name='NOMFECO']/value"/>
                  <xsl:text> (</xsl:text>
                  <xsl:value-of select="fields/field[name='NUFECO']/value"/>
                  <xsl:text>)</xsl:text>
                </td>
              </tr>
              <tr>
                <td class="separator" colspan="2"></td>
              </tr>
              <tr>
                <td class="label">
                  Surface
                </td>
                <td class="value">
                  <xsl:value-of select="fields/field[name='SURFACE']/value"/>
                  <xsl:text disable-output-escaping="yes"> m&#178;</xsl:text>
                </td>
              </tr>
              <tr>
                <td class="separator" colspan="2"></td>
              </tr>
            </table>
          </xsl:for-each>
        </xsl:when>
      </xsl:choose>
    </xsl:for-each>
  </xsl:template>
</xsl:stylesheet>