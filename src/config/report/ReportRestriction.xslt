<?xml version="1.0" encoding="iso-8859-1"?>
<!-- $Rev: 25012 $ -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template match="restriction">
    <head>
      <style type="text/css">
        /*
        testé sur impression: conversion pixels / mm: 1000/265
        7pt -> 9px -> 2.4mm
        9pt -> 12px -> 3.2mm
        15pt -> 21px -> 5.6mm
        18pt -> 24px -> 6.4mm
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
        div.title
        {
        font-size: 15pt;
        font-weight: bold;
        }
        td.separator
        {
        border: 0.07mm solid white;
        border-bottom-color: black;
        height: 2px;
        }
        td.title
        {
        font-weight: bold;
        vertical-align: top;
        width: 68mm;
        }
        td.content
        {
        width: 106mm;
        }
        td.symbol
        {
        vertical-align: middle;
        width: 10mm;
        }
        td.label
        {
        padding-top: 1mm;
        width: 62mm;
        }
        td.surface
        {
        padding-top: 1mm;
        text-align: right;
        width: 17mm;
        }
        td.percent
        {
        padding-top: 1mm;
        text-align: right;
        width: 17mm;
        }
        a
        {
        color: rgb(76,143,186);
        text-decoration: none;
        }
        div.valueLink
        {
        padding: 1mm 0mm 1mm 3mm;
        }
      </style>
    </head>
    <html>
      <body>
        <div class="title">
          <xsl:value-of select="title"/>
        </div>
        <div style="{titleMarginStyle}"></div>
        <div>
          <img src="{mapUrl}" />
        </div>
        <div style="height:5mm"></div>
        <table>
          <xsl:choose>
            <xsl:when test="geometryType='esriGeometryPoint'">
              <tr>
                <td class="title"></td>
                <td class="content">
                  <table style="font-size:6.5pt/8.5pt">
                    <tr>
                      <td class="symbol"></td>
                      <td class="label">Type</td>
                    </tr>
                  </table>
                </td>
              </tr>
              <tr>
                <td class="separator" colspan="2"></td>
              </tr>
              <xsl:if test="count(legends/legend)>0">
                <tr>
                  <td class="title">
                    <div>Légende des objets touchés</div>
                  </td>
                  <td class="content">
                    <table>
                      <xsl:for-each select="legends/legend">
                        <tr>
                          <td class="symbol">
                            <img src="{imageUrl}" />
                          </td>
                          <td class="label">
                            <xsl:value-of select="label"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
                <tr>
                  <td class="separator" colspan="2"></td>
                </tr>
              </xsl:if>
            </xsl:when>
            <xsl:when test="geometryType='esriGeometryPolyline'">
              <tr>
                <td class="title"></td>
                <td class="content">
                  <table style="font-size:6.5pt/8.5pt">
                    <tr>
                      <td class="symbol"></td>
                      <td class="label">Type</td>
                      <td class="length">Part</td>
                    </tr>
                  </table>
                </td>
              </tr>
              <tr>
                <td class="separator" colspan="2"></td>
              </tr>
              <xsl:if test="count(legends/legend)>0">
                <tr>
                  <td class="title">
                    <div>Légende des objets touchés</div>
                  </td>
                  <td class="content">
                    <table>
                      <xsl:for-each select="legends/legend">
                        <tr>
                          <td class="symbol">
                            <img src="{imageUrl}" />
                          </td>
                          <td class="label">
                            <xsl:value-of select="label"/>
                          </td>
                          <td class="surface">
                            <xsl:value-of select="length"/>
                            <xsl:text> m</xsl:text>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
                <tr>
                  <td class="separator" colspan="2"></td>
                </tr>
              </xsl:if>
            </xsl:when>
            <xsl:otherwise>
              <tr>
                <td class="title"></td>
                <td class="content">
                  <table style="font-size:6.5pt/8.5pt">
                    <tr>
                      <td class="symbol"></td>
                      <td class="label">Type</td>
                      <td class="surface">Part</td>
                      <td class="percent">Part en %</td>
                    </tr>
                  </table>
                </td>
              </tr>
              <tr>
                <td class="separator" colspan="2"></td>
              </tr>
              <xsl:if test="count(legends/legend)>0">
                <tr>
                  <td class="title">
                    <div>Légende des objets touchés</div>
                  </td>
                  <td class="content">
                    <table>
                      <xsl:for-each select="legends/legend">
                        <tr>
                          <td class="symbol">
                            <img src="{imageUrl}" />
                          </td>
                          <td class="label">
                            <xsl:value-of select="label"/>
                          </td>
                          <td class="surface">
                            <xsl:value-of select="surface"/>
                            <xsl:text disable-output-escaping="yes"> m&#178;</xsl:text>
                          </td>
                          <td class="percent">
                            <xsl:value-of select="surfPercentFormatted"/>
                            <xsl:text>%</xsl:text>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </table>
                  </td>
                </tr>
                <tr>
                  <td class="separator" colspan="2"></td>
                </tr>
              </xsl:if>
            </xsl:otherwise>
          </xsl:choose>
          <tr>
            <td class="title">
              <div>Autre légende</div>
              <div>(visible dans le cadre du plan)</div>
            </td>
            <td class="content">
              <xsl:choose>
                <xsl:when test="count(otherLegends/legend)>0">
                  <table>
                    <xsl:for-each select="otherLegends/legend">
                      <tr>
                        <td class="symbol">
                          <img src="{imageUrl}" />
                        </td>
                        <td class="label" style="width:96mm">
                          <xsl:value-of select="label"/>
                        </td>
                      </tr>
                    </xsl:for-each>
                  </table>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>-</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>
          <tr>
            <td class="separator" colspan="2"></td>
          </tr>
          <xsl:if test="count(additionnalLegends/legend)>0">
            <tr>
              <td class="title">
                <div>Légende complémentaire</div>
                <div>(Couches complémentaires visibles dans le cadre du plan)</div>
              </td>
              <td class="content">
                <table>
                  <xsl:for-each select="additionnalLegends/legend">
                    <tr>
                      <td class="symbol">
                        <img src="{imageUrl}" />
                      </td>
                      <td class="label" style="width:96mm">
                        <xsl:value-of select="label"/>
                      </td>
                    </tr>
                  </xsl:for-each>
                </table>
              </td>
            </tr>
            <tr>
              <td class="separator" colspan="2"></td>
            </tr>
          </xsl:if>
          <tr>
            <td class="title">
              <div>Légende complète</div>
            </td>
            <td class="content">
              <a href="{legendLink}">
                <xsl:value-of select="legendLink"/>
              </a>
            </td>
          </tr>
          <tr>
            <td class="separator" colspan="2"></td>
          </tr>
        </table>
        <xsl:choose>
          <xsl:when test="breakAfterLegend='true'">
            <div style="page-break-before: always"></div>
          </xsl:when>
          <xsl:otherwise>
            <div style="height:6mm"></div>
          </xsl:otherwise>
        </xsl:choose>
        <table>
          <tr>
            <td class="title">
              <div>Dispositions juridiques</div>
            </td>
            <td class="content">
              <div style="height:2mm"></div>
              <xsl:choose>
                <xsl:when test="count(regulations/regulation)>0">
                  <table>
                    <xsl:for-each select="regulations/regulation">
                      <tr>
                        <td>
                          <div>
                            <xsl:value-of select="label"/>
                            <xsl:text>:</xsl:text>
                          </div>
                          <xsl:for-each select="values/string">
                            <div class="valueLink">
                              <xsl:choose>
                                <xsl:when test="starts-with(., 'http')">
                                  <a href="{.}">
                                    <xsl:value-of select="."/>
                                  </a>
                                </xsl:when>
                                <xsl:otherwise>
                                  <xsl:value-of select="."/>
                                </xsl:otherwise>
                              </xsl:choose>
                            </div>
                          </xsl:for-each>
                        </td>
                      </tr>
                    </xsl:for-each>
                  </table>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>-</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>
          <tr>
            <td class="separator" colspan="2"></td>
          </tr>
        </table>
        <xsl:if test="breakAfterRegulation='true'">
          <div style="page-break-before: always"></div>
        </xsl:if>
        <table>
          <tr>
            <td class="title">
              <div>Bases légales</div>
            </td>
            <td class="content">
              <div style="height:2mm"></div>
              <xsl:choose>
                <xsl:when test="count(laws/law)>0">
                  <table>
                    <xsl:for-each select="laws/law">
                      <tr>
                        <td>
                          <div>
                            <xsl:value-of select="title"/>
                            <xsl:text>:</xsl:text>
                          </div>
                          <div class="valueLink">
                            <a href="{link}">
                              <xsl:value-of select="link"/>
                            </a>
                          </div>
                        </td>
                      </tr>
                    </xsl:for-each>
                  </table>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>-</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>
          <tr>
            <td class="separator" colspan="2"></td>
          </tr>
        </table>
        <xsl:if test="breakAfterLaw='true'">
          <div style="page-break-before: always"></div>
        </xsl:if>
        <table>
          <tr>
            <td class="title">
              <div>Informations et renvois supplémentaires</div>
            </td>
            <td class="content">
              <div style="height:2mm"></div>
              <xsl:choose>
                <xsl:when test="count(informations/information)>0">
                  <table>
                    <xsl:for-each select="informations/information">
                      <tr>
                        <td>
                          <div>
                            <xsl:value-of select="label"/>
                            <xsl:text>:</xsl:text>
                          </div>
                          <xsl:for-each select="values/string">
                            <div class="valueLink">
                              <xsl:choose>
                                <xsl:when test="starts-with(., 'http')">
                                  <a href="{.}">
                                    <xsl:value-of select="."/>
                                  </a>
                                </xsl:when>
                                <xsl:otherwise>
                                  <xsl:value-of select="."/>
                                </xsl:otherwise>
                              </xsl:choose>
                            </div>
                          </xsl:for-each>
                        </td>
                      </tr>
                    </xsl:for-each>
                  </table>
                </xsl:when>
                <xsl:otherwise>
                  <xsl:text>-</xsl:text>
                </xsl:otherwise>
              </xsl:choose>
            </td>
          </tr>
          <tr>
            <td class="separator" colspan="2"></td>
          </tr>
        </table>
        <xsl:if test="breakAfterInfo='true'">
          <div style="page-break-before: always"></div>
        </xsl:if>
        <table>
          <tr>
            <td class="title">
              <div>Service compétent</div>
            </td>
            <td class="content">
              <div style="height:2mm"></div>
              <table>
                <tr>
                  <td>
                    <xsl:value-of select="service/name"/>
                    <xsl:if test="string-length(service/link)>0">
                      <xsl:text>: </xsl:text>
                      <div style="height:2mm"></div>
                      <div style="padding-left:3mm">
                        <a href="{service/link}">
                          <xsl:value-of select="service/link"/>
                        </a>
                      </div>
                      <div style="height:1mm"></div>
                    </xsl:if>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td class="separator" colspan="2"></td>
          </tr>
        </table>
        <xsl:if test="count(annexes/annex)>0">
          <xsl:if test="breakAfterService='true'">
            <div style="page-break-before: always"></div>
          </xsl:if>
          <table>
            <tr>
              <td class="title">
                <div>Annexes</div>
              </td>
              <td class="content">
                <div style="height:2mm"></div>
                <table>
                  <xsl:for-each select="annexes/annex">
                    <tr>
                      <td>
                        <xsl:text>A</xsl:text>
                        <xsl:value-of select="number"/>
                        <xsl:text>: </xsl:text>
                        <span>
                          <xsl:value-of select="title"/>
                        </span>
                      </td>
                    </tr>
                  </xsl:for-each>
                </table>
              </td>
            </tr>
            <tr>
              <td class="separator" colspan="2"></td>
            </tr>
          </table>
        </xsl:if>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>