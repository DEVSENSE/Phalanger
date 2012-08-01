using System;
using System.Collections.Generic;
using System.IO;
namespace gpcc
{
	internal class GPCG
	{
		public static bool LINES = true;
		public static bool REPORT = false;
		private static void Main(string[] args)
		{
			try
			{
				string filename;
				string text;
				string log;
				if (GPCG.ProcessOptions(args, out filename, out text, out log))
				{
					using (TextWriter textWriter = (text != null) ? File.CreateText(text) : Console.Out)
					{
						Parser parser = new Parser();
						Grammar grammar = parser.Parse(filename);
						LALRGenerator lALRGenerator = new LALRGenerator(grammar);
						List<State> states = lALRGenerator.BuildStates();
						lALRGenerator.ComputeLookAhead();
						lALRGenerator.BuildParseTable();
						if (GPCG.REPORT)
						{
							lALRGenerator.Report(log);
						}
						CodeGenerator codeGenerator = new CodeGenerator(textWriter);
						codeGenerator.Generate(states, grammar);
					}
				}
			}
			catch (Scanner.ParseException ex)
			{
				Console.Error.WriteLine("Parse error (line {0}, column {1}): {2}", ex.line, ex.column, ex.Message);
			}
			catch (Exception ex2)
			{
				Console.Error.WriteLine("Unexpected Error {0}", ex2.Message);
				Console.Error.WriteLine("Please report to w.kelly@qut.edu.au");
			}
		}
		private static bool ProcessOptions(string[] args, out string src, out string dst, out string log)
		{
			src = null;
			dst = null;
			log = null;
			for (int i = 0; i < args.Length; i++)
			{
				string text = args[i];
				if (text[0] == '-' || text[0] == '/')
				{
					string key;
					if ((key = text.Substring(1)) != null)
					{
                        switch (key)
                        {
                            case "?":
                            case "h":
                            case "help":
							    GPCG.DisplayHelp();
								return false;

                            case "v":
                            case "version":
                                GPCG.DisplayVersion();
								return false;

                            case "l":
                            case "no-lines":
                                GPCG.LINES = false;
                                break;

                            case "r":
                            case "report":
                                GPCG.REPORT = true;
								break;
                            
                            default:
								break;
						}
					}
				}
				else
				{
					if (src == null)
					{
						src = text;
					}
					else
					{
						if (dst == null)
						{
							dst = text;
						}
						else
						{
							if (log == null)
							{
								log = text;
							}
						}
					}
				}
			}
			if (src == null)
			{
				GPCG.DisplayHelp();
				return false;
			}
			return true;
		}
		private static void DisplayHelp()
		{
			Console.WriteLine("Usage gppg [options] src_file [dst_file]");
			Console.WriteLine();
			Console.WriteLine("-help:       Display this help message");
			Console.WriteLine("-version:    Display version information");
			Console.WriteLine("-report:     Display LALR(1) parsing states");
			Console.WriteLine("-no-lines:   Suppress the generation of #line directives");
			Console.WriteLine();
		}
		private static void DisplayVersion()
		{
			Console.WriteLine("Gardens Point Parser Generator (gppg) beta 0.81 28/10/2005");
			Console.WriteLine("Written by Wayne Kelly");
			Console.WriteLine("w.kelly@qut.edu.au");
			Console.WriteLine("Queensland University of Technology");
			Console.WriteLine();
		}
	}
}
