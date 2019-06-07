<?xml version="1.0" encoding="utf-8"?>
<!-- $Rev: 19053 $ -->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:template match="glossaryRoot">
        <head>
            <style type="text/css">
                html, body, table
                {
                font: 9pt Cadastra,Arial,Verdana,sans-serif;
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
                padding-bottom: 7mm;
                }
                td.entry
                {
                padding: 4px 0px;
                border: 1px dotted white;
                border-bottom-color: #999;
                }
                span.label
                {
                font-weight: bold;
                }
            </style>
        </head>
        <html>
            <body>                
                <div class="title">
                    Glossaire/Abréviations
                </div>
                <table>
                    <xsl:for-each select="glossary">
                        <tr>
                            <td class="entry">
                                <span class="label">
                                    <xsl:value-of select="title"/>
                                    <xsl:text>:</xsl:text>
                                </span>
                                <span>
                                    <xsl:value-of select="content"/>
                                </span>
                            </td>
                        </tr>
                    </xsl:for-each>
                </table>
            </body>
        </html>
    </xsl:template>
</xsl:stylesheet>