using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;

namespace PHP.Library.Data
{
    public sealed class PDOLibraryDescriptor : PhpLibraryDescriptor
    {
        /// <summary>
        /// Stores one and only instance of the class that is created when the assembly is loaded.
        /// </summary>
        internal static PDOLibraryDescriptor Singleton { get { return singleton; } }
        private static PDOLibraryDescriptor singleton;

        internal const string ExtensionName = "pdo";

        /// <summary>
        /// Called by the Core after the library is loaded.
        /// </summary>
        protected override void Loaded(PhpLibraryAttribute assemblyAttribute, LibraryConfigStore configStore)
        {
            base.Loaded(assemblyAttribute, configStore);
            singleton = this;
            PDOConfiguration.RegisterLegacyOptions();
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
            return new ConfigContextBase(new PDOLocalConfig(), new PDOGlobalConfig());
        }

        private static readonly Dictionary<string, PDODriver> m_providers = new Dictionary<string, PDODriver>(StringComparer.Ordinal);

        public static void RegisterProvider(PDODriver driver)
        {
            if (driver == null)
                throw new ArgumentNullException();

            string scheme = driver.Scheme;
            if (!m_providers.ContainsKey(scheme))
            {
                m_providers.Add(scheme, driver);
            }
        }

        internal static PDODriver GetProvider(string drvName)
        {
            if (m_providers.ContainsKey(drvName))
            {
                return m_providers[drvName];
            }
            else
            {
                return null;
            }
        }

        internal static string[] GetDrivers()
        {
            return m_providers.Keys.ToArray();
        }
    }
}
