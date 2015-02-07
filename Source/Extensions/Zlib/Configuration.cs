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

namespace PHP.Library.Zlib
{
	#region Local Configuration

	/// <summary>
    /// Script independent Zlib configuration.
	/// </summary>
	[Serializable]
	public sealed class ZlibLocalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
        internal ZlibLocalConfig() { }

		/// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
            return (ZlibLocalConfig)this.MemberwiseClone();
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
	public sealed class ZlibGlobalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
		internal ZlibGlobalConfig() { }

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
            return (ZlibGlobalConfig)this.MemberwiseClone();
		}
	}

	#endregion

	/// <summary>
    /// Zlib extension configuration.
	/// </summary>
    public static class ZlibConfiguration
	{
		#region Legacy Configuration

		/// <summary>
		/// Gets, sets, or restores a value of a legacy configuration option.
		/// </summary>
		private static object GetSetRestore(LocalConfiguration config, string option, object value, IniAction action)
		{
            ZlibLocalConfig local = (ZlibLocalConfig)config.GetLibraryConfig(ZlibLibraryDescriptor.Singleton);
            ZlibLocalConfig @default = DefaultLocal;
            ZlibGlobalConfig global = Global;

			switch (option)
			{
                //// local:

                //case "mssql.connect_timeout":
                //return PhpIni.GSR(ref local.ConnectTimeout, @default.ConnectTimeout, value, action);

                //case "mssql.timeout":
                //return PhpIni.GSR(ref local.Timeout, @default.Timeout, value, action);

                //case "mssql.batchsize":
                //return PhpIni.GSR(ref local.BatchSize, @default.BatchSize, value, action);

                // global:  

                case "zlib.output_compression":
                Debug.Assert(action == IniAction.Get, "Setting zlib.output_compression is not currently implemented.");
                return false;

                case "zlib.output_compression_level":
                Debug.Assert(action == IniAction.Get, "Setting zlib.output_compression_level is not currently implemented.");
                return -1;

                case "zlib.output_handler":
                Debug.Assert(action == IniAction.Get, "Setting zlib.output_handler is not currently implemented.");
                return "";
			}

			Debug.Fail("Option '" + option + "' is not currently supported.");
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

            ZlibLocalConfig local = new ZlibLocalConfig();
			ZlibGlobalConfig global = new ZlibGlobalConfig();
			PhpIniXmlWriter ow = new PhpIniXmlWriter(writer, options, writePhpNames);

            ow.StartSection("zlib");

            //ow.WriteOption("zlib.output_compression", "OutputCompression", "Off", "Off");
            //ow.WriteOption("zlib.output_compression_level", "OutputCompression", -1, -1);

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
			const string s = ZlibLibraryDescriptor.ExtensionName;
			GetSetRestoreDelegate d = new GetSetRestoreDelegate(GetSetRestore);

            IniOptions.Register("zlib.output_compression", IniFlags.Supported | IniFlags.Global, d, s);
            IniOptions.Register("zlib.output_compression_level", IniFlags.Supported | IniFlags.Global, d, s);
            IniOptions.Register("zlib.output_handler", IniFlags.Supported | IniFlags.Global, d, s);

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
        public static ZlibLocalConfig Local
		{
			get
			{
                return (ZlibLocalConfig)Configuration.Local.GetLibraryConfig(ZlibLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets the default library configuration.
		/// </summary>
        public static ZlibLocalConfig DefaultLocal
		{
			get
			{
                return (ZlibLocalConfig)Configuration.DefaultLocal.GetLibraryConfig(ZlibLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets the global library configuration.
		/// </summary>
		public static ZlibGlobalConfig Global
		{
			get
			{
				return (ZlibGlobalConfig)Configuration.Global.GetLibraryConfig(ZlibLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets local configuration associated with a specified script context.
		/// </summary>
		/// <param name="context">Scritp context.</param>
		/// <returns>Local library configuration.</returns>
        public static ZlibLocalConfig GetLocal(ScriptContext/*!*/ context)
		{
			if (context == null)
				throw new ArgumentNullException("context");

            return (ZlibLocalConfig)context.Config.GetLibraryConfig(ZlibLibraryDescriptor.Singleton);
		}

		#endregion
	}
}
