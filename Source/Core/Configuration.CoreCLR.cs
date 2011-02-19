using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using PHP.CoreCLR;
using PHP.Core.Reflection;

namespace PHP.Core
{
	// library configuration

	#region LibraryConfigStore

	/// <summary>
	/// Class that abstracts library configuration.
	/// On CoreCLR it is not used (so far!)
	/// </summary>
	public class LibraryConfigStore
	{
		internal LibraryConfigStore() { }
	}

	#endregion

	#region Library Configuration Interface

	/// <summary>
	/// Interface implemented by all configuration sections loaded from XML config file.
	/// </summary>
	public interface IPhpConfigurationSection
	{
	}

	#endregion

	// partial classes with initialization

	#region Application Configuration

	/// <summary>
	/// The configuration containing per-application configuration. 
	/// The confguration can be defined only in Machine.config and 
	/// some can be changed also in Web.config files in the appliciation root directory or above.
	/// </summary>
	public sealed partial class ApplicationConfiguration
	{
		#region Compiler

		/// <summary>
		/// Compiler options.
		/// </summary>
		public sealed partial class CompilerSection : IPhpConfigurationSection
		{
			public CompilerSection()
			{
				disabledWarningNumbers = new int[0];
				disabledWarnings = WarningGroups.None;
			}
		}

		#endregion

		#region Globalization

		/// <summary>
		/// Configuration related to culture.
		/// </summary>
		public sealed partial class GlobalizationSection : IPhpConfigurationSection
		{
			public GlobalizationSection()
			{
				pageEncoding = Encoding.UTF8;
			}
		}

		#endregion
	}

	#endregion

	// access to configuration (on CoreCLR)

	#region Configuration

	/// <summary>
	/// Provides access to the current configuration records.
	/// </summary>
	[DebuggerNonUserCode]
	public sealed class Configuration
	{
		private static Configuration current = null;
		private readonly GlobalConfiguration/*!*/ global;
		private readonly LocalConfiguration/*!*/ defaultLocal;
		internal static ApplicationConfiguration application = new ApplicationConfiguration();

		private Configuration(GlobalConfiguration/*!*/ global, LocalConfiguration/*!*/ defaultLocal)
		{
			this.global = global;
			this.defaultLocal = defaultLocal;
		}


		/// <summary>
		/// Loads configuration and returns configuration record.
		/// </summary>
		/// <exception cref="ConfigurationErrorsException">Configuration is invalid or incomplete.</exception>
		public static void Load(ApplicationContext/*!*/ appContext)
		{
			if (current == null)
			{
				// no configuration loaded from .config files:
				current = new Configuration(new GlobalConfiguration(), new LocalConfiguration());
			}
		}


		/// <summary>
		/// Default values for local (script dependent) configuration.
		/// Different requsts (threads) may have different global configurations as it depends on the 
		/// directory the request is targetting. Requests to the same directory share the same record.
		/// </summary>
		public static LocalConfiguration DefaultLocal
		{
			get
			{
				Load(ApplicationContext.Default);
				Debug.Assert(current != null);
				return current.defaultLocal;
			}
		}

		/// <summary>
		/// Gets script local configuration record, which is unique per request.
		/// </summary>
		public static LocalConfiguration Local
		{
			get
			{
				return ScriptContext.CurrentContext.Config;
			}
		}


		/// <summary>
		/// Global (script independent) configuration.
		/// Different requsts (threads) may have different global configurations as it depends on the 
		/// directory the request is targetting. Requests to the same directory share the same record.
		/// </summary>
		public static GlobalConfiguration Global
		{
			get
			{
				Load(ApplicationContext.Default);
				Debug.Assert(current != null);
				return current.global;
			}
		}


		/// <summary>
		/// Gets application configuration record.
		/// The record is shared among all requests (threads) of the application.
		/// </summary>
		public static ApplicationConfiguration Application
		{
			get
			{
				// note: more threads can start loading the configuration, but that ok:
				if (!application.IsLoaded) Load(ApplicationContext.Default);
				return application;
			}
		}
	}

	#endregion

	class ConfigurationContext
	{
		/// <summary>
		/// Loads a library and adds a new section to the list of sections if available.
		/// </summary>
		internal static bool AddLibrary(string assemblyName, Uri assemblyUrl, string sectionName)
		{
			Debug.Assert(assemblyName != null ^ assemblyUrl != null);

			DAssembly assembly = ScriptContext.CurrentContext.ApplicationContext.
				AssemblyLoader.Load(assemblyName, assemblyUrl, new LibraryConfigStore());
			PhpLibraryAssembly lib_assembly = assembly as PhpLibraryAssembly;

			// not a PHP library or the library is loaded for reflection only:
			if (lib_assembly == null)
				return true;

/*			PhpLibraryDescriptor descriptor = lib_assembly.Descriptor;

			// section name not stated or the descriptor is not available (reflected-only assembly):
			if (sectionName == null || descriptor == null)
				return true;

			if (descriptor.ConfigurationSectionName == sectionName)
			{
				// an assembly has already been assigned a section? => ok
				if (sections.ContainsKey(sectionName)) return true;

				// TODO (TP): Consider whther this is correct behavior?
				//       This occures under stress test, because ASP.NET calls 
				//       ConfigurationSectionHandler.Create even though we already loaded assemblies
				Debug.WriteLine("CONFIG", "WARNING: Loading configuration for section '{0}'. " +
					"Library has been loaded, but the section is missing.", sectionName);
			}
			else if (descriptor.ConfigurationSectionName != null)
			{
				// an assembly has already been loaded with another section name => error:
				throw new ConfigurationErrorsException(CoreResources.GetString("cannot_change_library_section",
					descriptor.RealAssembly.FullName, descriptor.ConfigurationSectionName), node);
			}

			// checks whether the section has not been used yet:
			LibrarySection existing_section;
			if (sections.TryGetValue(sectionName, out existing_section))
			{
				Assembly conflicting_assembly = existing_section.Descriptor.RealAssembly;
				throw new ConfigurationErrorsException(CoreResources.GetString("library_section_redeclared",
						sectionName, conflicting_assembly.FullName), node);
			}

			// maps section name to the library descriptor:
			descriptor.WriteConfigurationUp(sectionName);
			sections.Add(sectionName, new LibrarySection(descriptor));
			*/
			return true;
		}
	}
}
