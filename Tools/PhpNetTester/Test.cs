using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Web;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;

namespace PHP.Testing
{
	public enum TestResult
	{
		Succees, CtError, Skipped,
		UnexpectedOutput, PhpcMisbehaviourScript, CannotCompileExpect, ScriptHangUp, ExpectHangUp, PhpcHangUp,
		PhpHangUp, PhpMisbehaviour, PhpNotFound, ExpectedWarningNotDisplayed
	}

	public enum Directive
	{
		None, Expect, ExpectCtError, ExpectCtWarning, ExpectPhp, ExpectExact, File, Config, Comment,
        SkipIf, NumberOfRuns, AdditionalScripts, Pure, Clr
	}

	public class Test
	{
	    private readonly string sourcePath;
	    private readonly string sourcePathRelative;
        private List<string> script;
        private List<string> comment;
        private List<string> skipIf;

		private TestResult expectedTestResult = TestResult.Succees;
        private List<List<string>> expect;
        private List<string> expectExact;
        private List<string> configuration;
        private List<string> expectCtError;
        private List<List<string>> expectCtWarning;
        private List<string> additionalScripts;

	    private bool isPure;
        private bool isClr;

        public TestResult RealTestResult { get { return realTestResult; } }
		private TestResult realTestResult = TestResult.Skipped;
		private string compilerErrorOutput;
		private string compilerStdOutput;
		private string scriptOutput;
		private bool expectPhp = false;
	    private bool expectF = false;   // Loose validation.
        private bool expectRegex = false;   // Loose validation.
        private bool skipped = false;

		private string expectWhereFailed = null;

		private bool verbose;
		private bool clean;
		private bool compileOnly;

		private bool benchmarks;
		private double compilationTime = 0;
		private double runningTime = 0;
		private double phpTime = 0;
		private int numberOfRuns = 0;

        private const string PHP_FILENAME_SUFIX = "__tmp.phpscript";
	    private const int TIMEOUT_MS = 30000;

		/// <summary>
		/// Creates new test according to file specified.
		/// </summary>
		/// <param name="sourcePath">Full path with source file to test.</param>
		/// <param name="verbose"><c>True</c> if detail information should be printed to console.</param>
		/// <param name="clean"><B>True</B>if created files should be deleted.</param>
		/// <param name="compileOnly"><B>True</B> if only compilation should be performed.</param>
		public Test(string sourcePath, bool verbose, bool clean, bool compileOnly,
			        bool benchmarks, int defaultNumberOfRuns)
		{
            this.sourcePath = Path.GetFullPath(sourcePath);
            Uri currentDir = new Uri(Directory.GetCurrentDirectory() + "\\");
            this.sourcePathRelative = currentDir.MakeRelativeUri(new Uri(this.sourcePath)).ToString();

            this.verbose = verbose;
			this.clean = clean;
			this.compileOnly = compileOnly;
            this.expect = new List<List<string>>();
            this.expectCtWarning = new List<List<string>>();
			this.benchmarks = benchmarks;
			this.numberOfRuns = defaultNumberOfRuns;

			// overwrites numberOfRuns if there is specified
			ReadFile();

			if (numberOfRuns < 1 || !benchmarks)
				numberOfRuns = 1;
		}

		private void ReadFile()
		{
		    var directive = Directive.None;
			var block = new List<string>();

			using (var sr = new StreamReader(sourcePath))
			{
			    string line;
			    while ((line = sr.ReadLine()) != null)
				{
					Directive d;
					if ((d = StringToDirective(ref line)) != Directive.None)
					{
                        // next directive
						if (block.Count > 0 || Utils.CanBeEmptyDirective(directive))
						{
							SaveBlock(block, directive);
							block = new List<string>(); // reference is stored, we cannot just call Clear()
						}

						directive = d;
						if (line != null && line.Trim().Length > 0) // after directive is some text
						{
						    block.Add(line);
						}
					}
					else
					{
                        // still current directive
						block.Add(line);
					}
				}

				// finish
				if (block.Count > 0)
				{
				    SaveBlock(block, directive);
				}
			}

			// check if we have all required test parts
			if (script == null)
			{
			    throw new TestException(String.Format("Test {0} has no [file] section", this.sourcePath));
			}
		}

		/// <summary>
		/// Returns <see cref="Directive"/> that is at the beginning of <paramref name="str"/> and in <c>str</c>
		/// leaves remaining characters.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private Directive StringToDirective(ref string str)
		{
            // Skip the DONE directive of phpt.
            if (MatchDirective(ref str, "===done==="))
		    {
                return Directive.None;
		    }

            if (MatchDirective(ref str, "--expect--", "[expect]"))
            {
				return Directive.Expect;
			}

            // In phpt EXPECTF and EXPECTREGEX are used to allow looser validation.
            if (MatchDirective(ref str, "--expectf--", "[expectf]"))
            {
                expectF = true;
                return Directive.Expect;
            }

            // In phpt EXPECTF and EXPECTREGEX are used to allow looser validation.
            if (MatchDirective(ref str, "--expectregex--", "[expectregex]"))
            {
                expectRegex = true;
                return Directive.Expect;
            }

            if (MatchDirective(ref str, "--expect ct-error--", "[expect ct-error]", "--expect errors--", "[expect errors]"))
			{
				return Directive.ExpectCtError;
			}

            if (MatchDirective(ref str, "--expect ct-warning--", "[expect ct-warning]"))
			{
				return Directive.ExpectCtWarning;
			}

            if (MatchDirective(ref str, "--file--", "[file]"))
			{
				return Directive.File;
			}

            if (MatchDirective(ref str, "--expect php--", "[expect php]"))
			{
				return Directive.ExpectPhp;
			}

            if (MatchDirective(ref str, "--expect exact--", "[expect exact]"))
			{
				return Directive.ExpectExact;
			}

            if (MatchDirective(ref str, "--config--", "[config]"))
			{
				return Directive.Config;
			}

            if (MatchDirective(ref str, "--comment--", "[comment]"))
			{
				return Directive.Comment;
			}

            if (MatchDirective(ref str, "--test--", "[test]"))
			{
				return Directive.Comment;
			}

            if (MatchDirective(ref str, "--skipif--", "[skipif]"))
            {
                return Directive.SkipIf;
            }

            if (MatchDirective(ref str, "--runs--", "[runs]"))
			{
				return Directive.NumberOfRuns;
			}

            if (MatchDirective(ref str, "--additional scripts--", "[additional scripts]"))
			{
				return Directive.AdditionalScripts;
			}

            if (MatchDirective(ref str, "[pure]"))
            {
                return Directive.Pure;
            }

            if (MatchDirective(ref str, "[clr]"))
            {
                return Directive.Clr;
            }

			return Directive.None;
		}

	    private static bool MatchDirective(ref string str, params string[] directiveNames)
	    {
	        foreach (var directiveName in directiveNames)
	        {
                if (str.StartsWith(directiveName, StringComparison.InvariantCultureIgnoreCase))
                {
                    str = str.Substring(directiveName.Length);
                    return true;
                }

            }

            return false;
        }

	    private void SaveBlock(List<string> block, Directive directive)
		{
            if (block == null || block.Count == 0 || directive == Directive.None)
            {
                return;
            }

            switch (directive)
            {
                case Directive.Expect:
                    this.expect.Add(block);
                    break;

                case Directive.ExpectCtError:
                    if (this.expectCtError != null)
                        throw new TestException(String.Format("{0}: [expect ct-error] redefinition", this.sourcePath));
                    this.expectedTestResult = TestResult.CtError;
                    this.expectCtError = block;
                    break;

                case Directive.ExpectCtWarning:
                    this.expectCtWarning.Add(block);
                    break;

                case Directive.ExpectExact:
                    if (this.expectExact != null)
                        throw new TestException(String.Format("{0}: [expect exact] redefinition", this.sourcePath));
                    this.expectExact = block;
                    break;

                case Directive.ExpectPhp:
                    if (expectPhp)
                        throw new TestException(String.Format("{0}: [expect php] specified twice", this.sourcePath));
                    expectPhp = true;
                    break;

                case Directive.Config:
                    if (configuration != null)
                        throw new TestException(String.Format("{0}: [configuration] specified twice", this.sourcePath));
                    configuration = block;
                    break;

                case Directive.File:
                    if (this.script != null)
                        throw new TestException(String.Format("{0}: [script] redefinition", this.sourcePath));
                    this.script = block;
                    break;

                case Directive.Comment:
                    if (this.comment != null)
                        throw new TestException(String.Format("{0}: [test] redefinition", this.sourcePath));
                    this.comment = block;
                    break;

                case Directive.SkipIf:
                    if (this.skipIf != null)
                        throw new TestException(String.Format("{0}: [skipif] redefinition", this.sourcePath));
                    this.skipIf = block;
                    break;

                case Directive.NumberOfRuns:
                    if (this.numberOfRuns > 0)
                    {
                        throw new TestException(String.Format("{0}: [runs] redefinition", sourcePath));
                    }

                    if (!Int32.TryParse(Utils.ListToString(block), out numberOfRuns))
                    {
                        throw new TestException(String.Format("{0}: [runs] invalid value", sourcePath));  
                    }

                    break;

                case Directive.AdditionalScripts:
                    if (this.additionalScripts != null)
                        throw new TestException(String.Format("{0}: [additional scripts] redefinition", this.sourcePath));
                    this.additionalScripts = block;
                    break;

                case Directive.Pure:
                    this.isPure = true;
                    break;
                case Directive.Clr:
                    this.isClr = true;
                    break;
            }
		}

		/// <summary>
		/// Runs the test. (Script compilation and execution.)
		/// </summary>
		/// <param name="loaderPath">Full path to the exe loader or <B>null</B>.</param>
		/// <param name="compilerPath">A full path to the Phalanger.</param>
		/// <param name="phpPath">A full path to PHP (original) executable file.</param>
		public void Run(string loaderPath, string compilerPath, string phpPath)
		{
			string compiled_script_path = Path.Combine(Path.GetDirectoryName(sourcePath), String.Concat(Path.GetFileNameWithoutExtension(sourcePath), "_file", ".exe"));
			string compiled_expect_path = Path.Combine(Path.GetDirectoryName(sourcePath), String.Concat(Path.GetFileNameWithoutExtension(sourcePath), "_expect", ".exe"));
			string expect_output;

            if (comment != null)
            {
                string name = Utils.ListToString(comment).Trim();
                if (name.Length > 0)
                {
                    Console.Write("[" + name + "] ");
                }
            }

            // First, if we have a SkipIf block, execute it.
            if (!compileOnly && skipIf != null && skipIf.Count > 0)
            {
                bool realVerbose = verbose;
                verbose = false;
                int realNumberOfRuns = numberOfRuns;
                numberOfRuns = 1;

                // compile and run script
                if (!Compile(loaderPath, compilerPath, skipIf, compiled_script_path, false))
                {
                    // Compile sets realTestResult for compiling script
                    return;
                }

                if (!RunCompiledScript(loaderPath, compiled_script_path, out scriptOutput, true))
                {
                    realTestResult = TestResult.ScriptHangUp;
                    if (clean) File.Delete(compiled_script_path);
                    return;
                }
                if (clean) File.Delete(compiled_script_path);

                if (scriptOutput.IndexOf("skip", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    // Skipped test.
                    skipped = true;
                    return;
                }

                verbose = realVerbose;
                numberOfRuns = realNumberOfRuns;
            }

			// assume success
			realTestResult = TestResult.Succees;

			// compile and run script
			if (!Compile(loaderPath, compilerPath, script, compiled_script_path, false))
			{
				// Compile sets realTestResult for compiling script
				return;
			}

			// stop if we should only compile
			if (compileOnly) return;

			if (!RunCompiledScript(loaderPath, compiled_script_path, out scriptOutput, true))
			{
				realTestResult = TestResult.ScriptHangUp;
				if (clean) File.Delete(compiled_script_path);
				return;
			}
			if (clean) File.Delete(compiled_script_path);

			// compile and run expect exact section
			if (expectExact != null)
			{
				expect_output = Utils.OutputWithoutCompiling(expectExact);
				if (expect_output == null)
				{
					if (!Compile(loaderPath, compilerPath, expectExact, compiled_expect_path, true))
					{
						realTestResult = TestResult.CannotCompileExpect;
						return;
					}
					if (!RunCompiledScript(loaderPath, compiled_expect_path, out expect_output))
					{
						realTestResult = TestResult.ExpectHangUp;
						if (clean) File.Delete(compiled_expect_path);
						return;
					}
					if (clean) File.Delete(compiled_expect_path);
				}
				if (!CompareOutputsExact(expect_output, scriptOutput, true))
				{
					if (verbose) Console.WriteLine(String.Format("Unexpected output, expected exact: {0}", expect_output));
					realTestResult = TestResult.UnexpectedOutput;
					expectWhereFailed = expect_output;
					return;
				}
			}


			string script_output_cutted = scriptOutput;
			// compile and run all expect sections
			foreach (List<string> e in expect)
			{
				expect_output = Utils.OutputWithoutCompiling(e);
				if (expect_output == null)
				{
					if (!Compile(loaderPath, compilerPath, e, compiled_expect_path, true))
					{
						realTestResult = TestResult.CannotCompileExpect;
						return;
					}
					if (!RunCompiledScript(loaderPath, compiled_expect_path, out expect_output))
					{
						realTestResult = TestResult.ExpectHangUp;
						if (clean) File.Delete(compiled_expect_path);
						return;
					}
					if (clean) File.Delete(compiled_expect_path);
				}

				if (!CompareOutputsSubstring(expect_output, ref script_output_cutted, true))
				{
					if (verbose) Console.WriteLine(String.Format("Unexpected output, expected: {0}", expect_output));
					realTestResult = TestResult.UnexpectedOutput;
					expectWhereFailed = expect_output;
					return;
				}
			}

			// run php for expect php
			if (expectPhp || benchmarks)
			{
				string php_output;
				if (!RunPhp(phpPath, script, out php_output))
				{
					realTestResult = TestResult.PhpHangUp;
					return;
				}

				if (!benchmarks)
				{
					expectWhereFailed = php_output;

					if (!CompareOutputsExact(php_output, scriptOutput, true))
					{
						if (verbose) Console.WriteLine(String.Format("Unexpected output, expected: {0}", php_output));
						realTestResult = TestResult.UnexpectedOutput;
						return;
					}
				}
			}
		}

		/// <summary>
		/// Compiles script and creates exe file with the same name next to the source script.
		/// </summary>
		/// <param name="compilerPath">Full path to phpc.exe</param>
		/// <returns><c>True</c> if the script has been compiled succesfuly, <c>false</c> otherwise.</returns>
		private bool Compile(string loaderPath, string compilerPath, IEnumerable<string> scriptLines, string output, bool isExpect)
		{
		    string scriptFilename = Path.GetFileNameWithoutExtension(sourcePath) + PHP_FILENAME_SUFIX;
			string scriptPath = Path.Combine(Path.GetDirectoryName(sourcePath), scriptFilename);
            Utils.DumpToFile(scriptLines, scriptPath);

			string rootDir = Path.GetDirectoryName(sourcePath);

            var sb = new StringBuilder(256);
            sb.Append(isPure ? "/pure+ " : "");
            sb.Append(isClr ? "/lang:clr " : "");
            sb.Append(isPure || isClr ? "/r:mscorlib /r:\"System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\" " : "");
            sb.Append("/dw:CompilerStrict /static+ /target:exe /out:\"").Append(output).Append("\" ");
            sb.Append("/root:. /entrypoint:\"").Append(scriptFilename).Append("\" \"").Append(scriptFilename).Append("\"");

			// put additional scripts to command line
			if (additionalScripts != null)
			{ 
                // relative to /root: - we do not need to put whole path
				foreach (var s in additionalScripts)
				{
					string arg = s.Trim();
					if (arg.Length > 0)
					{
					    sb.Append(" \"").Append(s).Append("\"");
					}
				}
			}

		    string arguments = sb.ToString();
            Process compiler = new Process();
            if (loaderPath != null)
				compiler.StartInfo = new ProcessStartInfo(loaderPath, String.Format("\"{0}\" {1}", compilerPath, arguments));
			else
				compiler.StartInfo = new ProcessStartInfo(compilerPath, arguments);

			compiler.StartInfo.UseShellExecute = false;
            compiler.StartInfo.WorkingDirectory = rootDir;
			compiler.StartInfo.RedirectStandardError = true;
			compiler.StartInfo.RedirectStandardOutput = true;
			compiler.StartInfo.RedirectStandardInput = true;
			if (verbose) Console.WriteLine("Running phpc compiler with options: {0}", compiler.StartInfo.Arguments);

			compiler.Start();

			// compiler waits for enter key after work
			compiler.StandardInput.WriteLine();

			compilerErrorOutput = compiler.StandardError.ReadToEnd();
			if (!compiler.WaitForExit(TIMEOUT_MS))
			{
				compiler.Kill();
				realTestResult = TestResult.PhpcHangUp;
				if (verbose) Console.WriteLine("Compiler hung up.");
				if (clean) File.Delete(scriptPath);
				return false;
			}
			if (!isExpect) this.compilationTime = compiler.ExitTime.Subtract(compiler.StartTime).TotalMilliseconds;
			if (clean) File.Delete(scriptPath);

			if (verbose && compilerErrorOutput.Length > 0) Console.WriteLine("phpc error output: {0}", compilerErrorOutput);
			compilerStdOutput = compiler.StandardOutput.ReadToEnd();
			if (verbose && compilerStdOutput.Length > 0) Console.WriteLine("phpc std output: {0}", compilerStdOutput);

			if (compiler.ExitCode != 0)
			{
				if (verbose) Console.WriteLine("Compiler exited with code: {0}", compiler.ExitCode);

				// do we expect error?
				if (!isExpect && expectCtError != null)
				{
                    var errors = compilerErrorOutput.Split(new char[]{'\n'}, StringSplitOptions.RemoveEmptyEntries);

                    int found = errors.Count(errorline => expectCtError.Any(errorline.Contains));
				    if (found == errors.Length || expectCtError.Count == 0/*just expecting some ct errors*/)
                    {
                        realTestResult = TestResult.CtError;
                        return false;
                    }
				}

				realTestResult = TestResult.PhpcMisbehaviourScript;
				if (verbose) Console.WriteLine("Test result: {0}", Utils.ResultToString(realTestResult));
				return false;
			}

			if (!isExpect && expectCtWarning.Count > 0)
			{
				string comp_output = compilerErrorOutput;
				foreach (List<string> warn in expectCtWarning)
				{
					if (!(CompareOutputsSubstring("warning", ref comp_output, true) && CompareOutputsSubstring(Utils.ListToString(warn), ref comp_output, true)))
					{
						realTestResult = TestResult.ExpectedWarningNotDisplayed;
						return false;
					}
				}
			}

			// succesfuly compiled
			return true;
		}

		private bool RunCompiledScript(string loaderPath, string scriptPath, out string output)
		{
			return RunCompiledScript(loaderPath, scriptPath, out output, false);
		}

		private bool RunCompiledScript(string loaderPath, string scriptPath, out string output, bool mainScript)
		{
			// save configuration if any
			if (configuration != null)
			{
                using (var sw = new StreamWriter(String.Concat(scriptPath, ".config")))
				{
					sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
					sw.WriteLine("<configuration>");

					foreach (string s in configuration)
					{
					    sw.WriteLine(s);
					}

					sw.WriteLine("</configuration>");
					sw.Close();
				}
            }

			// run script
		    bool res = false;
            using (var script = new Process())
            {
                script.StartInfo = loaderPath != null
                                    ? new ProcessStartInfo(loaderPath, "\"" + scriptPath + "\"")
                                    : new ProcessStartInfo(scriptPath);

                script.StartInfo.UseShellExecute = false;
                script.StartInfo.RedirectStandardOutput = true;
                script.StartInfo.ErrorDialog = false;
                script.StartInfo.CreateNoWindow = true;
                script.StartInfo.RedirectStandardError = true;
                script.StartInfo.WorkingDirectory = Path.GetDirectoryName(scriptPath);
                script.EnableRaisingEvents = false;

                if (verbose)
                {
                    Console.WriteLine(String.Format("Starting {0}..", scriptPath));
                }

                res = RunTestProcess(scriptPath, out output, mainScript, script);
            }

		    if (verbose)
		    {
		        Console.WriteLine(String.Concat("Script output: ", output));
		    }

			return res;
		}

	    private bool RunTestProcess(string scriptPath, out string output, bool mainScript, Process script)
	    {
	        bool res = false;
	        output = null;
            var sb = new StringBuilder(1024);
	        using (var outputWaitHandle = new AutoResetEvent(false))
	        {
	            using (var errorWaitHandle = new AutoResetEvent(false))
	            {
	                script.OutputDataReceived += (sender, e) =>
	                                                    {
	                                                        if (e.Data == null)
	                                                        {
	                                                            outputWaitHandle.Set();
	                                                        }
	                                                        else
	                                                        {
	                                                            sb.AppendLine(e.Data);
	                                                        }
	                                                    };
	                script.ErrorDataReceived += (sender, e) =>
	                                                {
	                                                    if (e.Data == null)
	                                                    {
	                                                        errorWaitHandle.Set();
	                                                    }
	                                                    else
	                                                    {
	                                                        sb.AppendLine(e.Data);
	                                                    }
	                                                };
                    for (int i = 0; i < numberOfRuns; i++)
                    {
                        sb.Clear();

                        script.Start();
                        script.BeginOutputReadLine();
                        script.BeginErrorReadLine();

	                    if (script.WaitForExit(TIMEOUT_MS) &&
	                        outputWaitHandle.WaitOne(TIMEOUT_MS) &&
	                        errorWaitHandle.WaitOne(TIMEOUT_MS))
	                    {
                            script.CancelErrorRead();
                            script.CancelOutputRead();
	                        output = sb.ToString();
	                        if (script.ExitCode != 0)
	                        {
	                            if (verbose)
	                            {
	                                Console.WriteLine("Script {0} did not exit properly.", scriptPath);
	                            }

	                            break;
	                        }

	                        res = true;
	                    }
	                    else
	                    {
	                        // Timed out.
                            script.Kill();
                            script.CancelErrorRead();
                            script.CancelOutputRead();
                            output = sb.ToString();
	                        if (verbose)
	                        {
	                            Console.WriteLine("Script {0} hung up.", scriptPath);
	                        }

	                        break;
	                    }

	                    if (!mainScript)
	                    {
	                        break;
	                    }

	                    if (i >= PhpNetTester.BenchmarkWarmup)
	                    {
                            runningTime += script.ExitTime.Subtract(script.StartTime).TotalMilliseconds;
	                    }
	                }
	            }
	        }

	        return res;
	    }

	    private bool RunPhp(string phpPath, IEnumerable<string> scriptLines, out string output)
		{
			output = null;

            string scriptFilename = Path.GetFileNameWithoutExtension(sourcePath) + PHP_FILENAME_SUFIX;
            string scriptPath = Path.Combine(Path.GetDirectoryName(sourcePath), scriptFilename);
            Utils.DumpToFile(scriptLines, scriptPath);

			if (!File.Exists(phpPath))
			{
				realTestResult = TestResult.PhpNotFound;
				if (verbose) Console.WriteLine(Utils.ResultToString(realTestResult));
				if (clean) File.Delete(scriptPath);
				return false;
			}

            // use our custom php.ini, don't use the global one if any
            string phpIniPath = new Uri(new Uri(phpPath), "php.ini").LocalPath;

            //TODO: Refactor and reuse the improved process execution code.
			Process php = new Process();
            php.StartInfo = new ProcessStartInfo(phpPath, String.Concat("-c \"", phpIniPath, "\" \"", scriptPath, "\""));
			php.StartInfo.UseShellExecute = false;
			php.StartInfo.RedirectStandardOutput = true;
            php.StartInfo.RedirectStandardError = true;
            php.StartInfo.WorkingDirectory = Path.GetDirectoryName(scriptPath);

			for (int i = 0; i < numberOfRuns; i++)
			{
				php.Start();

				output = Utils.RemoveCR(php.StandardOutput.ReadToEnd().Trim());
				if (!php.WaitForExit(TIMEOUT_MS))
				{
					php.Kill();
					realTestResult = TestResult.PhpHangUp;
					if (verbose) Console.WriteLine(Utils.ResultToString(realTestResult));
					if (clean) File.Delete(scriptPath);
					return false;
				}

				if (i >= PhpNetTester.BenchmarkWarmup)
					this.phpTime += php.ExitTime.Subtract(php.StartTime).TotalMilliseconds;
			}

			php.Dispose();
			if (clean) File.Delete(scriptPath);

			if (verbose) Console.WriteLine(String.Concat("Php output: ", output));
			return true;
        }

	    #region Comparing outputs (expected (PHP) and real (Phalanger))

        /// <summary>
        /// called on every match
        /// </summary>
        /// <param name="match"></param>
        /// <returns>only values in colections are used</returns>
        internal string CompareModifier(Match match)
        {
            string str = null;
            foreach (Group gr in match.Groups)
            {
                if (gr != match)
                {
                    str += gr.Value;
                }
            }

            return str;
        }

        internal void ModifyOutput(ref string real_str, ref string exp_str)
        {
            // ignore known Phalanger differences

            // modify real_str, exp_str

            // at expression on line NUM, column NUM. => on line NUM
            Regex reColumn = new Regex(@"(\son\sline\s[0-9]+),\scolumn [0-9]+\.");
            real_str = reColumn.Replace(real_str, CompareModifier);

            // array(NUM)\n*\s*{ => array(NUM){
            Regex reArray = new Regex(@"(array\([0-9]+\))\n*\s*({)", RegexOptions.Multiline);
            real_str = reArray.Replace(real_str, CompareModifier);
            exp_str = reArray.Replace(exp_str, CompareModifier);

            // integer(NUM) => int(NUM)
            Regex reInt = new Regex(@"(int)eger(\([0-9]+\))");
            real_str = reInt.Replace(real_str, CompareModifier);
            exp_str = reInt.Replace(exp_str, CompareModifier);

            // [ID]\s*=>\n?\s*TYPE     remove whitespaces
            Regex reAss = new Regex(@"(\[)([^\]]+)(\])\s*(\=\>)\n?\s+([^\n]+)", RegexOptions.Multiline);
            real_str = reAss.Replace(real_str, CompareModifier);
            exp_str = reAss.Replace(exp_str, CompareModifier);

            // ['ID']=>TYPE   remove ''
            reAss = new Regex(@"(\[)[\'\" + '"' + @"]([^\]]+)[\'\" + '"' + @"](\]\=\>[^\n]+)", RegexOptions.Multiline);
            real_str = reAss.Replace(real_str, CompareModifier);
            exp_str = reAss.Replace(exp_str, CompareModifier);

            // Deprecated: .*          remove
            Regex reDepr = new Regex(@"\n?Deprecated\:[^\n]+\n", RegexOptions.Multiline);
            real_str = reDepr.Replace(real_str, CompareModifier);
            exp_str = reDepr.Replace(exp_str, CompareModifier);

            // Strict Standards: .*     remove
            reDepr = new Regex(@"\n?Strict\sStandards\:[^\n]+\n", RegexOptions.Multiline);
            real_str = reDepr.Replace(real_str, CompareModifier);
            exp_str = reDepr.Replace(exp_str, CompareModifier);
         }

        /// <summary>
		/// Compares two script outputs.
		/// </summary>
		/// <param name="expected"><seealso cref="String"/> or <seealso cref="ArrayList"/> of <seealso cref="String"/>s.</param>
		/// <param name="real"><seealso cref="String"/> or <seealso cref="ArrayList"/> of <seealso cref="String"/>s.</param>
		/// <param name="substring"><c>True</c> if for success is sufficient that <paramref name="expected"/>
		/// is substring of <paramref name="real"/>.</param>
		/// <returns><c>True</c> if outputs are same.</returns>
		private bool CompareOutputsExact(string expected, string real, bool ignoreKnownPhalangerDifferences)
		{
            expected = expected.Trim().Replace("\n\r", "\n").Replace("\r\n", "\n");
            real = real.Trim().Replace("\n\r", "\n").Replace("\r\n", "\n");

            if (expected == real)
            {
                return true;
            }

            if (!ignoreKnownPhalangerDifferences && !expectF && !expectRegex)
            {
                DebugCompareOutput(expected, real);
                return false;
            }

            // Here we need to do a fuzzy match.
            ModifyOutput(ref real, ref expected);

            //FIXME: Add scanf and regex parsing.
            // For now, just do a fuzzy comparison.
            expected = Utils.RemoveWhitespace(expected);
            real = Utils.RemoveWhitespace(real);
            return (expected == real);
		}

	    private bool CompareOutputsSubstring(string expected, ref string real, bool ignoreKnownPhalangerDifferences)
		{
            return CompareOutputsSubstring(expected, ref real, false, ignoreKnownPhalangerDifferences);
		}

		/// <summary>
		/// Searches for <paramref name="expected"/> in <paramref name="real"/>. If not found returns <B>false</B>,
		/// otherwise <B>true</B> and <paramref name="real"/> is changed to substring remaining after the match.
		/// </summary>
		/// <param name="expected"></param>
		/// <param name="real"></param>
		/// <param name="noCase"></param>
		/// <returns></returns>
		private bool CompareOutputsSubstring(string expected, ref string real, bool noCase, bool ignoreKnownPhalangerDifferences)
		{
            Debug.Assert(expected != null);
            Debug.Assert(real != null);

            expected = expected.Trim().Replace("\n\r", "\n").Replace("\r\n", "\n");
            real = real.Trim().Replace("\n\r", "\n").Replace("\r\n", "\n");

            if (ignoreKnownPhalangerDifferences)
            {
                ModifyOutput(ref real, ref expected);
            }

			if (noCase)
			{
			    real = real.ToLower();
			}

            //FIXME: Add scanf and regex parsing.
            // For now, just do a fuzzy comparison.
            if (expectF || expectRegex)
            {
                expected = Utils.RemoveWhitespace(expected);
                real = Utils.RemoveWhitespace(real);
            }

            if (expected.Length == 0)
            {
                return real.Length == 0;
            }

            int index = real.IndexOf(expected);
			if (index < 0)
			{
			    DebugCompareOutput(expected, real);
                
			    // not a substring
                return false;
			}

		    real = real.Substring(index + expected.Length);
			return true;
        }

        [Conditional("DEBUG")]
        private void DebugCompareOutput(string expected, string real)
	    {
	        if (verbose)
	        {
	            string e = Utils.RemoveWhitespace(expected).ToLowerInvariant();
	            string r = Utils.RemoveWhitespace(real).ToLowerInvariant();
	            ModifyOutput(ref r, ref e);
	            if (r.IndexOf(e, StringComparison.Ordinal) >= 0)
	            {
	                Console.WriteLine("Output has superficial differences from expected.");
	                Console.WriteLine("Got: " + r);
	                Console.WriteLine("Expected: " + e);
	            }
	        }
	    }

	    #endregion

        /// <summary>
		/// Writes one row of html table for this test.
		/// </summary>
		/// <param name="tw"><see cref="TextWriter"/> where to write output.</param>
		/// <param name="fullLog">True if detail info is written also if test succeeded.</param>
        /// <param name="testIndex">The test unique identifier (for the purposes of HTML generation).</param>
		public void WriteTableRow(TextWriter tw, bool fullLog, int testIndex)
		{
            bool displayDetails = (!Succeeded || Skipped || fullLog || benchmarks);

            string detailsRowId = "details" + testIndex;

			string classAttr = String.Concat(" class=\"", Succeeded ? "succeeded" : (Skipped ? "skipped" : "failed"), "\"");
			tw.WriteLine("<tr class=\"status\">");
			tw.WriteLine(String.Concat("<td", classAttr, " colspan=\"5\">",
                                        "<a", classAttr, " href=\"", SourcePathRelative, "\">", SourcePathRelative,
                                        "</a>: <font color=\"", Succeeded ? "green" : (Skipped ? "blue" : "red"), "\"><b>",
                                        Succeeded ? "SUCCEEDED" : (Skipped ? "SKIPPED" : "FAILED"), "</b></font>",
                                        "</td>"));
			tw.WriteLine("</tr>");

			if (displayDetails)
			{	// write details

				bool compileErrors = compilerErrorOutput.Length > 0;

                tw.WriteLine("<tr>");
				tw.Write(Utils.MakeTColumn(Utils.ResultToString(this.realTestResult), classAttr, "rowspan", compileErrors ? 2 : 1));
				if (benchmarks)
				{
					tw.Write(Utils.MakeTColumn(compilationTime.ToString()));
					tw.Write(Utils.MakeTColumn(runningTime.ToString()));
					tw.Write(Utils.MakeTColumn(phpTime.ToString()));
					tw.Write(Utils.MakeTColumn(numberOfRuns.ToString() + "/" + PhpNetTester.BenchmarkWarmup));
				}
				else
                {
                    string exp_str = expectWhereFailed;
                    string real_str = this.scriptOutput;

                    //HighlightDifferences(ref exp_str, ref real_str);

                    string expectedOutputHtml = String.Concat("<pre>", HttpUtility.HtmlEncode(exp_str), "</pre>");
                    string realOutputHtml = String.Concat("<pre>", HttpUtility.HtmlEncode(real_str), "</pre>");

                    tw.Write(Utils.MakeTColumn(compileErrors ? "See below" : "No errors", classAttr));
                    tw.Write(Utils.MakeTColumn(Utils.ResultToString(expectedTestResult), classAttr));



                    tw.Write(Utils.MakeTColumn(
                        "<table>"+
                        "<tr><td>Expected test output</td><td>Real script output</td></tr>"+
                        "<tr><td>"+expectedOutputHtml+"</td><td>"+realOutputHtml+"</td></tr>"+
                        "</table>",
                        classAttr, "colspan", 2));
                    //tw.Write(Utils.MakeTColumn(expectedOutputHtml, classAttr));
                    //tw.Write(Utils.MakeTColumn(realOutputHtml, classAttr));
				}
				tw.WriteLine("</tr>");

               	if (compileErrors)
				{
					tw.WriteLine("<tr>");
					tw.Write(Utils.MakeTColumn(String.Concat("<pre>", HttpUtility.HtmlEncode(compilerErrorOutput), "</pre>"), classAttr, "colspan", 5));
					tw.WriteLine("</tr>");
				}
			}
		}

		/// <summary>
		/// Returns true if the test succeded.
		/// </summary>
		public bool Succeeded
		{
			get { return realTestResult == expectedTestResult; }
		}

        /// <summary>
        /// Returns true if the test skipped.
        /// </summary>
        public bool Skipped
        {
            get { return skipped; }
        }

	    public string SourcePathRelative
	    {
	        get { return sourcePathRelative; }
	    }
	}
}
