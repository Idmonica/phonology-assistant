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
using System.Linq;
using System.Windows.Forms;
using L10NSharp;
using SIL.Pa.DataSource.FieldWorks;
using SIL.Pa.UI.Controls;
using SilTools;

namespace SIL.Pa.UI.Dialogs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A dialog that allows the user to specify a FieldWorks database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwProjectsDlg : OKCancelDlgBase
	{
		/// ------------------------------------------------------------------------------------
		public FwProjectsDlg()
		{
			InitializeComponent();

			lblMsg.Font = FontHelper.UIFont;
			lstFwProjects.Font = FontHelper.UIFont;
			lblProjects.Font = FontHelper.UIFont;
			lblNetwork.Font = FontHelper.UIFont;
			tvNetwork.Font = FontHelper.UIFont;
			txtMsg.Font = FontHelper.UIFont;

			txtMsg.Dock = DockStyle.Fill;
			txtMsg.BringToFront();

			lblProjects.Height = FontHelper.UIFont.Height + 10;
			lblNetwork.Height = FontHelper.UIFont.Height + 10;

			tvNetwork.Load();
			Application.Idle += Application_Idle;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the properties button is only enabled when there's a selected database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Application_Idle(object sender, EventArgs e)
		{
			btnOK.Enabled = (ChosenDatabase != null);
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			var loc = Properties.Settings.Default.FwProjectsDlgSplitLoc;
			if (loc > 0 && loc >= splitContainer1.Panel1MinSize)
				splitContainer1.SplitterDistance = loc;
		}

		/// ------------------------------------------------------------------------------------
		protected override void SaveSettings()
		{
			Properties.Settings.Default.FwProjectsDlgSplitLoc = splitContainer1.SplitterDistance;
			base.SaveSettings();
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnClosed(EventArgs e)
		{
			Application.Idle -= Application_Idle;
			base.OnClosed(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the chosen database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwDataSourceInfo ChosenDatabase
		{
			get { return lstFwProjects.SelectedItem as FwDataSourceInfo; }
		}

		/// ------------------------------------------------------------------------------------
		private void HandleNetworkTreeViewAfterSelect(object sender, TreeViewEventArgs e)
		{
			var node = e.Node as NetworkTreeNode;
			if (node == null)
				return;

			Utils.WaitCursors(true);
			btnOK.Enabled = false;
			lstFwProjects.SelectedIndex = -1;
			lstFwProjects.Items.Clear();
			txtMsg.Text = string.Empty;
			txtMsg.Visible = true;
			lstFwProjects.Visible = false;

			if (!string.IsNullOrEmpty(node.MachineName))
			{
				txtMsg.Text = LocalizationManager.GetString("DialogBoxes.Fw6ProjectsDlg.SearchingForFwDatabasesMsg", "Searching...");
				txtMsg.Visible = true;
				Application.DoEvents();

				lstFwProjects.Items.Clear();
				
				var dsInfo = FwDBUtils.GetFwDataSourceInfoList(node.MachineName, true).ToArray();
				if (dsInfo.Length > 0)
				{
					lstFwProjects.Items.AddRange(dsInfo);
					lstFwProjects.SelectedIndex = 0;
					lstFwProjects.Visible = true;
					txtMsg.Visible = false;
				}
				else
				{
					var fmt = LocalizationManager.GetString("DialogBoxes.Fw6ProjectsDlg.NoFwProjectsFoundMsg", "No projects found on '{0}'.");
					txtMsg.Text = string.Format(fmt, node.MachineName);
				}
			}

			Utils.WaitCursors(false);
		}

        protected override bool ProcessCmdKey(ref Message message, Keys keys)
        {
            switch (keys)
            {
                case Keys.Escape:
                    {
                        this.Close();
                        return true;
                    }
                case Keys.Control | Keys.Tab:
                    {
                        return true;
                    }
                case Keys.Control | Keys.Shift | Keys.Tab:
                    {
                        return true;
                    }
            }
            return base.ProcessCmdKey(ref message, keys);
        }
	}
}