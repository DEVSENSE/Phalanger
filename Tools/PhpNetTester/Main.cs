using System;
using System.IO;
using System.Diagnostics;
using System.Collections;

namespace PHP.Testing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class PhpNetTester
	{
		private static TestsCollection testsCollection;
		private static ArrayList testDirsAndFiles;
		private static string outputDir;
		private static string compiler;
		private static string loader;
		private static string php;
		private static bool fullLog = false;
		private static bool verbose = false;
		private static bool clean = false;
		private static bool compileOnly = false;
		private static bool benchmarks = false;
		private static int numberOfBenchmarkRuns = 1;

		public static int BenchmarkWarmup { get { return benchmarkWarmup; } }
		private static int benchmarkWarmup = 1;

		#region Command Line

		private static void ShowHelp()
		{
			Console.WriteLine("Usage:");
			Console.WriteLine("  /compiler:<absolute or relative path to Phalanger (phpc.exe)>");
			Console.WriteLine("                - set to phpc.exe in current directory if not specified");
			Console.WriteLine("  /php:<absolute or relative path to PHP executable file>");
			Console.WriteLine("                - set to php.exe in current directory if not specified");
			Console.WriteLine("  /loader:<absolute or relative path to a .NET executable loader>");
			Console.WriteLine("                - useful for testing on Mono (loader = mono.exe)");
			Console.WriteLine("                - .NET executables are run directly if the option is not specified");
			Console.WriteLine("  /log:full|short");
			Console.WriteLine("                - default is short");
			Console.WriteLine("  /out:<absolute or relative path to directory where should be log created>");
			Console.WriteLine("                - default is current directory");
			Console.WriteLine("  /verbose      - writes much more information to console while testing");
			Console.WriteLine("  /clean        - deletes all files created during the test");
			Console.WriteLine("  /compileonly  - test only compilation, do not run compiled scripts");
			Console.WriteLine("  /benchmark:<runs>[/<warmup>] - run benchmarks");
			Console.WriteLine("  arguments <absolute or relative directory paths where are directories to test>");
			Console.WriteLine("         or <absolute or relative paths to test files>");
			Console.WriteLine("                - current directory if no argument is specified");
		}

		/// <summary>
		/// Processes command line arguments.
		/// </summary>
		/// <param name="args">The command line arguments.</param>
		/// <returns>Whether to run compilation.</returns>
		private static bool ProcessArguments(string[] args)
		{
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
						string name = args[i].Substring(1, colon - 1).Trim();
						string value = args[i].Substring(colon + 1).Trim();

						switch (name)
						{
							case "compiler":
							if (compiler != null)
								throw new InvalidArgumentException(String.Format("Option {0} specified twice.", name));
							compiler = Path.GetFullPath(value);
							if (!File.Exists(compiler))
								throw new InvalidArgumentException(String.Format("Compiler {0} not found.", compiler));
							break;

							case "loader":
							if (loader != null)
								throw new InvalidArgumentException(String.Format("Option {0} specified twice.", name));
							loader = Path.GetFullPath(value);
							if (!File.Exists(loader))
								throw new InvalidArgumentException(String.Format("Compiler {0} not found.", loader));
							break;

							case "out":
							if (outputDir != null)
								throw new InvalidArgumentException(String.Format("Option {0} specified twice.", name));
							outputDir = Path.GetFullPath(value);
							if (!Directory.Exists(outputDir))
								throw new InvalidArgumentException(String.Format("Output directory {0} not found.", outputDir));
							break;

							case "php":
							if (php != null)
								throw new InvalidArgumentException(String.Format("Option {0} specified twice.", name));
							php = Path.GetFullPath(value);
							if (!File.Exists(php))
								throw new InvalidArgumentException(String.Format("PHP (original) executable file {0} not found.", php));
							break;

							case "log":
							if (value == "full")
								fullLog = true;
							else if (value == "short")
								fullLog = false;
							else
								throw new InvalidArgumentException(String.Format("Illegal /log:{0} option.", value));
							break;

							case "benchmark":
							if (benchmarks)
								throw new InvalidArgumentException("/benchmark option specified twice");
							benchmarks = true;
							try
							{
								int slash = value.IndexOf('/');
								if (slash < 0)
								{
									numberOfBenchmarkRuns = Int32.Parse(value);
								}
								else
								{
									numberOfBenchmarkRuns = Int32.Parse(value.Substring(0, slash));
									benchmarkWarmup = Int32.Parse(value.Substring(slash + 1));
								}
							}
							catch (Exception)
							{
								throw new TestException("Error /benchmark value.");
							}
							break;

							default:
							throw new InvalidArgumentException(String.Format("Invalid option {0}.", name));
						}
					}
					else
					{	// option without ':'
						string name = args[i].Substring(1).Trim();

						switch (name)
						{
							case "verbose":
							if (verbose)
								throw new InvalidArgumentException("/verbose option specified twice");
							verbose = true;
							break;
							case "clean":
							if (clean)
								throw new InvalidArgumentException("/clean option specified twice");
							clean = true;
							break;
							case "compileonly":
							if (compileOnly)
								throw new InvalidArgumentException("/compileonly option specified twice");
							compileOnly = true;
							break;
							default:
							throw new InvalidArgumentException(String.Format("Invalid option {0}.", args[i]));
						}

					}
				}
				else
				{ // arguments
					testDirsAndFiles.Add(args[i]);
				}
			}

			// default values
			if (testDirsAndFiles.Count == 0)
				testDirsAndFiles.Add(Directory.GetCurrentDirectory());
			if (compiler == null)
				compiler = Path.Combine(Directory.GetCurrentDirectory(), "phpc.exe");
			if (php == null)
				php = Path.Combine(Directory.GetCurrentDirectory(), "php.exe");
			if (outputDir == null)
				outputDir = Directory.GetCurrentDirectory();

			return true;
		}

		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main(string[] args)
		{
			bool tests_started = false;
			int failed_num = -1;
			Console.WriteLine("Starting tests...");

			try
			{
				testDirsAndFiles = new ArrayList();
				ProcessArguments(args);

				testsCollection = new TestsCollection(testDirsAndFiles, verbose, clean, compileOnly,
					benchmarks, numberOfBenchmarkRuns);
				testsCollection.LoadTests();

				tests_started = true;
				failed_num = testsCollection.RunTests(loader, compiler, php);
			}
			catch (InvalidArgumentException e)
			{
				Console.WriteLine(e.Message);
				ShowHelp();
				Console.ReadLine();
			}
			catch (TestException e)
			{
				Console.WriteLine("Testing failed: " + e.Message);
				Console.ReadLine();
			}
			catch (Exception e)
			{
				Console.Write("Unexpected error: ");
				Console.WriteLine(e.Message);
				Console.ReadLine();
			}
			finally
			{
				if (tests_started)
					testsCollection.WriteLog(Path.Combine(outputDir, "TestLog.htm"), fullLog);
			}

			Console.WriteLine("Done.");

			return failed_num;
		}
	}
}
