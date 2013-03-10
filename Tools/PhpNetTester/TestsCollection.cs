using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PHP.Testing
{
	public class TestsCollection
	{
        public enum ConcurrencyLevel
        {
            None,           //< Everything sequential.
            Folder,         //< Tests in separate folders executed concurrently.
            Compile,        //< Only compiled concurrently in same folder.
            SkipIf,         //< SkipIf tests executed concurrently in same folder.
            Full            //< All possible operations done concurrently.
        }

		private List<Test> tests;
		private List<string> testDirsAndFiles;

		private bool verbose;
		private bool clean;
		private bool compileOnly;
		private bool benchmarks;
		private int defaultNumberOfRuns;
        private int maxThreads = 1;
        private ConcurrencyLevel concurrencyLevel = ConcurrencyLevel.None;

		public TestsCollection(List<string> testDirsAndFiles, bool verbose, bool clean, bool compileOnly,
                               bool benchmarks, int defaultNumberOfRuns, ConcurrencyLevel concurrencyLevel, int maxThreads)
		{
			this.tests = new List<Test>();
			this.testDirsAndFiles = testDirsAndFiles;
			this.verbose = verbose;
			this.clean = clean;
			this.compileOnly = compileOnly;
			this.benchmarks = benchmarks;
			this.defaultNumberOfRuns = defaultNumberOfRuns;
		    this.concurrencyLevel = concurrencyLevel;
		    this.maxThreads = maxThreads;
		}

	    private void LoadTestsFromDirectory(string dir)
		{
			if (Directory.GetFiles(dir, "__skip").Length > 0)
			{
				Console.WriteLine("Skipping directory {0}", dir);
				return;
			}

			// tests are files with extension php or phpt
			foreach (string file in Directory.GetFiles(dir, "*.php"))
			{
			    LoadTestFile(file);
			}

			// and process all subdirectories
			foreach (string subdir in Directory.GetDirectories(dir))
			{
			    LoadTestsFromDirectory(subdir);
			}
		}

		/// <summary>
		/// Searches for tests in all sub folders containing "test" string
		/// </summary>
		public void LoadTests()
		{
			foreach (string path in testDirsAndFiles)
			{
				if (Directory.Exists(path))
				{	// is directory
					LoadTestsFromDirectory(path);
				}
				else if (File.Exists(path))
				{	//is file
					LoadTestFile(path);
				}
				else
				{ // is not directory or file
					throw new InvalidArgumentException(String.Format("File or directory {0} does not exist.", path));
				}
			}
		}

		private void LoadTestFile(string file)
		{
			tests.Add(new Test(file, verbose, clean, compileOnly, benchmarks, defaultNumberOfRuns));
		}

		/// <summary>
		/// Runs all tests
		/// </summary>
		/// <param name="loader">Full path to exe loader or <B>null</B>.</param>
		/// <param name="compiler">Full path to PHP.NET Compiler</param>
		/// <param name="php">Full path to PHP (original) executable file.</param>
		/// <returns>Number of tests that failed.</returns>
		public int RunTests(string loader, string compiler, string php)
		{
		    switch (concurrencyLevel)
		    {
                case ConcurrencyLevel.None:
                case ConcurrencyLevel.Full:
		            return RunTestsDefault(loader, compiler, php);

                case ConcurrencyLevel.Folder:
                case ConcurrencyLevel.Compile:
                case ConcurrencyLevel.SkipIf:
		            break;
		    }

		    int failedCount = 0;
			foreach (Test t in tests)
			{
				Console.Write("Running {0}.. ", t.SourcePathRelative);
                
                t.Run(loader, compiler, php);
				if (!t.Skipped && !t.Succeeded)
				{
				    ++failedCount;
				}

                Console.WriteLine(t.Succeeded ? "Pass" : (t.Skipped ? "Skipped" : "Failed"));
			}

			return failedCount;
		}

        /// <summary>
        /// Runs all tests
        /// </summary>
        /// <param name="loader">Full path to exe loader or <B>null</B>.</param>
        /// <param name="compiler">Full path to PHP.NET Compiler</param>
        /// <param name="php">Full path to PHP (original) executable file.</param>
        /// <returns>Number of tests that failed.</returns>
        public int RunTestsDefault(string loader, string compiler, string php)
        {
            int failedCount = 0;
            Parallel.ForEach(tests, new ParallelOptions { MaxDegreeOfParallelism = maxThreads },
                             test =>
                                 {
                                     Console.Write("Running {0}.. ", test.SourcePathRelative);

                                     test.Run(loader, compiler, php);
                                     if (!test.Skipped && !test.Succeeded)
                                     {
                                         Interlocked.Increment(ref failedCount);
                                     }

                                     Console.WriteLine(test.Succeeded ? "Pass" : (test.Skipped ? "Skipped" : "Failed"));
                                 });

            return failedCount;
        }

		/// <summary>
		/// Writes html log into file specified.
		/// </summary>
		/// <param name="file">File where to write log. If the file exist, is overwritten.</param>
		/// <param name="fullLog">True if detail info is written also if test succeeded.</param>
		public void WriteLog(string file, bool fullLog)
		{
			using (var sw = new StreamWriter(file))
			{
				sw.WriteLine("<html>");
				sw.WriteLine("<head>");
				sw.WriteLine("<title>PHP.NET Compiler test log</title>");
				sw.WriteLine("<style type=\"text/css\">");
				sw.WriteLine("BODY { FONT-SIZE: 12px; FONT-FAMILY: Verdana, Arial, Helvetica, sans-serif }");

				sw.WriteLine("TABLE { border-collapse:collapse }");
				sw.WriteLine("TD { FONT-SIZE: 12px; FONT-FAMILY: Verdana, Arial, Helvetica, sans-serif; border: 1px solid DimGray; }");
				sw.WriteLine("TR.status TD.succeeded { background-color: #CFC }");
                sw.WriteLine("TR.status TD.skipped { background-color: #CCF }");
				sw.WriteLine("TR.status TD.failed { background-color: #FCC }");
				sw.WriteLine("TD.succeeded { background-color: #EFE }");
                sw.WriteLine("TD.skipped { background-color: #EEF }");
                sw.WriteLine("TD.failed { background-color: #FEE }");

                sw.WriteLine("A.succeeded:link, A.succeeded:visited { color:green; text-decoration: none; }");
                sw.WriteLine("A.skipped:link, A.skipped:visited { color:blue; text-decoration: none; }");
                sw.WriteLine("A.failed:link, A.failed:visited { color:red; text-decoration: none; }");
                sw.WriteLine("TR.detail{visibility:hidden;position:absolute;");
				sw.WriteLine("</style>");
                sw.WriteLine("</head>");
				sw.WriteLine("<body>");

				sw.WriteLine("<h1>PHP.NET Compiler test log</h1>");

                sw.WriteLine(GetStatusMessage() + " <br />");

				sw.WriteLine("<table width=\"100%\" border=\"1\" cellspacing=\"0\" cellpadding=\"3\">");
				WriteTableHead(sw);
			    for (int testIndex = 0; testIndex < tests.Count; ++testIndex)
			    {
			        tests[testIndex].WriteTableRow(sw, fullLog, testIndex);
			    }

			    sw.WriteLine("</table>");
				sw.WriteLine("</body>");
				sw.WriteLine("</html>");
			}

		}

		public string GetStatusMessage()
		{
            int succeeded = 0;
            int skipped = 0;
            int failed = 0;
			foreach (Test t in tests)
			{
                if (t.Succeeded)
                {
                    ++succeeded;
                }
                else
                if (t.Skipped)
                {
                    ++skipped;
                }
                else
                {
                    ++failed;
                }
			}

		    int total = succeeded + skipped + failed;
			return string.Format("({0}%) {1} succeeded, {2} skipped, {3} failed ({4} total test{5})",
                                 Math.Round(succeeded * 100.0 / total), succeeded, skipped, failed, total, total > 1 ? "s" : "");
		}

		private void WriteTableHead(TextWriter tw)
		{
			tw.WriteLine("<tr>");

			tw.Write(Utils.MakeTColumn("Test Result", true));

			if (benchmarks)
			{
				tw.Write(Utils.MakeTColumn("Compilation time", true));
				tw.Write(Utils.MakeTColumn("Running time", true));
				tw.Write(Utils.MakeTColumn("PHP time", true));
				tw.Write(Utils.MakeTColumn("No. of runs", true));
			}
			else
			{
				tw.Write(Utils.MakeTColumn("Compiler error output", true));
				tw.Write(Utils.MakeTColumn("Expected test result", true));
				tw.Write(Utils.MakeTColumn(/*"Expected test output"*/"", true));
				tw.Write(Utils.MakeTColumn(/*"Real script output"*/"", true));
			}
			tw.WriteLine("<tr>");
		}

	}
}