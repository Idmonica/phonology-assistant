﻿<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <!-- phonology_project_to_CV_chart_2.xsl 2010-03-25 -->

	<xsl:output method="xml" version="1.0" encoding="UTF-8" omit-xml-declaration="no" indent="no" />

	<xsl:variable name="colgroupFeatures" select="/inventory/colgroupFeatures" />
	<xsl:variable name="rowgroupFeatures" select="/inventory/rowgroupFeatures" />
	<xsl:variable name="units" select="/inventory/units" />

	<!-- Copy all attributes and nodes, and then define more specific template rules. -->
	<xsl:template match="@*|node()">
		<xsl:copy>
			<xsl:apply-templates select="@*|node()" />
		</xsl:copy>
	</xsl:template>

	<xsl:template match="inventory">
		<!-- The attributes are not essential for the PhoneChart element. -->
		<PhoneChart>
			<ColHeadings>
				<xsl:apply-templates select="colgroupFeatures/feature" />
			</ColHeadings>
			<RowHeadings>
				<xsl:apply-templates select="rowgroupFeatures/feature" />
			</RowHeadings>
			<Phones>
				<xsl:apply-templates select="units/unit" />
			</Phones>
		</PhoneChart>
	</xsl:template>

	<xsl:template match="colgroupFeatures/feature">
		<xsl:variable name="featureName">
			<xsl:choose>
				<xsl:when test=". = 'Front' and $units/unit[articulatoryFeatures/feature[. = 'Near-front']]">
					<xsl:value-of select="'Front or Near-front'" />
				</xsl:when>
				<xsl:when test=". = 'Back' and $units/unit[articulatoryFeatures/feature[. = 'Near-back']]">
					<xsl:value-of select="'Back or Near-back'" />
				</xsl:when>
				<xsl:otherwise>
					<xsl:value-of select="." />
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>
		<!-- The SubHeadingsVisible and Group attributes are not essential. -->
		<!--
		<Heading text="{$featureName}" SubHeadingsVisible="false" Group="0">
		-->
		<Heading text="{$featureName}">
			<SubHeading />
			<SubHeading />
		</Heading>
	</xsl:template>

	<xsl:template match="rowgroupFeatures/feature">
		<xsl:variable name="featureName" select="name" />
		<xsl:variable name="group" select="@group" />
		<!-- The SubHeadingsVisible attribute is not essential but the Group is. -->
		<!--
		<Heading text="{$featureName}" SubHeadingsVisible="false" Group="{$group}">
		-->
		<Heading text="{$featureName}" Group="{$group}">
			<xsl:for-each select="row">
				<SubHeading />
			</xsl:for-each>
		</Heading>
	</xsl:template>

	<xsl:template match="units/unit">
		<xsl:variable name="literal" select="@literal" />
		<xsl:variable name="rowgroupFeature" select="keys/chartKey[@class = 'rowgroup']" />
		<xsl:variable name="group" select="$rowgroupFeatures/feature[name = $rowgroupFeature]/@group" />
		<xsl:variable name="rowKey" select="keys/chartKey[@class = 'row']" />
		<xsl:variable name="row" select="count($rowgroupFeatures/feature[name = $rowgroupFeature]/row[. = $rowKey]/preceding::row)" />
		<xsl:variable name="colgroupFeature" select="keys/chartKey[@class = 'colgroup']" />
		<xsl:variable name="colgroup" select="number($colgroupFeatures/feature[. = $colgroupFeature]/@column)" />
		<xsl:variable name="col" select="keys/chartKey[@class = 'col']" />
		<!-- The Visible attributes is not essential. -->
		<!-- An empty SiblingUncertainties child element is not essential. -->
		<!--
		<Phone text="{$literal}" Visible="true" Row="{$row}" Column="{$colgroup + $col}" Group="{$group}" />
		-->
		<Phone text="{$literal}" Row="{$row}" Column="{$colgroup + $col}" Group="{$group}" />
	</xsl:template>

</xsl:stylesheet>