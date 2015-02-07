/*

 Copyright (c) 2005-2011 Devsense.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Web;
using System.Xml;
using System.Collections;
using System.Configuration;

using PHP.Core;

namespace PHP.Library.GetText
{
    #region Local Configuration

    /// <summary>
    /// Script independent mbstring configuration.
    /// </summary>
    [Serializable]
    public sealed class GetTextLocalConfig : IPhpConfiguration, IPhpConfigurationSection
    {
        internal GetTextLocalConfig() { }

        /// <summary>
        /// Creates a deep copy of the configuration record.
        /// </summary>
        /// <returns>The copy.</returns>
        public IPhpConfiguration DeepCopy()
        {
            return (GetTextLocalConfig)this.MemberwiseClone();
        }

        /// <summary>
        /// Loads configuration from XML.
        /// </summary>
        public bool Parse(string name, string value, XmlNode node)
        {
            switch (name)
            {
                default:
                    return false;
            }
        }
    }

    #endregion

    #region Global Configuration

    /// <summary>
    /// Script dependent MSSQL configuration.
    /// </summary>
    [Serializable]
    public sealed class GetTextGlobalConfig : IPhpConfiguration, IPhpConfigurationSection
    {
        internal GetTextGlobalConfig() { }

        /// <summary>
        /// Loads configuration from XML.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        public bool Parse(string name, string value, XmlNode node)
        {
            switch (name)
            {
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates a deep copy of the configuration record.
        /// </summary>
        /// <returns>The copy.</returns>
        public IPhpConfiguration DeepCopy()
        {
            return (GetTextGlobalConfig)this.MemberwiseClone();
        }
    }

    #endregion

    /// <summary>
    /// mbstring extension configuration.
    /// </summary>
    public static class GetTextConfiguration
    {
        #region Legacy Configuration

        /// <summary>
        /// Gets, sets, or restores a value of a legacy configuration option.
        /// </summary>
        private static object GetSetRestore(LocalConfiguration config, string option, object value, IniAction action)
        {
            GetTextLocalConfig local = (GetTextLocalConfig)config.GetLibraryConfig(GetTextLibraryDescriptor.Singleton);
            GetTextLocalConfig @default = DefaultLocal;
            GetTextGlobalConfig global = Global;

            //switch (option)
            //{
            //// local:

            //case "mssql.connect_timeout":
            //return PhpIni.GSR(ref local.ConnectTimeout, @default.ConnectTimeout, value, action);

            //case "mssql.timeout":
            //return PhpIni.GSR(ref local.Timeout, @default.Timeout, value, action);

            //case "mssql.batchsize":
            //return PhpIni.GSR(ref local.BatchSize, @default.BatchSize, value, action);

            //// global:  

            //case "mssql.max_links":
            //Debug.Assert(action == IniAction.Get);
            //return PhpIni.GSR(ref global.MaxConnections, 0, null, action);

            //case "mssql.secure_connection":
            //Debug.Assert(action == IniAction.Get);
            //return PhpIni.GSR(ref global.NTAuthentication, false, null, action);
            //}

            Debug.Fail("Option '" + option + "' is supported but not implemented.");
            return null;
        }

        /// <summary>
        /// Writes MySql legacy options and their values to XML text stream.
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

            GetTextLocalConfig local = new GetTextLocalConfig();
            GetTextGlobalConfig global = new GetTextGlobalConfig();
            PhpIniXmlWriter ow = new PhpIniXmlWriter(writer, options, writePhpNames);

            //ow.StartSection("gd2");

            //// local:
            //ow.WriteOption("mssql.connect_timeout", "ConnectTimeout", 5, local.ConnectTimeout);
            //ow.WriteOption("mssql.timeout", "Timeout", 60, local.Timeout);
            //ow.WriteOption("mssql.batchsize", "BatchSize", 0, local.BatchSize);

            //// global:
            //ow.WriteOption("mssql.max_links", "MaxConnections", -1, global.MaxConnections);
            //ow.WriteOption("mssql.secure_connection", "NTAuthentication", false, global.NTAuthentication);

            //ow.WriteEnd();
        }

        /// <summary>
        /// Registers legacy ini-options.
        /// </summary>
        internal static void RegisterLegacyOptions()
        {
            //const string s = MbstringLibraryDescriptor.ExtensionName;
            //GetSetRestoreDelegate d = new GetSetRestoreDelegate(GetSetRestore);

            //// global:
            //IniOptions.Register("mssql.max_links", IniFlags.Supported | IniFlags.Global, d, s);
            //IniOptions.Register("mssql.secure_connection", IniFlags.Supported | IniFlags.Global, d, s);
            //IniOptions.Register("mssql.allow_persistent", IniFlags.Unsupported | IniFlags.Global, d, s);
            //IniOptions.Register("mssql.max_persistent", IniFlags.Unsupported | IniFlags.Global, d, s);

            //// local:
            //IniOptions.Register("mssql.connect_timeout", IniFlags.Supported | IniFlags.Local, d, s);
            //IniOptions.Register("mssql.timeout", IniFlags.Supported | IniFlags.Local, d, s);
            //IniOptions.Register("mssql.batchsize", IniFlags.Supported | IniFlags.Local, d, s);
            //IniOptions.Register("mssql.min_error_severity", IniFlags.Unsupported | IniFlags.Local, d, s);
            //IniOptions.Register("mssql.min_message_severity", IniFlags.Unsupported | IniFlags.Local, d, s);
            //IniOptions.Register("mssql.compatability_mode", IniFlags.Unsupported | IniFlags.Local, d, s);
            //IniOptions.Register("mssql.textsize", IniFlags.Unsupported | IniFlags.Local, d, s);
            //IniOptions.Register("mssql.textlimit", IniFlags.Unsupported | IniFlags.Local, d, s);
            //IniOptions.Register("mssql.datetimeconvert", IniFlags.Unsupported | IniFlags.Local, d, s);
            //IniOptions.Register("mssql.max_procs", IniFlags.Unsupported | IniFlags.Local, d, s);
        }

        #endregion

        #region Configuration Getters

        /// <summary>
        /// Gets the library configuration associated with the current script context.
        /// </summary>
        public static GetTextLocalConfig Local
        {
            get
            {
                return (GetTextLocalConfig)PHP.Core.Configuration.Local.GetLibraryConfig(GetTextLibraryDescriptor.Singleton);
            }
        }

        /// <summary>
        /// Gets the default library configuration.
        /// </summary>
        public static GetTextLocalConfig DefaultLocal
        {
            get
            {
                return (GetTextLocalConfig)PHP.Core.Configuration.DefaultLocal.GetLibraryConfig(GetTextLibraryDescriptor.Singleton);
            }
        }

        /// <summary>
        /// Gets the global library configuration.
        /// </summary>
        public static GetTextGlobalConfig Global
        {
            get
            {
                return (GetTextGlobalConfig)PHP.Core.Configuration.Global.GetLibraryConfig(GetTextLibraryDescriptor.Singleton);
            }
        }

        /// <summary>
        /// Gets local configuration associated with a specified script context.
        /// </summary>
        /// <param name="context">Scritp context.</param>
        /// <returns>Local library configuration.</returns>
        public static GetTextLocalConfig GetLocal(ScriptContext/*!*/ context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return (GetTextLocalConfig)context.Config.GetLibraryConfig(GetTextLibraryDescriptor.Singleton);
        }

        #endregion
    }
}
