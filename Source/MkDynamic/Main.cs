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
using System.Reflection;
using PHP.Core.Emit;

namespace PHP.Core
{
	/// <summary>
	/// Dynamic library wrapper generator.
	/// </summary>
	class DynamicLibraryWrapperGenerator
	{
		/// <summary>
		/// Writes PHP.NET dynamic wrapper logo on console.
		/// </summary>
		static void Logo()
		{
			Version php_net = Assembly.GetExecutingAssembly().GetName().Version;

			Console.WriteLine("The Phalanger Class Library Dynamic Wrapper Generator v{0}.{1}", php_net.Major, php_net.Minor);
		}

		/// <summary>
		/// Displays a help.
		/// </summary>
		private static void ShowHelp()
		{
			Console.WriteLine("Usage: mkdynamic [-dynamic] <assembly file path> [<target directory>]");
			Console.WriteLine();
			Console.WriteLine("Generates a dynamic wrapper of a specified Class Library assembly to the target "+
				"directory or to the current dynamic directory if not specified.\n\n"+
				"Use the '-dynamic' switch to dynamically resolve 'ImplementsFunctionAttribute' used to annotate"+
				" the library from a file 'PhpNetCore.dll' in the current directory.");
		}

		static string assemblyName;
		static string directory;
		static bool resolve = false;

		static bool ProcessArguments(string[] args)
		{
			int index = 0;
			if (args[0] == "-dynamic")
			{
				resolve = true;
				index++;
			}

			if (args.Length < 1 || args.Length > 3 || args[0] != "-dynamic")
			{
				ShowHelp();
				return false;
			}

			// the name of the library file:
			assemblyName = args[index];

			// the target directory:
			if (args.Length == index + 2)
				directory = args[index + 1];

			return true;
		}

		/// <summary>
		/// The entry point.
		/// </summary>
		static void Main(string[] args)
		{
			Environment.ExitCode = 1;

			Logo();

			if (!ProcessArguments(args)) return;

			Type attr = null;
			if (resolve)
			{
				try
				{
					Assembly coreAsm = Assembly.LoadFrom("PhpNetCore.dll");
					attr = coreAsm.GetType("PHP.Core.ImplementsFunctionAttribute");
				}
				catch(Exception e)
				{
					Console.WriteLine("Dynamic resolution of the 'ImplementsFunctionAttribute' failed: ", e.Message);
					return;
				}
			}

			Assembly assembly;
			try
			{
				assembly = Assembly.LoadFrom(assemblyName);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error occured while loading assembly: {0}", e.Message);
				return;
			}

			string dynamic_assembly_path;
			try
			{
                dynamic_assembly_path = LibraryBuilder.CreateDynamicWrapper(attr, assembly, directory, PHP.Core.Reflection.PhpLibraryModule.DynamicWrapperFileName(assembly, 0));
			}
			catch (Exception e)
			{
				Console.WriteLine("Error occured while generating wrapper: {0}", e.Message);
				return;
			}

			Console.WriteLine("The dynamic wrapper '{0}' has been generated.", Path.GetFullPath(dynamic_assembly_path));
			Environment.ExitCode = 0;
		}
	}
}
