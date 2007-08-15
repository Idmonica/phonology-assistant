﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:2.0.50727.832
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SIL.Pa.Data.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "2.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("SIL.Pa.Data.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The file that contains FieldWorks queries ({0}) is either missing or corrupt. Until this problem is corrected, FieldWorks data sources cannot be accessed or added..
        /// </summary>
        internal static string kstidErrorLoadingQueriesMsg {
            get {
                return ResourceManager.GetString("kstidErrorLoadingQueriesMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There was an error retrieving vernacular writing systems from the {0}\ndatabase. It&apos;s possible the file {1} is either missing or corrupt.\n\n{2}.
        /// </summary>
        internal static string kstidErrorRetrievingAnalWsMsg {
            get {
                return ResourceManager.GetString("kstidErrorRetrievingAnalWsMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There was an error retrieving the data from the {0} database.\nIt&apos;s possible the file {1} is either missing or\ncorrupt. Reading this data will be skipped.\n\n{2}.
        /// </summary>
        internal static string kstidErrorRetrievingFwDataMsg {
            get {
                return ResourceManager.GetString("kstidErrorRetrievingFwDataMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There was an error retrieving vernacular writing systems from the {0}\ndatabase. It&apos;s possible the file {1} is either missing or corrupt.\n\n{2}.
        /// </summary>
        internal static string kstidErrorRetrievingVernWsMsg {
            get {
                return ResourceManager.GetString("kstidErrorRetrievingVernWsMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to SQL Server cannot be started. It may not be installed.\nThe following error was reported:\n\n{0}\n\nMake sure FieldWorks Language Explorer has been installed.\nOr, restart Phonology Assistant to try again..
        /// </summary>
        internal static string kstidErrorStartingSQLServer1 {
            get {
                return ResourceManager.GetString("kstidErrorStartingSQLServer1", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Phonology Assistant waited {0} seconds for SQL Server to fully start up.\nEither that is not enough time for your computer or it may not be installed.\nMake sure FieldWorks Language Explorer has been installed. Would you\nlike to try again?.
        /// </summary>
        internal static string kstidErrorStartingSQLServer2 {
            get {
                return ResourceManager.GetString("kstidErrorStartingSQLServer2", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Feature &apos;{0}&apos; is not a custom feature. It cannot be deleted..
        /// </summary>
        internal static string kstidFeatureCantBeDeletedMsg {
            get {
                return ResourceManager.GetString("kstidFeatureCantBeDeletedMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Feature &apos;{0}&apos; already exists..
        /// </summary>
        internal static string kstidFeatureExistsMsg {
            get {
                return ResourceManager.GetString("kstidFeatureExistsMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to {0} - ({1}).
        /// </summary>
        internal static string kstidFWDataSourceInfo {
            get {
                return ResourceManager.GetString("kstidFWDataSourceInfo", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to FieldWorks database &apos;{0}&apos; is missing..
        /// </summary>
        internal static string kstidFwDBMissing {
            get {
                return ResourceManager.GetString("kstidFwDBMissing", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Gathering FieldWorks Project Information....
        /// </summary>
        internal static string kstidGettingFwProjInfoMsg {
            get {
                return ResourceManager.GetString("kstidGettingFwProjInfoMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There are no writing system properties specified for the &apos;{0}&apos;\nFieldWorks project. Therefore, no data can be displayed for it.\n\nTo fix this problem, modify the FieldWorks data source\nproperties for this project by selecting &apos;Project Settings&apos;\nfrom the File menu. Then click the button next to the\nproject name in the list of data sources..
        /// </summary>
        internal static string kstidMissingWsMsg {
            get {
                return ResourceManager.GetString("kstidMissingWsMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The following error occurred when trying to establish\na connection to the {0} database.\n\n{1}.
        /// </summary>
        internal static string kstidSQLConnectionErrMsg {
            get {
                return ResourceManager.GetString("kstidSQLConnectionErrMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Access to FieldWorks projects requires SQL Server but it is not installed on this computer..
        /// </summary>
        internal static string kstidSQLServerNotInstalledMsg {
            get {
                return ResourceManager.GetString("kstidSQLServerNotInstalledMsg", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Starting SQL Server....
        /// </summary>
        internal static string kstidStartingSQLServerMsg {
            get {
                return ResourceManager.GetString("kstidStartingSQLServerMsg", resourceCulture);
            }
        }
    }
}
