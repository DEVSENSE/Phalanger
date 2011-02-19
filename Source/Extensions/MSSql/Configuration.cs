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

namespace PHP.Library.Data
{
	#region Local Configuration

	/// <summary>
	/// Script independent MSSQL configuration.
	/// </summary>
	[Serializable]
	public sealed class MsSqlLocalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
		internal MsSqlLocalConfig() { }

		/// <summary>
		/// Request timeout in seconds. Non-positive value means no timeout.
		/// </summary>
		public int Timeout = 60;

		/// <summary>
		/// Connect timeout in seconds. Non-positive value means no timeout.
		/// </summary>
		public int ConnectTimeout = 5;

		/// <summary>
		/// Limit on size of a batch. Non-positive value means no limit.
		/// </summary>
		public int BatchSize = 0;

		/// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
			return (MsSqlLocalConfig)this.MemberwiseClone();
		}

		/// <summary>
		/// Loads configuration from XML.
		/// </summary>
		public bool Parse(string name, string value, XmlNode node)
		{
			switch (name)
			{
				case "Timeout":
				Timeout = ConfigUtils.ParseInteger(value, Int32.MinValue, Int32.MaxValue, node);
				break;

				case "ConnectTimeout":
				ConnectTimeout = ConfigUtils.ParseInteger(value, Int32.MinValue, Int32.MaxValue, node);
				break;

				case "BatchSize":
				BatchSize = ConfigUtils.ParseInteger(value, 0, Int32.MaxValue, node);
				break;

				default:
				return false;
			}
			return true;
		}
	}

	#endregion

	#region Global Configuration

	/// <summary>
	/// Script dependent MSSQL configuration.
	/// </summary>
	[Serializable]
	public sealed class MsSqlGlobalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
		internal MsSqlGlobalConfig() { }

		/// <summary>
		/// Maximum number of connections per request. Negative value means no limit.
		/// </summary>
		public int MaxConnections = -1;

		/// <summary>
		/// Use NT authentication when connecting to the server.
		/// </summary>
		public bool NTAuthentication = false;

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
				case "NTAuthentication":
				NTAuthentication = value == "true";
				break;

				case "MaxConnections":
				MaxConnections = ConfigUtils.ParseInteger(value, Int32.MinValue, Int32.MaxValue, node);
				break;

				default:
				return false;
			}
			return true;
		}

		/// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
			return (MsSqlGlobalConfig)this.MemberwiseClone();
		}
	}

	#endregion

	/// <summary>
	/// MSSQL extension configuration.
	/// </summary>
	public static class MsSqlConfiguration
	{
		#region Legacy Configuration

		/// <summary>
		/// Gets, sets, or restores a value of a legacy configuration option.
		/// </summary>
		private static object GetSetRestore(LocalConfiguration config, string option, object value, IniAction action)
		{
			MsSqlLocalConfig local = (MsSqlLocalConfig)config.GetLibraryConfig(MsSqlLibraryDescriptor.Singleton);
			MsSqlLocalConfig @default = DefaultLocal;
			MsSqlGlobalConfig global = Global;

			switch (option)
			{
				// local:

				case "mssql.connect_timeout":
				return PhpIni.GSR(ref local.ConnectTimeout, @default.ConnectTimeout, value, action);

				case "mssql.timeout":
				return PhpIni.GSR(ref local.Timeout, @default.Timeout, value, action);

				case "mssql.batchsize":
				return PhpIni.GSR(ref local.BatchSize, @default.BatchSize, value, action);

				// global:  

				case "mssql.max_links":
				Debug.Assert(action == IniAction.Get);
				return PhpIni.GSR(ref global.MaxConnections, 0, null, action);

				case "mssql.secure_connection":
				Debug.Assert(action == IniAction.Get);
				return PhpIni.GSR(ref global.NTAuthentication, false, null, action);
			}

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

			MsSqlLocalConfig local = new MsSqlLocalConfig();
			MsSqlGlobalConfig global = new MsSqlGlobalConfig();
			PhpIniXmlWriter ow = new PhpIniXmlWriter(writer, options, writePhpNames);

			ow.StartSection("mssql");

			// local:
			ow.WriteOption("mssql.connect_timeout", "ConnectTimeout", 5, local.ConnectTimeout);
			ow.WriteOption("mssql.timeout", "Timeout", 60, local.Timeout);
			ow.WriteOption("mssql.batchsize", "BatchSize", 0, local.BatchSize);

			// global:
			ow.WriteOption("mssql.max_links", "MaxConnections", -1, global.MaxConnections);
			ow.WriteOption("mssql.secure_connection", "NTAuthentication", false, global.NTAuthentication);

			ow.WriteEnd();
		}

		/// <summary>
		/// Registers legacy ini-options.
		/// </summary>
		internal static void RegisterLegacyOptions()
		{
			const string s = MsSqlLibraryDescriptor.ExtensionName;
			GetSetRestoreDelegate d = new GetSetRestoreDelegate(GetSetRestore);

			// global:
			IniOptions.Register("mssql.max_links", IniFlags.Supported | IniFlags.Global, d, s);
			IniOptions.Register("mssql.secure_connection", IniFlags.Supported | IniFlags.Global, d, s);
			IniOptions.Register("mssql.allow_persistent", IniFlags.Unsupported | IniFlags.Global, d, s);
			IniOptions.Register("mssql.max_persistent", IniFlags.Unsupported | IniFlags.Global, d, s);

			// local:
			IniOptions.Register("mssql.connect_timeout", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("mssql.timeout", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("mssql.batchsize", IniFlags.Supported | IniFlags.Local, d, s);
			IniOptions.Register("mssql.min_error_severity", IniFlags.Unsupported | IniFlags.Local, d, s);
			IniOptions.Register("mssql.min_message_severity", IniFlags.Unsupported | IniFlags.Local, d, s);
			IniOptions.Register("mssql.compatability_mode", IniFlags.Unsupported | IniFlags.Local, d, s);
			IniOptions.Register("mssql.textsize", IniFlags.Unsupported | IniFlags.Local, d, s);
			IniOptions.Register("mssql.textlimit", IniFlags.Unsupported | IniFlags.Local, d, s);
			IniOptions.Register("mssql.datetimeconvert", IniFlags.Unsupported | IniFlags.Local, d, s);
			IniOptions.Register("mssql.max_procs", IniFlags.Unsupported | IniFlags.Local, d, s);
		}

		#endregion

		#region Configuration Getters

		/// <summary>
		/// Gets the library configuration associated with the current script context.
		/// </summary>
		public static MsSqlLocalConfig Local
		{
			get
			{
				return (MsSqlLocalConfig)Configuration.Local.GetLibraryConfig(MsSqlLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets the default library configuration.
		/// </summary>
		public static MsSqlLocalConfig DefaultLocal
		{
			get
			{
				return (MsSqlLocalConfig)Configuration.DefaultLocal.GetLibraryConfig(MsSqlLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets the global library configuration.
		/// </summary>
		public static MsSqlGlobalConfig Global
		{
			get
			{
				return (MsSqlGlobalConfig)Configuration.Global.GetLibraryConfig(MsSqlLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets local configuration associated with a specified script context.
		/// </summary>
		/// <param name="context">Scritp context.</param>
		/// <returns>Local library configuration.</returns>
		public static MsSqlLocalConfig GetLocal(ScriptContext/*!*/ context)
		{
			if (context == null)
				throw new ArgumentNullException("context");

			return (MsSqlLocalConfig)context.Config.GetLibraryConfig(MsSqlLibraryDescriptor.Singleton);
		}

		#endregion
	}
}
