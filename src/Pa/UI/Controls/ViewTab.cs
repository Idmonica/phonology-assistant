using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using SIL.Pa.Model;
using SilTools;

namespace SIL.Pa.UI.Controls
{
	/// ----------------------------------------------------------------------------------------
	public class ViewTab : Label
	{
		private static bool s_viewSelectionInProgress;
		private bool m_ignoreTabSelection;
		private Point m_mouseDownLocation = Point.Empty;
		private bool m_mouseOver;
		private bool m_selected;
		private UndockedViewWnd m_viewsForm;
		private bool m_viewDocked;
		private bool m_undockingInProgress;
		private readonly PaProject _project;

		/// <summary>
		/// This flag gets set when a view is undocking. Suppose view A is being undocked.
		/// That means  another view in the main application window has to become active within
		/// the main window. Suppose that view is B. If view B was not previously opened
		/// (opening a view for the first time also triggers a docking of that view) then it
		/// will be and this flag will prevent the opening and docking of view B from causing
		/// its parent window (i.e. the main application window) to become activated, which
		/// is the normal behavior when a view is docked.
		/// </summary>
		private static bool s_undockingInProgress;

		public Func<string> GetHelpToolTipAction { get; set; }

		/// ------------------------------------------------------------------------------------
		public ViewTab(PaProject project, ViewTabGroup owningTabControl, Image img, Type viewType)
		{
			base.DoubleBuffered = true;
			base.AutoSize = false;
			base.AllowDrop = true;
			base.Font = ViewTabGroup.s_tabFont;

			_project = project;
			OwningTabGroup = owningTabControl;
			ViewType = viewType;
			TabImage = img;

			if (App.MainForm != null)
				App.MainForm.Activated += MainForm_Activated;
		}

		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (App.MainForm != null)
					App.MainForm.Activated -= MainForm_Activated;

				if (m_viewsForm != null && !m_viewsForm.IsDisposed)
				{
					m_viewsForm.FormClosing -= m_viewsForm_FormClosing;
					m_viewsForm.FormClosed -= m_viewsForm_FormClosed;
					m_viewsForm.Activated -= m_viewsForm_Activated;
					m_viewsForm.Dispose();
				}
			}
			
			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		public Control OpenView()
		{
			App.StatusBarLabel.Text = string.Empty;

			// Check if the view is already loaded.
			if (View != null || ViewType == null)
			{
				if (m_viewsForm != null)
					m_viewsForm.Activate();
				else if ( View != null)
					View.Visible = true;

				return View;
			}

			// Create an instance of the view's form
			View = (Control)ViewType.Assembly.CreateInstance(ViewType.FullName, false,
				BindingFlags.CreateInstance, null, new[] { _project }, null, null);
			
			App.MsgMediator.SendMessage("BeginViewOpen", View);
			View.Dock = DockStyle.Fill;

			if (!(View is ITabView))
				Utils.MsgBox(string.Format("Error: {0} is not based on ITabView!", ViewType));

			try
			{
				if (View is IxCoreColleague)
					App.AddMediatorColleague(View as IxCoreColleague);
			}
			catch { }

			DockView();
			App.MsgMediator.SendMessage("ViewOpened", View);
			return View;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the tab's view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseView()
		{
			if (m_undockingInProgress || View == null)
				return;

			App.MsgMediator.SendMessage("BeginViewClosing", View);
			Visible = true;

			if (!View.IsDisposed)
			{
				if (OwningTabGroup != null && OwningTabGroup.Controls.Contains(View))
					OwningTabGroup.Controls.Remove(View);

				if (View is IxCoreColleague)
					App.RemoveMediatorColleague(View as IxCoreColleague);

				if (View is ITabView)
					((ITabView)View).TMAdapter.Dispose();

				View.Dispose();
				View = null;
			}

			if (m_viewsForm != null)
				m_viewsForm.Close();

			App.MsgMediator.SendMessage("ViewClosed", ViewType);
			m_viewDocked = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the view's control from its form to the tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void DockView()
		{
			if (m_undockingInProgress || m_viewDocked)
				return;

			App.MsgMediator.SendMessage("BeginViewDocking", View);
			Utils.SetWindowRedraw(OwningTabGroup, false, false);
			Visible = true;
			
			OwningTabGroup.ViewWasDocked(this);
			View.Size = OwningTabGroup.ClientSize;
			OwningTabGroup.Controls.Add(View);
			View.PerformLayout();
			View.BringToFront();

			m_viewDocked = true;
			m_ignoreTabSelection = true;
			OwningTabGroup.SelectTab(this);
			m_ignoreTabSelection = false;

			Utils.SetWindowRedraw(OwningTabGroup, true, true);
			View.Focus();
			OwningTabGroup.SetActiveView(View as ITabView, false);
			App.MsgMediator.SendMessage("ViewDocked", View);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves the view's control from its form to the tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UnDockView()
		{
			if (s_undockingInProgress || !m_viewDocked)
				return;

			App.MsgMediator.SendMessage("BeginViewUnDocking", View);
			m_undockingInProgress = true;
			s_undockingInProgress = true;

			if (OwningTabGroup.Controls.Contains(View))
				OwningTabGroup.Controls.Remove(View);

			// Prepare the undocked view's form to host the view and be displayed.
			m_viewsForm = new UndockedViewWnd(_project, View);
			m_viewsForm.FormClosing += m_viewsForm_FormClosing;
			m_viewsForm.FormClosed += m_viewsForm_FormClosed;
			m_viewsForm.Activated += m_viewsForm_Activated;

			if (TabImage != null)
				m_viewsForm.Icon = Icon.FromHandle(((Bitmap)TabImage).GetHicon());
			
			// Strip out accelerator key prefixes but keep ampersands that should be kept.
			var caption = Utils.RemoveAcceleratorPrefix(Text);
			var fmt = App.GetString("UndockedViewCaptionFormat", "{0} ({1}) - {2}",
				"Parameter one is the project name; parameter 2 is the view name; parameter 3 is the application name.");
			m_viewsForm.Text = string.Format(fmt, App.Project.Name, caption, Application.ProductName);
			
			Visible = false;

			// Inform the tab group that one of it's views has been undocked.
			m_ignoreTabSelection = true;
			OwningTabGroup.ViewWasUnDocked(this);
			m_ignoreTabSelection = false;
			s_undockingInProgress = false;
			m_viewDocked = false;
			m_undockingInProgress = false;

			m_viewsForm.Show();
			m_viewsForm.Activate();
			App.MsgMediator.SendMessage("ViewUndocked", View);
		}

		/// ------------------------------------------------------------------------------------
		private void MainForm_Activated(object sender, EventArgs e)
		{
			if (m_viewDocked && View != null && View.Visible)
				OwningTabGroup.SetActiveView(View as ITabView, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the global view and view type are set when an undocked view gets focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void m_viewsForm_Activated(object sender, EventArgs e)
		{
			OwningTabGroup.SetActiveView(View as ITabView, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When closing because the user closed the window, we just want to treat that as
		/// docking back into the tab. So dock the control and cancel the closing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_viewsForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			if (m_undockingInProgress)
			{
				e.Cancel = true;
				return;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure to clean up after a view is closed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_viewsForm_FormClosed(object sender, FormClosedEventArgs e)
		{
			m_viewsForm.FormClosing -= m_viewsForm_FormClosing;
			m_viewsForm.FormClosed -= m_viewsForm_FormClosed;
			m_viewsForm.Activated -= m_viewsForm_Activated;
			
			if (View != null)
				DockView();
			
			m_viewsForm.Dispose();
			m_viewsForm = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes and reopens the view's form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshView()
		{
			CloseView();
			OpenView();
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		public Image TabImage { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the help topic ID for the view's overview topic.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string HelpTopicId { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tab's view type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Type ViewType { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tab's view form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Control View { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether or not the tab's view is docked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsViewDocked
		{
			get { return m_viewDocked; }
			set
			{
				if (m_viewDocked != value)
				{
					if (value)
						DockView();
					else
						UnDockView();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		public bool Selected
		{
			get { return m_selected; }
			set
			{
				if (m_selected == value || s_viewSelectionInProgress)
					return;

				s_viewSelectionInProgress = true;
				m_selected = value;
				Invalidate();
				Utils.UpdateWindow(Handle);

				// Invalidate the tab to the left of this one in
				// case it needs to redraw its etched right border.
				ViewTab adjacentTab = OwningTabGroup.FindFirstVisibleTabToLeft(this);
				if (adjacentTab != null)
				{
					adjacentTab.Invalidate();
					Utils.UpdateWindow(adjacentTab.Handle);
				}

				if (!m_ignoreTabSelection)
				{
					if (value)
						OpenView();
					else if (View != null && m_viewDocked)
						View.Visible = false;
				}

				s_viewSelectionInProgress = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the tab's owning group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ViewTabGroup OwningTabGroup { get; set; }

		#endregion

		#region Overridden methods and event handlers
		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Make sure the current tab is selected when its grid get's focus.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private void HandleResultViewEnter(object sender, EventArgs e)
		//{
		//    if (!m_selected || !m_owningTabGroup.IsCurrent)
		//        m_owningTabGroup.SelectTab(this, true);
		//}

		/// ------------------------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				m_mouseDownLocation = e.Location;
			else
			{
				Form frm = FindForm();
				if (!App.IsFormActive(frm))
					frm.Focus();
			}

			base.OnMouseDown(e);
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			m_mouseDownLocation = Point.Empty; 
			base.OnMouseUp(e);
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// This will be empty when the mouse button is not down.
			if (m_mouseDownLocation.IsEmpty)
				return;
			
			// Begin draging a tab when the mouse is held down
			// and has moved 4 or more pixels in any direction.
			int dx = Math.Abs(m_mouseDownLocation.X - e.X);
			int dy = Math.Abs(m_mouseDownLocation.Y - e.Y);
			if (dx >= 4 || dy >= 4)
			{
				m_mouseDownLocation = Point.Empty;
				DoDragDrop(this, DragDropEffects.Move);
			}
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnMouseEnter(EventArgs e)
		{
			m_mouseOver = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnMouseLeave(EventArgs e)
		{
			m_mouseOver = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			DrawBackground(e.Graphics);
			DrawImage(e.Graphics);
			DrawText(e.Graphics);
			DrawHoverIndicator(e.Graphics);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the tab's background.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawBackground(Graphics g)
		{
			var rc = ClientRectangle;

			// First, fill the entire background with the control color.
			using (var br = new SolidBrush(AppColor.ViewTabGroupBackground))
				g.FillRectangle(br, rc);

			if (m_selected)
			{
				var pts = new[] {new Point(0, rc.Bottom), new Point(0, rc.Top + 3),
					new Point(3, 0), new Point(rc.Right - 4, 0), new Point(rc.Right - 1, rc.Top + 3),
					new Point(rc.Right - 1, rc.Bottom)};

				using (var br = new SolidBrush(Color.White))
					g.FillPolygon(br, pts);

				using (var pen = new Pen(AppColor.ViewTabBackgroundActiveBorder))
					g.DrawLines(pen, pts);
			}
			else
			{
				// Draw the etched line on the right edge to act as a separator. But
				// only draw it when the tab to the right of this one is not selected.
				if (!OwningTabGroup.IsRightAdjacentTabSelected(this))
				{
					g.DrawLine(SystemPens.ControlDark, rc.Width - 2, 1, rc.Width - 2, rc.Height - 5);
					g.DrawLine(SystemPens.ControlLight, rc.Width - 1, 1, rc.Width - 1, rc.Height - 5);
				}

				// The tab is not selected tab, so draw a
				// line across the bottom of the tab.
				g.DrawLine(SystemPens.ControlDark, 0, rc.Bottom - 1, rc.Right, rc.Bottom - 1);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw's the tab's image.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawImage(Graphics g)
		{
			if (TabImage == null)
				return;

			var rc = ClientRectangle;
			rc.X = 7;
			rc.Y = (rc.Height - TabImage.Height) / 2;
			rc.Size = TabImage.Size;

			if (m_selected)
				rc.Y++;

			g.DrawImage(TabImage, rc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw the tab's text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawText(IDeviceContext g)
		{
			const TextFormatFlags kFlags = TextFormatFlags.VerticalCenter |
				TextFormatFlags.HorizontalCenter | TextFormatFlags.WordEllipsis |
				TextFormatFlags.SingleLine | TextFormatFlags.NoPadding |
				TextFormatFlags.HidePrefix | TextFormatFlags.PreserveGraphicsClipping;

			var clrText = (m_selected ? AppColor.ViewTabForegroundActive :
				AppColor.ViewTabForegroundInactive);
			
			var rc = ClientRectangle;

			// Account for the image if there is one.
			if (TabImage != null)
			{
				rc.X += (5 + TabImage.Width);
				rc.Width -= (5 + TabImage.Width);
			}

			// When the tab is selected, then bump the text down a couple of pixels.
			if (m_selected)
			{
				rc.Y += 2;
				rc.Height -= 2;
			}

			TextRenderer.DrawText(g, Text, Font, rc, clrText, kFlags);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the mouse is over the tab, draw a line across the top to hightlight the tab
		/// the mouse is over.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawHoverIndicator(Graphics g)
		{
			if (!m_mouseOver)
				return;

			var rc = ClientRectangle;

			// Draw the lines that only show when the mouse is over the tab.
			using (Pen pen = new Pen(AppColor.ViewTabMouseHoverLine))
			{
				if (m_selected)
				{
					g.DrawLine(pen, 3, 1, rc.Right - 4, 1);
					g.DrawLine(pen, 2, 2, rc.Right - 3, 2);
				}
				else
				{
					g.DrawLine(pen, 0, 0, rc.Right - 3, 0);
					g.DrawLine(pen, 0, 1, rc.Right - 3, 1);
				}
			}
		}

		#endregion
	}
}
