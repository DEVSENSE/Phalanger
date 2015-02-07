using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Xml;
using System.Collections;

namespace PHP.Library.Data
{
    public sealed class SQLiteConfiguration
    {
        private SQLiteConfiguration() { }

        #region Legacy Configuration

        /// <summary>
        /// Gets, sets, or restores a value of a legacy configuration option.
        /// </summary>
        private static object GetSetRestore(LocalConfiguration config, string option, object value, IniAction action)
        {
            SQLiteLocalConfig local = (SQLiteLocalConfig)config.GetLibraryConfig(SQLiteLibraryDescriptor.Singleton);
            SQLiteLocalConfig @default = DefaultLocal;
            SQLiteGlobalConfig global = Global;

            switch (option)
            {
                // local:

                // global:
                case "sqlite.assoc_case":
                    Debug.Assert(action == IniAction.Get);
                    return PhpIni.GSR(ref global.AssocCase, 0, value, action);
            }

            Debug.Fail("Option '" + option + "' is supported but not implemented.");
            return null;
        }

        /// <summary>
        /// Writes SQLite legacy options and their values to XML text stream.
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

            SQLiteLocalConfig local = new SQLiteLocalConfig();
            SQLiteGlobalConfig global = new SQLiteGlobalConfig();
            PhpIniXmlWriter ow = new PhpIniXmlWriter(writer, options, writePhpNames);

            ow.StartSection("sqlite");

            // local:

            // global:
            ow.WriteOption("sqlite.assoc_case", "AssocCase", 0, global.AssocCase);

            ow.WriteEnd();
        }

        /// <summary>
        /// Registers legacy ini-options.
        /// </summary>
        internal static void RegisterLegacyOptions()
        {
            const string s = SQLiteLibraryDescriptor.ExtensionName;
            GetSetRestoreDelegate d = new GetSetRestoreDelegate(GetSetRestore);

            // local:

            // global:
            IniOptions.Register("sqlite.assoc_case", IniFlags.Supported | IniFlags.Global, d, s);
        }

        #endregion

        #region Configuration Getters

        /// <summary>
        /// Gets the library configuration associated with the current script context.
        /// </summary>
        public static SQLiteLocalConfig Local
        {
            get
            {
                return (SQLiteLocalConfig)Configuration.Local.GetLibraryConfig(SQLiteLibraryDescriptor.Singleton);
            }
        }

        /// <summary>
        /// Gets the default library configuration.
        /// </summary>
        public static SQLiteLocalConfig DefaultLocal
        {
            get
            {
                return (SQLiteLocalConfig)Configuration.DefaultLocal.GetLibraryConfig(SQLiteLibraryDescriptor.Singleton);
            }
        }

        /// <summary>
        /// Gets the global library configuration.
        /// </summary>
        public static SQLiteGlobalConfig Global
        {
            get
            {
                return (SQLiteGlobalConfig)Configuration.Global.GetLibraryConfig(SQLiteLibraryDescriptor.Singleton);
            }
        }

        #endregion
    }
}
