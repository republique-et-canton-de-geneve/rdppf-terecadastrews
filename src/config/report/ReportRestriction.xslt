<?xml version="1.0" encoding="iso-8859-1"?>
<!-- $Rev: 21379 $ -->
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
        font: 7pt Cadastra,Arial,Verdana,sans-serif;
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
        padding-bottom: 14mm;
        }
        td.separator
        {
        border: 1px dotted white;
        border-bottom-color: #999;
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
        height: 5mm;
        }
        td.label
        {
        width: 58mm;
        }
        td.length
        {
        text-align: right;
        width: 38mm;
        }
        td.surface
        {
        text-align: right;
        width: 21mm;
        }
        td.percent
        {
        text-align: right;
        width: 17mm;
        }
        span.link
        {
        color: rgb(76,143,186);
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
                  <table>
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
                  <table>
                    <tr>
                      <td class="symbol"></td>
                      <td class="label">Type</td>
                      <td class="length">Longueur</td>
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
                          <td class="length">
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
                  <table>
                    <tr>
                      <td class="symbol"></td>
                      <td class="label">Type</td>
                      <td class="surface">Surface</td>
                      <td class="percent">Part</td>
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
                            <xsl:value-of select="surfPercent"/>
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
          <xsl:if test="count(otherLegends/legend)>0">
            <tr>
              <td class="title">
                <div>Autre légende</div>
                <div>(visible dans le cadre du plan)</div>
              </td>
              <td class="content">
                <table>
                  <xsl:for-each select="otherLegends/legend">
                    <tr>
                      <td class="symbol">
                        <img src="{imageUrl}" />
                      </td>
                      <td>
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
                      <td>
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
        </table>
        <xsl:choose>
          <xsl:when test="breakAfterLegend='true'">
            <div style="page-break-before: always"></div>
          </xsl:when>
          <xsl:otherwise>
            <div style="height:4mm"></div>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:if test="count(regulations/regulation)>0">
          <table>
            <tr>
              <td class="title">
                <div>Dispositions juridiques</div>
              </td>
              <td class="content">
                <table>
                  <tr>
                    <td style="height:2mm"></td>
                  </tr>
                  <xsl:for-each select="regulations/regulation">
                    <tr>
                      <td>
                        <div>
                          <xsl:value-of select="label"/>
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
              </td>
            </tr>
            <tr>
              <td class="separator" colspan="2"></td>
            </tr>
          </table>
        </xsl:if>
        <xsl:if test="breakAfterRegulation='true'">
          <div style="page-break-before: always"></div>
        </xsl:if>
        <table>
          <xsl:if test="count(laws/law)>0">
            <tr>
              <td class="title">
                <div>Bases légales</div>
              </td>
              <td class="content">
                <table>
                  <tr>
                    <td style="height:2mm"></td>
                  </tr>
                  <xsl:for-each select="laws/law">
                    <tr>
                      <td>
                        <div>
                          <xsl:value-of select="title"/>
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
              </td>
            </tr>
            <tr>
              <td class="separator" colspan="2"></td>
            </tr>
          </xsl:if>
          <xsl:if test="count(informations/information)>0">
            <tr>
              <td class="title">
                <div>Informations et renvois supplémentaires</div>
              </td>
              <td class="content">
                <table>
                  <tr>
                    <td style="height:2mm"></td>
                  </tr>
                  <xsl:for-each select="informations/information">
                    <tr>
                      <td>
                        <div>
                          <xsl:value-of select="label"/>
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
              </td>
            </tr>
            <tr>
              <td class="separator" colspan="2"></td>
            </tr>
          </xsl:if>
          <tr>
            <td class="title">
              <div>Service compétent</div>
            </td>
            <td class="content">
              <table>
                <tr>
                  <td style="height:2mm"></td>
                </tr>
                <tr>
                  <td>
                    <xsl:value-of select="service/name"/>
                    <xsl:if test="string-length(service/link)>0">
                      <xsl:text>: </xsl:text>
                      <span class="link">
                        <xsl:value-of select="service/link"/>
                      </span>
                    </xsl:if>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
          <tr>
            <td class="separator" colspan="2"></td>
          </tr>
          <xsl:if test="count(annexes/annex)>0">
            <tr>
              <td class="title">
                <div>Annexes</div>
              </td>
              <td class="content">
                <table>
                  <tr>
                    <td style="height:2mm"></td>
                  </tr>
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
          </xsl:if>
        </table>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>