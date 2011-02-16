﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.Pa.Model;
using SilTools;

namespace SIL.Pa.UI.Controls
{
	public class FieldMappingGrid : SilGrid
	{
		private readonly Func<string> m_sourceFieldColumnHeadingTextHandler;
		private readonly Func<string> m_targetFieldColumnHeadingTextHandler;

		private readonly List<FieldMapping> m_mappings;
		private readonly IDictionary<FieldType, string> m_displayableFieldTypes;
		private readonly IEnumerable<PaField> m_potentialFields;
		private readonly CellCustomDropDownList m_potentialFieldsDropDown;

		/// ------------------------------------------------------------------------------------
		public FieldMappingGrid()
		{
			VirtualMode = true;
			Font = FontHelper.UIFont;
			RowHeadersVisible = false;
			BorderStyle = BorderStyle.None;
			App.SetGridSelectionColors(this, true);
			
			m_potentialFieldsDropDown = new CellCustomDropDownList();
			
			m_displayableFieldTypes =
				PaField.GetDisplayableFieldTypes().ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}

		/// ------------------------------------------------------------------------------------
		public FieldMappingGrid(IEnumerable<PaField> potentialFields, IEnumerable<FieldMapping> mappings,
			Func<string> srcFldColHdgTextHandler, Func<string> tgtFldColHdgTextHandler)
			: this()
		{
			m_potentialFields = potentialFields.Where(f => f.AllowUserToMap);
			m_mappings = mappings.Select(m => m.Copy()).ToList();
			m_sourceFieldColumnHeadingTextHandler = srcFldColHdgTextHandler;
			m_targetFieldColumnHeadingTextHandler = tgtFldColHdgTextHandler;
			
			AddColumns();
			RowCount = m_mappings.Count;
		}

		/// ------------------------------------------------------------------------------------
		public FieldMappingGrid(IEnumerable<PaField> potentialFields, IEnumerable<FieldMapping> mappings)
			: this(potentialFields, mappings, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		private void AddColumns()
		{
			// Create source field column.
			DataGridViewColumn col = CreateTextBoxColumn("srcfield");
			col.ReadOnly = true;
			var text = (m_sourceFieldColumnHeadingTextHandler != null ?
				m_sourceFieldColumnHeadingTextHandler() : null);

			if (string.IsNullOrEmpty(text))
			{
				text = App.LocalizeString(
					"FieldMappingGrid.DefaultSourceFieldColumnHeadingText",
					"Field in Source Data", App.kLocalizationGroupUICtrls);
			}
	
			col.HeaderText = text;
			Columns.Add(col);

			// Create the column for the arrow.
			col = CreateImageColumn("arrow");
			col.HeaderText = string.Empty;
			col.ReadOnly = true;
			col.Width = Properties.Resources.FieldMappingArrow.Width + 6;
			col.Resizable = DataGridViewTriState.False;
			col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			Columns.Add(col);

			// Create target field column.
			col = new SilButtonColumn("tgtfield");
			((SilButtonColumn)col).ButtonStyle = SilButtonColumn.ButtonType.MinimalistCombo;
			((SilButtonColumn)col).ButtonClicked += OnFieldColumnButtonClicked;
			((SilButtonColumn)col).DrawDefaultComboButtonWidth = false;
			col.SortMode = DataGridViewColumnSortMode.NotSortable;

			text = (m_targetFieldColumnHeadingTextHandler != null ?
				m_targetFieldColumnHeadingTextHandler() : null);

			if (string.IsNullOrEmpty(text))
			{
				text = App.LocalizeString(
					"FieldMappingGrid.TargetFieldColumnHeadingText", "PA Field",
					App.kLocalizationGroupUICtrls);
			}

			col.HeaderText = text;
			Columns.Add(col);

			// Create field type column.
			col = CreateDropDownListComboBoxColumn("fieldtype", m_displayableFieldTypes.Values);
			int i = Columns.Add(col);
			App.LocalizeObject(Columns[i], "FieldMappingGrid.FieldTypeColumnHeadingText",
				"Type", App.kLocalizationGroupUICtrls);

			// Create the parsed column.
			col = CreateCheckBoxColumn("parsed");
			i = Columns.Add(col);
			App.LocalizeObject(Columns[i], "FieldMappingGrid.FieldIsParsedColumnHeadingText",
				"Is Parsed?", App.kLocalizationGroupUICtrls);

			// Create the interlinear column.
			col = CreateCheckBoxColumn("interlinear");
			i = Columns.Add(col);
			App.LocalizeObject(Columns[i], "FieldMappingGrid.FieldCanBeInterlinearColumnHeadingText",
				"Is Interlinear?", App.kLocalizationGroupUICtrls);

			// TODO: Add font columns.
		}

		/// ------------------------------------------------------------------------------------
		public IEnumerable<FieldMapping> Mappings
		{
			get { return m_mappings.Where(m => m.Field != null); }
		}

		/// ------------------------------------------------------------------------------------
		public void ShowIsParsedColumn(bool show)
		{
			Columns["parsed"].Visible = show;
		}

		/// ------------------------------------------------------------------------------------
		public void ShowIsInterlinearColumn(bool show)
		{
			Columns["interlinear"].Visible = show;
		}

		/// ------------------------------------------------------------------------------------
		protected virtual void OnFieldColumnButtonClicked(object sender, DataGridViewCellMouseEventArgs e)
		{
			var potentialFieldNames = m_potentialFields.Select(f => f.DisplayName).ToList();
			potentialFieldNames.Insert(0, GetNoMappingText());
			m_potentialFieldsDropDown.Show(this[e.ColumnIndex, e.RowIndex], potentialFieldNames);
		}

		/// ------------------------------------------------------------------------------------
		private static string GetNoMappingText()
		{
			return App.LocalizeString("FieldMappingGrid.NoMappingText",
				"(no mapping)", App.kLocalizationGroupUICtrls);
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			if (e.ColumnIndex >= 0 && e.RowIndex >= 0)
			{
				var mapping = m_mappings[e.RowIndex];

				switch (Columns[e.ColumnIndex].Name)
				{
					case "srcfield": e.Value = mapping.NameInDataSource; break;
					case "tgtfield": e.Value = (mapping.Field == null ? null : mapping.Field.DisplayName); break;
					case "fieldtype": e.Value = m_displayableFieldTypes[GetTypeAtOrDefault(e.RowIndex)]; break;
					case "parsed": e.Value = mapping.IsParsed; break;
					case "interlinear": e.Value = mapping.IsInterlinear; break;
					case "arrow": e.Value = Properties.Resources.FieldMappingArrow; break;
				}
			}

			base.OnCellValueNeeded(e);
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnCellValuePushed(DataGridViewCellValueEventArgs e)
		{
			base.OnCellValuePushed(e);

			if (e.ColumnIndex < 0 || e.RowIndex < 0)
				return;

			var valAsString = (e.Value as string ?? string.Empty);
			valAsString = valAsString.Trim();
			var mapping = m_mappings[e.RowIndex];

			switch (Columns[e.ColumnIndex].Name)
			{
				case "tgtfield":
					if (valAsString == GetNoMappingText() || valAsString == string.Empty)
					{
						mapping.Field = null;
						mapping.IsParsed = false;
						mapping.IsInterlinear = false;
					}
					else
					{
						mapping.Field =
							m_potentialFields.SingleOrDefault(f => f.DisplayName == valAsString) ??
							new PaField(valAsString, GetTypeAtOrDefault(e.RowIndex));
					}

					break;

				case "fieldtype":
					var newType = m_displayableFieldTypes.Single(kvp => kvp.Value == valAsString).Key;
					if (mapping.Field != null)
						mapping.Field.Type = newType;
					else
						mapping.Field = new PaField(null, newType);

					break;
				
				case "parsed":
					mapping.IsParsed = (bool)e.Value;

					// Unparsed fields cannot also be interlinear fields.
					// So make sure that property is turned off.
					if (!mapping.IsParsed)
						mapping.IsInterlinear = false;
					
					break;

				case "interlinear":
					mapping.IsInterlinear = (bool)e.Value;
					break;
			}

			InvalidateRow(e.RowIndex);
		}

		/// ------------------------------------------------------------------------------------
		private FieldType GetTypeAtOrDefault(int index)
		{
			return (index >= 0 && m_mappings[index].Field != null ?
				m_mappings[index].Field.Type : default(FieldType));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the cells for fields that can't be parsed or interlinear are set to
		/// readonly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
			{
				this[e.ColumnIndex, e.RowIndex].ReadOnly = false;
				var colName = Columns[e.ColumnIndex].Name;
				var mapping = m_mappings[e.RowIndex];
				var type = GetTypeAtOrDefault(e.RowIndex);

				if (colName == "parsed" && !PaField.GetIsTypeParsable(type))
				{
					this[e.ColumnIndex, e.RowIndex].ReadOnly = true;
				}
				else if (colName == "interlinear" &&
					(!PaField.GetIsTypeInterlinearizable(type) || !mapping.IsParsed))
				{
					this[e.ColumnIndex, e.RowIndex].ReadOnly = true;
				}
			}
	
			base.OnCellFormatting(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure any field that can't be marked as parsed or interlinear has it's check
		/// box painted over so the check box cannot be seen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			base.OnCellPainting(e);

			if (e.RowIndex < 0 || e.ColumnIndex < 0)
				return;

			var colName = Columns[e.ColumnIndex].Name;
			var type = GetTypeAtOrDefault(e.RowIndex);

			if (!("parsed interlinear").Contains(colName) ||
				(colName == "parsed" && PaField.GetIsTypeParsable(type)) ||
				(colName == "interlinear" && PaField.GetIsTypeInterlinearizable(type)))
			{
				return;
			}

			var parts = e.PaintParts & ~DataGridViewPaintParts.ContentForeground;
			e.Paint(e.ClipBounds, parts);
			e.Handled = true;
		}

		/// ------------------------------------------------------------------------------------
		public bool GetAreAnyFieldsMappedMultipleTimes()
		{
			return Mappings.Select(m => m.Field.Name)
				.Any(fname => Mappings.Count(m => m.Field.Name == fname) > 1);
		}

		/// ------------------------------------------------------------------------------------
		public bool GetIsTargetFieldMapped(string fieldName)
		{
			return Mappings.Any(m => m.Field.Name == fieldName);
		}

		/// ------------------------------------------------------------------------------------
		public bool GetIsSourceFieldMapped(string fieldName)
		{
			return Mappings.Any(m => m.NameInDataSource == fieldName);
		}

		/// ------------------------------------------------------------------------------------
		public PaField GetMappedFieldForSourceField(string fieldName)
		{
			return (from m in Mappings where m.Field.Name == fieldName select m.Field).FirstOrDefault();
		}
	}
}