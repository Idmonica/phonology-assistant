// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.   
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.   
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
#endregion
// 
// File: Commands.cs
// Authorship History: John Hatton  
// Last reviewed: 
// 
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Xml;
using System.Collections;
using System.Globalization;
using System.Windows.Forms;

using SIL.FieldWorks.Common.Utils;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for Commands.
	/// </summary>
	public class CommandSet : Hashtable, IFWDisposable
	{
		protected Mediator m_mediator;
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="Commands"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public CommandSet(Mediator mediator)
		{
			m_mediator = mediator;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~CommandSet()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue 
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		/// 
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 
		/// 2. If disposing is false, the method has been called by the 
		/// runtime from inside the finalizer and you should not reference (access) 
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// 
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				foreach (Command cmd in Values)
					cmd.Dispose();
				Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		public void Init(XmlNode windowNode)
		{
			CheckDisposed();
			XmlNodeList commands =windowNode.SelectNodes("commands/command");
			foreach (XmlNode node in commands)
			{
				Command command = new Command(m_mediator, node);
				this.Add(command.Id, command);
			}
		}
	}

	//!!!!!!!!!!!!!!!!!1
	//listSets objects are currently unused, as we are just accessing the raw XML for now.
	//I have not figured out whether this raw approach will last once we have dynamic lists
	//randy, now please resist the temptation to delete this stuff
//	public class ChoiceListSet : Hashtable
//	{
//		/// -----------------------------------------------------------------------------------
//		/// <summary>
//		/// Initializes a new instance of the <see cref="ListSet"/> class.
//		/// </summary>
//		/// -----------------------------------------------------------------------------------
//		public ChoiceListSet()
//		{
//		}
//
//		public void Init(Mediator mediator,XmlNode windowNode)
//		{
//			XmlNodeList lists =windowNode.SelectNodes("lists/list");
//			foreach (XmlNode node in lists)
//			{
//				ChoiceList list = new ChoiceList(mediator, node);
//				this.Add(list.ID, list);
//			}
//		}
//	}
/*
	public class ChoiceList : ArrayList
	{
		protected string m_id;
		protected XmlNode m_configurationNode;

		public ChoiceList(Mediator mediator, XmlNode configurationNode)
		{
			m_configurationNode= configurationNode;
			m_id = XmlUtils.GetAttributeValue(configurationNode, "id");
			XmlNodeList lists =configurationNode.SelectNodes("item");
			foreach (XmlNode node in lists)
			{
				Command command = new Command(mediator, node);
				this.Add(command);
			}
		}
		public string ID
		{
			get
			{
				return m_id;
			}
		}
		public XmlNode ConfigurationXml
		{
			get
			{
				return m_configurationNode;
			}
		}
	}
*/

	public class Command : IFWDisposable
	{
		#region Fields
		protected Mediator m_mediator;
		protected string m_id;
		protected string m_label;
		protected string m_shortcut;
		protected string m_toolTipInsert;
		//only one of these two will be used
		protected string m_valueString;
		protected string m_messageString;

		protected XmlNode m_configurationNode;
		#endregion Fields

		#region Properties
		public string Id
		{
			get
			{
				CheckDisposed();
				return m_id;
			}
		}

		public string Label
		{
			get
			{
				CheckDisposed();
				return m_label;
			}
		}

		/// <summary>
		/// A string to be inserted later into a toolTip associated with the command. 
		/// Set by AdjustInsertCommandName().
		/// </summary>
		public string ToolTipInsert
		{
			get
			{
				CheckDisposed();
				return m_toolTipInsert;
			}
			set
			{
				CheckDisposed();
				m_toolTipInsert = value;
			}
		}

		/// <summary>
		/// It might seem strange to give this a way to strangers, but it is needed for the strangers to create helpful error messages.
		/// </summary>
		public XmlNode ConfigurationNode
		{
			get
			{
				CheckDisposed();
				return m_configurationNode;
			}
		}

		public string ValueString
		{
			get
			{
				CheckDisposed();
				return m_valueString;
			}
		}
		public string IconName
		{
			get
			{
				CheckDisposed();
				return  XmlUtils.GetAttributeValue(m_configurationNode, "icon");
			}
		}		
		//used for choice lists

		//used for command items
		public string MessageString
		{
			get
			{
				CheckDisposed();
				return m_messageString;
			}
		}

		public System.Windows.Forms.Keys Shortcut
		{
			get
			{
				CheckDisposed();
				if(m_configurationNode==null)
					return Keys.None;

				string sc = XmlUtils.GetAttributeValue(m_configurationNode, "shortcut", "");
				if (sc != "")
				{
					// Note: On a German system, KeysConverter().ConvertFromString, ConvertToString,
					// ConvertToInvariantString, etc. all use German strings (e.g., Strg for Ctrl)
					// regardless of using the locale specific calls. So to get things to work, we
					// need to parse the English shortcut string and reconstruct the internal keys.
					Keys keys = Keys.None;
					try
					{
						string s;
						string srest = sc.ToLower();
						while (srest.Length > 0)
						{
							int i = srest.IndexOf('+');
							if (i < 0)
							{
								s = srest;
								srest = "";
							}
							else
							{
								s = srest.Substring(0, i);
								srest = srest.Substring(i + 1);
							}
							switch (s)
							{
								default:
									keys |= (Keys)new KeysConverter().ConvertFromString(s.ToUpper());
									break;
								case "alt":
									keys |= Keys.Alt;
									break;
								case "back":
									keys |= Keys.Back;
									break;
								case "cancel":
									keys |= Keys.Cancel;
									break;
								case "ctrl":
									keys |= Keys.Control;
									break;
								case "del":
									keys |= Keys.Delete;
									break;
								case "down":
									keys |= Keys.Down;
									break;
								case "end":
									keys |= Keys.End;
									break;
								case "enter":
									keys |= Keys.Enter;
									break;
								case "esc":
									keys |= Keys.Escape;
									break;
								case "home":
									keys |= Keys.Home;
									break;
								case "ins":
									keys |= Keys.Insert;
									break;
								case "left":
									keys |= Keys.Left;
									break;
								case "pgdwn":
									keys |= Keys.PageDown;
									break;
								case "pgup":
									keys |= Keys.PageUp;
									break;
								case "right":
									keys |= Keys.Right;
									break;
								case "shift":
									keys |= Keys.Shift;
									break;
								case "tab":
									keys |= Keys.Tab;
									break;
								case "up":
									keys |= Keys.Up;
									break;
							};
						};
					}
					catch
					{
						throw new ConfigurationException("The System.Windows.Forms.KeysConverter() did not understand this key description:"
							+ sc + ".", m_configurationNode);
					}
					return keys;
				}
				else
					return Keys.None;
			}
		}

		#endregion

		#region Initialization
		public Command(Mediator mediator, XmlNode commandNode)
		{
			m_mediator = mediator;
			m_configurationNode = commandNode;
			StringTable tbl = null;
			if (mediator != null && mediator.HasStringTable)
				tbl = mediator.StringTbl;
			m_id = XmlUtils.GetAttributeValue(commandNode, "id");
			m_label = XmlUtils.GetLocalizedAttributeValue(tbl, commandNode, "label", null);
			m_shortcut = XmlUtils.GetAttributeValue(commandNode,"shortcut");
			m_messageString = XmlUtils.GetAttributeValue(commandNode,"message");
			m_valueString = XmlUtils.GetAttributeValue(commandNode,"value");
		}
		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~Command()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue 
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		/// 
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 
		/// 2. If disposing is false, the method has been called by the 
		/// runtime from inside the finalizer and you should not reference (access) 
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// 
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;
			m_id = null;
			m_label = null;
			m_shortcut = null;
			m_valueString = null;
			m_messageString = null;
			m_configurationNode = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// <summary>
		/// a list of child elements of the command element.
		/// </summary>
		public XmlNodeList Parameters
		{
			get
			{
				CheckDisposed();
				return m_configurationNode.ChildNodes;
			}
		}

		/// <summary>
		/// Get a value of an optional attribute of a parameter element
		/// </summary>
		/// <param name="elementName">the name of the element containing the attribute</param>
		/// <param name="attributeName">the name of the attribute</param>
		/// <param name="defaultValue">the default value if the element or attribute are not found</param>
		/// <returns></returns>
		/// <remarks> this version allows you to use any element names you want.</remarks>
		public string GetParameter(string elementName, string attributeName, string defaultValue)
		{
			CheckDisposed();
			foreach(XmlNode element in Parameters)
			{
				if (element.Name==elementName)
				{
					return XmlUtils.GetAttributeValue(element, attributeName, defaultValue);
				}
			}
			return defaultValue;
		}
		/// <summary>
		/// Get a value of an optional attribute of a <parameters> element
		/// </summary>
		/// <param name="attributeName">the name of the attribute</param>
		/// <param name="defaultValue">the default value if the element or attribute are not found</param>
		/// <returns></returns>
		///<remarks> this version assumes the element containing attribute is named "parameters"</remarks>
		public string GetParameter(string attributeName, string defaultValue)
		{
			CheckDisposed();
			return GetParameter("parameters", attributeName, defaultValue);
		}

		/// <summary>
		/// Get a value of a mandatory attribute of a <parameters> element
		/// </summary>
		/// <param name="attributeName">the name of the attribute</param>
		/// <exception cref="ConfigurationException">in the parameter is not found</exception>
		/// <returns></returns>
		///<remarks> this version assumes the element containing attribute is named "parameters"</remarks>
		public string GetParameter(string attributeName)
		{
			CheckDisposed();
			string result = GetParameter(attributeName, null);
			if (result == null)
				throw new ConfigurationException("The command '"+this.Id+"' must have a parameter attribute named '"+attributeName +"'.",this.ConfigurationNode);
			return result;
		}

		public void InvokeCommand()
		{
			CheckDisposed();
			if (m_messageString != null && m_messageString != "")
			{
				using(new BusyIndicator())
				{
					m_mediator.Log("Start: "+this.m_messageString);
					m_mediator.SendMessage("ProgressReset", this);
					m_mediator.SendMessage(m_messageString, this);
					// The "ExitApplication" command disposes us,
					// so that we don't even have a
					// mediator at this point.
					if (m_mediator != null)
					{
						m_mediator.SendMessage("ProgressReset", this);
						m_mediator.Log("Done: "+this.m_messageString);
					}
				}
			}
		}
	}

	public class BusyIndicator : IFWDisposable
	{
		private System.Windows.Forms.Cursor m_old;

		public BusyIndicator()
		{
			m_old = System.Windows.Forms.Cursor.Current;
			System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~BusyIndicator()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue 
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		/// 
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 
		/// 2. If disposing is false, the method has been called by the 
		/// runtime from inside the finalizer and you should not reference (access) 
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// 
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				Cursor.Current = m_old;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation
	}
}