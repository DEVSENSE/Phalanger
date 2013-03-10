using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace PHP.Testing
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class PhpNetTester
	{
		private static TestsCollection testsCollection;
        private static readonly List<string> testDirsAndFiles = new List<string>();
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
        private static int maxThreads = 1;
        private static TestsCollection.ConcurrencyLevel concurrencyLevel = TestsCollection.ConcurrencyLevel.SkipIf;

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
            Console.WriteLine("  /j[:max-threads] - execute in parallel (may affect benchmarks)");
            Console.WriteLine("  /p:<none|folder|compile|skipif|full> - concurrency level:");
            Console.WriteLine("      none - Everything sequential. Disables /j option.");
            Console.WriteLine("      folder - Tests in separate folders executed concurrently.");
            Console.WriteLine("      compile - Only compiled concurrently in same folder.");
            Console.WriteLine("      skipif - SkipIf tests executed concurrently in same folder.");
            Console.WriteLine("      full - All possible operations done concurrently.");
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
							{
							    throw new InvalidArgumentException("/benchmark option specified twice");
							}

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

                            case "j":
                                maxThreads = Math.Max(Int32.Parse(value), 1);
                            break;

                            case "p":
                                switch (value)
                                {
                                    case "none":
                                        maxThreads = 1;
                                        concurrencyLevel = TestsCollection.ConcurrencyLevel.None;
                                        break;
                                    case "folder":
                                        concurrencyLevel = TestsCollection.ConcurrencyLevel.Folder;
                                        break;
                                    case "compile":
                                        concurrencyLevel = TestsCollection.ConcurrencyLevel.Compile;
                                        break;
                                    case "skipif":
                                        concurrencyLevel = TestsCollection.ConcurrencyLevel.SkipIf;
                                        break;
                                    case "full":
                                        concurrencyLevel = TestsCollection.ConcurrencyLevel.Full;
                                        break;
                                    default:
                                        throw new InvalidArgumentException(
                                            String.Format(
                                                "Invalid value for conncurrency-level [{0}] in option [{1}].", value, name));
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
							    verbose = true;
							break;
							case "clean":
    							clean = true;
							break;
							case "compileonly":
    							compileOnly = true;
							break;
                            case "j":
                                maxThreads = Environment.ProcessorCount;
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

            if (maxThreads <= 1)
            {
                concurrencyLevel = TestsCollection.ConcurrencyLevel.None;
            }
            else
            if (concurrencyLevel == TestsCollection.ConcurrencyLevel.None)
            {
                maxThreads = 1;
            }

			// default values
			if (testDirsAndFiles.Count == 0)
			{
			    testDirsAndFiles.Add(Directory.GetCurrentDirectory());
			}

			if (compiler == null)
			{
			    compiler = Path.Combine(Directory.GetCurrentDirectory(), "phpc.exe");
			}

			if (php == null)
			{
			    php = Path.Combine(Directory.GetCurrentDirectory(), "php.exe");
			}

			if (outputDir == null)
			{
			    outputDir = Directory.GetCurrentDirectory();
			}

			return true;
		}

		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static int Main(string[] args)
		{
		    var sw = Stopwatch.StartNew();
			bool testingStarted = false;
			Console.WriteLine("Starting tests...");
			try
			{
				ProcessArguments(args);

				testsCollection = new TestsCollection(testDirsAndFiles, verbose, clean, compileOnly,
					                                  benchmarks, numberOfBenchmarkRuns, concurrencyLevel, maxThreads);
				testsCollection.LoadTests();

				testingStarted = true;
				return testsCollection.RunTests(loader, compiler, php);
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
				if (testingStarted)
				{
				    testsCollection.WriteLog(Path.Combine(outputDir, "TestLog.htm"), fullLog);
		            Console.WriteLine();
                    Console.WriteLine("Done. " + testsCollection.GetStatusMessage());
                    Console.WriteLine("Time: " + sw.Elapsed);
				}
			}

			return 0;
		}
	}
}
