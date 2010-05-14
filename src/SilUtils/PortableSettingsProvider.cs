// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.   
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
#endregion
// 
// File: PortableSettingsProvider.cs
// Responsibility: D. Olson
// 
// <remarks>
// This class is based on a class by the same name found at
// http://www.codeproject.com/KB/vb/CustomSettingsProvider.aspx. The original was written in
// VB.Net so this is a C# port of that. Other changes include some variable name changes,
// making the settings file path a public static property, special handling of string
// collections, getting rid of the IsRoaming support and all-around method rewriting.
// The original code is under the CPOL license (http://www.codeproject.com/info/cpol10.aspx).
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

namespace SilUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class is a settings provider that allows applications to specify in what file and
	/// where in a file system it's settings will be stored. It's good for portable apps.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PortableSettingsProvider : SettingsProvider
	{
		// XML Root Node name.
		private const string Root = "Settings";
		private const string StrCollectionAttrib = "stringcollection";

		protected XmlDocument m_settingsXml;

		// This allows tests to specify a temp. location which can be deleted on test cleanup.
		public static string SettingsFileFolder { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PortableSettingsProvider()
		{
			if (SettingsFileFolder == null)
			{
				var appFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				appFolder = Path.Combine(appFolder, ApplicationName);
				if (!Directory.Exists(appFolder))
					Directory.CreateDirectory(appFolder);

				SettingsFileFolder = appFolder;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the specified name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Initialize(string name, NameValueCollection nvc)
		{
			base.Initialize(ApplicationName, nvc);

			m_settingsXml = new XmlDocument();

			try
			{
				m_settingsXml.Load(Path.Combine(SettingsFilePath, SettingsFilename));
			}
			catch
			{
				XmlDeclaration dec = m_settingsXml.CreateXmlDeclaration("1.0", "utf-8", null);
				m_settingsXml.AppendChild(dec);
				XmlNode nodeRoot = m_settingsXml.CreateNode(XmlNodeType.Element, Root, null);
				m_settingsXml.AppendChild(nodeRoot);
			}
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the currently running application.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ApplicationName
		{
			get
			{
				return (Application.ProductName.Trim().Length > 0 ? Application.ProductName :
					Path.GetFileNameWithoutExtension(Application.ExecutablePath));
			}
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to the application's settings file. This path does not include the
		/// file name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string SettingsFilePath
		{
			get { return SettingsFileFolder; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets only the name of the application settings file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string SettingsFilename
		{
			get { return ApplicationName + ".settings"; }
		}

		#endregion

		#region Methods for getting property values from XML.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a collection of property values.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override SettingsPropertyValueCollection GetPropertyValues(
			SettingsContext context, SettingsPropertyCollection props)
		{
			SettingsPropertyValueCollection propValues = new SettingsPropertyValueCollection();

			foreach (SettingsProperty setting in props)
				propValues.Add(GetValue(setting));

			return propValues;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets from the XML the value for the specified property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private SettingsPropertyValue GetValue(SettingsProperty setting)
		{
			var value = new SettingsPropertyValue(setting);
			value.IsDirty = false;
			value.SerializedValue = string.Empty;

			try
			{
				XmlNode node = m_settingsXml.SelectSingleNode(Root + "/" + setting.Name);

				if (!GetStringCollection(value, node))
				{
					if (!GetGridSettings(value, node))
						value.SerializedValue = node.InnerText;
				}
			}
			catch
			{
				if ((setting.DefaultValue != null))
					value.SerializedValue = setting.DefaultValue.ToString();
			}

			return value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool GetStringCollection(SettingsPropertyValue value, XmlNode node)
		{
			// StringCollections will have an indicating attribute and they
			// receive special handling.
			if (node.Attributes.GetNamedItem(StrCollectionAttrib) != null &&
				node.Attributes[StrCollectionAttrib].Value == "true")
			{
				var sc = new StringCollection();
				foreach (XmlNode childNode in node.ChildNodes)
					sc.Add(childNode.InnerText);

				value.PropertyValue = sc;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool GetGridSettings(SettingsPropertyValue value, XmlNode node)
		{
			if (node.Attributes.GetNamedItem("type") != null &&
				node.Attributes["type"].Value == typeof(GridSettings).FullName)
			{
				value.PropertyValue =
					XmlSerializationHelper.DeserializeFromString<GridSettings>(node.InnerXml) ?? new GridSettings();

				return true;
			}

			return false;
		}

		#endregion

		#region Methods for saving property values to XML.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the values for the specified properties and saves the XML file in which
		/// they're stored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void SetPropertyValues(SettingsContext context,
			SettingsPropertyValueCollection propvals)
		{
			// Iterate through the settings to be stored. Only dirty settings are included
			// in propvals, and only ones relevant to this provider
			foreach (SettingsPropertyValue propVal in propvals)
			{
				if (propVal.Property.Attributes.ContainsKey(typeof(ApplicationScopedSettingAttribute)))
					continue;
				
				XmlElement settingNode = null;

				try
				{
					settingNode = (XmlElement)m_settingsXml.SelectSingleNode(Root + "/" + propVal.Name);
				}
				catch { }

				// Check if node exists.
				if ((settingNode != null))
					SetPropNodeValue(settingNode, propVal);
				else
				{
					// Node does not exist so create one.
					settingNode = m_settingsXml.CreateElement(propVal.Name);
					SetPropNodeValue(settingNode, propVal);
					m_settingsXml.SelectSingleNode(Root).AppendChild(settingNode);
				}
			}
			
			try
			{
				// Loading the XML into a new document will make all the indentation correct.
				var tmpXmlDoc = new XmlDocument();
				tmpXmlDoc.LoadXml(m_settingsXml.OuterXml);
				tmpXmlDoc.Save(Path.Combine(SettingsFilePath, SettingsFilename));
			}
			catch { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the value of a node to that found in the specified property. This method
		/// specially handles various types of properties (e.g. string collections).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetPropNodeValue(XmlNode propNode, SettingsPropertyValue propVal)
		{
			if (propVal.Property.PropertyType == typeof(StringCollection))
				SetStringCollection(propVal, propNode);
			else if (propVal.Property.PropertyType == typeof(GridSettings))
				SetGridSettings(propVal, propNode);
			else
				propNode.InnerText = propVal.SerializedValue.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetStringCollection(SettingsPropertyValue propVal, XmlNode propNode)
		{
			if (propVal.PropertyValue == null)
				return;

			propNode.RemoveAll();
			var attrib = m_settingsXml.CreateAttribute(StrCollectionAttrib);
			attrib.Value = "true";
			propNode.Attributes.Append(attrib);
			int i = 0;
			foreach (string str in propVal.PropertyValue as StringCollection)
			{
				var node = m_settingsXml.CreateElement(propVal.Name + i++);
				node.AppendChild(m_settingsXml.CreateTextNode(str));
				propNode.AppendChild(node);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetGridSettings(SettingsPropertyValue propVal, XmlNode propNode)
		{
			var gridSettings = propVal.PropertyValue as GridSettings;
			if (gridSettings == null)
				return;

			propNode.RemoveAll();
			var attrib = m_settingsXml.CreateAttribute("type");
			attrib.Value = typeof(GridSettings).FullName;
			propNode.Attributes.Append(attrib);

			propNode.InnerXml =
				(XmlSerializationHelper.SerializeToString(gridSettings, true) ?? string.Empty);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a comma-delimited string from an array of integers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetStringFromIntArray(int[] array)
		{
			if (array == null)
				return string.Empty;

			StringBuilder bldr = new StringBuilder();
			foreach (int i in array)
				bldr.AppendFormat("{0}, ", i);

			return bldr.ToString().TrimEnd(',', ' ');
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an int array from a comma-delimited string of numbers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int[] GetIntArrayFromString(string str)
		{
			List<int> array = new List<int>();

			if (str != null)
			{
				string[] pieces = str.Split(',');
				foreach (string piece in pieces)
				{
					int i;
					if (int.TryParse(piece, out i))
						array.Add(i);
				}
			}

			return array.ToArray();
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("gridSettings")]
	public class GridSettings
	{
		[XmlElement("columnHeaderHeight")]
		public int ColumnHeaderHeight { get; set; }
		[XmlElement("dpi")]
		public float DPI { get; set; }
		[XmlArray("columns"), XmlArrayItem("col")]
		public GridColumnSettings[] Columns { get; set; }

		private readonly float m_currDpi;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal GridSettings()
		{
			ColumnHeaderHeight = -1;
			Columns = new GridColumnSettings[] { };
			using (Form frm = new Form())
			using (Graphics g = frm.CreateGraphics())
				m_currDpi = DPI = g.DpiX;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static GridSettings Create(DataGridView grid)
		{
			var gridSettings = new GridSettings();

			gridSettings.ColumnHeaderHeight = grid.ColumnHeadersHeight;

			var colSettings = new List<GridColumnSettings>();

			foreach (DataGridViewColumn col in grid.Columns)
			{
				colSettings.Add(new GridColumnSettings { Id = col.Name, Width = col.Width,
					Visible = col.Visible, DisplayIndex = col.DisplayIndex });
			}

			gridSettings.Columns = colSettings.ToArray();
			return gridSettings;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeGrid(DataGridView grid)
		{
			foreach (var col in Columns)
			{
				grid.Columns[col.Id].Visible = col.Visible;

				if (col.Width >= 0)
                    grid.Columns[col.Id].Width = col.Width;
				
				if (col.DisplayIndex >= 0)
					grid.Columns[col.Id].DisplayIndex = col.DisplayIndex;
			}

			// If the column header height or the former dpi settings are different,
			// then auto. calculate the height of the column headings.
			if (ColumnHeaderHeight <= 0 || DPI != m_currDpi)
				grid.AutoResizeColumnHeadersHeight();
			else
				grid.ColumnHeadersHeight = ColumnHeaderHeight;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class GridColumnSettings
	{
		[XmlAttribute("id")]
		public string Id { get; set; }
		[XmlAttribute("visible")]
		public bool Visible { get; set; }
		[XmlAttribute("width")]
		public int Width { get; set; }
		[XmlAttribute("displayIndex")]
		public int DisplayIndex { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal GridColumnSettings()
		{
			Visible = true;
			Width = -1;
			DisplayIndex = -1;
		}
	}
}
