// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005-2015, SIL International.
// <copyright from='2005' to='2015' company='SIL International'>
//		Copyright (c) 2005-2015, SIL International.
//    
//		This software is distributed under the MIT License, as specified in the LICENSE.txt file.
// </copyright> 
#endregion
// 
// File: AutoHeightRadioButton.cs
// Responsibility: Olson
// 
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;

namespace SilTools.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class extends the RadioButton control to automatically adjust the height to
	/// accomodate all the text in the control. Then it can be added to a stacked group of
	/// controls in a flow layout panel and the controls below will automatically get pushed
	/// down as the RadioButton grows.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AutoHeighRadioButton : RadioButton
	{
		/// ------------------------------------------------------------------------------------
		public AutoHeighRadioButton()
		{
			AutoSize = false;
			AutoEllipsis = true;
		}

		/// ------------------------------------------------------------------------------------
		protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
		{
			base.SetBoundsCore(x, y, width, PreferredSize.Height, specified);
		}

		/// ------------------------------------------------------------------------------------
		public override Size GetPreferredSize(Size proposedSize)
		{
			if (!string.IsNullOrEmpty(Text))
			{
				var constraints = new Size(Width, 0);

				using (var g = CreateGraphics())
				{
					proposedSize.Height = TextRenderer.MeasureText(g, Text, Font, constraints,
						TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter).Height + 4;
				}
			}

			return proposedSize;
		}

		///// ------------------------------------------------------------------------------------
		//protected override void SetBoundsCore(int x, int y, int width, int height, BoundsSpecified specified)
		//{
		//    bool isHeightSpecified = (int)((BoundsSpecified.All | BoundsSpecified.Height | BoundsSpecified.Size) &
		//        specified) > 0;

		//    if (!AutoSize && !string.IsNullOrEmpty(Text) && isHeightSpecified)
		//    {
		//        var constraints = new Size(width, 0);

		//        using (var g = CreateGraphics())
		//        {
		//            height = Math.Max(TextRenderer.MeasureText(g, Text, Font, constraints,
		//                TextFormatFlags.WordBreak | TextFormatFlags.VerticalCenter).Height + 4, height);
		//        }
		//    }

		//    base.SetBoundsCore(x, y, width, height, specified);
		//}
	}
}
