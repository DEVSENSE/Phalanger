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
	/// <summary>
	/// Manager for a single script compiler.
	/// </summary>
	internal sealed class ApplicationCompilerManager : ICompilerManager
	{
		#region Fields

		private bool successful;

		/// <summary>
		/// Assembly builder.
		/// </summary>
		public PhpAssemblyBuilder/*!*/ AssemblyBuilder { get { return assemblyBuilder; } }
		private PhpAssemblyBuilder/*!*/ assemblyBuilder;

        private ApplicationContext applicationContext;

		#endregion

		#region Construction

		/// <summary>
		/// Creates an instance of the manager.
		/// </summary>
		internal ApplicationCompilerManager(ApplicationContext applicationContext, PhpAssemblyBuilder/*!*/ assemblyBuilder)
		{
			Debug.Assert(assemblyBuilder != null);
            Debug.Assert(applicationContext != null);

			this.successful = true;
			this.assemblyBuilder = assemblyBuilder;
            this.applicationContext = applicationContext;
		}

		#endregion

		#region ICompilerManager Members

		/// <summary>
		/// Returns compiled module if exists.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <param name="ctx">A compilation context.</param>
		/// <returns>The <see cref="PhpModule"/> or a <B>null</B> reference.</returns>
		/// <remarks>
		/// Checks whether a module associated with a <paramref name="sourceFile"/> has already been compiled.
		/// If so returns the respective <see cref="PhpModule"/>. Otherwise a <B>null</B> reference is returned.
		/// Does no locking since application compiler is thread unsafe.
		/// </remarks>
		public PhpModule LockForCompiling(PhpSourceFile/*!*/ sourceFile, CompilationContext/*!*/ ctx)
		{
            // take a look into script library first
            if (applicationContext.ScriptLibraryDatabase.ContainsScript(sourceFile.FullPath))
            {
                return applicationContext.ScriptLibraryDatabase.GetScriptModule(sourceFile.FullPath);
            }

			return assemblyBuilder.Assembly.GetModule(sourceFile);
		}

		/// <summary>
		/// Adds a new module to application's assembly.
		/// </summary>
		/// <param name="compiledUnit">Unit being compiled.</param>
		/// <param name="ctx">A compilation context.</param>
		/// <returns>The builder where compiler should emit the resulting code.</returns>
		public IPhpModuleBuilder/*!*/ DefineModuleBuilder(CompilationUnitBase/*!*/ compiledUnit, CompilationContext ctx)
		{
			return assemblyBuilder.DefineModule(compiledUnit);
		}

		/// <summary>
		/// Ignored. All modules are stored in a single assembly which is persisted in <see cref="Finish"/>.
		/// </summary>
		public void Persist(CompilationUnitBase/*!*/ compiledUnit, CompilationContext/*!*/ ctx)
		{
			// nop //
		}
		
		/// <summary>
		/// Remembers whether compilation has been successful.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <param name="successful">Whether compilation was successful.</param>
		/// <param name="ctx">A compilation context.</param>
		public void UnlockForCompiling(PhpSourceFile/*!*/ sourceFile, bool successful, CompilationContext/*!*/ ctx)
		{
			this.successful &= successful;
		}

		/// <summary>
		/// Triggered by the compiler on events such are start of compilation, end of compilation etc.
		/// </summary>
		/// <param name="sourceFile">A source path to a script being processed.</param>
		/// <param name="ctx">A compilation context.</param>
		public void Info(PhpSourceFile/*!*/ sourceFile, CompilationContext/*!*/ ctx)
		{
			Console.WriteLine(sourceFile.RelativePath);
		}

		public void Finish(bool successful)
		{
			this.successful &= successful;

			if (this.successful)
			{
				// TODO: redirect 
				Console.WriteLine(CoreResources.GetString("generating_assembly"));
				assemblyBuilder.Save();
			}
		}

		#endregion
	}
}