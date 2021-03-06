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
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using L10NSharp;
using SIL.Pa.Model;
using SIL.Pa.PhoneticSearching;
using SIL.Pa.UI.Controls;
using SilTools;

namespace SIL.Pa.UI.Dialogs
{
	/// ----------------------------------------------------------------------------------------
	public partial class DefineFeatureClassDlgBase : DefineClassBaseDlg
	{
		protected readonly FeatureListViewBase _lvFeatures;
		protected PhonesInFeatureViewer _conViewer;
		protected PhonesInFeatureViewer _vowViewer;
		protected SplitContainer _splitterOuter;
		protected SplitContainer _splitterCV;
		
		private RadioButton _radioMatchAll;
		private RadioButton _radioMatchAny;
		private bool _splitterSettingsLoaded;

		#region Construction and setup
		/// ------------------------------------------------------------------------------------
		private DefineFeatureClassDlgBase()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		public DefineFeatureClassDlgBase(ClassListViewItem classInfo, ClassesDlg classDlg,
			FeatureListViewBase lvFeatures, FeatureMask emptyMask)
			: base(classInfo ?? new ClassListViewItem { ClassType = SearchClassType.Articulatory }, classDlg)
		{
			_lvFeatures = lvFeatures;
			_lvFeatures.Load();
			_lvFeatures.Dock = DockStyle.Fill;
			_lvFeatures.Visible = true;
			_lvFeatures.LabelEdit = false;
			_lvFeatures.FeatureChanged += HandleFeatureChanged;
			_lvFeatures.TabIndex = _textBoxClassName.TabIndex + 1;
			_lvFeatures.CurrentMask = (m_classInfo.Mask ?? emptyMask);
			
			SetupPhoneViewers();
			SetupSplitters();
			SetupRadioButtons();
			UpdateCharacterViewers();
		}

		/// ------------------------------------------------------------------------------------
		private void SetupRadioButtons()
		{
			// TODO: Internationalize these radio buttons.
			
			_radioMatchAll = new RadioButton();
			_radioMatchAll.AutoSize = true;
			_radioMatchAll.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			_radioMatchAll.Font = FontHelper.UIFont;
			_radioMatchAll.BackColor = Color.Transparent;
			_radioMatchAll.Margin = new Padding(3, 6, 8, 3);
			_radioMatchAll.TabIndex = _textBoxMembers.TabIndex + 1;
			_radioMatchAll.TabStop = true;
			_radioMatchAll.CheckedChanged += HandleScopeClick;
			_radioMatchAll.Text = LocalizationManager.GetString(
				"DialogBoxes.DefineClassDlgBase.MatchAllSelectedFeaturesRadioButton",
				"Match A&ll Selected Features", null, _radioMatchAll);

			_radioMatchAny = new RadioButton();
			_radioMatchAny.AutoSize = true;
			_radioMatchAny.Anchor = AnchorStyles.Left | AnchorStyles.Right;
			_radioMatchAny.Font = FontHelper.UIFont;
			_radioMatchAny.BackColor = Color.Transparent;
			_radioMatchAny.Margin = new Padding(7, 6, 3, 3);
			_radioMatchAny.TabIndex = _radioMatchAll.TabIndex + 1;
			_radioMatchAny.TabStop = true;
			_radioMatchAny.CheckedChanged += HandleScopeClick;
			_radioMatchAny.Text = LocalizationManager.GetString(
				"DialogBoxes.DefineClassDlgBase.MatchAnySelectedFeaturesRadioButton",
				"Match A&ny Selected Features", null, _radioMatchAny);
				
			_tableLayout.Controls.Add(_radioMatchAll, 1, 3);
			_tableLayout.Controls.Add(_radioMatchAny, 2, 3);

			_radioMatchAll.Checked = m_classInfo.ANDFeatures;
			_radioMatchAny.Checked = !_radioMatchAll.Checked;
		}

		/// ------------------------------------------------------------------------------------
		private void SetupSplitters()
		{
			_splitterCV = GetSplitter(59, 0);
			_splitterCV.Panel1.Controls.Add(_conViewer);
			_splitterCV.Panel2.Controls.Add(_vowViewer);

			_splitterOuter = GetSplitter(89, 1);
			_splitterOuter.BackColor = SystemColors.Control;
			_splitterOuter.Orientation = Orientation.Horizontal;
			_splitterOuter.Panel2.Controls.Add(_lvFeatures);
			_splitterOuter.Panel1.Controls.Add(_splitterCV);
			
			_panelMemberPickingContainer.Controls.Add(_splitterOuter);
		}

		/// ------------------------------------------------------------------------------------
		private SplitContainer GetSplitter(int splitDistance, int tabIndex)
		{
			var splitter = new SplitContainer();
			splitter.SplitterDistance = splitDistance;
			splitter.TabIndex = tabIndex;
			splitter.SplitterWidth = 6;
			splitter.Panel1.BackColor = SystemColors.Window;
			splitter.Panel2.BackColor = SystemColors.Window;
			splitter.Dock = DockStyle.Fill;
			return splitter;
		}

		/// ------------------------------------------------------------------------------------
		private void SetupPhoneViewers()
		{
			_conViewer = new PhonesInFeatureViewer(
				m_classesDlg.Project.PhoneCache.Values.Where(p => p.CharType == IPASymbolType.consonant).Select(p => p as PhoneInfo).OrderBy(p => p.MOAKey),
				UseCompactConsonantView, compactVw => UseCompactConsonantView = compactVw);
			
			_conViewer.HeaderText = LocalizationManager.GetString("DialogBoxes.DefineFeatureClassDlgBase.ConsonantViewerHeaderText", "&Consonants");
			_conViewer.Dock = DockStyle.Fill;

			_vowViewer = new PhonesInFeatureViewer(
				m_classesDlg.Project.PhoneCache.Values.Where(p => p.CharType == IPASymbolType.vowel).Select(p => p as PhoneInfo).OrderBy(p => p.MOAKey),
				UseCompactVowelView, compactVw => UseCompactVowelView = compactVw);

			_vowViewer.HeaderText = LocalizationManager.GetString("DialogBoxes.DefineFeatureClassDlgBase.VowelViewerHeaderText", "&Vowels");
			_vowViewer.Dock = DockStyle.Fill;
		}

		protected virtual bool UseCompactConsonantView { get; set; }
		protected virtual bool UseCompactVowelView { get; set; }

		/// ------------------------------------------------------------------------------------
		protected virtual void LoadSplitterSettings()
		{
			_splitterSettingsLoaded = true;
		}

		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			if (!_splitterSettingsLoaded)
				LoadSplitterSettings();

			UpdateCharacterViewers();

			base.OnShown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the pattern that would be built from the contents of the members text box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string CurrentPattern
		{
			get { return _textBoxMembers.Text.Trim(); }
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the user choosing a feature.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleFeatureChanged(object sender, FeatureMask newMask)
		{
			m_classInfo.Mask = newMask;
			_textBoxMembers.Text = m_classInfo.FormattedMembersString;
			m_classInfo.IsDirty = true;
			UpdateCharacterViewers();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the user choosing one of the items in the tsbWhatToInclude drop-down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleScopeClick(object sender, EventArgs e)
		{
			m_classInfo.ANDFeatures = _radioMatchAll.Checked;
			_textBoxMembers.Text = m_classInfo.FormattedMembersString;
			m_classInfo.IsDirty = true;
			UpdateCharacterViewers();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the appropriate character viewer is up to date.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void UpdateCharacterViewers()
		{
			if (!DesignMode)
				throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For classes other than for IPA characters, delete all the text when the user
		/// presses the backspace or delete keys.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void HandleMembersTextBoxKeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
			{
				_textBoxMembers.Text = string.Empty;
				m_classInfo.Mask.Clear();
					
				// Fix for PA-555
				_lvFeatures.CurrentMask.Clear();
			}
		}

		#endregion
	}
}