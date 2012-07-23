using System;
using System.IO;
namespace Lex
{
	public class Lex
	{
		public const int MAXBUF = 8192;
		public const int MAXSTR = 128;
		private static string currentOption;
		private static string currentValue;
		private static string inFile;
		private static string outFile;
		private static int version;
		public static bool ProcessArguments(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				string text = args[i];
				if (text[0] == '/')
				{
					int num = text.IndexOf(':');
					if (num >= 0)
					{
						Lex.currentOption = text.Substring(1, num - 1).Trim();
						Lex.currentValue = text.Substring(num + 1).Trim();
						string a;
						if ((a = Lex.currentOption.ToLower()) != null)
						{
							if (!(a == "v") && !(a == "version"))
							{
								if (a == "help")
								{
									Lex.DisplayHelp();
									return false;
								}
							}
							else
							{
								if (Lex.currentValue == "1")
								{
									Lex.version = 1;
									goto IL_13C;
								}
								if (Lex.currentValue == "2")
								{
									Lex.version = 2;
									goto IL_13C;
								}
								throw new ApplicationException("Invalid version number. Specify either '1' or '2'.");
							}
						}
						throw new ApplicationException("Unknown option.");
					}
					Lex.currentOption = text.Substring(1).Trim();
					Lex.currentOption.ToLower();
					throw new ApplicationException("Unknown option.");
				}
				else
				{
					if (Lex.inFile == null)
					{
						Lex.inFile = text;
					}
					else
					{
						if (Lex.outFile != null)
						{
							Lex.currentOption = text;
							throw new ApplicationException("Invalid option.");
						}
						Lex.outFile = text;
					}
				}
				IL_13C:;
			}
			if (Lex.outFile == null)
			{
				Lex.outFile = Path.ChangeExtension(Lex.inFile, ".cs");
			}
			return true;
		}
		private static void DisplayHelp()
		{
			Console.WriteLine("lex <filename> [<outfile>] [<options>]");
			Console.WriteLine();
			Console.WriteLine("/help                   Displays this help.");
			Console.WriteLine("/v[ersion]:<version>    Version of C# to use for generated code.");
		}
		public static void Main(string[] args)
		{
			try
			{
				if (!Lex.ProcessArguments(args))
				{
					return;
				}
			}
			catch (ApplicationException ex)
			{
				if (Lex.currentOption != null)
				{
					Console.WriteLine("Option '{0}', value '{1}'.", Lex.currentOption, Lex.currentValue);
				}
				Console.WriteLine(ex.Message);
				Console.WriteLine();
				Environment.ExitCode = 1;
			}
			try
			{
				Gen gen = new Gen(Lex.inFile, Lex.outFile, Lex.version);
				gen.Generate();
			}
			catch (ApplicationException ex2)
			{
				Console.WriteLine(ex2.Message);
				Console.WriteLine();
				Environment.ExitCode = 1;
			}
			catch (Exception ex3)
			{
				Console.WriteLine(ex3.Message);
				Console.WriteLine(ex3.StackTrace);
				Environment.ExitCode = 1;
			}
		}
	}
}
