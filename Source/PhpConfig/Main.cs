/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Xml;
using System.Text;
using System.Reflection;
using System.Collections;

using PHP.Library;
using PHP.Core;
using System.Configuration;

namespace PHP
{
	/// <summary>
	/// Configuration manager.
	/// </summary>
	class PhpConfig
	{
		static void Logo()
		{
			Version php_net = Assembly.GetExecutingAssembly().GetName().Version;

			Console.WriteLine("The Phalanger Configuration Manager v{0}.{1}", php_net.Major, php_net.Minor);
		}

		/// <summary>
		/// Displays a help.
		/// </summary>
		private static void ShowHelp()
		{
			Console.WriteLine("Usage: phpconfig [<options>] <php.ini file path> <target .config fle path>");
			Console.WriteLine();
			Console.WriteLine("Available options:");
			PrintOptionDesc("/phpnames[+-]", "Adds 'pnpName' attribute containing original PHP name of the option to option nodes. Off by default.");
			PrintOptionDesc("/help", "Shows this help.");
			Console.WriteLine("Converts configuration stored in php.ini file to Phalanger XML .config file.");
		}

		private static void PrintOptionDesc(string option, string description)
		{
			Console.WriteLine("{0}\n{1}\n", option, description);
		}

		static string iniFile = null;
		static string configFile = null;
		static bool phpNames = false;

		static bool ProcessArguments(string[] args)
		{
			string currentOption;

			for (int i = 0; i < args.Length; i++)
			{
				Debug.Assert(args[i].Length > 0);

				// option:
				if (args[i][0] == '/')
				{
					int colon = args[i].IndexOf(':');

					if (colon >= 0)
					{
						// option having format "/name:value"
						// currentOption = args[i].Substring(1,colon-1).Trim();
						// currentValue = args[i].Substring(colon+1).Trim();
					}
					else
					{
						// option having format "/name"
						currentOption = args[i].Substring(1).Trim();

                        switch (currentOption.ToLowerInvariant())
						{
							case "phpnames":
							case "phpnames+": phpNames = true; break;
							case "phpnames-": phpNames = false; break;

							case "?":
							case "h":
							case "help": return false;
						}
					}
				}
				else
				{
					if (iniFile == null)
						iniFile = args[i];
					else
						if (configFile == null)
							configFile = args[i];
						else
							throw new ApplicationException(String.Format("Superfluous argument: '{0}'.", args[i]));
				}
			}

			if (iniFile == null || configFile == null)
				return false;

			return true;
		}

		static PhpLibraryDescriptor LoadExtension(string/*!*/ nativeFileName)
		{
			Assembly assembly = Assembly.LoadFrom(Externals.GetWrapperPath(nativeFileName));
			// TODO: ApplicationContext.AddConstants(assembly);
			//return ApplicationContext.Loa(assembly, null);
			return null;
		}

		class OptionsProcessor : PhpIniParser.IParserCallbacks
		{
			public readonly Hashtable Extensions; // GENERICS: <PhpLibraryDescriptor,Hashtable> 
			public readonly Hashtable Options;    // GENERICS: <string,string>

			public OptionsProcessor()
			{
				Extensions = new Hashtable();
				Options = new Hashtable();
			}

			#region PhpIniParser.IParserCallbacks Members

			public void ProcessOption(object key, string value)
			{
				string option = key.ToString();

				switch (option)
				{
					case "extension":
						{
							PhpLibraryDescriptor descriptor = LoadExtension(value);
							Extensions.Add(descriptor, new Hashtable());
							break;
						}

					default:
						Options[option] = value;
						break;
				}
			}

			public object GetConstantValue(string name)
			{
				return ScriptContext.CurrentContext.GetConstantValue(name, true, true);
			}

			public void ProcessSection(object sectionName)
			{
				// ignored //
			}

			#endregion

			public void WriteAll(string fileName)
			{
				XmlTextWriter writer = new XmlTextWriter(fileName, Encoding.UTF8);
				writer.WriteStartDocument();

				writer.Formatting = Formatting.Indented;
				writer.Indentation = 1;
				writer.IndentChar = '\t';

				writer.WriteStartElement("configuration");
				writer.WriteStartElement(PHP.Core.Configuration.SectionName);

				// write libraries:
				if (Extensions.Count > 0)
				{
					writer.WriteStartElement("classLibrary");
					foreach (DictionaryEntry entry in Extensions)
					{
						PhpLibraryDescriptor descriptor = (PhpLibraryDescriptor)entry.Key;

						writer.WriteStartElement("add");
						writer.WriteAttributeString("assembly", descriptor.RealAssembly.FullName);
						// TODO: writer.WriteAttributeString("section", descriptor.Name);
						writer.WriteEndElement();
					}
					writer.WriteEndElement();
				}

				// writes Core options and removes them from hashtable:
				PhpIni.CoreOptionsToXml(writer, Options, phpNames);

				// writes BCL options and removes them from hashtable:
				writer.WriteStartElement("bcl");
				LibraryConfiguration.LegacyOptionsToXml(writer, Options, phpNames);
				writer.WriteEndElement();

				// distributes remaining options to extensions buckets:
				DistributeExtensionOptions();

				// write extension options:
				ExtensionOptionsToXml(writer);

				writer.WriteEndElement();
				writer.WriteEndDocument();
				writer.Close();

				if (Options.Count > 0)
				{
					string[] opt_names = new string[Options.Count];
					Options.Keys.CopyTo(opt_names, 0);
					Array.Sort(opt_names);

					Console.Error.WriteLine("Following options has been skipped. They are either not supported or the declaring extension is not loaded by the .ini file.");
					foreach (string option in opt_names)
						Console.Error.WriteLine(option);
				}
			}

			private void DistributeExtensionOptions()
			{
				string[] opt_names = new string[Options.Count];
				Options.Keys.CopyTo(opt_names, 0);

				foreach (string option in opt_names)
				{
					foreach (DictionaryEntry ext_entry in Extensions)
					{
						PhpLibraryDescriptor descriptor = (PhpLibraryDescriptor)ext_entry.Key;
						Hashtable ext_options = (Hashtable)ext_entry.Value;
						string ext_name = Path.ChangeExtension(descriptor.RealAssembly.GetName().Name, null);

						if (Externals.IniOptionExists(ext_name, option))
						{
							ext_options.Add(option, Options[option]);
							Options.Remove(option);
						}
					}
				}
			}

			private void ExtensionOptionsToXml(XmlTextWriter writer)
			{
				foreach (DictionaryEntry entry in Extensions)
				{
					PhpLibraryDescriptor descriptor = (PhpLibraryDescriptor)entry.Key;
					Hashtable options = (Hashtable)entry.Value;

					if (options.Count > 0)
					{
						// TODO: 
						writer.WriteStartElement("x"/*descriptor.Name*/);

						foreach (DictionaryEntry opt_entry in options)
						{
							writer.WriteStartElement("set");

							writer.WriteAttributeString("name", (string)opt_entry.Key);
							writer.WriteAttributeString("value", (string)opt_entry.Value);

							writer.WriteEndElement();
						}

						writer.WriteEndElement();
					}
				}
			}
		}

		static void TranslateOptions()
		{
			OptionsProcessor processor = new OptionsProcessor();

			// adds BCL to library (if not added yet):
			// TODO:
			// ApplicationContext.AddModule(typeof(PhpIni).Assembly, null);

			using (PhpStream stream = PhpStream.Open(iniFile, "rb", StreamOpenOptions.ReportErrors, StreamContext.Default))
			{
				if (stream == null)
					throw new ApplicationException(String.Format("Unable to open file '{0}' for reading.", iniFile));

				PhpIniParser.Parse(stream, processor);
			}

			processor.WriteAll(configFile);
		}

		/// <summary>
		/// The entry point.
		/// </summary>
		static void Main(string[] args)
		{
			Environment.ExitCode = 1;

			Logo();

			try
			{
				if (!ProcessArguments(args))
				{
					ShowHelp();
					goto end;
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine(e.Message);
				Console.WriteLine();
				Console.WriteLine();
				ShowHelp();
				goto end;
			}


			// loads entire configuration:
			try
			{
				PHP.Core.Configuration.Load(ApplicationContext.Default);
			}
			catch (ConfigurationErrorsException e)
			{
				Console.Error.WriteLine("Configuration error:");
				Console.Error.WriteLine(e.Message);
				goto end;
			}

			try
			{
				TranslateOptions();
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Error: {0}", e.Message);
				goto end;
			}

			Console.WriteLine("The configuration file '{0}' has been created.", Path.GetFullPath(configFile));
			Environment.ExitCode = 0;

		end: ;
#if DEBUG
			Console.ReadLine();
#endif
		}
	}
}
