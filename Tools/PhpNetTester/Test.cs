using System;
using System.IO;
using System.Web;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;


namespace PHP.Testing
{
	public enum TestResult
	{
		Succees, CtError,
		UnexpectedOutput, PhpcMisbehaviourScript, CannotCompileExpect, ScriptHangUp, ExpectHangUp, PhpcHangUp,
		PhpHangUp, PhpMisbehaviour, PhpNotFound, ExpectedWarningNotDisplayed
	}

	public enum Directive
	{
		None, Expect, ExpectCtError, ExpectCtWarning, ExpectPhp, ExpectExact, File, Config, Comment,
		NumberOfRuns, AdditionalScripts, Pure, Clr
	}


	public class Test
	{
		protected const int buf_size = 100;

        public readonly string SourcePath;
        public readonly string SourcePathRelative;
		private ArrayList script;

		private TestResult expectedTestResult = TestResult.Succees;
		private ArrayList expect;
		private ArrayList expectExact = null;
		private ArrayList configuration;
		private ArrayList expectCtError;
		private ArrayList expectCtWarning;
		private ArrayList additionalScripts;

        private bool isPure = false, isClr = false;

		public TestResult RealTestResult { get { return realTestResult; } }
		private TestResult realTestResult;
		private string compilerErrorOutput;
		private string compilerStdOutput;
		private string scriptOutput;
		private bool expectPhp = false;

		private string expectWhereFailed = null;

		private bool verbose;
		private bool clean;
		private bool compileOnly;

		private bool benchmarks;
		private double compilationTime = 0;
		private double runningTime = 0;
		private double phpTime = 0;
		private int numberOfRuns = 0;

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
            this.SourcePath = Path.GetFullPath(sourcePath);
            Uri currentDir = new Uri(Directory.GetCurrentDirectory() + "\\");
            this.SourcePathRelative = currentDir.MakeRelativeUri(new Uri(this.SourcePath)).ToString();

            this.verbose = verbose;
			this.clean = clean;
			this.compileOnly = compileOnly;
			this.expect = new ArrayList();
			this.expectCtWarning = new ArrayList();
			this.benchmarks = benchmarks;
			this.numberOfRuns = defaultNumberOfRuns;

			// overwrites numberOfRuns if there is specified
			ReadFile();

			if (numberOfRuns < 1 || !benchmarks)
				numberOfRuns = 1;
		}

		private void ReadFile()
		{
			string line;
			Directive current_directive = Directive.None;
			ArrayList block = new ArrayList();

			using (StreamReader sr = new StreamReader(SourcePath))
			{
				while ((line = sr.ReadLine()) != null)
				{
					Directive d;
					if ((d = StringToDirective(ref line)) != Directive.None)
					{	// next directive
						if (block.Count > 0 || Utils.CanBeEmptyDirective(current_directive))
						{
							SaveBlock(block, current_directive);
							block = new ArrayList(); // reference is stored, we cannot just call Clear()
						}
						current_directive = d;
						if (line != null && line.Trim().Length > 0) // after directive is some text
							block.Add(line);
					}
					else
					{	// still current directive
						block.Add(line);
					}
				}

				// finish
				if (block.Count > 0)
					SaveBlock(block, current_directive);
			}

			// check if we have all required test parts
			if (script == null)
				throw new TestException(String.Format("Test {0} has no [file] section", this.SourcePath));
		}

		/// <summary>
		/// Returns <see cref="Directive"/> that is at the beginning of <paramref name="str"/> and in <c>str</c>
		/// leaves remaining characters.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		private Directive StringToDirective(ref string str)
		{
			string str_lower = str.ToLower();

			if (str_lower.StartsWith("--expect--"))
			{
				str = str.Substring("--expect--".Length);
				return Directive.Expect;
			}
			if (str_lower.StartsWith("[expect]"))
			{
				str = str.Substring("[expect]".Length);
				return Directive.Expect;
			}

			if (str_lower.StartsWith("--expect ct-error--"))
			{
				str = str.Substring("--expect ct-error--".Length);
				return Directive.ExpectCtError;
			}
			if (str_lower.StartsWith("[expect ct-error]"))
			{
				str = str.Substring("[expect ct-error]".Length);
				return Directive.ExpectCtError;
			}
            if (str_lower.StartsWith("[expect errors]"))
            {
                str = str.Substring("[expect errors]".Length);
                return Directive.ExpectCtError;
            }

			if (str_lower.StartsWith("--expect ct-warning--"))
			{
				str = str.Substring("--expect ct-warning--".Length);
				return Directive.ExpectCtWarning;
			}
			if (str_lower.StartsWith("[expect ct-warning]"))
			{
				str = str.Substring("[expect ct-warning]".Length);
				return Directive.ExpectCtWarning;
			}

			if (str_lower.StartsWith("--file--"))
			{
				str = str.Substring("--file--".Length);
				return Directive.File;
			}
			if (str_lower.StartsWith("[file]"))
			{
				str = str.Substring("[file]".Length);
				return Directive.File;
			}

			if (str_lower.StartsWith("--expect php--"))
			{
				str = str.Substring("--expect php--".Length);
				return Directive.ExpectPhp;
			}
			if (str_lower.StartsWith("[expect php]"))
			{
				str = str.Substring("[expect php]".Length);
				return Directive.ExpectPhp;
			}

			if (str_lower.StartsWith("--expect exact--"))
			{
				str = str.Substring("--expect exact--".Length);
				return Directive.ExpectExact;
			}
			if (str_lower.StartsWith("[expect exact]"))
			{
				str = str.Substring("[expect exact]".Length);
				return Directive.ExpectExact;
			}

			if (str_lower.StartsWith("--config--"))
			{
				str = str.Substring("--config--".Length);
				return Directive.Config;
			}
			if (str_lower.StartsWith("[config]"))
			{
				str = str.Substring("[config]".Length);
				return Directive.Config;
			}

			if (str_lower.StartsWith("--comment--"))
			{
				str = str.Substring("--comment--".Length);
				return Directive.Comment;
			}
			if (str_lower.StartsWith("[comment]"))
			{
				str = str.Substring("[comment]".Length);
				return Directive.Comment;
			}

			if (str_lower.StartsWith("--test--"))
			{
				str = str.Substring("--test--".Length);
				return Directive.Comment;
			}
			if (str_lower.StartsWith("[test]"))
			{
				str = str.Substring("[test]".Length);
				return Directive.Comment;
			}

			if (str_lower.StartsWith("--runs--"))
			{
				str = str.Substring("--runs--".Length);
				return Directive.NumberOfRuns;
			}
			if (str_lower.StartsWith("[runs]"))
			{
				str = str.Substring("[runs]".Length);
				return Directive.NumberOfRuns;
			}

			if (str_lower.StartsWith("--additional scripts--"))
			{
				str = str.Substring("--additional scripts--".Length);
				return Directive.AdditionalScripts;
			}
			if (str_lower.StartsWith("[additional scripts]"))
			{
				str = str.Substring("[additional scripts]".Length);
				return Directive.AdditionalScripts;
			}


            if (str_lower == "[pure]")
            {
                return Directive.Pure;
            }
            if (str_lower == "[clr]")
            {
                return Directive.Clr;
            }

			return Directive.None;
		}

		private void SaveBlock(ArrayList block, Directive directive)
		{
			if (directive == Directive.None || directive == Directive.Comment)
				return;

            switch (directive)
            {
                case Directive.Expect:
                    this.expect.Add(block);
                    break;

                case Directive.ExpectCtError:
                    if (this.expectCtError != null)
                        throw new TestException(String.Format("{0}: [expect ct-error] redefinition", this.SourcePath));
                    this.expectedTestResult = TestResult.CtError;
                    this.expectCtError = block;
                    break;

                case Directive.ExpectCtWarning:
                    this.expectCtWarning.Add(block);
                    break;

                case Directive.ExpectExact:
                    if (this.expectExact != null)
                        throw new TestException(String.Format("{0}: [expect exact] redefinition", this.SourcePath));
                    this.expectExact = block;
                    break;

                case Directive.ExpectPhp:
                    if (expectPhp)
                        throw new TestException(String.Format("{0}: [expect php] specified twice", this.SourcePath));
                    expectPhp = true;
                    break;

                case Directive.Config:
                    if (configuration != null)
                        throw new TestException(String.Format("{0}: [configuration] specified twice", this.SourcePath));
                    configuration = block;
                    break;

                case Directive.File:
                    if (this.script != null)
                        throw new TestException(String.Format("{0}: [script] redefinition", this.SourcePath));
                    this.script = block;
                    break;

                case Directive.NumberOfRuns:
                    if (this.numberOfRuns > 0)
                        throw new TestException(String.Format("{0}: [runs] redefinition", this.SourcePath));

                    try { this.numberOfRuns = Int32.Parse(Utils.ArrayListToString(block).Trim()); }
                    catch (FormatException)
                    { throw new TestException(String.Format("{0}: [runs] invalid value", this.SourcePath)); }
                    catch (OverflowException)
                    { throw new TestException(String.Format("{0}: [runs] invalid value", this.SourcePath)); }

                    break;

                case Directive.AdditionalScripts:
                    if (this.additionalScripts != null)
                        throw new TestException(String.Format("{0}: [additional scripts] redefinition", this.SourcePath));
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
			string compiled_script_path = Path.Combine(Path.GetDirectoryName(SourcePath), String.Concat(Path.GetFileNameWithoutExtension(SourcePath), "_file", ".exe"));
			string compiled_expect_path = Path.Combine(Path.GetDirectoryName(SourcePath), String.Concat(Path.GetFileNameWithoutExtension(SourcePath), "_expect", ".exe"));
			string expect_output;

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
			foreach (ArrayList e in expect)
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
		private bool Compile(string loaderPath, string compilerPath, ArrayList script, string output, bool isExpect)
		{
			string script_path = Path.Combine(Path.GetDirectoryName(SourcePath), "__input.txt");
			using (StreamWriter sw = new StreamWriter(script_path))
			{
				foreach (string s in script)
					sw.WriteLine(s);
				sw.Close();
			}

			Process compiler = new Process();
            
			string rootDir = Path.GetDirectoryName(SourcePath);
            string arguments = String.Concat("/dw:CompilerStrict /static+ /target:exe /out:\"", output, "\" /root:. /entrypoint:\"", Path.GetFileName(script_path), "\" \"", Path.GetFileName(script_path), "\"");
            if (isPure) arguments = "/pure+ " + arguments;
            if (isClr) arguments = "/lang:clr " + arguments;
            if (isPure || isClr) arguments = "/r:mscorlib /r:\"System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\" " + arguments;

			// put additional scripts to command line
			if (additionalScripts != null)
			{ // relative to /root: - we do not need to put whole path
				foreach (object o in additionalScripts)
				{
					string arg = ((string)o).Trim();
					if (arg.Length > 0)
						arguments += String.Concat(" \"", (string)o, "\"");
				}
			}

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

			DateTime startTime = System.DateTime.Now;
			compiler.Start();

			// compiler waits for enter key after work
			compiler.StandardInput.WriteLine();

			compilerErrorOutput = compiler.StandardError.ReadToEnd();
			if (!compiler.WaitForExit(30000))
			{
				compiler.Kill();
				realTestResult = TestResult.PhpcHangUp;
				if (verbose) Console.WriteLine("Compiler hung up.");
				if (clean) File.Delete(script_path);
				return false;
			}
			if (!isExpect) this.compilationTime = compiler.ExitTime.Subtract(startTime).TotalMilliseconds;
			if (clean) File.Delete(script_path);

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

                    int found = 0;
                    foreach (var errorline in errors)
                    {
                        foreach (string expectedstr in expectCtError)
                            if (errorline.Contains(expectedstr))
                            {
                                found++;
                                break;
                            }
                    }
                    if (found == errors.Length || expectCtError.Count == 0/*just expecting some ct errors*/)
                    {
                        realTestResult = TestResult.CtError;
                        return false;
                    }
                    
                    //if (CompareOutputsSubstring("error", ref comp_output, true) && CompareOutputsSubstring(Utils.ArrayListToString(expectCtError), ref comp_output, true))
                    //{
                    //    realTestResult = TestResult.CtError;
                    //    return false;
                    //}
				}

				realTestResult = TestResult.PhpcMisbehaviourScript;
				if (verbose) Console.WriteLine("Test result: {0}", Utils.ResultToString(realTestResult));
				return false;
			}

			if (!isExpect && expectCtWarning.Count > 0)
			{
				string comp_output = compilerErrorOutput;
				foreach (ArrayList warn in expectCtWarning)
				{
					if (!(CompareOutputsSubstring("warning", ref comp_output, true) && CompareOutputsSubstring(Utils.ArrayListToString(warn), ref comp_output, true)))
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
			output = null;

			// save configuration if any
			if (configuration != null)
				using (StreamWriter sw = new StreamWriter(String.Concat(scriptPath, ".config")))
				{
					sw.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
					sw.WriteLine("<configuration>");

					foreach (string s in configuration)
						sw.WriteLine(s);

					sw.WriteLine("</configuration>");

					sw.Close();
				}

			// run script
			Process script = new Process();

			if (loaderPath != null)
				script.StartInfo = new ProcessStartInfo(loaderPath, "\"" + scriptPath + "\"");
			else
				script.StartInfo = new ProcessStartInfo(scriptPath);

			script.StartInfo.UseShellExecute = false;
			script.StartInfo.RedirectStandardOutput = true;
            script.StartInfo.ErrorDialog = false;
            script.StartInfo.CreateNoWindow = true;
            script.StartInfo.RedirectStandardError = true;
            script.StartInfo.WorkingDirectory = Path.GetDirectoryName(scriptPath);

            script.EnableRaisingEvents = false;
           

			if (verbose) Console.WriteLine(String.Format("Starting {0}..", scriptPath));

			for (int i = 0; i < numberOfRuns; i++)
			{
				DateTime start = DateTime.Now;
				script.Start();

				output = Utils.RemoveCR(script.StandardOutput.ReadToEnd().Trim());
				if (!script.WaitForExit(30000))
				{
					script.Kill();
					if (verbose) Console.WriteLine(String.Format("Script {0} hung up.", scriptPath));
					return false;
				}
				if (script.ExitCode != 0)
				{
					if (verbose) Console.WriteLine(String.Format("Script {0} does not exit properly.", scriptPath));
                    output = script.StandardError.ReadToEnd();
					return false;
				}

				if (!mainScript) break; // only once if it is expect
				if (i >= PhpNetTester.BenchmarkWarmup) runningTime += script.ExitTime.Subtract(start).TotalMilliseconds;
			}
			script.Dispose();
			if (verbose) Console.WriteLine(String.Concat("Script output: ", output));
			return true;
		}

		private bool RunPhp(string phpPath, ArrayList script, out string output)
		{
			output = null;

			string script_path = Path.Combine(Path.GetDirectoryName(SourcePath), "__input.txt");
			using (StreamWriter sw = new StreamWriter(script_path))
			{
				foreach (string s in script)
					sw.WriteLine(s);
				sw.Close();
			}

			if (!File.Exists(phpPath))
			{
				realTestResult = TestResult.PhpNotFound;
				if (verbose) Console.WriteLine(Utils.ResultToString(realTestResult));
				if (clean) File.Delete(script_path);
				return false;
			}

            string phpIniPath = new Uri(new Uri(phpPath), "php.ini").LocalPath;// use our custom php.ini, don't use the global one if any

			Process php = new Process();
            php.StartInfo = new ProcessStartInfo(phpPath, String.Concat("-c \"", phpIniPath, "\" \"", script_path, "\""));
			php.StartInfo.UseShellExecute = false;
			php.StartInfo.RedirectStandardOutput = true;
            php.StartInfo.RedirectStandardError = true;
            php.StartInfo.WorkingDirectory = Path.GetDirectoryName(script_path);

			for (int i = 0; i < numberOfRuns; i++)
			{
				DateTime start = DateTime.Now;
				php.Start();

				output = Utils.RemoveCR(php.StandardOutput.ReadToEnd().Trim());
				if (!php.WaitForExit(30000))
				{
					php.Kill();
					realTestResult = TestResult.PhpHangUp;
					if (verbose) Console.WriteLine(Utils.ResultToString(realTestResult));
					if (clean) File.Delete(script_path);
					return false;
				}

				if (i >= PhpNetTester.BenchmarkWarmup)
					this.phpTime += php.ExitTime.Subtract(start).TotalMilliseconds;
			}

			php.Dispose();
			if (clean) File.Delete(script_path);

			if (verbose) Console.WriteLine(String.Concat("Php output: ", output));
			return true;
        }


        #region Comparing outputs (expected (PHP) and real (Phalanger))

        /// <summary>
        /// called on every match
        /// </summary>
        /// <param name="match"></param>
        /// <returns>only values in colections are used</returns>
        internal string CompareModifier( Match match )
        {
            string str = null;

            foreach (Group gr in match.Groups)
            {
                if ( gr != match )
                    str += gr.Value;
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
		private bool CompareOutputsExact(object expected, object real, bool ignoreKnownPhalangerDifferences)
		{
			string exp_str = expected as string;
			string real_str = real as string;

			if (exp_str == null)
			{
				Debug.Assert(expected is ArrayList);
				exp_str = "";
				foreach (string s in (ArrayList)expected)
					exp_str += String.Concat(s, "\n");
			}
			if (real_str == null)
			{
				Debug.Assert(real is ArrayList);
				real_str = "";
				foreach (string s in (ArrayList)real)
					real_str += String.Concat(s, "\n");
			}

			exp_str = exp_str.Trim();
			real_str = real_str.Trim();

            if (exp_str == real_str)
                return true;
            else if (!ignoreKnownPhalangerDifferences)
                return false;

            ModifyOutput(ref real_str, ref exp_str);

            return (exp_str == real_str);
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
            if (ignoreKnownPhalangerDifferences)
                ModifyOutput(ref real, ref expected);

			if (noCase)
                real = real.ToLower();

            int index = real.IndexOf(expected);

			if (index < 0)
				// not a substring
				return false;

			real = real.Substring(index + expected.Length);
			return true;
        }

        #endregion
        /*
        internal void HighlightDifferences(ref string exp_str, ref string real_str)
        {
            if (exp_str == null || real_str == null)
                return;

            string[] exp_lines = exp_str.Split(new char[] { '\n' });
            string[] real_lines = real_str.Split(new char[] { '\n' });

            int exp_l = 0, real_l = 0;

            int nChanges = 0;

            while ( exp_l < exp_lines.Length && real_l < real_lines.Length )
            {
                if (exp_lines[exp_l] != real_lines[real_l])
                {
                    ++nChanges;
                    real_lines[real_l] = String.Concat("<span style='background:white;'>",real_lines[real_l],"</span>");
                }

                do { exp_l++; }
                while (exp_l < exp_lines.Length && exp_lines[exp_l].Length == 0);

                do { real_l++; }
                while (real_l < real_lines.Length && real_lines[real_l].Length == 0);
            }

            if (nChanges > 0 && nChanges < real_lines.Length)//not all lines highlighted
            {
                real_str = null;
                foreach(string line in real_lines)
                {
                    if (real_str != null) real_str += "\n" + line;
                    else real_str = line;
                }
            }
        }*/

        /// <summary>
		/// Writes one row of html table for this test.
		/// </summary>
		/// <param name="tw"><see cref="TextWriter"/> where to write output.</param>
		/// <param name="fullLog">True if detail info is written also if test succeeded.</param>
        /// <param name="testIndex">The test unique identifier (for the purposes of HTML generation).</param>
		public void WriteTableRow(TextWriter tw, bool fullLog, int testIndex)
		{
            bool displayDetails = (!Succeeded || fullLog || benchmarks);

            string detailsRowId = "details" + testIndex;

			string classAttr = String.Concat(" class=\"", Succeeded ? "succeeded" : "failed", "\"");
			tw.WriteLine("<tr class=\"status\">");
			tw.WriteLine(String.Concat("<td", classAttr, " colspan=\"5\">",
                "<a", classAttr, " href=\"", SourcePathRelative, "\">", SourcePathRelative, "</a>: <font color=\"", Succeeded ? "green" : "red", "\"><b>", Succeeded ? "SUCCEEDED" : "FAILED", "</b></font>",
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
			get
			{
				if (realTestResult == expectedTestResult)
					return true;

				return false;
			}
		}

	}
}