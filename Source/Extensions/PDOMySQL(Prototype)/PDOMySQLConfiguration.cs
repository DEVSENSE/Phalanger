using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;
using System.Collections;

namespace PHP.Library.Data
{
    public sealed class PDOMySQLConfiguration
    {
        private PDOMySQLConfiguration() { }

        #region Legacy Configuration

        /// <summary>
        /// Gets, sets, or restores a value of a legacy configuration option.
        /// </summary>
        private static object GetSetRestore(LocalConfiguration config, string option, object value, IniAction action)
        {
            PDOMySQLLocalConfig local = (PDOMySQLLocalConfig)config.GetLibraryConfig(PDOMySQLLibraryDescriptor.Singleton);
            PDOMySQLLocalConfig @default = DefaultLocal;
            PDOMySQLGlobalConfig global = Global;

            //switch (option)
            //{
            //    // local:

            //    // global:

            //}

            Debug.Fail("Option '" + option + "' is supported but not implemented.");
            return null;
        }

        /// <summary>
        /// Writes PDO legacy options and their values to XML text stream.
        /// Skips options whose values are the same as default values of Phalanger.
        /// </summary>
        /// <param name="writer">XML writer.</param>
        /// <param name="options">A hashtable containing PHP names and option values. Consumed options are removed from the table.</param>
        /// <param name="writePhpNames">Whether to add "phpName" attribute to option nodes.</param>
        public static void LegacyOptionsToXml(XmlTextWriter writer, Hashtable options, bool writePhpNames) // GENERICS:<string,string>
        {
            if (writer == null)
                throw new ArgumentNullException("writer");
            if (options == null)
                throw new ArgumentNullException("options");

            PDOMySQLLocalConfig local = new PDOMySQLLocalConfig();
            PDOMySQLGlobalConfig global = new PDOMySQLGlobalConfig();
            PhpIniXmlWriter ow = new PhpIniXmlWriter(writer, options, writePhpNames);

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
            const string s = PDOMySQLLibraryDescriptor.ExtensionName;
            GetSetRestoreDelegate d = new GetSetRestoreDelegate(GetSetRestore);

            // local:

            // global:
        }

        #endregion

        #region Configuration Getters

        /// <summary>
        /// Gets the library configuration associated with the current script context.
        /// </summary>
        public static PDOMySQLLocalConfig Local
        {
            get
            {
                return (PDOMySQLLocalConfig)Configuration.Local.GetLibraryConfig(PDOMySQLLibraryDescriptor.Singleton);
            }
        }

        /// <summary>
        /// Gets the default library configuration.
        /// </summary>
        public static PDOMySQLLocalConfig DefaultLocal
        {
            get
            {
                return (PDOMySQLLocalConfig)Configuration.DefaultLocal.GetLibraryConfig(PDOMySQLLibraryDescriptor.Singleton);
            }
        }

        /// <summary>
        /// Gets the global library configuration.
        /// </summary>
        public static PDOMySQLGlobalConfig Global
        {
            get
            {
                return (PDOMySQLGlobalConfig)Configuration.Global.GetLibraryConfig(PDOMySQLLibraryDescriptor.Singleton);
            }
        }

        #endregion
    }
}
