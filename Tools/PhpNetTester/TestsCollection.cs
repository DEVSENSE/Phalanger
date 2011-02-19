using System;
using System.Collections;
using System.IO;

namespace PHP.Testing
{
	public class TestsCollection
	{
		private ArrayList tests;
		private ArrayList testDirsAndFiles;

		private bool verbose;
		private bool clean;
		private bool compileOnly;
		private bool benchmarks;
		private int defaultNumberOfRuns;

		public TestsCollection(ArrayList testDirsAndFiles, bool verbose, bool clean, bool compileOnly,
			bool benchmarks, int defaultNumberOfRuns)
		{
			this.tests = new ArrayList();
			this.testDirsAndFiles = testDirsAndFiles;
			this.verbose = verbose;
			this.clean = clean;
			this.compileOnly = compileOnly;
			this.benchmarks = benchmarks;
			this.defaultNumberOfRuns = defaultNumberOfRuns;
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
				LoadTestFile(file);

			// and process all subdirectories
			foreach (string subdir in Directory.GetDirectories(dir))
				LoadTestsFromDirectory(subdir);
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
		/// <param name="verbose"><c>True</c> if detail information should be printed to console.</param>
		/// <returns>Number of tests that failed.</returns>
		public int RunTests(string loader, string compiler, string php)
		{
			int failed_num = 0;

			foreach (Test t in tests)
			{
				Console.Write(String.Format("Running {0}.. ", t.SourcePathRelative));

				t.Run(loader, compiler, php);
				if (!t.Succeeded)
					failed_num++;

				Console.WriteLine(t.Succeeded ? "OK" : "Failed");
			}

			return failed_num;
		}

		/// <summary>
		/// Writes html log into file specified.
		/// </summary>
		/// <param name="file">File where to write log. If the file exist, is overwritten.</param>
		/// <param name="fullLog">True if detail info is written also if test succeeded.</param>
		public void WriteLog(string file, bool fullLog)
		{
			using (StreamWriter sw = new StreamWriter(file))
			{
				sw.WriteLine("<html>");
				sw.WriteLine("<head>");
				sw.WriteLine("<title>PHP.NET Compiler test log</title>");
				sw.WriteLine("<style type=\"text/css\">");
				sw.WriteLine("BODY { FONT-SIZE: 12px; FONT-FAMILY: Verdana, Arial, Helvetica, sans-serif }");

				sw.WriteLine("TABLE { border-collapse:collapse }");
				sw.WriteLine("TD { FONT-SIZE: 12px; FONT-FAMILY: Verdana, Arial, Helvetica, sans-serif; border: 1px solid DimGray; }");
				sw.WriteLine("TR.status TD.succeeded { background-color: #CFC }");
				sw.WriteLine("TR.status TD.failed { background-color: #FCC }");
				sw.WriteLine("TD.succeeded { background-color: #EFE }");
				sw.WriteLine("TD.failed { background-color: #FEE }");

				sw.WriteLine("A.succeeded:link, A.succeeded:visited { color:green; text-decoration: none; }");
				sw.WriteLine("A.failed:link, A.failed:visited { color:red; text-decoration: none; }");
                sw.WriteLine("TR.detail{visibility:hidden;position:absolute;");
				sw.WriteLine("</style>");
                sw.WriteLine("</head>");
				sw.WriteLine("<body>");

				sw.WriteLine("<h1>PHP.NET Compiler test log</h1>");

				WriteStatus(sw);

				sw.WriteLine("<table width=\"100%\" border=\"1\" cellspacing=\"0\" cellpadding=\"3\">");
				WriteTableHead(sw);
                int testIndex = 0;
				foreach (Test t in tests)
					t.WriteTableRow(sw, fullLog, ++testIndex);
				sw.WriteLine("</table>");

				sw.WriteLine("</body>");
				sw.WriteLine("</html>");
			}

		}

		public void WriteStatus(StreamWriter sw)
		{
			int succeeded = 0;
			int failed = 0;

			foreach (Test t in tests)
			{
				if (t.Succeeded) succeeded++;
				else failed++;
			}

			sw.WriteLine("({2}%) {0} succeeded, {1} failed <br>", succeeded, failed, Math.Round((double)succeeded * 100 / (failed + succeeded)));
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