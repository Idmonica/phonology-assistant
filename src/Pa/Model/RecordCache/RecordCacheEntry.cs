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
using System.Linq;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using SIL.Pa.DataSource;

namespace SIL.Pa.Model
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class contains a sinlge item in a record cache. There is only one record cache per
	/// instance of PA. A RecordCacheEntry contains the data associated with a single record
	/// read from the data source. When a record contains multiple phonetic, tone, phonemic,
	/// orthographic, gloss, POS or CVPattern "words", the RecordCacheEntry references
	/// multiple WordCacheEntry objects for those "words".
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("Record")]
	public class RecordCacheEntry
	{
		// This is only used for deserialization
		private List<FieldValue> m_fieldValuesList;
		private IDictionary<string, FieldValue> m_fieldValues;
		private Dictionary<string, IEnumerable<string>> m_collectionValues;

		private static int s_counter;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows the RecordCache to reset the counter that assigns Id's to record cache
		/// entries.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ResetCounter()
		{
			s_counter = 0;
		}

		/// ------------------------------------------------------------------------------------
		public RecordCacheEntry(PaProject project)
		{
			Project = project;
			Id = s_counter++;
			AudioOffset = -1;
			AudioLength = -1;
			m_fieldValues = project.Fields.ToDictionary(f => f.Name, f => new FieldValue(f.Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Only use this constructor when the data source is not PAXML or FW6 (or older)
		/// data source.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RecordCacheEntry(bool newFromParsingSFMFile, PaProject project) : this(project)
		{
			CanBeEditedInToolbox = newFromParsingSFMFile;
		}

		#region Methods and Indexers for getting and setting field values
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempts to set the specified property with the specified value.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetValue(string field, string value)
		{
			if (string.IsNullOrEmpty(value))
				return;
			
			FieldValue fieldValue;

			if (field != PaField.kDataSourcePathFieldName && field != PaField.kDataSourceFieldName &&
				m_fieldValues.TryGetValue(field, out fieldValue))
			{
				fieldValue.Value = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		public string this[string field]
		{
			get { return GetValue(field); }
		}

		/// ------------------------------------------------------------------------------------
		public void SetCollection(string field, IEnumerable<string> collection)
		{
			if (string.IsNullOrEmpty(field) || collection == null)
				return;

			var tmpCollection = collection.ToArray();
			if (tmpCollection.Length == 0)
				return;

			if (m_collectionValues == null)
				m_collectionValues = new Dictionary<string, IEnumerable<string>>();

			m_collectionValues[field] = tmpCollection;
		}

		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> GetCollection(string field)
		{
			if (string.IsNullOrEmpty(field) || m_collectionValues == null || m_collectionValues.Count() == 0)
				return null;

			IEnumerable<string> collection;
			return (m_collectionValues.TryGetValue(field, out collection) ? collection : null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unlike GetValue, this method tries to get the value of the specified field and
		/// when it fails, null is returned and nothing else is tried, nor special handling
		/// given to certain fields. This method was introduced to fix PA-691.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string GetValueBasic(string field)
		{
			FieldValue fieldValue;
			if (m_fieldValues.TryGetValue(field, out fieldValue) && fieldValue.Value != null) 
				return fieldValue.Value;

			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the value of the specified field, giving specially handling for certain
		/// fields or deferring to the record's word entries if necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string GetValue(string fieldName)
		{
			// If the data source file path is being requested then defer
			// to the record's data source object to get that information.
			if (fieldName == PaField.kDataSourcePathFieldName)
			{
				return (DataSource.Type == DataSourceType.FW &&
					DataSource.FwSourceDirectFromDB ? DataSource.FwDataSourceInfo.Server :
					Path.GetDirectoryName(DataSource.SourceFile)); 
			}

			// If the data source name is being requested then defer to
			// the record's data source object to get that information.
			if (fieldName == PaField.kDataSourceFieldName)
			{
				return (DataSource.Type == DataSourceType.FW &&
					DataSource.FwSourceDirectFromDB ? DataSource.FwDataSourceInfo.ToString() :
					Path.GetFileName(DataSource.SourceFile));
			}

			FieldValue fieldValue;
			if (m_fieldValues.TryGetValue(fieldName, out fieldValue) && fieldValue.Value != null)
				return fieldValue.Value;

			if (fieldName != PaField.kCVPatternFieldName)
			{
				// If the field isn't in the word cache entry's values, check if it's a parsed field.
				var mapping = DataSource.FieldMappings.SingleOrDefault(m => m.Field.Name == fieldName);
				if (mapping == null || !mapping.IsParsed)
					return null;
			}

			// At this point, we know 2 things: 1) either this record cache entry doesn't
			// contain a value for the specified field or it does and the value is null, or
			// 2) the specified field is a parsed field. Therefore, gather together all the
			// words (from this record cache entry's owned word cache) for the field.
			// When gathering the word cache entries, use the GetField method instead of
			// wentry[field] because when using wentry[field] to get word cache entry values
			// and any of the word cache entry values are null, word cache entries defer to
			// the value from their owning record cache entry. But that will put us right back
			// here, thus causing a circular problem ending in a stack overflow. Hence the
			// call to GetField() passing false in the second argument.
			var words = new StringBuilder();

			if (WordEntries != null)
			{
				foreach (var wentry in WordEntries)
				{
					words.Append(wentry.GetField(fieldName, false));
					words.Append(' ');
				}
			}

			var trimmedWords = words.ToString().Trim();
			return (trimmedWords == string.Empty ? null : trimmedWords);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all the values for an interlinear field. The values can be considered all
		/// the column values for the specified field. When the fieldInfo parameter is for
		/// the phonetic field, then useOriginalPhonetic tells the method to get the value
		/// of the phonetic as it was before applying any experimental transcriptions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string[] GetParsedFieldValues(PaField field, bool useOriginalPhonetic)
		{
			if (field == null)
				return null;

			bool getOrigPhonetic = (useOriginalPhonetic && field.Type == FieldType.Phonetic);

			// Go through the parsed word entries and get the values for the specified field.
			var values = WordEntries.Select(we =>
			{
				var val = (getOrigPhonetic ? we.OriginalPhoneticValue : we.GetField(field.Name, false));
				return (val != null ? val.Trim() : string.Empty);
			}).ToArray();

			return (values.Length == 0 ? null : values.ToArray());
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public PaProject Project { get; private set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public object Tag { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a number that uniquely identifies the record entry among its peers within
		/// the application's record cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public int Id { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the record can be edited in a toolbox
		/// database (i.e. true means PA assumes the record came from a Toolbox DB).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public bool CanBeEditedInToolbox { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether or not the record entry needs to be parsed. (Entries created
		/// from an XML file or from a FW6 (or earlier) project should not need parsing,
		/// while those created from all other data sources, should.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public bool NeedsParsing { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the record's data source object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public PaDataSource DataSource { get; set; }

		/// ------------------------------------------------------------------------------------
		public string FirstInterlinearField { get; set; }

		/// ------------------------------------------------------------------------------------
		public List<string> InterlinearFields { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the record contains interlinear data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public bool HasInterlinearData
		{
			get
			{
				// If data source is not an SFM file then set this to true and let the
				// values of m_interlinearFields and m_firstInterlinearField determine
				// what's returned. Otherwise, this data source's parse type must be
				// interlinear.
				bool projParseTypeOK = ((DataSource.Type != DataSourceType.SFM &&
					DataSource.Type != DataSourceType.Toolbox) ||
					DataSource.ParseType == DataSourceParseType.Interlinear);

				return (projParseTypeOK && InterlinearFields != null &&
					FirstInterlinearField != null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This property is really only used for serialization and deserialization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray("Fields")]
		public List<FieldValue> FieldValues
		{
			get
			{
				if (m_fieldValues != null)
					m_fieldValuesList = m_fieldValues.Values.ToList();

				return m_fieldValuesList;
			}
			set {m_fieldValuesList = value;}
		}

		/// ------------------------------------------------------------------------------------
		[XmlArray("ParsedFields")]
		public List<WordCacheEntry> WordEntries { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Number of channels in the record's audio file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public int Channels { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Number of samples per second in the record's audio file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public long SamplesPerSecond { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Number of bits per sample in the record's audio file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public int BitsPerSample { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public Guid Guid { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public long AudioOffset { get; set; }

		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public long AudioLength { get; set; }

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the specified field is one of the
		/// interlinear fields.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool GetIsInterlinearField(string field)
		{
			return (HasInterlinearData && InterlinearFields.Contains(field));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Because deserialization cannot deserialize a dictionary, moving field values from
		/// the deserialized values list to a dictionary has to be done in a separate process.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void PostDeserializeProcess(PaDataSource dataSource, PaProject project)
		{
			DataSource = dataSource;
			Project = project;

			if (m_fieldValuesList != null && m_fieldValuesList.Count() > 0)
			{
				m_fieldValues = m_fieldValuesList.ToDictionary(fv => fv.Name, fv => fv);
				m_fieldValuesList = null;
			}

			if (WordEntries == null)
				return;
			
			int i = 0;
			foreach (var entry in WordEntries)
			{
				if (entry.RecordEntry == null)
					entry.RecordEntry = this;

				entry.PostDeserializeProcess();
				entry.WordIndex = i++;
			}
		}
	}
}
