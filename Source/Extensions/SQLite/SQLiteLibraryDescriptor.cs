using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;

namespace PHP.Library.Data
{
    /// <summary>
    /// Describes the class library assembly content and configuration.
    /// </summary>
    public sealed class SQLiteLibraryDescriptor : PhpLibraryDescriptor
    {
        /// <summary>
        /// Stores one and only instance of the class that is created when the assembly is loaded.
        /// </summary>
        internal static SQLiteLibraryDescriptor Singleton { get { return singleton; } }
        private static SQLiteLibraryDescriptor singleton;

        ///// <summary>
        ///// Gets a list of implemented extensions.
        ///// </summary>
        //public override string[] ImplementedExtensions
        //{
        //  get { return new string[] { ExtensionName }; } 
        //}
        internal const string ExtensionName = "sqlite";

        /// <summary>
        /// Called by the Core after the library is loaded.
        /// </summary>
        protected override void Loaded(PhpLibraryAttribute assemblyAttribute, LibraryConfigStore configStore)
        {
            base.Loaded(assemblyAttribute, configStore);
            singleton = this;
            SQLiteConfiguration.RegisterLegacyOptions();
        }

        /// <summary>
        /// Parses a configuration section belonging to the MySql library. 
        /// </summary>
        /// <param name="result">A configuration context.</param>
        /// <param name="context">The context of the configuration created by Phalanger Core.</param>
        /// <param name="section">A XML node containing the configuration or its part.</param>
        /// <returns>Updated configuration context.</returns>
        protected override ConfigContextBase ParseConfig(ConfigContextBase result, PhpConfigurationContext context, XmlNode section)
        {
            // parses XML tree:
            ConfigUtils.ParseNameValueList(section, context, (SQLiteLocalConfig)result.Local, (SQLiteGlobalConfig)result.Global);

            return result;
        }

        /// <summary>
        /// Creates empty library configuration context.
        /// </summary>
        /// <returns>An initialized configuration context. Should not be a <B>null</B> reference.</returns>
        protected override ConfigContextBase CreateConfigContext()
        {
            return new ConfigContextBase(new SQLiteLocalConfig(), new SQLiteGlobalConfig());
        }
    }
}
