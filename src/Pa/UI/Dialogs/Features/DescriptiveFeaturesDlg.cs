﻿// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005-2015, SIL International.
// <copyright from='2005' to='2015' company='SIL International'>
//		Copyright (c) 2005-2015, SIL International.
//    
//		This software is distributed under the MIT License, as specified in the LICENSE.txt file.
// </copyright> 
#endregion
// 
using SIL.Pa.UI.Controls;
using SilTools;

namespace SIL.Pa.UI.Dialogs
{
	/// ----------------------------------------------------------------------------------------
	public partial class DescriptiveFeaturesDlg : FeaturesDlgBase
	{
		/// ------------------------------------------------------------------------------------
		public DescriptiveFeaturesDlg(FeaturesDlgViewModel viewModel)
			: base(viewModel, new DescriptiveFeatureListView())
		{
			InitializeComponent();
			_listView.EmphasizeCheckedItems = true;
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnShown(System.EventArgs e)
		{
			if (Properties.Settings.Default.DescriptiveFeaturesDlgPhoneGrid != null)
				Properties.Settings.Default.DescriptiveFeaturesDlgPhoneGrid.InitializeGrid(_gridPhones);

			_gridPhones.AdjustGridRows(Properties.Settings.Default.DescriptiveFeaturesDlgGridExtraRowHeight);
			
			int savedLoc = Properties.Settings.Default.DescriptiveFeaturesDlgSplitLoc;
			if (savedLoc > 0 && savedLoc >= _splitFeatures.Panel1MinSize)
				_splitFeatures.SplitterDistance = savedLoc;

			base.OnShown(e);
		}

		/// ------------------------------------------------------------------------------------
		protected override void SaveSettings()
		{
			Properties.Settings.Default.DescriptiveFeaturesDlgPhoneGrid = GridSettings.Create(_gridPhones);
			Properties.Settings.Default.DescriptiveFeaturesDlgSplitLoc = _splitFeatures.SplitterDistance;
			base.SaveSettings();
		}

		/// ------------------------------------------------------------------------------------
		protected override void Reset()
		{
			_viewModel.GetPhoneInfo(_gridPhones.CurrentCellAddress.Y).ResetAFeatures();
		}

		/// ------------------------------------------------------------------------------------
		protected override bool GetDoesPhoneHaveOverrides()
		{
			return GetDoesPhoneHaveOverrides(_gridPhones.CurrentCellAddress.Y);
		}

		/// ------------------------------------------------------------------------------------
		protected override bool GetDoesPhoneHaveOverrides(int rowIndex)
		{
			return _viewModel.GetPhoneInfo(rowIndex).HasAFeatureOverrides;
		}
	}
}
