/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Configuration;
using System.Reflection;
using System.Threading;
using System.Text;
using System.Text.RegularExpressions;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Core
{
	/// <summary>
	/// Command line compiler.
	/// </summary>
	public sealed class PhpNetCompiler
	{
		private TextWriter/*!*/ output;
		private TextWriter/*!*/ errors;

		private CommandLineParser/*!*/ commandLineParser = new CommandLineParser();

		#region Helpers

		/// <summary>
		/// Writes Phalanger logo on output.
		/// </summary>
		private void ShowLogo()
		{
			Version php_net = Assembly.GetExecutingAssembly().GetName().Version;
			Version runtime = Environment.Version;

			output.WriteLine("Phalanger - the PHP Language Compiler - version {0}.{1}", php_net.Major, php_net.Minor);
			output.WriteLine("for Microsoft (R) .NET Framework version {0}.{1}", runtime.Major, runtime.Minor);
		}

		/// <summary>
		/// Displays a help.
		/// </summary>
		private void ShowHelp()
		{
			output.WriteLine("Usage:");

			foreach (KeyValuePair<string, string> option in CommandLineParser.GetSupportedOptions())
			{
				output.WriteLine("{0}\n{1}\n", option.Key, option.Value);
			}

			output.WriteLine();
			output.WriteLine(CoreResources.GetString("phpc_other_args"));
			output.WriteLine();
		}

		private void DumpArguments(List<string>/*!*/ args)
		{
			if (commandLineParser.Verbose && args.Count > 0)
			{
				output.WriteLine(CoreResources.GetString("Arguments") + ":");
				for (int i = 0; i < args.Count; i++) output.WriteLine(args[i]);
				output.WriteLine();
			}
		}

		private void DumpLoadedLibraries()
		{
			output.WriteLine(CoreResources.GetString("loaded_libraries") + ":");

			foreach (DAssembly assembly in ApplicationContext.Default.GetLoadedAssemblies())
				output.WriteLine(assembly.DisplayName);

			output.WriteLine();
		}
		
		#endregion

        #region Handling assembly load

        /// <summary>
        /// Name and file path to the specified assembly.
        /// </summary>
        private struct AssemblyInfo
        {
            public AssemblyName Name;
            public string Path;
        }

        /// <summary>
        /// Add AssemblyResolve handler that handles loading of assemblies in specified directory by their FullName.
        /// </summary>
        /// <param name="directoryName">The directory to load assemblies by their FullName from.</param>
        private void HandleAssemblies(string directoryName)
        {
            if (!Directory.Exists(directoryName))
                return;

            var files = Directory.GetFiles(directoryName, "*.dll", SearchOption.TopDirectoryOnly);
            var assembliesInDirectory = new List<AssemblyInfo>(files.Length);

            foreach (string file in files)
            {
                try
                {
                    assembliesInDirectory.Add(new AssemblyInfo()
                    {
                        Name = AssemblyName.GetAssemblyName(file),
                        Path = file
                    });
                }
                catch
                {

                }
            }

            //set domain AssemblyResolve to lookup assemblies found in directoryName
            if (assembliesInDirectory.Count > 0)
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(
                    delegate(object o, ResolveEventArgs ea)
                    {
                        AssemblyName name = new AssemblyName(ea.Name);

                        foreach (var assembly in assembliesInDirectory)
                        {
                            if (AssemblyName.ReferenceMatchesDefinition(assembly.Name, name))
                                return Assembly.LoadFile(assembly.Path);
                        }

                        return null;
                    }
                );
        }

        #endregion

        #region Main

        /// <summary>
		/// Runs the compiler with specified options.
		/// </summary>
		/// <param name="args">Command line arguments.</param>
		/// <returns>Whether the compilation was successful.</returns>
		public bool Compile(List<string>/*!*/ args)
		{
			if (args == null)
				throw new ArgumentNullException("args");
			
			TextErrorSink errorSink = null;

			// processes arguments:
			try
			{
				try
				{
					commandLineParser.Parse(args);
				}
				finally
				{
					if (commandLineParser.Quiet)
					{
						output = errors = TextWriter.Null;
					}
					else if (commandLineParser.RedirectErrors)
					{
						output = errors = Console.Out;	
					}
					else
					{
						output = Console.Out;	
						errors = Console.Error;
					}

					errorSink = new TextErrorSink(errors);

					ShowLogo();

					if (commandLineParser.ShowHelp)
					{
						ShowHelp();
					}
					else
					{	
						DumpArguments(args);
					}	
				}
			}
			catch (InvalidCommandLineArgumentException e)
			{
				e.Report(errorSink);
				return false;
			}

			if (commandLineParser.ShowHelp)
				return true;
            
            // allow loading of all assemblies in /Bin directory by their FullName
            HandleAssemblies(Path.Combine(commandLineParser.Parameters.SourceRoot, "Bin"));            
			
            //
			ApplicationContext.DefineDefaultContext(false, true, false);
			ApplicationContext app_context = ApplicationContext.Default;

			CompilerConfiguration compiler_config;

			// loads entire configuration:
			try
			{
				if (commandLineParser.Parameters.ConfigPaths.Count == 0)
				{
					// Add config files for known targets
					switch (commandLineParser.Parameters.Target)
					{
						case ApplicationCompiler.Targets.Web:
							if (File.Exists("web.config"))
								commandLineParser.Parameters.ConfigPaths.Add(new FullPath("web.config"));
							break;
						case ApplicationCompiler.Targets.WinApp:
							if (File.Exists("app.config"))
								commandLineParser.Parameters.ConfigPaths.Add(new FullPath("app.config"));
							break;
					}
				}
				compiler_config = ApplicationCompiler.LoadConfiguration(app_context, 
					commandLineParser.Parameters.ConfigPaths, output);
					
				commandLineParser.Parameters.ApplyToConfiguration(compiler_config);
			}
			catch (ConfigurationErrorsException e)
			{
				if (commandLineParser.Verbose)
				{
					output.WriteLine(CoreResources.GetString("reading_configuration") + ":");
					output.WriteLine();

					if (!String.IsNullOrEmpty(e.Filename)) // Mono puts here null
					{
						output.WriteLine(FileSystemUtils.ReadFileLine(e.Filename, e.Line).Trim());
						output.WriteLine();
					}
				}
				
				errorSink.AddConfigurationError(e);
				return false;
			}

			// load referenced assemblies:
			try
			{
				try
				{
					app_context.AssemblyLoader.Load(commandLineParser.Parameters.References);
				}
				finally
				{
					if (commandLineParser.Verbose)
						DumpLoadedLibraries();
				}	
			}
			catch (ConfigurationErrorsException e)
			{
				errorSink.AddConfigurationError(e);
				return false;
			}
			
			output.WriteLine(CoreResources.GetString("performing_compilation") + " ...");

			try
			{
				CommandLineParser p = commandLineParser;
				Statistics.DrawGraph = p.DrawInclusionGraph;

				errorSink.DisabledGroups = compiler_config.Compiler.DisabledWarnings;
				errorSink.DisabledWarnings = compiler_config.Compiler.DisabledWarningNumbers;
                errorSink.TreatWarningsAsErrors = compiler_config.Compiler.TreatWarningsAsErrors;
			
				// initializes log:
				Debug.ConsoleInitialize(Path.GetDirectoryName(p.Parameters.OutPath));

                new ApplicationCompiler().Compile(app_context, compiler_config, errorSink, p.Parameters);
			}
			catch (InvalidSourceException e)
			{
				e.Report(errorSink);
				return false;
			}
			catch (Exception e)
			{
				errorSink.AddInternalError(e);
				return false;
			}

            var errorscount = errorSink.ErrorCount + errorSink.FatalErrorCount;
            var warningcount = errorSink.WarningCount + errorSink.WarningAsErrorCount;
            
            output.WriteLine();
            output.WriteLine("Build complete -- {0} error{1}, {2} warning{3}.",
                errorscount, (errorscount == 1) ? "" : "s",
                warningcount, (warningcount == 1) ? "" : "s");

			return !errorSink.AnyError;
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		public static int Main(string[] args)
		{
			return new PhpNetCompiler().Compile(new List<string>(args)) ? 0 : 1;
		}

		#endregion
	}
}
