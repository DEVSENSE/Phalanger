/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using System.Globalization;

namespace PHP.Core.CodeDom
{
	/// <summary>
	/// PHP <see cref="ICodeCompiler"/> implementation.
	/// </summary>
	/// <remarks>
	/// Since Beta 3, the compiler is not based on <see cref="CodeCompiler"/> but compiles
	/// in a separate appdomain without spawning a new <c>phpc</c> process.
	/// </remarks>
	internal sealed class PhpCodeCompiler : ICodeCompiler
	{
		#region CodeDomErrorSink

		/// <summary>
		/// An error sink that adds errors to <see cref="CompilerResults"/>.
		/// </summary>
		private class CodeDomErrorSink : ErrorSink
		{
			private CompilerResults/*!*/ results;

			/// <summary>
			/// Creates a new <see cref="CodeDomErrorSink"/> with a specified <see cref="CompilerResults"/>.
			/// </summary>
			/// <param name="results">The compiler results.</param>
			public CodeDomErrorSink(CompilerResults/*!*/ results)
			{
				Debug.Assert(results != null);
				this.results = results;
			}

			/// <summary>
			/// Called when an error/warning should be reported.
			/// </summary>
			protected override bool Add(int id, string message, ErrorSeverity severity, int group, string fullPath, ErrorPosition pos)
			{
				if (id >= 0)
				{
					CompilerError error = new CompilerError();

					error.FileName = fullPath;
					error.Line = pos.FirstLine;
					error.Column = pos.FirstColumn;
					error.ErrorNumber = String.Format("PHP{0:d4}", id);
					error.ErrorText = message;
					error.IsWarning = (severity.Value == ErrorSeverity.Values.Warning);

					results.Errors.Add(error);
				}

				// build the output line
				StringBuilder sb = new StringBuilder(128);
				if (fullPath != null)
				{
					sb.AppendFormat("{0}({1},{2}): ",
						fullPath,
						pos.FirstLine,
						pos.FirstColumn);
				}

				if (id >= 0)
				{
					sb.AppendFormat("{0} PHP{1:d4}: {2}",
						severity.ToCmdString(),
						id,
						message);
				}
				else sb.Append(message);

				results.Output.Add(sb.ToString());

				return true;
			}
		}

		#endregion

		#region AppCompilerStack

		/// <summary>
		/// Manages a stack of <see cref="ApplicationCompiler"/> instances.
		/// </summary>
		private static class AppCompilerStack
		{
			#region StackItem

			private struct StackItem
			{
				/// <summary>
				/// A compiler that compiles in a separate appdomain.
				/// </summary>
				public ApplicationCompiler/*!*/ Compiler;

				/// <summary>
				/// Counts compilations performed by the <see cref="Compiler"/>.
				/// </summary>
				public int CompileCounter;

				/// <summary>
				/// The number of assemblies loaded to the remote compilation appdomain after the first compilation.
				/// </summary>
				public int RemoteAssemblyCount;
			}

			#endregion

			#region CallBackDisplay

			/// <summary>
			/// Works around the lack of <see cref="SerializableAttribute"/> on display classes generated
			/// by C# compiler when an anonymous method is used.
			/// </summary>
			[Serializable]
			private sealed class CallBackDisplay
			{
				public int AssemblyCount;

				public void Handler()
				{
					AssemblyCount = AppDomain.CurrentDomain.GetAssemblies().Length;
				}
			}

			#endregion

			/// <summary>
			/// Maximum number of compilations without checking the number of assemblies loaded to the
			/// remote appdomain.
			/// </summary>
			private const int compileCounterTreshold = 10;

			/// <summary>
			/// The stack.
			/// </summary>
			private static Stack<StackItem>/*!*/ stack = new Stack<StackItem>();

			/// <summary>
			/// Compiles in a seperate appdomain utilitizing one of the compilers on the stack.
			/// </summary>
			public static void Compile(ErrorSink/*!*/ errorSink, CompilationParameters/*!*/ ps)
			{
				// obtain a compiler
				StackItem item = new StackItem();
				lock (stack)
				{
					if (stack.Count > 0) item = stack.Pop();
				}
				if (item.Compiler == null) item.Compiler = ApplicationCompiler.CreateRemoteCompiler();

				// compile
				item.Compiler.RemoteCompile(ref errorSink, ps);

				// check whether the separate appdomain is not too weedy
				if (++item.CompileCounter == 1 || item.CompileCounter == compileCounterTreshold)
				{
					item.CompileCounter = 1;

					CallBackDisplay display = new CallBackDisplay();

					// avoid passing the array of assemblies across appdomain boundary
					item.Compiler.Domain.DoCallBack(display.Handler);

					if (item.RemoteAssemblyCount == 0) item.RemoteAssemblyCount = display.AssemblyCount;
					else
					{
						if (display.AssemblyCount > (2 * item.RemoteAssemblyCount))
						{
							AppDomain.Unload(item.Compiler.Domain);
							return;
						}
					}
				}

				// recycle the compiler
				lock (stack) stack.Push(item);
			}
		}

		#endregion

		#region ICodeCompiler Members

		/// <summary>
		/// Compiles an assembly from the <see cref="System.CodeDom"/> tree contained in the specified
		/// <see cref="CodeCompileUnit"/>, using the specified compiler settings. 
		/// </summary>
		public CompilerResults/*!*/ CompileAssemblyFromDom(CompilerParameters/*!*/ options, CodeCompileUnit/*!*/ compilationUnit)
		{
			if (options == null) throw new ArgumentNullException("options");
			if (compilationUnit == null) throw new ArgumentNullException("compilationUnit");

			CompilationParameters parameters = new CompilationParameters();
			throw new NotImplementedException(); //parameters.SourceReaders.Add(GenerateCompilationUnit(compilationUnit));
            //ResolveReferencedAssemblies(options, compilationUnit);

            //return Compile(parameters, options);
		}

		/// <summary>
		/// Compiles an assembly based on the <see cref="System.CodeDom"/> trees contained in the specified array of
		/// <see cref="CodeCompileUnit"/> objects, using the specified compiler settings.
		/// </summary>
		public CompilerResults/*!*/ CompileAssemblyFromDomBatch(CompilerParameters/*!*/ options, CodeCompileUnit[]/*!*/ compilationUnits)
		{
			if (options == null) throw new ArgumentNullException("options");
			if (compilationUnits == null) throw new ArgumentNullException("compilationUnits");

			CompilationParameters parameters = new CompilationParameters();

            throw new NotImplementedException();

            //for (int i = 0; i < compilationUnits.Length; i++)
            //{
            //    parameters.SourceReaders.Add(GenerateCompilationUnit(compilationUnits[i]));
            //    ResolveReferencedAssemblies(options, compilationUnits[i]);
            //}

            //return Compile(parameters, options);
		}

		/// <summary>
		/// Compiles an assembly from the source code contained within the specified file, using the specified compiler settings.
		/// </summary>
		public CompilerResults/*!*/ CompileAssemblyFromFile(CompilerParameters/*!*/ options, string/*!*/ fileName)
		{
			if (options == null) throw new ArgumentNullException("options");
			if (fileName == null) throw new ArgumentNullException("fileName");

			CompilationParameters parameters = new CompilationParameters();
			parameters.SourcePaths.Add(new FullPath(fileName));

			return Compile(parameters, options);
		}

		/// <summary>
		/// Compiles an assembly from the source code contained within the specified files, using the specified compiler settings.
		/// </summary>
		public CompilerResults/*!*/ CompileAssemblyFromFileBatch(CompilerParameters/*!*/ options, string[]/*!*/ fileNames)
		{
			if (options == null) throw new ArgumentNullException("options");
			if (fileNames == null) throw new ArgumentNullException("fileNames");

			CompilationParameters parameters = new CompilationParameters();
			for (int i = 0; i < fileNames.Length; i++)
			{
				parameters.SourcePaths.Add(new FullPath(fileNames[i]));
			}

			return Compile(parameters, options);
		}

		/// <summary>
		/// Compiles an assembly from the specified string containing source code, using the specified compiler settings.
		/// </summary>
		public CompilerResults/*!*/ CompileAssemblyFromSource(CompilerParameters/*!*/ options, string/*!*/ source)
		{
			if (options == null) throw new ArgumentNullException("options");
			if (source == null) throw new ArgumentNullException("source");

			CompilationParameters parameters = new CompilationParameters();
			throw new NotImplementedException(); //parameters.SourceReaders.Add(new StringReader(source));

			//return Compile(parameters, options);
		}

		/// <summary>
		/// Compiles an assembly from the specified array of strings containing source code, using the specified compiler settings.
		/// </summary>
		public CompilerResults/*!*/ CompileAssemblyFromSourceBatch(CompilerParameters/*!*/ options, string[]/*!*/ sources)
		{
			if (options == null) throw new ArgumentNullException("options");
			if (sources == null) throw new ArgumentNullException("sources");

			CompilationParameters parameters = new CompilationParameters();

            throw new NotImplementedException();

            //for (int i = 0; i < sources.Length; i++)
            //{
            //     //parameters.SourceReaders.Add(new StringReader(sources[i]));
            //}

            //return Compile(parameters, options);
		}

		#endregion

		#region Compile

		/// <summary>
		/// Performs the compilation.
		/// </summary>
		/// <param name="parameters">Parameters that already contain the source files/streams to compile.</param>
		/// <param name="options">The options specified by CodeCom user.</param>
		/// <returns>The compiler results.</returns>
		private CompilerResults/*!*/ Compile(CompilationParameters/*!*/ parameters, CompilerParameters/*!*/ options)
		{
			// translate options to parameters
			SetupCompilerParameters(parameters, options);

			// set up compiler results
            CompilerResults results = new CompilerResults(options.TempFiles);   // J: SecurityAction.LinkDemand, "FullTrust"
			CodeDomErrorSink error_sink = new CodeDomErrorSink(results);

			results.Output.Add("Phalanger - the PHP Language Compiler - commencing compilation in a separate appdomain");
			results.Output.Add("Source files to compile:");
			for (int i = 0; i < parameters.SourcePaths.Count; i++)
			{
				results.Output.Add(parameters.SourcePaths[i].ToString());
			}

			// compile the files/streams in a separate appdomain
			AppCompilerStack.Compile(error_sink, parameters);

			// set up the compiler results
			results.PathToAssembly = parameters.OutPath.ToString();
			results.NativeCompilerReturnValue = (results.Errors.HasErrors ? 1 : 0);

            // J: obsolete, FullTrust demanded earlier
            //new SecurityPermission(SecurityPermissionFlag.ControlEvidence).Assert();
            //try
            //{
            //    results.Evidence = options.Evidence;   // J: SecurityAction.LinkDemand, "FullTrust" // same as CompilerResults above
            //}
            //finally
            //{
            //    CodeAccessPermission.RevertAssert();
            //}

			return results;
		}

		#endregion

		#region Helper methods

		/// <summary>
		/// Sets up <paramref name="parameters"/> according to the supplied <paramref name="options"/>.
		/// </summary>
		/// <param name="parameters">The parameters to set up.</param>
		/// <param name="options">The options passed to CodeDom.</param>
		private void SetupCompilerParameters(CompilationParameters/*!*/ parameters, CompilerParameters/*!*/ options)
		{
			parameters.Target = (options.GenerateExecutable ? ApplicationCompiler.Targets.Console : ApplicationCompiler.Targets.Dll);
			parameters.Debuggable = options.IncludeDebugInformation;
			parameters.SourceRoot = DetermineSourceRoot(parameters);

			if (!String.IsNullOrEmpty(options.OutputAssembly))
			{
				parameters.OutPath = new FullPath(options.OutputAssembly);
			}

			// referenced assemblies
			foreach (string reference in options.ReferencedAssemblies)
			{
                parameters.References.Add(new CompilationParameters.ReferenceItem() { Reference = reference });
			}

			// referenced resource files
			foreach (string resource in options.LinkedResources)
			{
				parameters.Resources.Add(new FileReference(resource));
			}

			parameters.Pure = true;

			// parse user-provided compiler options
			if (!String.IsNullOrEmpty(options.CompilerOptions))
			{
				CommandLineParser parser = new CommandLineParser(parameters);
				parser.Parse(CommandLineParser.StringToArgumentList(options.CompilerOptions));
			}
		}

		/// <summary>
		/// Adjusts <see cref="CompilerParameters"/>' referenced assemblies according to a given compile unit.
		/// </summary>
		/// <param name="options">The compiler parameters.</param>
		/// <param name="e">The compile unit.</param>
		/// <remarks>
		/// Copied from the <see cref="System.CodeDom.Compiler.CodeCompiler"/> implementation.
		/// </remarks>
		private void ResolveReferencedAssemblies(CompilerParameters options, CodeCompileUnit e)
		{
			if (e.ReferencedAssemblies.Count > 0)
			{
				foreach (string ass in e.ReferencedAssemblies)
				{
					if (!options.ReferencedAssemblies.Contains(ass))
					{
						options.ReferencedAssemblies.Add(ass);
					}
				}
			}
		}

		/// <summary>
		/// Determines the source root for a compilation based on the source file paths and output path.
		/// </summary>
		/// <param name="parameters">The parameters describing source files and the output file.</param>
		/// <returns>A suitable source root, preferrably a common superdirectory of all sources.</returns>
		private FullPath DetermineSourceRoot(CompilationParameters/*!*/ parameters)
		{
			// try to obtain a common superdirectory of all source files
			if (parameters.SourcePaths.Count > 0 && parameters.SourcePaths[0].DirectoryExists)
			{
				CultureInfo culture = System.Threading.Thread.CurrentThread.CurrentCulture;
				string result = Path.GetDirectoryName(parameters.SourcePaths[0].ToString());

				for (int i = 1; i < parameters.SourcePaths.Count; i++)
				{
					string path = parameters.SourcePaths[i].ToString();

					int limit = Math.Min(result.Length, path.Length);
					int index = -1;

					for (int j = 0; j < limit; j++)
					{
						// remember the last directory separator position
						if (result[j] == Path.DirectorySeparatorChar) index = j;

						if (Char.ToLower(result[j], culture) != Char.ToLower(path[j], culture))
						{
							if (index >= 0) result = result.Substring(0, index);
							else result = null;
						}
					}

					if (result == null) break;
				}

				if (result != null) return new FullPath(result);
			}

			// otherwise the output directory
			if (!parameters.OutPath.IsEmpty)
			{
				return new FullPath(Path.GetDirectoryName(parameters.OutPath.ToString()));
			}

			// otherwise fall back to default
			return new FullPath(Environment.SystemDirectory);
		}

		#endregion
	}
}
