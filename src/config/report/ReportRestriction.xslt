<?xml version="1.0" encoding="iso-8859-1"?>
<!-- $Rev: 30309 $ -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template match="restriction">
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
        div.title
        {
        font-size: 15pt;
        font-weight: bold;
        padding-bottom: 2mm;
        }
        div.lawstatus
        {
        font-size: 8pt;
        font-weight: bold;
        padding-bottom: 6mm;
        }
        td.title
        {
        font-weight: bold;
        vertical-align: top;
        width: 68mm;
        padding: 1mm 0;
        border: 0.07mm solid white;
        border-bottom-color: black;
        }
        td.content
        {
        width: 106mm;
        max-width: 106mm;
        padding: 1mm 0;
        border: 0.07mm solid white;
        border-bottom-color: black;
        page-break-inside: avoid;
        }
        td.symbol
        {
        height: 4mm;
        vertical-align: middle;
        width: 10mm;
        }
        td.label
        {
        height: 4mm;
        vertical-align: middle;
        width: 62mm;
        }
        td.surface
        {
        height: 4mm;
        vertical-align: middle;
        text-align: right;
        width: 17mm;
        }
        td.percent
        {
        height: 4mm;
        vertical-align: middle;
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
        padding: 1mm 0mm 0mm 3mm;
        max-width: 103mm;
        word-break: break-all;
        }
      </style>
    </head>
    <html>
      <body>
        <div class="title">
          <xsl:value-of select="title"/>
        </div>
        <div class="lawstatus">
          <xsl:value-of select="lawStatus"/>
        </div>
        <div>
          <img src="{mapUrl}" width="657" height="374" />
        </div>
        <xsl:choose>
          <xsl:when test="string-length(title)>50">
            <div style="height:1mm"></div>
          </xsl:when>
          <xsl:otherwise>
            <div style="height:7mm"></div>
          </xsl:otherwise>
        </xsl:choose>
        <table>
          <tr>
            <td class="title"></td>
            <td class="content">
              <table>
                <tr>
                  <td class="symbol"></td>
                  <td class="label">Type</td>
                  <td class="surface">Type Part</td>
                  <td class="percent">Part en %</td>
                </tr>
              </table>
            </td>
          </tr>
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
                    <xsl:choose>
                      <xsl:when test="geometryType='esriGeometryPolygon'">
                        <td class="surface">
                          <xsl:value-of select="surface"/>
                          <xsl:text disable-output-escaping="yes"> m&#178;</xsl:text>
                        </td>
                        <td class="percent">
                          <xsl:value-of select="surfPercentFormatted"/>
                          <xsl:text>%</xsl:text>
                        </td>
                      </xsl:when>
                      <xsl:when test="geometryType='esriGeometryPolyline'">
                        <td class="surface">
                          <xsl:value-of select="length"/>
                          <xsl:text disable-output-escaping="yes"> m</xsl:text>
                        </td>
                        <td class="percent">
                          <xsl:text>-</xsl:text>
                        </td>
                      </xsl:when>
                      <xsl:otherwise>
                        <td class="surface">
                          <xsl:value-of select="points"/>
                          <xsl:text disable-output-escaping="yes"> éléments</xsl:text>
                        </td>
                        <td class="percent">
                          <xsl:text>-</xsl:text>
                        </td>
                      </xsl:otherwise>
                    </xsl:choose>
                  </tr>
                </xsl:for-each>
                <xsl:for-each select="additionalLegends/legend">
                  <tr>
                    <td class="symbol">
                      <img src="{imageUrl}" />
                    </td>
                    <td class="label">
                      <xsl:value-of select="label"/>
                    </td>
                    <td class="surface">
                      <xsl:text></xsl:text>
                    </td>
                    <td class="percent">
                      <xsl:text></xsl:text>
                    </td>
                  </tr>
                </xsl:for-each>
              </table>
            </td>
          </tr>
          <tr>
            <td class="title">
              <div>Autre légende (visible dans le cadre du plan)</div>
            </td>
            <td class="content">
              <xsl:choose>
                <xsl:when test="count(otherLegends/legend)>0 or count(additionalLegendsOnMap/legend)>0">
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
                    <xsl:for-each select="additionalLegendsOnMap/legend">
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
        </table>
        <table>
          <tr>
            <td class="title">
              <div>Dispositions juridiques</div>
            </td>
            <td class="content">
              <xsl:choose>
                <xsl:when test="count(legalProvisions/legalProvision)>0">
                  <table>
                    <xsl:for-each select="legalProvisions/legalProvision">
                      <tr>
                        <td>
                          <div>
                            <xsl:value-of select="label"/>
                            <xsl:if test="count(values/string)>0">
                              <xsl:text>:</xsl:text>
                            </xsl:if>
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
            <td class="title">
              <div>Bases légales</div>
            </td>
            <td class="content">
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
            <td class="title">
              <div>
                Informations et renvois<br />supplémentaires
              </div>
            </td>
            <td class="content">
              <xsl:choose>
                <xsl:when test="count(hints/hint)>0">
                  <table>
                    <xsl:for-each select="hints/hint">
                      <tr>
                        <td>
                          <div>
                            <xsl:value-of select="label"/>
                            <xsl:if test="count(values/string)>0">
                              <xsl:text>:</xsl:text>
                            </xsl:if>
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
            <td class="title">
              <div>Service compétent</div>
            </td>
            <td class="content">
              <table>
                <tr>
                  <td>
                    <xsl:value-of select="service/name"/>
                    <xsl:if test="string-length(service/link)>0">
                      <xsl:text>: </xsl:text>
                      <div class="valueLink">
                        <a href="{service/link}">
                          <xsl:value-of select="service/link"/>
                        </a>
                      </div>
                    </xsl:if>
                  </td>
                </tr>
              </table>
            </td>
          </tr>
        </table>
      </body>
    </html>
  </xsl:template>
</xsl:stylesheet>