<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:ns="http://www.want.ch/fillred">
  
  <xsl:template match="ns:LayoutDataset">
    <LayoutDataset xmlns="http://www.want.ch/fillred">
      <xsl:apply-templates select="ns:Record">
        <xsl:sort select="ns:Name"/>
      </xsl:apply-templates>
      <xsl:apply-templates select="ns:Field">
        <xsl:sort select="ns:RecordName"/>
        <xsl:sort select="ns:Index" data-type="number"/>
      </xsl:apply-templates>
    </LayoutDataset>
  </xsl:template>

  <!-- Not copying attributes here, use match="*|@*" if we ever need to -->
  <xsl:template match="*">
    <xsl:copy>
      <xsl:apply-templates/>
    </xsl:copy>
  </xsl:template>

</xsl:stylesheet>