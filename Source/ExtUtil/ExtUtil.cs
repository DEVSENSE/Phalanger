/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Collections;

using PHP.Core;

namespace extutil
{
	/// <summary>
	/// Simple command line utility that invokes certain static methods in <c>PHP.Core.Externals</c>.
	/// </summary>
	class ExtUtil
	{
		/// <summary>
		/// Lists functions in a specified extension.
		/// </summary>
		/// <param name="module">The extension name.</param>
		static void ListFunctions(string module)
		{
			ICollection functions = null;
			try
			{
				functions = Externals.GetFunctionsByModule(module, false);
			}
			catch (Exception e)
			{
				Console.WriteLine("Unable to get function list.");
				Console.WriteLine(e.ToString());
				return;
			}

			if (functions == null)
			{
				Console.WriteLine("Unable to get function list. Extension not loaded.");
				return;
			}

			Console.WriteLine("Functions in " + module + ":");
			foreach (string item in functions)
			{
				Console.WriteLine(item);
			}
		}

		/// <summary>
		/// Lists classes in a specified extension.
		/// </summary>
		/// <param name="module">The extension name.</param>
		static void ListClasses(string module)
		{
			ICollection classes = null;
			try
			{
				classes = Externals.GetClassesByModule(module, false);
			}
			catch (Exception e)
			{
				Console.WriteLine("Unable to get class list.");
				Console.WriteLine(e.ToString());
				return;
			}

			if (classes == null)
			{
				Console.WriteLine("Unable to get class list. Extension not loaded.");
				return;
			}

			Console.WriteLine("Classes in " + module + ":");
			foreach (string item in classes)
			{
				Console.WriteLine(item);
			}
		}

		/// <summary>
		/// Generates managed wrapper of a specified extension.
		/// </summary>
		/// <param name="module">The extension name.</param>
        static void GenerateWrapper(string module)
        {
            Console.WriteLine("Generating managed wrapper for " + module);
            string message = null;
            try
            {
                message = Externals.GenerateManagedWrapper(module);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to generate managed wrapper.");
                Console.WriteLine(e.ToString());
                return;
            }

            if (message != null && message.Length > 0) Console.WriteLine(message);
        }

		/// <summary>
		/// Callback used in <see cref="DoForAllExtensions"/> and <see cref="DoForAllAvailableExtensions"/>.
		/// </summary>
		delegate void Callback(string module);

		/// <summary>
		/// Calls <paramref name="cb"/> for each loaded extension.
		/// </summary>
		/// <param name="cb">The callback.</param>
		static void DoForAllExtensions(Callback cb)
		{
			ICollection modules = null;
			try
			{
				modules = Externals.GetModules(false);
			}
			catch (Exception e)
			{
				Console.WriteLine("Unable to get extension list.");
				Console.WriteLine(e.ToString());
				return;
			}

			if (modules != null)
			{
				foreach (string item in modules)
				{
					if (!item.StartsWith("#"))
					{
						cb(item);
						Console.WriteLine();
					}
				}
			}
		}

		/// <summary>
		/// Calls <paramref name="cb"/> for each available native extension.
		/// </summary>
		/// <param name="cb">The callback.</param>
		static void DoForAllAvailableExtensions(Callback cb)
		{
			try
			{
				foreach (string name in Directory.GetFiles(PHP.Core.Configuration.Application.Paths.ExtNatives, "*.dll"))
				{
					string item = Path.GetFileName(name);
					if (item.ToLower().EndsWith(".dll")) item = item.Substring(0, item.Length - 4);

					cb(item);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("Unable to get native extension list.");
				Console.WriteLine(e.ToString());
				return;
			}
		}

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Console.WriteLine();
			if (args.Length == 0)
			{
				Console.WriteLine("Usage: extutil <option> [<extension>]");//just name of the extension, not a path(it looks to defined Extension directory)
				Console.WriteLine("  Options:\n");
				Console.WriteLine("  -e  Lists errors that occured during extension loading and startup.\n");
				Console.WriteLine("  -m  Lists loaded extensions.\n");
				Console.WriteLine("  -f  Lists functions in all loaded extensions or");
				Console.WriteLine("      in a particular <extension>.\n");
				Console.WriteLine("  -c  Lists classes in all loaded extensions or");
				Console.WriteLine("      in a particular <extension>.\n");
				Console.WriteLine("  -w  Generates managed wrappers for all available extensions or");
				Console.WriteLine("      for a particular <extension> (need not be loaded).");
				return;
			}

			if (args.Length >= 2)
			{
				args[1] = args[1].ToLower();
				if (args[1].EndsWith(".dll")) args[1] = args[1].Substring(0, args[1].Length - 4);
			}

			switch (args[0])
			{
				case "-e":
				{
					ICollection errors = null;
					try
					{
						errors = Externals.GetStartupErrors();
					}
					catch (Exception e)
					{
						Console.WriteLine("Unable to get error list.");
						Console.WriteLine(e.ToString());
						return;
					}

					if (errors != null && errors.Count > 0)
					{
						Console.WriteLine("Startup errors:");
						foreach (string item in errors)
						{
							Console.WriteLine(item);
						}
					}
					else Console.WriteLine("No errors.");
				}
				break;

				case "-m":
				{
					Console.WriteLine("Loaded extensions:");
					DoForAllExtensions(new Callback(Console.WriteLine));
					break;
				}

				case "-f":
				{
					if (args.Length >= 2) ListFunctions(args[1]);
					else DoForAllExtensions(new Callback(ListFunctions));
					break;
				}

				case "-c":
				{
					if (args.Length >= 2) ListClasses(args[1]);
					else DoForAllExtensions(new Callback(ListClasses));
					break;
				}

				case "-w":
				{
					if (args.Length >= 2) GenerateWrapper(args[1]);
					else DoForAllAvailableExtensions(new Callback(GenerateWrapper));
					break;
				}

				default:
				Console.WriteLine("Unknown option: " + args[0]);
				break;
			}

#if DEBUG
            Console.Write("Press Enter to close this window ...");
			Console.ReadLine();
#endif
		}
	}
}
