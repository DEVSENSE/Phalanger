using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;

namespace PHP.Library.Zip
{
    public sealed class ZipLibraryDescriptor : PhpLibraryDescriptor
    {
        /// Stores one and only instance of the class that is created when the assembly is loaded.
        /// </summary>
        internal static ZipLibraryDescriptor Singleton { get { return singleton; } }
        private static ZipLibraryDescriptor singleton;

        internal const string ExtensionName = "Zip";

        /// <summary>
        /// Called by the Core after the library is loaded.
        /// </summary>
        protected override void Loaded(PhpLibraryAttribute assemblyAttribute, LibraryConfigStore configStore)
        {
            base.Loaded(assemblyAttribute, configStore);
            singleton = this;
            ZipConfiguration.RegisterLegacyOptions();

            StreamWrapper.RegisterSystemWrapper(new ZipStreamWrapper());
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
            ConfigUtils.ParseNameValueList(section, context, (ZipLocalConfig)result.Local, (ZipGlobalConfig)result.Global);

            return result;
        }

        /// <summary>
        /// Creates empty library configuration context.
        /// </summary>
        /// <returns>An initialized configuration context. Should not be a <B>null</B> reference.</returns>
        protected override ConfigContextBase CreateConfigContext()
        {
            return new ConfigContextBase(new ZipLocalConfig(), new ZipGlobalConfig());
        }
    }
}
