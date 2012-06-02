using System;
using PHP.Core;
using PHP.Core.Emit;
using System.Reflection;
using System.Reflection.Emit;

namespace ReflectionCacheGen
{
	class RCG
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			if (args.Length != 1 && args.Length != 2)
			{
				Console.WriteLine("Usage: rcg <type name> [<binding flags>]");
				return;
			}

			Type type = typeof(PHP.Core.Operators).Assembly.GetType(args[0], false, true);
			if (type == null)
			{
				Console.WriteLine("Type {0} not found in PhpNetCore assembly.", args[0]);
				return;
			}

			BindingFlags flags;
			if (args.Length >= 2)
			{
				try
				{
					flags = (BindingFlags)Enum.Parse(typeof(BindingFlags), args[1], true);
				}
				catch (Exception)
				{
					Console.WriteLine("Invalid binding flags {0}.", args[1]);
					return;
				}
			}
			else
			{
				flags = BindingFlags.Public | BindingFlags.Static;
			}

			PHP.Core.Emit.MethodsGenerator.Generate(type, flags);
		}
	}
}
