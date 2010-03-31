<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:fo="http://www.w3.org/1999/XSL/Format">
<xsl:output method="xml" encoding="utf-8" indent="yes" omit-xml-declaration="yes"/>
<!--Generates HTML table from XML export of word lists in Phonology Assistant Unicode version 3-->
<!-- Add title
min height 
min width
-->

<xsl:template match="/">
	<html>
		<head>
			<title>
				<xsl:value-of select="table/@language"/>
				<xsl:value-of select="table/@view"/>
				<xsl:text>Word List</xsl:text>
			</title>
			<meta http-equiv="Content-Type" content="text/html; charset=UTF-8"/>
			<style type="text/css">
				table {border-collapse: collapse;}
				td {border-style:solid; border-width:thin; border-color:black;font-family: Arial,sans-serif;}

				tr {min-height:30px;}
				td.colhead {font-family: Arial,sans-serif;}
				td.colhead {font-size: .8em;}
				td.colhead {font-weight: bold;}
				td.colhead {text-align: left;}
				td.colhead {padding: 3px 6px 3px 6px;}

				tr {min-height:20px;}

				td.groupheadtext {padding: 3px;}
				td.groupheadtext {border-width: 1px;}
				td.groupheadtext {background-color: rgb(230, 230, 230);}
				td.groupheadtext {border-color: rgb(153, 153, 153);}
				td.groupheadtext {font-weight: bold;}

				/* These two lines will be replaced by the font information for the field */
				/* on which a word list is grouped. If the word list is showing minimal   */
				/* pairs, then this will be the phonetic font information. Otherwise it   */
				/* will be the font information for the field on which the word list is   */
				/* sort primarily, since it is the primary sort field on which word lists */
				/* are grouped.                                                           */
				/*~~|td.groupheadtext {Group-Head-Font-Name-Goes-Here}|~~*/
				/*~~|td.groupheadtext {Group-Head-Font-Size-Goes-Here}|~~*/

				/* The following line is uncommented by the program when group heading   */
				/* rows are written with group child counts (i.e. elements whose class   */
				/* is groupheadcount) to the html files. If group headings don't include */
				/* counts (which is only the case if one column is written to the table) */
				/* then the groupheadertext element spans the entire table width and,    */
				/* therefore, it's right border is fine to be turned on.                 */
				/*==|td.groupheadtext {border-right-style: none;}|==*/

				td.groupheadcount {font-family: Arial,sans-serif;}
				td.groupheadcount {font-size: .8em;}
				td.groupheadcount {font-weight: bold;}
				td.groupheadcount {text-align: right;}
				td.groupheadcount {padding: 3px;}
				td.groupheadcount {background-color: rgb(230,230,230);}
				td.groupheadcount {border-color: rgb(153, 153, 153);}
				td.groupheadcount {border-width: 1px;}
				td.groupheadcount {border-left-style: none;}

				td.d {font-family: Arial,sans-serif;}
				td.d {font-size: 1.0em;}
				td.d {text-align: left;}
				td.d {width:1.5em;}
				td.d {height:1.5em;}
				td.d {border-width: 1px;}
				td.d {border-color: rgb(153, 153, 153);}
				td.d {padding: 3px 6px 3px 6px;}

				td.phbefore {border-right: none;}
				td.phbefore {text-align: right;}
				td.phbefore {border-width: 1px;}
				td.phbefore {border-color: rgb(153, 153, 153);}

				td.phtarget {text-align: center;}
				td.phtarget {background-color: rgb(230,230,230);}
				td.phtarget {border-left: none;}
				td.phtarget {border-right: none;}
				td.phtarget {border-width: 1px;}
				td.phtarget {border-color: rgb(153, 153, 153);}
				td.phtarget {width: 1.0em;}

				td.phafter {border-left: none;}
				td.phafter {border-width: 1px;}
				td.phafter {border-color: rgb(153, 153, 153);}

				/* To override the font sizes used by Phonology Assistant when exporting */
				/* word lists to HTML, replace the # between the square brackets with a  */
				/* numeric value which will be treated as an 'em' value. All the data,   */
				/* other than column and group heading data in the HTML output will use  */
				/* the 'em' value instead of the sizes specified in PA.                  */
				/*Do not delete the following line*/
				/*Alternate-Font-Size [#]*/

				/* The following is where Phonology Assistant will insert the style    */
				/* information for each table field in the HTML output of a word list. */
				/*Do not delete the following line*/
				/*Field-Settings-Go-Here*/
			</style>
		</head>
		<body>
			<xsl:apply-templates/>
		</body>
	</html>
</xsl:template>
<xsl:template match="table">
	<xsl:copy>
		<xsl:apply-templates select="@* | node()"/>
	</xsl:copy>
</xsl:template>
<xsl:template match="@* | node()">
	<xsl:copy>
		<xsl:apply-templates select="@class | @id | @rowspan | @colspan | node()"/>
	</xsl:copy>
</xsl:template>
</xsl:stylesheet>