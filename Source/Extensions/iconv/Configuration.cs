using System;
using System.Web;
using System.Xml;
using System.Collections;
using System.Configuration;

using PHP.Core;

namespace PHP.Library.Iconv
{
	#region Local Configuration

	/// <summary>
    /// Script independent Iconv configuration.
	/// </summary>
	[Serializable]
	public sealed class IconvLocalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
        internal IconvLocalConfig() { }

        public string InputEncoding = "ISO-8859-1";
        public string InternalEncoding = "ISO-8859-1";
        public string OutputEncoding = "ISO-8859-1";

		/// <summary>
		/// Creates a deep copy of the configuration record.
		/// </summary>
		/// <returns>The copy.</returns>
		public IPhpConfiguration DeepCopy()
		{
            return (IconvLocalConfig)this.MemberwiseClone();
		}

		/// <summary>
		/// Loads configuration from XML.
		/// </summary>
		public bool Parse(string name, string value, XmlNode node)
		{
			switch (name)
			{
                case "iconv.input_encoding":    // legacy option name
                case "InputEncoding":
                    InputEncoding = value;
                    break;
                case "iconv.internal_encoding": // legacy option name
                case "InternalEncoding":
                    InternalEncoding = value;
                    break;
                case "iconv.output_encoding":   // legacy option name
                case "OutputEncoding":
                    OutputEncoding = value;
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
    /// Script dependent Iconv configuration.
	/// </summary>
	[Serializable]
	public sealed class IconvGlobalConfig : IPhpConfiguration, IPhpConfigurationSection
	{
		internal IconvGlobalConfig() { }

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
            return (IconvGlobalConfig)this.MemberwiseClone();
		}
	}

	#endregion

	/// <summary>
    /// Iconv extension configuration.
	/// </summary>
    public static class IconvConfiguration
	{
		#region Legacy Configuration

		/// <summary>
		/// Gets, sets, or restores a value of a legacy configuration option.
		/// </summary>
		private static object GetSetRestore(LocalConfiguration config, string option, object value, IniAction action)
		{
            IconvLocalConfig local = (IconvLocalConfig)config.GetLibraryConfig(IconvLibraryDescriptor.Singleton);
            IconvLocalConfig @default = DefaultLocal;
            IconvGlobalConfig global = Global;

            //[iconv]
            //;iconv.input_encoding = ISO-8859-1
            //;iconv.internal_encoding = ISO-8859-1
            //;iconv.output_encoding = ISO-8859-1

			switch (option)
			{
                //// local:

                case "iconv.input_encoding":
                    return PhpIni.GSR(ref local.InputEncoding, @default.InputEncoding, value, action);

                case "iconv.internal_encoding":
                    return PhpIni.GSR(ref local.InternalEncoding, @default.InternalEncoding, value, action);

                case "iconv.output_encoding":
                    return PhpIni.GSR(ref local.OutputEncoding, @default.OutputEncoding, value, action);

            }

			Debug.Fail("Option '" + option + "' is supported but not implemented.");
			return null;
		}

		/// <summary>
        /// Writes Iconv legacy options and their values to XML text stream.
		/// Skips options whose values are the same as default values of Phalanger.
		/// </summary>
		/// <param name="writer">XML writer.</param>
		/// <param name="options">A hashtable containing PHP names and option values. Consumed options are removed from the table.</param>
		/// <param name="writePhpNames">Whether to add "phpName" attribute to option nodes.</param>
		public static void LegacyOptionsToIconv(XmlTextWriter writer, Hashtable options, bool writePhpNames) // GENERICS:<string,string>
		{
			if (writer == null)
				throw new ArgumentNullException("writer");
			if (options == null)
				throw new ArgumentNullException("options");

            var local = new IconvLocalConfig();
			var global = new IconvGlobalConfig();
			var ow = new PhpIniXmlWriter(writer, options, writePhpNames);

            ow.StartSection("iconv");

            // local:
            ow.WriteOption("iconv.input_encoding", "InputEncoding", "ISO-8859-1", local.InputEncoding);
            ow.WriteOption("iconv.internal_encoding", "InternalEncoding", "ISO-8859-1", local.InternalEncoding);
            ow.WriteOption("iconv.output_encoding", "OutputEncoding", "ISO-8859-1", local.OutputEncoding);

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
			const string s = "iconv";
			GetSetRestoreDelegate d = new GetSetRestoreDelegate(GetSetRestore);

            IniOptions.Register("iconv.input_encoding", IniFlags.Supported | IniFlags.Local, d, s);
            IniOptions.Register("iconv.internal_encoding", IniFlags.Supported | IniFlags.Local, d, s);
            IniOptions.Register("iconv.output_encoding", IniFlags.Supported | IniFlags.Local, d, s);
		}

		#endregion

		#region Configuration Getters

		/// <summary>
		/// Gets the library configuration associated with the current script context.
		/// </summary>
        public static IconvLocalConfig Local
		{
			get
			{
                return (IconvLocalConfig)Configuration.Local.GetLibraryConfig(IconvLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets the default library configuration.
		/// </summary>
        public static IconvLocalConfig DefaultLocal
		{
			get
			{
                return (IconvLocalConfig)Configuration.DefaultLocal.GetLibraryConfig(IconvLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets the global library configuration.
		/// </summary>
		public static IconvGlobalConfig Global
		{
			get
			{
                return (IconvGlobalConfig)Configuration.Global.GetLibraryConfig(IconvLibraryDescriptor.Singleton);
			}
		}

		/// <summary>
		/// Gets local configuration associated with a specified script context.
		/// </summary>
		/// <param name="context">Scritp context.</param>
		/// <returns>Local library configuration.</returns>
        public static IconvLocalConfig GetLocal(ScriptContext/*!*/ context)
		{
			if (context == null)
				throw new ArgumentNullException("context");

            return (IconvLocalConfig)context.Config.GetLibraryConfig(IconvLibraryDescriptor.Singleton);
		}

		#endregion
	}
}
