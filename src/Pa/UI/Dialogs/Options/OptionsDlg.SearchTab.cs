// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005-2015, SIL International.
// <copyright from='2005' to='2015' company='SIL International'>
//		Copyright (c) 2005-2015, SIL International.
//    
//		This software is distributed under the MIT License, as specified in the LICENSE.txt file.
// </copyright> 
#endregion
// 
namespace SIL.Pa.UI.Dialogs
{
	public partial class OptionsDlg
	{
		///// ------------------------------------------------------------------------------------
		//private void InitializeFindPhonesTab()
		//{
		//    // This tab isn't valid if there is no project loaded.
		//    if (App.Project == null)
		//    {
		//        tabOptions.TabPages.Remove(tpgFindPhones);
		//        return;
		//    }
			
		//    lblClassDisplayBehavior.Font = FontHelper.UIFont;
		//    rdoClassName.Font = FontHelper.UIFont;
		//    rdoClassMembers.Font = FontHelper.UIFont;
		//    chkShowDiamondPattern.Font = FontHelper.UIFont;
		//    lblShowDiamondPattern.Font = FontHelper.UIFont;

		//    lblShowDiamondPattern.Text = string.Format(lblShowDiamondPattern.Text,
		//        App.kEmptyDiamondPattern);

		//    // Adjust the height of the label control to fit the text more tightly.
		//    using (Graphics g = lblClassDisplayBehavior.CreateGraphics())
		//    {
		//        lblClassDisplayBehavior.Height = (int)Math.Ceiling(
		//            g.MeasureString(lblClassDisplayBehavior.Text, FontHelper.UIFont,
		//            lblClassDisplayBehavior.ClientSize.Width).Height) + 2;
		//    }

		//    rdoClassName.Top = lblClassDisplayBehavior.Bottom + 6;
		//    rdoClassMembers.Top = rdoClassName.Bottom + 4;
		//    rdoClassName.Checked = App.Project.ShowClassNamesInSearchPatterns;
		//    rdoClassMembers.Checked = !rdoClassName.Checked;

		//    chkShowDiamondPattern.Checked = App.Project.ShowDiamondsInEmptySearchPattern;
		//}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// 
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private bool IsFindPhoneTabDirty
		//{
		//    get
		//    {
		//        return rdoClassName.Checked != App.Project.ShowClassNamesInSearchPatterns ||
		//          chkShowDiamondPattern.Checked != App.Project.ShowDiamondsInEmptySearchPattern;
		//    }
		//}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Saves changed find phones information if needed.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private void SaveFindPhonesTabChanges()
		//{
		//    if (!IsFindPhoneTabDirty)
		//        return;

		//    if (rdoClassName.Checked != App.Project.ShowClassNamesInSearchPatterns)
		//    {
		//        App.Project.ShowClassNamesInSearchPatterns = rdoClassName.Checked;
		//        App.MsgMediator.SendMessage("ClassDisplayBehaviorChanged", null);
		//    }

		//    App.Project.ShowDiamondsInEmptySearchPattern = chkShowDiamondPattern.Checked;
		//    App.MsgMediator.SendMessage("FindPhonesSettingsChanged", null);
		//}
	}
}
