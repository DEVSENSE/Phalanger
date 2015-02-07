/*

 Copyright (c) 2005-2006 Tomas Matousek.  

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

namespace PHP.Library.Xml
{
	#region Local Configuration

	/// <summary>
    /// Script independent Zlib configuration.
	/// </summary>
	[Serializable]
	public sealed class XmlLocalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
        internal XmlLocalConfig() { }

		/// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
            return (XmlLocalConfig)this.MemberwiseClone();
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
			//return true;
		}
	}

	#endregion

	#region Global Configuration

	/// <summary>
	/// Script dependent MSSQL configuration.
	/// </summary>
	[Serializable]
	public sealed class XmlGlobalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
		internal XmlGlobalConfig() { }

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
			//return true;
		}

		/// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
            return (XmlGlobalConfig)this.MemberwiseClone();
		}
	}

	#endregion

	/// <summary>
    /// Zlib extension configuration.
	/// </summary>
    public static class XmlConfiguration
	{
		#region Legacy Configuration

		/// <summary>
		/// Gets, sets, or restores a value of a legacy configuration option.
		/// </summary>
		private static object GetSetRestore(LocalConfiguration config, string option, object value, IniAction action)
		{
            XmlLocalConfig local = (XmlLocalConfig)config.GetLibraryConfig(XmlLibraryDescriptor.Singleton);
            XmlLocalConfig @default = DefaultLocal;
            XmlGlobalConfig global = Global;

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

            XmlLocalConfig local = new XmlLocalConfig();
			XmlGlobalConfig global = new XmlGlobalConfig();
			PhpIniXmlWriter ow = new PhpIniXmlWriter(writer, options, writePhpNames);

            ow.StartSection("zlib");

            //// local:
            //ow.WriteOption("mssql.connect_timeout", "ConnectTimeout", 5, local.ConnectTimeout);
            //ow.WriteOption("mssql.timeout", "Timeout", 60, local.Timeout);
            //ow.WriteOption("mssql.batchsize", "BatchSize", 0, local.BatchSize);

            //// global:
            //ow.WriteOption("mssql.max_links", "MaxConnections", -1, global.MaxConnections);
            //ow.WriteOption("mssql.secure_connection", "NTAuthentication", false, global.NTAuthentication);

			ow.WriteEnd();
		}

		/// <summary>
		/// Registers legacy ini-options.
		/// </summary>
		internal static void RegisterLegacyOptions()
		{
			//const string s = ZlibLibraryDescriptor.ExtensionName;
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
        public static XmlLocalConfig Local
		{
			get
			{
                return (XmlLocalConfig)Configuration.Local.GetLibraryConfig(XmlLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets the default library configuration.
		/// </summary>
        public static XmlLocalConfig DefaultLocal
		{
			get
			{
                return (XmlLocalConfig)Configuration.DefaultLocal.GetLibraryConfig(XmlLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets the global library configuration.
		/// </summary>
		public static XmlGlobalConfig Global
		{
			get
			{
                return (XmlGlobalConfig)Configuration.Global.GetLibraryConfig(XmlLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets local configuration associated with a specified script context.
		/// </summary>
		/// <param name="context">Scritp context.</param>
		/// <returns>Local library configuration.</returns>
        public static XmlLocalConfig GetLocal(ScriptContext/*!*/ context)
		{
			if (context == null)
				throw new ArgumentNullException("context");

            return (XmlLocalConfig)context.Config.GetLibraryConfig(XmlLibraryDescriptor.Singleton);
		}

		#endregion
	}
}
