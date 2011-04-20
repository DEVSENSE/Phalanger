<?xml version="1.0" encoding="iso-8859-1"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" 
              version="1.0" 
              encoding="UTF-8" 
              indent="yes" 
              omit-xml-declaration="no"
              doctype-system="module.dtd"/>

  <xsl:template match="/">
    <xsl:apply-templates select="module"/>
  </xsl:template>

  <xsl:template match="module">
    <xsl:element name="module">

      <xsl:if test="@earlyInit != 'false'">
        <xsl:attribute name="earlyInit">
          <xsl:value-of select="@earlyInit" />
        </xsl:attribute>
      </xsl:if>

      <xsl:apply-templates select="class">
        <xsl:sort select="@name"/>
      </xsl:apply-templates>
      
      <xsl:apply-templates select="function">
        <xsl:sort select="@name"/>
      </xsl:apply-templates>

    </xsl:element>
  </xsl:template>

  <xsl:template match="class">
    <xsl:element name="class">

      <xsl:if test="@arrayGetter != ''">
        <xsl:attribute name="arrayGetter">
          <xsl:value-of select="@arrayGetter" />
        </xsl:attribute>
      </xsl:if>

      <xsl:if test="@arraySetter != ''">
        <xsl:attribute name="arraySetter">
          <xsl:value-of select="@arraySetter" />
        </xsl:attribute>
      </xsl:if>

      <xsl:attribute name="name">
        <xsl:value-of select="@name" />
      </xsl:attribute>


      <xsl:if test="@description != ''">
        <xsl:attribute name="description">
          <xsl:value-of select="@description" />
        </xsl:attribute>
      </xsl:if>

      <xsl:apply-templates select="function">
        <xsl:sort select="@name"/>
      </xsl:apply-templates>

    </xsl:element>
  </xsl:template>


  <xsl:template match="function">

    <xsl:element name="function">
      
      <xsl:if test="@castToFalse != 'false'">
        <xsl:attribute name="castToFalse">
          <xsl:value-of select="@castToFalse" />
        </xsl:attribute>
      </xsl:if>

      <xsl:if test="@marshalBoundVars != 'none'">
        <xsl:attribute name="marshalBoundVars">
          <xsl:value-of select="@marshalBoundVars" />
        </xsl:attribute>
      </xsl:if>

      <xsl:if test="@static != 'false'">
        <xsl:attribute name="static">
          <xsl:value-of select="@static" />
        </xsl:attribute>
      </xsl:if>
      
      <xsl:attribute name="returnType">
        <xsl:value-of select="@returnType" />
      </xsl:attribute>

      <xsl:attribute name="name">
        <xsl:value-of select="@name" />
      </xsl:attribute>

      <xsl:if test="@description != ''">
        <xsl:attribute name="description">
          <xsl:value-of select="@description" />
        </xsl:attribute>
      </xsl:if>

      <xsl:apply-templates select="param"/>
      <xsl:apply-templates select="alias"/>

    </xsl:element>

  </xsl:template>

  <xsl:template match="param">

    <xsl:element name="param">

      <xsl:if test="@bind != 'false'">
        <xsl:attribute name="bind">
          <xsl:value-of select="@bind" />
        </xsl:attribute>
      </xsl:if>
      
      <xsl:if test="@optional != 'false'">
        <xsl:attribute name="optional">
          <xsl:value-of select="@optional" />
        </xsl:attribute>
      </xsl:if>
      
      <xsl:attribute name="type">
        <xsl:value-of select="@type" />
      </xsl:attribute>

      <xsl:if test="@direction != 'in'">
        <xsl:attribute name="direction">
          <xsl:value-of select="@direction" />
        </xsl:attribute>
      </xsl:if>
      
      <xsl:attribute name="name">
        <xsl:value-of select="@name" />
      </xsl:attribute>
   

    </xsl:element>

  </xsl:template>


  <xsl:template match="alias">
    <xsl:element name="alias">
      <xsl:attribute name="name">
        <xsl:value-of select="@name" />
      </xsl:attribute>
    </xsl:element>
  </xsl:template>
  
</xsl:stylesheet>
