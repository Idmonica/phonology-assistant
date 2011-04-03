// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.   
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
#endregion
// 
// File: IPACharInfo.cs
// Responsibility: D. Olson
// 
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Xml.Serialization;

namespace SIL.Pa.Model
{
	/// ----------------------------------------------------------------------------------------
	public interface IFeatureBearer
	{
		FeatureMask AMask { get; set; }
		FeatureMask BMask { get; set; }
		void ResetAFeatures();
		void ResetBFeatures();
	}

	#region IPASymbol class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores information about IPA characters.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("symbol")]
	public class IPASymbol : IFeatureBearer
	{
		[XmlIgnore]
		public bool IsUndefined { get; set; }

		[XmlAttribute("decimal")]
		public int Decimal { get; set; }
		
		[XmlAttribute("literal")]
		public string Literal { get; set; }

		[XmlAttribute("hexadecimal")]
		public string Hexadecimal { get; set; }

		[XmlAttribute("IPANumber")]
		public string IPANumber { get; set; }

		[XmlElement("name")]
		public string Name { get; set; }

		[XmlElement("usage")]
		public IPASymbolUsage Usage { get; set; }

		[XmlElement("description")]
		public string Description { get; set; }

		[XmlElement("type")]
		public IPASymbolType Type { get; set; }

		[XmlElement("subtype")]
		public IPASymbolSubType SubType { get; set; }

		[XmlElement("ignoreType")]
		public IPASymbolIgnoreType IgnoreType { get; set; }

		[XmlElement("isBase")]
		public bool IsBase { get; set; }

		[XmlElement("canPrecedeBase")]
		public bool CanPrecedeBase { get; set; }

		[XmlElement("displayWithDottedCircle")]
		public bool DisplayWithDottedCircle { get; set; }

		[XmlElement("mannerOfArticulation")]
		public int MOArticulation { get; set; }

		[XmlElement("placeOfArticulation")]
		public int POArticulation { get; set; }

		[XmlElement("chartColumn")]
		public int ChartColumn { get; set; }
		
		[XmlElement("chartGroup")]
		public int ChartGroup { get; set; }

		private List<string> m_aFeatures;
		private List<string> m_bFeatures;
		private FeatureMask m_aMask;
		private FeatureMask m_bMask;

		/// ------------------------------------------------------------------------------------
		public IPASymbol Copy()
		{
			return new IPASymbol
			{
				IsUndefined = IsUndefined,
				Decimal = Decimal,
				Literal = Literal,
				Hexadecimal = Hexadecimal,
				IPANumber = IPANumber,
				Name = Name,
				Usage = Usage,
				Description = Description,
				Type = Type,
				SubType = SubType,
				IgnoreType = IgnoreType,
				IsBase = IsBase,
				CanPrecedeBase = CanPrecedeBase,
				DisplayWithDottedCircle = DisplayWithDottedCircle,
				MOArticulation = MOArticulation,
				POArticulation = POArticulation,
				AMask = AMask.Clone(),
				BMask = BMask.Clone(),
			};
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list of articulatory features.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray("articulatoryFeatures"), XmlArrayItem("feature")]
		public List<string> AFeatures
		{
			get
			{
				return (m_aFeatures == null && m_aMask != null && !m_aMask.IsEmpty ?
					InventoryHelper.AFeatureCache.GetFeatureList(m_aMask) : m_aFeatures);
			}
			set { m_aFeatures = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the list of binary features.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlArray("binaryFeatures"), XmlArrayItem("feature")]
		public List<string> BFeatures
		{
			get
			{
				return (m_bFeatures == null && m_bMask != null && !m_bMask.IsEmpty ?
					InventoryHelper.BFeatureCache.GetFeatureList(m_bMask) : m_bFeatures);
			}
			set { m_bFeatures = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the articulatory features mask.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public FeatureMask AMask
		{
			get
			{
				if (m_aMask == null || m_aMask.IsEmpty)
				{
					m_aMask = InventoryHelper.AFeatureCache.GetMask(m_aFeatures);
					if (m_aFeatures != null && m_aFeatures.Count > 0)
						m_aFeatures = null;
				}

				return m_aMask;
			}
			set { m_aMask = (value ?? InventoryHelper.AFeatureCache.GetEmptyMask()); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the binary features mask.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlIgnore]
		public FeatureMask BMask
		{
			get
			{
				if (m_bMask == null || m_bMask.IsEmpty)
				{
					m_bMask = InventoryHelper.BFeatureCache.GetMask(m_bFeatures);
					if (m_bFeatures != null && m_bFeatures.Count > 0)
						m_bFeatures = null;
				}

				return m_bMask;
			}
			set { m_bMask = (value ?? InventoryHelper.BFeatureCache.GetEmptyMask()); }
		}

		/// ------------------------------------------------------------------------------------
		public void ResetAFeatures()
		{
		}

		/// ------------------------------------------------------------------------------------
		public void ResetBFeatures()
		{
		}

		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return string.Format("{0}: U+{1:X4}, {2}, {3}", Literal, Decimal, Name, Description);
		}
	}

	#endregion

	#region IPASymbolUsage
	/// ----------------------------------------------------------------------------------------
	[XmlType("usage")]
	public class IPASymbolUsage
	{
		[XmlAttribute("replaceWith")]
		public string ReplaceWith { get; set; }

		[XmlText]
		public string Information { get; set; }
	}

	#endregion
}
