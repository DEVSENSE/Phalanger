/*

 Copyright (c) 2005-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Reflection;
using PHP.Core;

namespace PHP.Core
{
	/// <summary>
	/// Verifier.
	/// </summary>
	class Verifier
	{
		/// <summary>
		/// Writes PCL verifier logo on console.
		/// </summary>
		static void Logo()
		{
			Version php_net = Assembly.GetExecutingAssembly().GetName().Version;

			Console.WriteLine("The Phalanger Class Library Verifier v{0}.{1}", php_net.Major, php_net.Minor);
		}

		/// <summary>
		/// Displays a help.
		/// </summary>
		private static void ShowHelp()
		{
			Console.WriteLine("Usage: phpverify <assembly file path>");
			Console.WriteLine();
			Console.WriteLine("Checks whether the specified assembly is verifiable Phalanger Class Library assembly.");
		}

		/// <summary>
		/// The entry point.
		/// </summary>
		static void Main(string[] args)
		{
			Logo();

			if (args.Length != 1)
			{
				ShowHelp();
				return;
			}

			Assembly assembly;
			try
			{
				assembly = Assembly.LoadFrom(args[0]);
			}
			catch (Exception e)
			{
				Console.WriteLine("Error occurred while loading assembly: {0}", e.Message);
				goto end;
			}
			
            /*
            ArrayList errors, warnings;

			TODO: PhpLibraryVerifier.VerifyLibrary(assembly,out errors,out warnings);
            */

			  //if (errors.Count>0)
		//{
		//  Console.WriteLine();
		//  Console.WriteLine("Errors:");
		//  foreach(string error in errors)
		//    Console.WriteLine(error);
		//}

			  //if (warnings.Count>0)
		//{
		//  Console.WriteLine();
		//  Console.WriteLine("Warnings:");
		//  foreach(string warning in warnings)
		//    Console.WriteLine(warning);
		//}  

			  //if (errors.Count==0)
		//{
		//  Console.WriteLine();
		//  Console.WriteLine("The assembly has been verified.");
		//}

			end: ;
#if DEBUG
			Console.ReadLine();
#endif
		}
	}
}
