using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;

namespace PHP.Library.Data
{
    public sealed class PDOMySQLLibraryDescriptor : PhpLibraryDescriptor
    {
        /// <summary>
        /// Stores one and only instance of the class that is created when the assembly is loaded.
        /// </summary>
        internal static PDOMySQLLibraryDescriptor Singleton { get { return singleton; } }
        private static PDOMySQLLibraryDescriptor singleton;

        internal const string ExtensionName = "pdo_mysql";

        /// <summary>
        /// Called by the Core after the library is loaded.
        /// </summary>
        protected override void Loaded(PhpLibraryAttribute assemblyAttribute, LibraryConfigStore configStore)
        {
            base.Loaded(assemblyAttribute, configStore);
            singleton = this;

            PDOMySQLConfiguration.RegisterLegacyOptions();

            PDOLibraryDescriptor.RegisterProvider(new MySQLPDODriver());

            string fullname = typeof(PDO).Name;
            var tPDO = ApplicationContext.Default.GetType(new QualifiedName(new Name(typeof(PDO).FullName)), ref fullname);
            Core.Reflection.PhpMemberAttributes att = Core.Reflection.PhpMemberAttributes.Public | Core.Reflection.PhpMemberAttributes.Static;
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_USE_BUFFERED_QUERY", MySQLPDODriver.MYSQL_ATTR_USE_BUFFERED_QUERY);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_INIT_COMMAND", MySQLPDODriver.MYSQL_ATTR_INIT_COMMAND);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_READ_DEFAULT_FILE", MySQLPDODriver.MYSQL_ATTR_READ_DEFAULT_FILE);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_READ_DEFAULT_GROUP", MySQLPDODriver.MYSQL_ATTR_READ_DEFAULT_GROUP);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_MAX_BUFFER_SIZE", MySQLPDODriver.MYSQL_ATTR_MAX_BUFFER_SIZE);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_DIRECT_QUERY", MySQLPDODriver.MYSQL_ATTR_DIRECT_QUERY);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_FOUND_ROWS", MySQLPDODriver.MYSQL_ATTR_FOUND_ROWS);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_IGNORE_SPACE", MySQLPDODriver.MYSQL_ATTR_IGNORE_SPACE);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_COMPRESS", MySQLPDODriver.MYSQL_ATTR_COMPRESS);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_SSL_CA", MySQLPDODriver.MYSQL_ATTR_SSL_CA);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_SSL_CAPATH", MySQLPDODriver.MYSQL_ATTR_SSL_CAPATH);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_SSL_CERT", MySQLPDODriver.MYSQL_ATTR_SSL_CERT);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_CIPHER", MySQLPDODriver.MYSQL_ATTR_CIPHER);
            ApplicationContext.Default.AddConstantToType(tPDO.TypeDesc, att, "MYSQL_ATTR_KEY", MySQLPDODriver.MYSQL_ATTR_KEY);
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
            ConfigUtils.ParseNameValueList(section, context, (PDOMySQLLocalConfig)result.Local, (PDOMySQLGlobalConfig)result.Global);

            return result;
        }

        /// <summary>
        /// Creates empty library configuration context.
        /// </summary>
        /// <returns>An initialized configuration context. Should not be a <B>null</B> reference.</returns>
        protected override ConfigContextBase CreateConfigContext()
        {
            return new ConfigContextBase(new PDOMySQLLocalConfig(), new PDOMySQLGlobalConfig());
        }
    }
}
