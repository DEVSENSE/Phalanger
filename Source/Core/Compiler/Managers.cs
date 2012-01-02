/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Configuration.Assemblies;

using PHP.Core.Emit;
using PHP.Core.Reflection;

namespace PHP.Core
{
	#region CompilationContext

	public class CompilationContext
	{
        public ApplicationContext ApplicationContext { get { return applicationContext; } }
        protected readonly ApplicationContext applicationContext;

		public ICompilerManager/*!*/ Manager { get { return manager; } }
		protected readonly ICompilerManager/*!*/ manager;

		public CompilerConfiguration/*!*/ Config { get { return config; } }
		protected readonly CompilerConfiguration/*!*/ config;

		public string/*!*/ WorkingDirectory { get { return workingDirectory; } }
		private readonly string/*!*/ workingDirectory;

		public ErrorSink/*!*/ Errors { get { return errors; } }
		private readonly ErrorSink/*!*/ errors;

        /// <summary>
        /// Resulting assembly will be only saved to the file system. It won't be loaded or reflected.
        /// This disallows static inclusions, but allows to recompile scripts while debugging.
        /// </summary>
        /// <remarks><see cref="AssemblyBuilder"/> can be then created with <see cref="AssemblyBuilderAccess.Save"/> parameter only.</remarks>
        public virtual bool SaveOnlyAssembly { get { return false; } }

		/// <summary>
		/// Creates a compilation context.
		/// </summary>
        /// <param name="applicationContext">Application context.</param>
		/// <param name="manager">Manager.</param>
		/// <param name="config">Configuration.</param>
		/// <param name="errorSink">Error sink.</param>
		/// <param name="workingDirectory">Working directory.</param>
        internal CompilationContext(ApplicationContext/*!*/ applicationContext, ICompilerManager manager, CompilerConfiguration/*!*/ config, ErrorSink/*!*/ errorSink,
			string/*!*/ workingDirectory)
		{
            Debug.Assert(applicationContext != null);
			Debug.Assert(config != null && workingDirectory != null);

            this.applicationContext = applicationContext;
			this.manager = manager;
			this.config = config;
			this.errors = errorSink;
			this.workingDirectory = workingDirectory;
		}
	}

	#endregion

	#region ICompilerManager

	/// <summary>
	/// Manages compiler inputs (maps source paths of scripts to modules) and outputs (creates assemblies and modules
	/// using compiled code and metadata).
	/// </summary>
	public interface ICompilerManager
	{
		/// <summary>
		/// Notifies the CompilerManager that this Compiler is going to compile the assembly, 
		/// so other Compilers shouldn't do it.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <param name="context">The current compilation context.</param>
		/// <returns>
		/// Either a loaded module which should be treated as a result of compilation of <paramref name="sourceFile"/>
		/// or a <B>null</B> reference if no such assembly exists. 
		/// </returns>
		/// <remarks>
		/// Causes that the next calling LockForCompiling on this assembly 
		/// will suspend the calling Compiler until the compilation of the assembly is done
		/// (UnlockForCompiling is called).
		/// </remarks>
		PhpModule LockForCompiling(PhpSourceFile/*!*/ sourceFile, CompilationContext/*!*/ context);

		/// <summary>
		/// Notifies the CompilerManager that the assembly is no longer being compiled.
		/// </summary>
		/// <param name="sourceFile">Source file to unlock.</param>
		/// <param name="successful">Whether the assembly has been compiled successfuly.</param>
		/// <param name="context">The current compilation context.</param>
		/// <remarks>
		/// Causes that the next calling <see cref="LockForCompiling"/> (or calling that has been suspend)
		/// on this assembly will imediately return the <see cref="ScriptModule"/> just built
		/// (if success was true) or null (if succes was false). 
		/// </remarks>
		void UnlockForCompiling(PhpSourceFile/*!*/ sourceFile, bool successful, CompilationContext/*!*/ context);

		/// <summary>
		/// Returns a dynamic module builder prepared to be filled with emitted code and returns its builder.
		/// </summary>
		/// <param name="compiledUnit">Unit being compiled.</param>
		/// <param name="context">The current compilation context.</param>
		/// <returns>The builder.</returns>
		IPhpModuleBuilder DefineModuleBuilder(CompilationUnitBase/*!*/ compiledUnit, CompilationContext/*!*/ context);

		/// <summary>
		/// Persists the compiled unit (if applicable).
		/// </summary>
		/// <param name="compiledUnit">Compilation unit to be persisted.</param>
		/// <param name="context">The current compilation context.</param>
		void Persist(CompilationUnitBase/*!*/ compiledUnit, CompilationContext/*!*/ context);
		
		/// <summary>
		/// Informs inplementor about specific actions being performed during compilation.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <param name="context">The current compilation context.</param>
		void Info(PhpSourceFile/*!*/ sourceFile, CompilationContext/*!*/ context);

		void Finish(bool success);
	}

	#endregion
}