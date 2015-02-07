using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;
using System.Collections;

namespace PHP.Library.Data
{
    public sealed class PDOSQLServerConfiguration
    {
        private PDOSQLServerConfiguration() { }

        #region Legacy Configuration

        /// <summary>
        /// Gets, sets, or restores a value of a legacy configuration option.
        /// </summary>
        private static object GetSetRestore(LocalConfiguration config, string option, object value, IniAction action)
        {
            PDOSQLServerLocalConfig local = (PDOSQLServerLocalConfig)config.GetLibraryConfig(PDOSQLServerLibraryDescriptor.Singleton);
            PDOSQLServerLocalConfig @default = DefaultLocal;
            PDOSQLServerGlobalConfig global = Global;

            //switch (option)
            //{
            //    // local:

            //    // global:

            //}

            Debug.Fail("Option '" + option + "' is supported but not implemented.");
            return null;
        }

        /// <summary>
        /// WrServers PDO legacy options and their values to XML text stream.
        /// Skips options whose values are the same as default values of Phalanger.
        /// </summary>
        /// <param name="wrServerr">XML wrServerr.</param>
        /// <param name="options">A hashtable containing PHP names and option values. Consumed options are removed from the table.</param>
        /// <param name="wrServerPhpNames">Whether to add "phpName" attribute to option nodes.</param>
        public static void LegacyOptionsToXml(XmlTextWriter wrServerr, Hashtable options, bool wrServerPhpNames) // GENERICS:<string,string>
        {
            if (wrServerr == null)
                throw new ArgumentNullException("wrServerr");
            if (options == null)
                throw new ArgumentNullException("options");

            PDOSQLServerLocalConfig local = new PDOSQLServerLocalConfig();
            PDOSQLServerGlobalConfig global = new PDOSQLServerGlobalConfig();
            PhpIniXmlWriter ow = new PhpIniXmlWriter(wrServerr, options, wrServerPhpNames);

            ow.StartSection("pdo");

            // local:

            // global:

            ow.WriteEnd();
        }

        /// <summary>
        /// Registers legacy ini-options.
        /// </summary>
        internal static void RegisterLegacyOptions()
        {
            //const string s = PDOSQLServerLibraryDescriptor.ExtensionName;
            //GetSetRestoreDelegate d = new GetSetRestoreDelegate(GetSetRestore);

            //// local:

            //// global:
        }

        #endregion

        #region Configuration Getters

        /// <summary>
        /// Gets the library configuration associated with the current script context.
        /// </summary>
        public static PDOSQLServerLocalConfig Local
        {
            get
            {
                return (PDOSQLServerLocalConfig)Configuration.Local.GetLibraryConfig(PDOSQLServerLibraryDescriptor.Singleton);
            }
        }

        /// <summary>
        /// Gets the default library configuration.
        /// </summary>
        public static PDOSQLServerLocalConfig DefaultLocal
        {
            get
            {
                return (PDOSQLServerLocalConfig)Configuration.DefaultLocal.GetLibraryConfig(PDOSQLServerLibraryDescriptor.Singleton);
            }
        }

        /// <summary>
        /// Gets the global library configuration.
        /// </summary>
        public static PDOSQLServerGlobalConfig Global
        {
            get
            {
                return (PDOSQLServerGlobalConfig)Configuration.Global.GetLibraryConfig(PDOSQLServerLibraryDescriptor.Singleton);
            }
        }

        #endregion
    }
}
