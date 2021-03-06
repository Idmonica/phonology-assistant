// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005-2015, SIL International.
// <copyright from='2005' to='2015' company='SIL International'>
//		Copyright (c) 2005-2015, SIL International.
//    
//		This software is distributed under the MIT License, as specified in the LICENSE.txt file.
// </copyright> 
#endregion
// 
using System.Collections.Generic;
using System.Xml;
using SilUtils;

namespace SIL.Pa.AddOns
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AddOnMediator : IxCoreColleague
	{
		private static AddOnMediator s_addOnMediator;
		private static SortedDictionary<int, object> s_afterDataSourcesLoadedList;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AddOnMediator()
		{
			try
			{
				PaApp.AddMediatorColleague(this);
			}
			catch { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnAfterLoadingDataSources(object args)
		{
			if (s_afterDataSourcesLoadedList != null)
			{
				// Go through the add-ons that have register through this mediator to be
				// informed after the data sources have been loaded.
				foreach (object addOnClass in s_afterDataSourcesLoadedList.Values)
					ReflectionHelper.CallMethod(addOnClass, "AfterDataSourcesLoaded", args);
			}
	
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static SortedDictionary<int, object> AfterDataSourcesLoadedList
		{
			get { return s_afterDataSourcesLoadedList; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void RegisterForDataSourcesLoadedMsg(int priority, object addOnClass)
		{
			if (s_addOnMediator == null)
				s_addOnMediator = new AddOnMediator();
	
			if (s_afterDataSourcesLoadedList == null)
				s_afterDataSourcesLoadedList = new SortedDictionary<int, object>();

			s_afterDataSourcesLoadedList[priority] = addOnClass;
		}

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Never used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the message target.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			return (new IxCoreColleague[] {this});
		}

		#endregion
	}
}
