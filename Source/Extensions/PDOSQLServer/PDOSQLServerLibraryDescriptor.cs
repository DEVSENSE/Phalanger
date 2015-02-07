using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using PHP.Core.Reflection;
using System.Xml;

namespace PHP.Library.Data
{
    public sealed class PDOSQLServerLibraryDescriptor : PhpLibraryDescriptor
    {
        /// <summary>
        /// Stores one and only instance of the class that is created when the assembly is loaded.
        /// </summary>
        internal static PDOSQLServerLibraryDescriptor Singleton { get { return singleton; } }
        private static PDOSQLServerLibraryDescriptor singleton;

        internal const string ExtensionName = "pdo_sqlite";

        /// <summary>
        /// Called by the Core after the library is loaded.
        /// </summary>
        protected override void Loaded(PhpLibraryAttribute assemblyAttribute, LibraryConfigStore configStore)
        {
            base.Loaded(assemblyAttribute, configStore);
            singleton = this;

            PDOSQLServerConfiguration.RegisterLegacyOptions();

            PDOLibraryDescriptor.RegisterProvider(new SQLServerPDODriver());

            string fullname = typeof(PDO).Name;
            DType tPDO = ApplicationContext.Default.GetType(new QualifiedName(new Name(typeof(PDO).FullName)), ref fullname);
            Core.Reflection.PhpMemberAttributes att = Core.Reflection.PhpMemberAttributes.Public | Core.Reflection.PhpMemberAttributes.Static;
            //ApplicationContext.Default.AddMethodToType(tPDO.TypeDesc, att, "sqliteCreateFunction", SQLServerPDODriver.PDO_sqliteCreateFunction);

            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "SQLSRV_TXN_READ_UNCOMMITTED", SQLServerPDODriver.SQLSRV_TXN_READ_UNCOMMITTED);
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
            ConfigUtils.ParseNameValueList(section, context, (PDOLocalConfig)result.Local, (PDOGlobalConfig)result.Global);

            return result;
        }

        /// <summary>
        /// Creates empty library configuration context.
        /// </summary>
        /// <returns>An initialized configuration context. Should not be a <B>null</B> reference.</returns>
        protected override ConfigContextBase CreateConfigContext()
        {
            return new ConfigContextBase(new PDOSQLServerLocalConfig(), new PDOSQLServerGlobalConfig());
        }
    }
}
