/*

 Copyright (c) 2006-2012 Tomas Matousek, DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;
using System.Threading;
using System.Reflection.Emit;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core.Reflection
{
	#region PureAssembly

	public sealed class PureAssembly : PhpAssembly
	{
		internal const string ModuleName = "PurePhpModule";
		public static readonly Name EntryPointName = new Name("Main");

		internal override DModule/*!*/ ExportModule { get { return module; } }

		public PureModule/*!*/ Module { get { return module; } internal /* friend PAB */ set { module = value; } }
		private PureModule/*!*/ module;

		#region Construction

		/// <summary>
		/// Used by the loader.
		/// </summary>
		internal PureAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly,
			PurePhpAssemblyAttribute/*!*/ attribute, LibraryConfigStore configStore)
			: base(applicationContext, realAssembly)
		{
			module = new PureModule(this);
		}

		/// <summary>
		/// Used by the builder.
		/// </summary>
		internal PureAssembly(ApplicationContext/*!*/ applicationContext)
			: base(applicationContext)
		{
			// to be written-up
		}

		#endregion

		public override PhpModule GetModule(PhpSourceFile name)
		{
			return module;
		}

		internal override void LoadCompileTimeReferencedAssemblies(AssemblyLoader/*!*/ loader)
		{
			base.LoadCompileTimeReferencedAssemblies(loader);

			foreach (string full_name in GetAttribute().ReferencedAssemblies)
				loader.Load(full_name, null, null);
		}

		private PurePhpAssemblyAttribute GetAttribute()
		{
			return PurePhpAssemblyAttribute.Reflect(RealAssembly);
		}
	}

	#endregion

	#region ScriptAssembly

    [Serializable]
    public sealed class InvalidScriptAssemblyException : Exception
    {
        internal InvalidScriptAssemblyException(Assembly/*!*/ assembly)
            : base(CoreResources.GetString("invalid_script_assembly", assembly.Location))
        {
        }

        #region Serializable

        public InvalidScriptAssemblyException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            
        }

        [System.Security.SecurityCritical]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
        }

        #endregion
    }

	/// <summary>
	/// An abstract base class representing general script assembly.
	/// </summary>
	public abstract class ScriptAssembly : PhpAssembly
	{
		internal const string RealModuleName = "PhpScriptModule";
		internal const string EntryPointHelperName = "Run";
        
		public abstract bool IsMultiScript { get; }

		#region Construction

		/// <summary>
		/// Used by assembly loader.
		/// </summary>
		public ScriptAssembly(ApplicationContext/*!*/ applicationContext, Module/*!*/ realModule)
			: base(applicationContext, realModule)
		{
		}

		/// <summary>
		/// Used by assembly loader.
		/// </summary>
		public ScriptAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly)
			: this(applicationContext, realAssembly.ManifestModule)
		{
		}

		/// <summary>
		/// Used by builders (written-up).
		/// </summary>
		protected ScriptAssembly(ApplicationContext/*!*/ applicationContext)
			: base(applicationContext)
		{
		}

        internal static ScriptAssembly/*!*/ Create(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly,
            ScriptAssemblyAttribute/*!*/ scriptAttribute)
        {
            return Create(applicationContext, realAssembly, scriptAttribute, null);
        }

		internal static ScriptAssembly/*!*/ Create(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly,
            ScriptAssemblyAttribute/*!*/ scriptAttribute, string libraryRoot)
		{
            if (scriptAttribute.IsMultiScript)
                return new MultiScriptAssembly(applicationContext, realAssembly, libraryRoot);
            else
                return new SingleScriptAssembly(applicationContext, realAssembly, libraryRoot);
		}

		/// <summary>
		/// Loads a script assembly using a specified CLR assembly.
		/// </summary>
        public static ScriptAssembly/*!*/ LoadFromAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly)
		{
            return LoadFromAssembly(applicationContext, realAssembly, null);
		}

        /// <summary>
        /// Loads a script assembly using a specified CLR assembly with specified offset path.
        /// </summary>
        public static ScriptAssembly/*!*/ LoadFromAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly, string libraryRoot)
        {
            return Create(applicationContext, realAssembly, GetAttribute(realAssembly), libraryRoot);
        }

		#endregion

		public abstract IEnumerable<ScriptModule> GetModules();

		/// <summary>
		/// Gets a type name of the script type given a subnamespace.
		/// </summary>
		/// <param name="subnamespace">The subnamespace or a <B>null</B> reference.</param>
		/// <returns>Full name of the type.</returns>
		public string GetQualifiedScriptTypeName(string subnamespace)
		{
			Debug.Assert(String.IsNullOrEmpty(subnamespace) || subnamespace[subnamespace.Length - 1] == Type.Delimiter);

			return String.Concat(subnamespace, ScriptModule.ScriptTypeName);
		}

		/// <summary>
		/// Extracts metadata information associtated with the CLR assembly.
		/// </summary>
		/// <exception cref="InvalidScriptAssemblyException">The assembly is invalid.</exception>
		private static ScriptAssemblyAttribute/*!*/ GetAttribute(Assembly/*!*/ realAssembly)
		{
			ScriptAssemblyAttribute result = ScriptAssemblyAttribute.Reflect(realAssembly); 
			
			if (result == null)
				throw new InvalidScriptAssemblyException(realAssembly);
			
			return result;
		}
	}

	#endregion

	#region SingleScriptAssembly

	/// <summary>
	/// Represents a script assembly comprising of a single script module.
	/// </summary>
	public sealed class SingleScriptAssembly : ScriptAssembly
	{
		public ScriptModule Module { get { return module; } internal /* friend SSAB */ set { module = value; } }
		private ScriptModule module;
        private Type scriptType;

		public override bool IsMultiScript { get { return false; } }

		#region Construction

		/// <summary>
		/// Used by the loader.
		/// </summary>
        /// <param name="applicationContext">Current application context.</param>
        /// <param name="realAssembly">Underlying real assembly.</param>
        /// <param name="libraryRoot">Offset path for scripts.</param>
        internal SingleScriptAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly, string libraryRoot)
            : base(applicationContext, realAssembly)
        {
            var scriptType = this.GetScriptType();
            var subnamespace = string.IsNullOrEmpty(scriptType.Namespace) ? string.Empty : (scriptType.Namespace + ".");

            this.module = new ScriptModule(libraryRoot, scriptType, this, subnamespace);
        }

		/// <summary>
		/// Used by the builder, written-up.
		/// </summary>
		internal SingleScriptAssembly(ApplicationContext/*!*/ applicationContext)
			: base(applicationContext)
		{
			this.module = null; // to be set by the builder
		}

		#endregion

		/// <summary>
		/// Gets the script module contained in the assembly.
		/// </summary>
        public override PhpModule GetModule(PhpSourceFile name)
        {
            return this.module;
        }

		/// <summary>
		/// Gets a script type stored in a specified single-script assembly.
		/// </summary>
        internal Type/*!*/GetScriptType()
		{
            if (this.scriptType == null)
            {
                var attr = ScriptAssemblyAttribute.Reflect(RealModule.Assembly);
                Debug.Assert(attr != null);
                Debug.Assert(!attr.IsMultiScript);
                Debug.Assert(attr.SSAScriptType != null);

                this.scriptType = attr.SSAScriptType;
                Debug.Assert(this.scriptType != null);
            }
            return this.scriptType;
		}

        /// <summary>
        /// Gets an enumerator of script module stored in this single-script assembly.
        /// </summary>
        public override IEnumerable<ScriptModule> GetModules()
        {
            yield return this.module;
        }
	}

	#endregion

	#region MultiScriptAssembly

	/// <summary>
	/// Represents a script assembly comprising of multiple script modules.
	/// </summary>
	public sealed class MultiScriptAssembly : ScriptAssembly
	{
		/// <summary>
		/// Source files to modules mapping.
		/// </summary>
		internal Dictionary<PhpSourceFile, ScriptModule> Modules { get { return modules; } }
		private Dictionary<PhpSourceFile, ScriptModule> modules;

        /// <summary>
        /// Root path to script in this library.
        /// </summary>
        private string libraryRoot;

		public override bool IsMultiScript { get { return true; } }

		/// <summary>
		/// Used by assembly loader.
		/// </summary>
        /// <param name="applicationContext">Current application context.</param>
        /// <param name="realAssembly">Underlying real assembly.</param>
        /// <param name="libraryRoot">Relative path of root of the library scripts.</param>
        internal MultiScriptAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly, string libraryRoot)
            : base(applicationContext, realAssembly)
		{
            this.libraryRoot = libraryRoot;
		}

		/// <summary>
		/// Used by the builder (real assembly is written up).
		/// </summary>
		internal MultiScriptAssembly(ApplicationContext/*!*/ applicationContext)
			: base(applicationContext)
		{
			this.modules = new Dictionary<PhpSourceFile, ScriptModule>();
		}

        private void EnsureLibraryReflected()
        {
            if (modules == null)
                lock(this)
                    if (modules == null) 
                        ReflectAssemblyNoLock();
        }

        /// <summary>
        /// Reflects the assembly and creates ScriptModules.
        /// </summary>
        private void ReflectAssemblyNoLock()
        {
            // TODO: this should be changed into ScriptAttribute resolution when all assemblies have this attribute correctly generated
            // (same as WebCompilerManager does that)
            this.modules = new Dictionary<PhpSourceFile, ScriptModule>();

            // go through all types in the assembly
            foreach (Type type in RealAssembly.GetTypes()/*GetExportedTypes()*/)
            {
                //bool isScript = false;

                ////check whether type implements IPhpScript interface
                //foreach (Type iface in type.GetInterfaces())
                //{
                //    if (iface == typeof(IPhpScript))
                //    {
                //        isScript = true;
                //        break;
                //    }
                //}

                //if (!isScript) continue;
                if (type.IsVisible && type.Name == ScriptModule.ScriptTypeName)
                {
                    //customary . is required
                    string subnamespace = type.Namespace + ".";
                    
                    //get script's arbitrary path
                    string scriptPath = ScriptModule.GetPathFromSubnamespace(subnamespace).ToString();
                    if (libraryRoot != null)
                        scriptPath = System.IO.Path.Combine(libraryRoot, scriptPath);

                    modules.Add(new PhpSourceFile(Configuration.Application.Compiler.SourceRoot, new RelativePath(scriptPath)), new ScriptModule(scriptPath, type, this, subnamespace));
                }
            }
        }

		/// <summary>
		/// Gets a script module associated with a specified source file.
		/// </summary>
		public override PhpModule GetModule(PhpSourceFile/*!*/ sourceFile)
		{
            Debug.Assert(sourceFile != null);

            EnsureLibraryReflected();

			ScriptModule result;
			modules.TryGetValue(sourceFile, out result);
			return result;
		}

		/// <summary>
		/// Adds a new script module. Used by builder.
		/// </summary>
		internal void AddScriptModule(PhpSourceFile/*!*/ sourceFile, ScriptModule/*!*/ module)
		{
			modules.Add(sourceFile, module);
		}

		/// <summary>
		/// Gets a full qualified name of a script type given a sub-namespace.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <returns>The qualified name.</returns>
		public string GetQualifiedScriptTypeName(PhpSourceFile/*!*/ sourceFile)
		{
			Debug.Assert(sourceFile != null);

			return GetQualifiedScriptTypeName(ScriptModule.GetSubnamespace(sourceFile.RelativePath, true));
		}

        /// <summary>
        /// Determine if script specified by <paramref name="fullPath"/> is loaded in script library.
        /// </summary>
        /// <param name="fullPath">The script path.</param>
        /// <returns>True if given script is loaded.</returns>
		internal bool ScriptExists(FullPath fullPath)
		{
            EnsureLibraryReflected();

			PhpSourceFile source_file = new PhpSourceFile(Configuration.Application.Compiler.SourceRoot, fullPath);
            return modules.ContainsKey(source_file);
		}

        /// <summary>
        /// Gets an enumerator of script modules stored in this multi-script assembly.
        /// </summary>
        public override IEnumerable<ScriptModule> GetModules()
        {
            EnsureLibraryReflected();

            return modules.Values;
        }
	}

	#endregion
}
