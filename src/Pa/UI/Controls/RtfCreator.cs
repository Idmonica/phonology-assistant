// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005-2015, SIL International.
// <copyright from='2005' to='2015' company='SIL International'>
//		Copyright (c) 2005-2015, SIL International.
//    
//		This software is distributed under the MIT License, as specified in the LICENSE.txt file.
// </copyright> 
#endregion
// 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using L10NSharp;
using SIL.Pa.Model;
using SilTools;

namespace SIL.Pa.UI.Controls
{
	public class RtfCreator
	{
		// Enum's
		public enum ExportFormat { Table, TabDelimited };
		public enum ExportTarget { File, FileAndOpen, Clipboard };
		public enum CreateSearchItemTabs { FirstTab, SecondTab, FinishedTabs, Stop };

		private readonly ExportFormat m_exportFormat;
		private readonly ExportTarget m_exportTarget;
		private CreateSearchItemTabs m_createSearchItemTabs;

		// Constants
		private const string khdr = @"{\rtf1\ansi\deff0";
		private const string kline = @"{\line}";
		// The first 3 markups in 'kcellLine' draw a line under the column headers
		private const string kcellLine = @"\clbrdrb\brdrs\brdrw30\clvertalb\cellx{0}";
		private const string kcell = @"\cellx{0}\clvertalc";
		private const string ktxcell = @"\tx{0}";
		private const string kcellHdr = @"{0}{{\cell}}";
		private const string kcellValues = @"\f{0} \fs{1} {2}{{\cell}}";
		private const string ktab = @"{\tab}";
		private const string ktxHdr = @"{0}{{\tx}}";
		private const string ktxValues = @"\f{0} \fs{1} {2}{{\tab}}";
		
		// I was using the \highlight control word but, for some reason, OpenOffice.org
		// Writer doesn't recognize it. But it does recognize \chcbpat. Go figure.
		private const string khighlight = @"{{\chcbpat{0} {1}}}";
		private const string kparagraph = @"\par\pard";
		private const int kTwipsPerInch = 1440;
		private const int kTwipsPerCm = 567;
		private const int kheadingFontSize = 10;
		private const int kGroupHdgRowToken = 9999;
		private const int kGapBetweenSrchItemAndPrecedingEnv = 28;

		// This is a little extra padding given to the phonetic column when it
		// is for a search result word list and the output is to an RTF table.
		private const int kExtraTwipsForPhoneticSrchRsltCol = 20;

		// Member Variables
		private int m_numberOfRecords;
		private int m_uiFontNumber;
		private readonly float m_pixelsPerInch;
		private readonly SortedList<int, DataGridViewColumn> m_sortedColumns;
		private Dictionary<string, int> m_fontSizes = new Dictionary<string, int>();
		private readonly Dictionary<string, int> m_fontNumbers = new Dictionary<string, int>();
		private readonly Dictionary<int, string> m_alignedSearchItems = new Dictionary<int, string>();
		
		// Each dict represents a row. The key is the column index & the value is the cell's value
		private readonly List<Dictionary<int, object[]>> m_wordListRows = new List<Dictionary<int, object[]>>();

		// The key is the column index and the value is the width of the widest piece
		// of text found by scanning the contents of all the cells in the column.
		private readonly Dictionary<int, int> m_maxFieldWidths = new Dictionary<int, int>();

		private int m_paperWidth;
		private int m_paperHeight;
		private int m_pageWidth;
		private int m_leftMargin;
		private int m_rightMargin;
		private int m_topMargin;
		private int m_bottomMargin;
		
		private float m_columnStartPoint;
		private enum ArrayDataType { GroupingFieldName, RecordIndex, SilHierarchicalGridRow };
		private Dictionary<int, object[]> m_rowValues;
		private Font m_phoneticColFont;
		private int m_phoneticColIndex;
		private int m_beforeEnvTwipWidth = 1;
		private int m_maxSrchItemAftEnvTwipsWidth;
		private StringBuilder m_tabFormatBldr = new StringBuilder();
		private readonly PaProject m_project;
		private readonly string m_rtfEditor = string.Empty;
		private readonly StringBuilder m_rtfBldr;
		private readonly WordListCache m_cache;
		private readonly DataGridView m_grid;
		private readonly Graphics m_graphics;
		private readonly int m_searchItemColorRefNumber;
		private readonly StringBuilder m_cellFormatBldr = new StringBuilder();
		private readonly StringBuilder m_cellLineFormatBldr = new StringBuilder();
		private readonly int m_colRightPadding;

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// RtfCreator constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RtfCreator(PaProject project, ExportTarget target, ExportFormat format,
			DataGridView grid, WordListCache cache, string rtfEditor)
		{
			m_project = project;
			m_exportTarget = target;
			m_exportFormat = format;
			m_grid = grid;

			if (m_grid != null)
			{
				m_graphics = m_grid.CreateGraphics();

				// Store the dots per inch value from the grid's graphic object
				m_pixelsPerInch = m_graphics.DpiX;
			}

			m_cache = cache;
			m_rtfEditor = rtfEditor;
			m_createSearchItemTabs = CreateSearchItemTabs.FirstTab;
			m_rtfBldr = new StringBuilder();

			// Default value is 1/8" gap between columns.
			m_colRightPadding = Properties.Settings.Default.RTFExportGapBetweenColumns;

			// Add support for highlighting the search item
			if (m_cache.IsForSearchResults)
			{
				Dictionary<int, int> colorReferences;
				RtfHelper.ColorTable(Properties.Settings.Default.QuerySearchItemBackColor, out colorReferences);
				m_searchItemColorRefNumber = colorReferences[Properties.Settings.Default.QuerySearchItemBackColor.ToArgb()];
			}

			// Sort the visible columns by their display order.
			m_sortedColumns = new SortedList<int, DataGridViewColumn>();
			foreach (DataGridViewColumn col in m_grid.Columns)
			{
				if (col.Visible && !(col is SilHierarchicalGridColumn))
					m_sortedColumns[col.DisplayIndex] = col;
			}

			GetPaperAndMarginValues();
			CalculateColumnWidths();
			OutputRTFHeadingStuff();

			// This is only for running tests
			if (m_maxFieldWidths.Count > 0)
			{
				MakeFinalWidthAdjustments();
				m_rtfBldr.AppendFormat(@"\paperw{0}\paperh{1}", m_paperWidth, m_paperHeight);
				m_rtfBldr.AppendFormat(@"\margl{0}\margr{1}", m_leftMargin, m_rightMargin);
				m_rtfBldr.AppendFormat(@"\margt{0}\margb{1}", m_topMargin, m_bottomMargin);
				m_rtfBldr.AppendLine();

				OutputReportHeadingInformation();
				OutputColumnInformation();
				OutputDataRows();
				WriteToFileOrClipboard();
			}

			if (m_graphics != null)
				m_graphics.Dispose();
		}

		/// ------------------------------------------------------------------------------------
		private void GetPaperAndMarginValues()
		{
			var paperSize = Properties.Settings.Default.RTFExportPaperSize.ToLower();

			m_paperWidth = (int)(paperSize == "a4" ? kTwipsPerCm * 21 : kTwipsPerInch * 8.5);
			m_paperHeight = (int)(paperSize == "a4" ? kTwipsPerCm * 29.7 : kTwipsPerInch * 11);

			int defaultHMargin = (int)(paperSize == "a4" ? kTwipsPerCm * 1.5 : kTwipsPerInch * 0.75);
			int defaultVMargin = (int)(paperSize == "a4" ? kTwipsPerCm * 1.5 : kTwipsPerInch);

			m_leftMargin = (Properties.Settings.Default.RTFExportLeftMargin < 0 ? defaultHMargin :
				Properties.Settings.Default.RTFExportLeftMargin);

			m_rightMargin = (Properties.Settings.Default.RTFExportRightMargin < 0 ? defaultHMargin :
				Properties.Settings.Default.RTFExportRightMargin);

			m_topMargin = (Properties.Settings.Default.RTFExportTopMargin < 0 ? defaultVMargin :
				Properties.Settings.Default.RTFExportTopMargin);

			m_bottomMargin = (Properties.Settings.Default.RTFExportBottomMargin < 0 ? defaultVMargin :
				Properties.Settings.Default.RTFExportBottomMargin);

			m_pageWidth = m_paperWidth - (m_leftMargin + m_rightMargin);
		}

		/// ------------------------------------------------------------------------------------
		private int TextWidthInTwips(string text, Font fnt)
		{
			var flags = TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix;

			if (m_exportFormat == ExportFormat.TabDelimited)
				flags |= TextFormatFlags.SingleLine;

			int textWidth = TextRenderer.MeasureText(m_graphics, text, fnt, Size.Empty, flags).Width;
			return (int)((textWidth / m_pixelsPerInch) * kTwipsPerInch);
		}

		#endregion

		#region Methods for calculating field widths and table column widths
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate the maximum column widths.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CalculateColumnWidths()
		{
			// For testing
			if (m_grid == null)
				return;

			m_maxFieldWidths.Clear();

			// Calculate the maximum width of the header columns
			foreach (DataGridViewColumn col in m_sortedColumns.Values)
				CalculateWidestColumnHeadingText(col);

			// Go through the rows and save the widest text found for each column.
			foreach (DataGridViewRow row in m_grid.Rows)
			{
				if (!row.Visible)
					continue;

				m_rowValues = new Dictionary<int, object[]>();
				SilHierarchicalGridRow shgrow = row as SilHierarchicalGridRow;

				// Check if the row is a group heading row.
				if (shgrow != null)
				{
					if (shgrow.Expanded)
					{
						m_rowValues.Add(kGroupHdgRowToken, new object[] { shgrow.Text, row.Index, row });
						m_wordListRows.Add(m_rowValues);
					}
				}
				else
				{
					m_numberOfRecords++;

					// Calculate the maximum width of the fields in the cells in each column
					foreach (DataGridViewColumn col in m_sortedColumns.Values)
					{
						if (row.Cells[col.Index].Value == null)
							m_rowValues.Add(col.Index, new object[] { string.Empty, row.Index, null });
						else
							CheckWidthOfCellValue(row.Cells[col.Index]);
					}

					m_wordListRows.Add(m_rowValues);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Goes through each of the grid's rows and determines which field in those rows is
		/// the widest in the specified column.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CalculateWidestColumnHeadingText(DataGridViewColumn column)
		{
			var field = m_project.GetFieldForName(column.Name);

			using (Font fnt = new Font(FontHelper.UIFont.FontFamily, kheadingFontSize,
				FontStyle.Bold, GraphicsUnit.Point))
			{
				var text = new StringBuilder(column.HeaderText);
				
				// If the heading has a space in it, then insert a newline at the last
				// space and get the width necessary for the heading when it wraps on
				// the last word in the heading.
				if (m_exportFormat == ExportFormat.Table)
				{
					int i = column.HeaderText.LastIndexOf(' ');
					if (i > 0)
						text[i] = '\n';
				}

				m_maxFieldWidths[column.Index] = TextWidthInTwips(text.ToString(), fnt);

				if (field.Type == FieldType.Phonetic)
				{
					m_phoneticColFont = field.Font;
					m_phoneticColIndex = column.Index;

					// If we're calculating the column width for the phonetic column and
					// it's for a search result word list, then add in the gap between the
					// preceding environment and the search item.
					if (m_cache.IsForSearchResults)
						m_maxFieldWidths[column.Index] += kGapBetweenSrchItemAndPrecedingEnv;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculates the width of the phonetic search result for the specified cache entry.
		/// Then it compares it to those already saved up to this point and saves it if it's
		/// wider.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CalculatePhoneticSrchResultWidth(WordListCacheEntry cacheEntry)
		{
			int beforeEnvTextWidth = TextWidthInTwips(cacheEntry.EnvironmentBefore, m_phoneticColFont) +
				kExtraTwipsForPhoneticSrchRsltCol;

			string srchItemAftEnv = " " + cacheEntry.SearchItem + cacheEntry.EnvironmentAfter;
			int srchItemAftEnvTextWidth = TextWidthInTwips(srchItemAftEnv, m_phoneticColFont);
			m_beforeEnvTwipWidth = Math.Max(m_beforeEnvTwipWidth, beforeEnvTextWidth);
			m_maxSrchItemAftEnvTwipsWidth = Math.Max(m_maxSrchItemAftEnvTwipsWidth, srchItemAftEnvTextWidth);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Measures the text in the specified cell and saves it if it's longer than the width
		/// currently considered to be the maximum width.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CheckWidthOfCellValue(DataGridViewCell cell)
		{
			int textWidth = 0;
			string cellValue = cell.Value.ToString();
			int colIndex = cell.ColumnIndex;

			Font columnFont =
				(m_grid.Columns[colIndex].DefaultCellStyle.Font ?? FontHelper.UIFont);

			var field = m_project.GetFieldForName(m_grid.Columns[colIndex].Name);

			// Are we looking at a phonetic cell for a search result word list?
			if (field.Type == FieldType.Phonetic && m_cache.IsForSearchResults)
			{
				var row = m_grid.Rows[cell.RowIndex] as PaCacheGridRow;
				if (row != null)
				{
					// If the environment before ends with a space, change it to a non breaking
					// space because a regular space messes up the tab location of the search
					// item and the environment after.
					string envBefore = m_cache[row.CacheEntryIndex].EnvironmentBefore;
                    if (envBefore != null && envBefore.EndsWith(" ", StringComparison.Ordinal))
						envBefore = envBefore.Substring(0, envBefore.Length - 1) + '\u00A0';

					m_alignedSearchItems[cell.RowIndex] = ktab + envBefore + ktab +
						string.Format(khighlight, m_searchItemColorRefNumber,
						m_cache[row.CacheEntryIndex].SearchItem) +
						m_cache[row.CacheEntryIndex].EnvironmentAfter;

					CalculatePhoneticSrchResultWidth(m_cache[row.CacheEntryIndex]);
				}

				textWidth = (m_beforeEnvTwipWidth + m_maxSrchItemAftEnvTwipsWidth) +
					kGapBetweenSrchItemAndPrecedingEnv + kExtraTwipsForPhoneticSrchRsltCol;
			}
			else
			{
				// Only display the FileName of the audio file path
				// when the export format is TabDelimited
				if (m_exportFormat == ExportFormat.TabDelimited && field.Type == FieldType.AudioFilePath)
					cellValue = Path.GetFileName(cellValue);

				textWidth = TextWidthInTwips(cellValue, columnFont);
			}

			// Update the max column length if the cell text width is greater
			m_maxFieldWidths[colIndex] = Math.Max(textWidth, m_maxFieldWidths[colIndex]);
			m_rowValues[colIndex] = new object[] { cellValue, cell.RowIndex, null };
		}

		#endregion

		#region Export Rtf
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output the RTF header, font table, color table, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OutputRTFHeadingStuff()
		{
			m_rtfBldr.AppendLine(khdr);
			m_rtfBldr.AppendLine(RtfHelper.FontTable(m_fontNumbers, ref m_uiFontNumber));

			// Add color support
			if (m_cache.IsForSearchResults)
			{
				Dictionary<int, int> colorReferences;
				m_rtfBldr.AppendLine(RtfHelper.ColorTable(
					Properties.Settings.Default.QuerySearchItemBackColor, out colorReferences));
			}

			m_rtfBldr.AppendLine(@"\pard\plain ");
			m_rtfBldr.AppendFormat(ktxcell, 2160);
			m_rtfBldr.AppendLine();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output the report's heading information (e.g. project name, language name,
		/// date, etc.).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OutputReportHeadingInformation()
		{
			// For testing
			if (m_cache == null)
				return;

			m_rtfBldr.AppendFormat(@"\f{0} \fs18{{\b ", m_uiFontNumber);

			// SearchQuery is null when showing "Minimal Pairs"
			if (m_cache.IsForSearchResults && m_cache.SearchQuery != null)
			{
			    string fmt = (m_cache.IsMinimalPair
			        ? LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.SearchPatternForMinPairs",
			            "Search Pattern:{0}{1}  (Minimal Pairs)",
			            "The heading output for the Search Query when exporting find phone results to rtf. 1st param is a rtf tab; 2nd param is the search pattern.")
			        : m_cache.IsSimilarEnvironment
                        ? LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.SearchPatternForSimilarPairs",
                        "Search Pattern:{0}{1}  (Similar Pairs)",
                        "The heading output for the Search Query when exporting find phone results to rtf. 1st param is a rtf tab; 2nd param is the search pattern.")
			            : LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.SearchPatternNormal",
			                "Search Pattern:{0}{1}",
			                "The heading output for  the Search Query when exporting find phone results to rtf. 1st param is a rtf tab; 2nd param is the search pattern."));
				
				m_rtfBldr.AppendFormat(fmt,	ktab, m_cache.SearchQuery.Pattern);
			}
			else
			{
				m_rtfBldr.AppendFormat(LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.WordListName", "Word List:{0}{1}", "The heading output for Word List name when exporting word lists to rtf. 1st param is a rtf tab; 2nd param is the Word List name."),
					ktab, LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.AllWordsLabel", "All Words", "The value for the 'Word List' label when the exported list is not filtered or the result of a  search query."));
			}
			
			m_rtfBldr.AppendLine(kline);
			m_rtfBldr.AppendFormat(LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.NumberOfRecords", "Number of Records:{0}{1}", "The heading output for Number of Records when exporting word lists &amp; find phone results to rtf. 1st param is a rtf tab; 2nd param is the number or records."), ktab, m_numberOfRecords);

			// Add the field the word list is grouped on if it is grouped.
			if (m_grid is PaWordListGrid && ((PaWordListGrid)m_grid).GroupByField != null)
			{
				m_rtfBldr.AppendLine(kline);
				m_rtfBldr.AppendFormat(LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.GroupByField", "Grouped By:{0}{1}", "The heading output for the grouped on field when exporting find phone results to rtf. 1st param is a rtf tab; 2nd param is the search pattern."),
					ktab, ((PaWordListGrid)m_grid).GroupByField.DisplayName);
			}
			
			m_rtfBldr.AppendLine(kline);
			m_rtfBldr.AppendFormat(LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.ProjectName", "Project:{0}{1}", "The heading output for Project name when exporting word lists to rtf. 1st param is a rtf tab; 2nd param is the project name."), ktab, App.Project.Name);
			m_rtfBldr.AppendLine(kline);
			m_rtfBldr.AppendFormat(LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.LanguageName", "Language:{0}{1}", "The heading output for Language name when exporting word lists to rtf. 1st param is a rtf tab; 2nd param is the language name."), ktab, App.Project.LanguageName);
			m_rtfBldr.AppendLine(kline);
			m_rtfBldr.AppendFormat(LocalizationManager.GetString("Views.WordLists.RtfExport.HeadingInfo.DateTime", "Date/Time:{0}{1}", "The heading output for Date/Time name when exporting word lists to rtf. 1st param is a rtf tab; 2nd param is the Date/Time name."), ktab, DateTime.Now);
			
			m_rtfBldr.Append("}");
			m_rtfBldr.AppendLine(@"\par\par\pard");
			if (m_exportFormat == ExportFormat.Table)
				m_rtfBldr.Append(@"\trowd");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines how much page width is necessary to fit all the columns at their
		/// preferred widths. If the portrait orientation doesn't provide enough page width,
		/// then rotate the page to landscape and see if that provides enough room. When
		/// exporting a tab-delimited table, that's all the adjustment that's made. So, there
		/// may be more overflow than there would be in table mode. If in table mode and the
		/// preferred width of the table exceeds the page width, then all the non phonetic
		/// columns will be shrunk so they'll all fit within the margins. If they'll have to
		/// be shrunk too far, then they'll go to a minimum and will just have to overflow
		/// the right margin. It will be up to the user to make adjustments at that point.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MakeFinalWidthAdjustments()
		{
			// Calculate sum total of all the field widths to see if they all fit in the page width.
			int preferredPageWidth = m_maxFieldWidths.Values.Sum();

			// Add in the padding for each cell.
			//if (m_exportFormat == ExportFormat.Table)
			preferredPageWidth += (m_colRightPadding * (m_maxFieldWidths.Count - 1));

			// By default, if the data is too wide for portrait, landscape is tried.
			if (preferredPageWidth > m_pageWidth && Properties.Settings.Default.RTFExportTryLandscapeWhenDataTooWide)
			{
				m_rtfBldr.AppendLine(@"\landscape");
				int tmp = m_paperWidth;
				m_paperWidth = m_paperHeight;
				m_paperHeight = tmp;
				tmp = m_leftMargin;
				m_leftMargin = m_topMargin;
				m_topMargin = m_rightMargin;
				m_rightMargin = m_bottomMargin;
				m_bottomMargin = tmp;
				m_pageWidth = m_paperWidth - (m_leftMargin + m_rightMargin);
			}

			// By default, the paper width will not be set to a
			// custom width in order to accomodate all the data.
			if (preferredPageWidth > m_pageWidth && Properties.Settings.Default.RTFExportUseCustomPaperWidth)
			{
				m_paperWidth = preferredPageWidth + (m_leftMargin + m_rightMargin);
				m_pageWidth = preferredPageWidth;
			}

			if (m_exportFormat == ExportFormat.TabDelimited || preferredPageWidth <= m_pageWidth)
				return;

			// Default for minimum column width is 1/4" (only applies to non phonetic columns).
			int minColWidthAllowed = Properties.Settings.Default.RTFExportMinimumColumnWidth;

			List<int> colsAtMinWidth = new List<int>();
			Dictionary<int, int> tmpWidth = new Dictionary<int, int>(m_maxFieldWidths);

			// At this point, we know we're in table mode and we need to shrink the
			// column widths a little in order for the table to fit within the page
			// margins. So keep shrinking until all the columns fit or they've all
			// shrunk to their minimum width. Shrink a twip at a time.
			while (preferredPageWidth > m_pageWidth &&
				colsAtMinWidth.Count < m_maxFieldWidths.Count - 1)
			{
				foreach (KeyValuePair<int, int> fldWidth in tmpWidth)
				{
					// Skip the phonetic column. It's special.
					if (fldWidth.Key == m_phoneticColIndex)
						continue;

					// Is the column already at it's minimum width?
					if (m_maxFieldWidths[fldWidth.Key] <= minColWidthAllowed)
					{
						if (!colsAtMinWidth.Contains(fldWidth.Key))
							colsAtMinWidth.Add(fldWidth.Key);
					}
					else
					{
						m_maxFieldWidths[fldWidth.Key]--;
						preferredPageWidth--;
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Outputs the RTF for columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OutputColumnInformation()
		{
			foreach (KeyValuePair<int, int> fldWidth in m_maxFieldWidths)
				OutputDataColumnWidthInformation(fldWidth.Key, fldWidth.Value);

			if (m_exportFormat == ExportFormat.Table)
				m_rtfBldr.Append(m_cellLineFormatBldr.ToString());

			m_rtfBldr.AppendLine(string.Empty);

			if (m_exportFormat == ExportFormat.TabDelimited)
				m_rtfBldr.AppendLine(m_cellFormatBldr.ToString());

			OutputDataColumnHeadings();

			if (m_cache.IsForSearchResults)
				m_rtfBldr.AppendLine(m_tabFormatBldr.ToString());

			m_rtfBldr.AppendLine(m_cellFormatBldr.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Figures out the RTF codes for the specified column and its width.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OutputDataColumnWidthInformation(int colIndex, int width)
		{
			var field = m_project.GetFieldForName(m_grid.Columns[colIndex].Name);

			// Create the 2 tabs for the aligning the Phonetic column's search item
			if (field.Type == FieldType.Phonetic && m_cache.IsForSearchResults)
				m_tabFormatBldr = FormatSearchItemTabString(m_tabFormatBldr, m_columnStartPoint);

			m_columnStartPoint += width + m_colRightPadding;

			if (m_exportFormat == ExportFormat.TabDelimited)
				m_cellFormatBldr.AppendFormat(ktxcell, (int)m_columnStartPoint); // set tabs
			else
			{
				// cellLineFormatBldr has the bold underlined header cells
				m_cellLineFormatBldr.AppendFormat(kcellLine, (int)m_columnStartPoint);

				// cellFormatBldr holds the 'normal' formatted cells
				m_cellFormatBldr.AppendFormat(kcell, (int)m_columnStartPoint);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private StringBuilder FormatSearchItemTabString(StringBuilder tabFormatBldr,
			float columnStartPoint)
		{
			tabFormatBldr.Append(@"\tqr");
			while (m_createSearchItemTabs != CreateSearchItemTabs.Stop)
			{
				if (m_createSearchItemTabs == CreateSearchItemTabs.FirstTab)
				{
					if (m_exportFormat == ExportFormat.TabDelimited)
						m_beforeEnvTwipWidth += (int)columnStartPoint;

					m_createSearchItemTabs = CreateSearchItemTabs.SecondTab;
				}
				else if (m_createSearchItemTabs == CreateSearchItemTabs.SecondTab)
				{
					m_createSearchItemTabs = CreateSearchItemTabs.FinishedTabs;
					m_beforeEnvTwipWidth += kGapBetweenSrchItemAndPrecedingEnv;
				}

				tabFormatBldr.AppendFormat(ktxcell, m_beforeEnvTwipWidth);

				if (m_createSearchItemTabs == CreateSearchItemTabs.FinishedTabs)
					m_createSearchItemTabs = CreateSearchItemTabs.Stop;
			}

			return tabFormatBldr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the column headers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OutputDataColumnHeadings()
		{
			m_rtfBldr.Append(m_exportFormat == ExportFormat.Table ? "\n\\intbl" : "\n");
			m_rtfBldr.AppendFormat(@"\f{0} \fs{1}", m_uiFontNumber, kheadingFontSize * 2);
			m_rtfBldr.Append(@"{\b ");

			int i = m_sortedColumns.Count;
			foreach (DataGridViewColumn col in m_sortedColumns.Values)
			{
				if (m_exportFormat == ExportFormat.Table)
					m_rtfBldr.AppendFormat(kcellHdr, col.HeaderText);
				else
				{
					m_rtfBldr.AppendFormat(ktxHdr, col.HeaderText);

					// Only add a tab after the column heading if the heading is not the last one.
					if (--i > 0)
						m_rtfBldr.Append(ktab);
				}
			}

			if (m_exportFormat == ExportFormat.TabDelimited)
				m_rtfBldr.AppendLine("}" + kparagraph);
			else
			{
				m_rtfBldr.AppendLine(@"}\row");
				m_rtfBldr.Append(@"\trowd");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Output rows of data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OutputDataRows()
		{
			m_fontSizes = m_project.Fields.Where(f => f.Font != null)
				.ToDictionary(f => f.Name, f => (int)(f.Font.SizeInPoints * 2));

			foreach (var row in m_wordListRows)
				OutputSingleDataRow(row);

			m_rtfBldr.AppendLine("}");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Format the cell values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OutputSingleDataRow(Dictionary<int, object[]> row)
		{
			if (m_exportFormat == ExportFormat.Table)
				m_rtfBldr.Append(@"\intbl");

			// Iterate through the cells in the row.
			foreach (KeyValuePair<int, object[]> col in row)
			{
				if (col.Key == kGroupHdgRowToken)
				{
					OutputGroupHeading(col.Value);
					continue;
				}

				var colName = m_grid.Columns[col.Key].Name;
				if (colName == string.Empty)
					continue;

				var field = m_project.GetFieldForName(colName);
				int fontNumber = m_fontNumbers[colName];
				int fontSize = m_fontSizes[colName];
				var colValue = col.Value[(int)ArrayDataType.GroupingFieldName].ToString().Replace("\\", "\\\\");

				if (m_cache.IsForSearchResults && field.Type == FieldType.Phonetic)
				{
					m_rtfBldr.AppendFormat((m_exportFormat == ExportFormat.Table ?
						kcellValues : ktxValues), fontNumber, fontSize,
						m_alignedSearchItems[(int)col.Value[(int)ArrayDataType.RecordIndex]]);
				}
				else
				{
					if (m_exportFormat == ExportFormat.Table)
						m_rtfBldr.AppendFormat(kcellValues, fontNumber, fontSize, colValue);
					else
					{
						// Only display the FileName of the WaveFile when TabDelimited export fomat
						if (field.Type == FieldType.AudioFilePath)
							colValue = Path.GetFileName(colValue);

						m_rtfBldr.AppendFormat(ktxValues, fontNumber, fontSize, colValue);
					}
				}
			}

			// Removes the last "{\tab}"
			if (m_exportFormat == ExportFormat.TabDelimited)
			{
                if (m_rtfBldr.ToString().EndsWith(ktab, StringComparison.Ordinal))
					m_rtfBldr.Remove((m_rtfBldr.Length - ktab.Length), ktab.Length);
			}

			if (!row.ContainsKey(kGroupHdgRowToken))
				m_rtfBldr.AppendLine(m_exportFormat == ExportFormat.Table ? @"\row" : kline);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Outputs a group heading row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OutputGroupHeading(object[] col)
		{
			// Print the group heading with the child row counts
			var shgrow = col[(int)ArrayDataType.SilHierarchicalGridRow] as SilHierarchicalGridRow;

			if (shgrow == null)
				return;

			// Get the font number and size for the group heading text.
			string groupFieldName = null;
			var grid = m_grid as PaWordListGrid;

			if (grid != null && grid.GroupByField != null)
				groupFieldName = grid.GroupByField.Name;
            else if (m_cache.IsMinimalPair || m_cache.IsSimilarEnvironment)
				groupFieldName = m_project.GetPhoneticField().Name;
			
			int fontNumber = (string.IsNullOrEmpty(groupFieldName) ? 0 : m_fontNumbers[groupFieldName]);
			int fontSize = (string.IsNullOrEmpty(groupFieldName) ? 20 : m_fontSizes[groupFieldName]);

			if (m_exportFormat == ExportFormat.Table)
			{
				m_rtfBldr.Remove((m_rtfBldr.Length - @"\intbl".Length), @"\intbl".Length);
				m_rtfBldr.AppendLine(@"\trowd");
				m_rtfBldr.AppendFormat(kcell, (int)m_columnStartPoint);
				m_rtfBldr.AppendLine(@"\intbl");
			}
			else
			{
				// Make sure a group header starts a new paragraph.
				int len = 0;
				var rtf = m_rtfBldr.ToString();
                if (rtf.EndsWith(kline, StringComparison.Ordinal))
					len = kline.Length;
                else if (rtf.EndsWith(kline + Environment.NewLine, StringComparison.Ordinal))
					len = kline.Length + Environment.NewLine.Length;
					
				if (len > 0)
				{
					m_rtfBldr.Remove(m_rtfBldr.Length - len, len);
					m_rtfBldr.AppendLine(kparagraph);
				}
			}

			// By default, put 12 points of space between the
			// group heading and the row above it.
			int spaceB4GrpHdg = Properties.Settings.Default.RTFExportSpaceBeforeGroupHeading;
			m_rtfBldr.AppendFormat(@"\sb{0}\f{1} \fs{2}{{\b ", spaceB4GrpHdg, fontNumber, fontSize);
			m_rtfBldr.Append(col[(int)ArrayDataType.GroupingFieldName]);
			m_rtfBldr.Append("  ");
			m_rtfBldr.AppendFormat(shgrow.CountFormatStrings[0], shgrow.ChildCount);
			m_rtfBldr.Append("}");

			if (m_exportFormat == ExportFormat.Table)
				m_rtfBldr.Append(@"\cell\row\trowd");
			else
			{
				if (Properties.Settings.Default.RTFExportBorderUnderGroupHeading)
					m_rtfBldr.Append(@"\brdrb\brdrs\brdrw10\brsp20");

				m_rtfBldr.Append(kparagraph);
			}

			m_rtfBldr.AppendLine();
			m_rtfBldr.Append(@"\sb0");

			if (m_cache.IsForSearchResults)
				m_rtfBldr.AppendLine(m_tabFormatBldr.ToString());

			m_rtfBldr.AppendLine(m_cellFormatBldr.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Write the Rtf string to an output file or the clipboard.
		/// </summary>
		/// <returns>true if the message was handled</returns>
		/// ------------------------------------------------------------------------------------
		private void WriteToFileOrClipboard()
		{
			var rtf = RtfHelper.TranslateUnicodeChars(m_rtfBldr.ToString());

			if (m_exportTarget == ExportTarget.Clipboard)
			{
				Clipboard.SetText(rtf, TextDataFormat.Rtf);
				return;
			}

			string filter = App.kstidFiletypeRTF + "|" + App.kstidFileTypeAllFiles;

			int filterIndex = 0;

			var caption = LocalizationManager.GetString("Views.WordLists.RtfExport.SaveFileDialogText", "Save RTF File",
				"Save file dialog caption for specifying the rtf file exported to under RTF export");
			
			var filename = App.SaveFileDialog("rtf", filter, ref filterIndex, caption, string.Empty);
			if (filename != string.Empty)
			{
				try
				{
					using (var sw = new StreamWriter(filename))
						sw.Write(rtf);
				}
				catch (Exception ex)
				{
					Utils.MsgBox(ex.Message);
					return;
				}
                // Open the file with the specified RTF editor
                if (m_exportTarget == ExportTarget.FileAndOpen)
                    OpenInEditor(filename);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OpenInEditor(string filename)
		{
			Process prs = new Process();

			if (!File.Exists(m_rtfEditor))
				prs.StartInfo.FileName = filename;
			else
			{
				prs.StartInfo.FileName = m_rtfEditor;
				prs.StartInfo.Arguments = filename;
			}
			
			try
			{
				prs.Start();
			}
			catch (Exception ex)
			{
				string msg;

				if (string.IsNullOrEmpty(m_rtfEditor))
				{
					msg = string.Format(LocalizationManager.GetString("Views.WordLists.RtfExport.OpenErrorMsg1",
						"An error occurred trying to open the RTF file.\n\n{0}\n\n{1}",
						"First argument is rtf file; second argument is error message."),
						filename, ex.Message);
				}
				else
				{
					msg = string.Format(LocalizationManager.GetString("Views.WordLists.RtfExport.OpenErrorMsg2",
						"An error occurred trying to open the RTF file.\n\nFile: {0}\nEditor: {1}\n\n{2}",
						"First argument is rtf file; second argument is the editor; third argument is error message."),
						filename, m_rtfEditor, ex.Message);
				}
				
				Utils.MsgBox(msg);
			}
		}

		#endregion
	}
}
