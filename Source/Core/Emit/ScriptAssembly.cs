using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core;

/*
 
  Designed and implemented by Tomas Matousek.

*/

namespace PHP.Core.Emit
{
	#region ScriptAssembly

	public class InvalidScriptAssemblyException : Exception
	{
		internal InvalidScriptAssemblyException(Assembly/*!*/ assembly)
			: base(CoreResources.GetString("invalid_script_assembly", assembly.Location))
		{
		}
	}

	/// <summary>
	/// Interface implemented by the script assembly. 
	/// Replaces multiple inheritance.
	/// </summary>
	internal interface IScriptAssembly
	{
		bool Namespacing { get; }
		bool IsMultiScript { get; }
		string GetUserTypeNamespace(string subnamespace);
		string GetQualifiedScriptTypeName(string subnamespace);
		Type GetScriptType(PhpSourceFile sourceFile);
	}

	/// <summary>
	/// An abstract base class representing general script assembly.
	/// </summary>
	internal abstract class ScriptAssembly
	{
		#region Fields and Properties

		/// <summary>
		/// Name of the CLR module.
		/// </summary>
		protected const string moduleName = "PhpScriptModule";

		/// <summary>
		/// A CLR module where all scripts of this script assembly are contained in.
		/// </summary>
		public Module Module { get { return module; } }
		protected Module module;

		/// <summary>
		/// A CLR assembly where all scripts of this script assembly are contained in.
		/// </summary>
		public Assembly Assembly { get { return module.Assembly; } }

		/// <summary>
		/// Whether namespacing is applied on PHP types in this script assembly.
		/// </summary>
		public bool Namespacing { get { return namespacing; } }
		protected bool namespacing;

		public abstract bool IsMultiScript { get; }

		#endregion

		#region Construction

		protected ScriptAssembly() { }

		/// <summary>
		/// Creates an instance of <see cref="ScriptAssembly"/>.
		/// </summary>
		/// <param name="module">The CLR module.</param>
		/// <param name="namespacing">Whether namespacing is applied.</param>
		public ScriptAssembly(Module/*!*/ module, bool namespacing)
		{
			Debug.Assert(module != null);

			this.module = module;
			this.namespacing = namespacing;
		}

		/// <summary>
		/// Loads a script assembly using a specified CLR assembly.
		/// </summary>
		/// <param name="assembly">The assembly to be reflected.</param>
		/// <returns>The script assembly.</returns>
		/// <exception cref="InvalidScriptAssemblyException">The assembly is invalid.</exception>
		public static ScriptAssembly LoadFromAssembly(Assembly/*!*/ assembly)
		{
			Debug.Assert(assembly != null);

			ScriptAssembly result;
			ScriptAssemblyAttribute attr = GetAttribute(assembly);

			if (attr.IsMultiScript)
				result = new MultiScriptAssembly();
			else
				result = new SingleScriptAssembly();

			result.namespacing = attr.Namespacing;
			result.module = GetModule(assembly);
			result.LoadedFromAssembly(assembly);

			return result;
		}

		#endregion

		/// <summary>
		/// Called when the script assembly is loaded from a specified CLR assembly.
		/// </summary>
		protected abstract void LoadedFromAssembly(Assembly/*!*/ assembly);

		/// <summary>
		/// Returns a script module associated with a specified source path.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <returns>A script module.</returns>
		public abstract ScriptModule GetScriptModule(PhpSourceFile sourceFile);

		public abstract Type GetScriptType(PhpSourceFile sourceFile);

		/// <summary>
		/// Gets a CLR module where the script is stored.
		/// </summary>
		public static Module GetModule(Assembly/*!*/ assembly)
		{
			return assembly.GetModule(moduleName);
		}

		/// <summary>
		/// Gets a namespace where user types should be stored in.
		/// </summary>
		/// <param name="subnamespace">The subnamespace ending with a type delimiter or a <B>null</B> reference.</param>
		/// <returns>The namespace ending with a type delimiter.</returns>
		public string GetUserTypeNamespace(string subnamespace)
		{
			Debug.Assert(subnamespace == null || subnamespace == String.Empty || subnamespace[subnamespace.Length - 1] == Type.Delimiter);

			return Namespaces.User + Type.Delimiter + ((namespacing) ? subnamespace : null);
		}

		/// <summary>
		/// Gets a type name of the script type given a subnamespace.
		/// </summary>
		/// <param name="subnamespace">The subnamespace or a <B>null</B> reference.</param>
		/// <returns>Full name of the type.</returns>
		public string GetQualifiedScriptTypeName(string subnamespace)
		{
			Debug.Assert(subnamespace == null || subnamespace == String.Empty || subnamespace[subnamespace.Length - 1] == Type.Delimiter);

			if (namespacing)
				return String.Concat(Namespaces.User + Type.Delimiter, subnamespace, PhpScript.ScriptTypeName);
			else
				return String.Concat(Namespaces.User + Type.Delimiter + PhpScript.ScriptTypeName + Type.Delimiter, subnamespace, PhpScript.ScriptTypeName);
		}

		/// <summary>
		/// Extracts metadata information associtated with the CLR assembly.
		/// </summary>
		/// <exception cref="InvalidScriptAssemblyException">The assembly is invalid.</exception>
		internal static ScriptAssemblyAttribute GetAttribute(Assembly/*!*/ assembly)
		{
			object[] attrs = assembly.GetCustomAttributes(typeof(ScriptAssemblyAttribute), false);

			if (attrs.Length != 1)
				throw new InvalidScriptAssemblyException(assembly);

			return (ScriptAssemblyAttribute)attrs[0];
		}
	}

	#endregion

	#region SingleScriptAssembly

	/// <summary>
	/// Represents a script assembly comprising of a single script module.
	/// </summary>
	internal class SingleScriptAssembly : ScriptAssembly, IScriptAssembly
	{
		internal SingleScriptAssembly() { }

		protected ScriptModule script;

		public override bool IsMultiScript { get { return false; } }

		/// <summary>
		/// Invoked when the script assembly is being loaded from CLR assembly.
		/// </summary>
		protected override void LoadedFromAssembly(Assembly/*!*/ assembly)
		{
			script = new ScriptModule(this, "");
		}

		/// <summary>
		/// Gets the script module contained in the assembly.
		/// </summary>
		/// <returns>The one and only script module of the assembly.</returns>
		public override ScriptModule GetScriptModule(PhpSourceFile dummy)
		{
			return script;
		}

		/// <summary>
		/// Gets a script type stored in a specified single-script assembly.
		/// </summary>
		public override Type GetScriptType(PhpSourceFile dummy)
		{
			return GetScriptType();
		}

		/// <summary>
		/// Gets a script type stored in a specified single-script assembly.
		/// </summary>
		public Type GetScriptType()
		{
			return module.GetType(GetQualifiedScriptTypeName(null), false, true);
		}
	}

	#endregion

	#region MultiScriptAssembly

	/// <summary>
	/// Represents a script assembly comprising of multiple script modules.
	/// </summary>
	internal class MultiScriptAssembly : ScriptAssembly, IScriptAssembly
	{
		internal MultiScriptAssembly() { }

		/// <summary>
		/// Source file to module mapping.
		/// </summary>
		protected readonly Dictionary<PhpSourceFile, ScriptModule> /*!*/ scripts = new Dictionary<PhpSourceFile, ScriptModule>();

		public override bool IsMultiScript { get { return true; } }

		/// <summary>
		/// Invoked when the script assembly is being loaded from CLR assembly.
		/// </summary>
		protected override void LoadedFromAssembly(Assembly/*!*/ assembly)
		{
		}

		/// <summary>
		/// Gets a script module associated with a specified source file.
		/// </summary>
		public override ScriptModule GetScriptModule(PhpSourceFile/*!*/ sourceFile)
		{
			ScriptModule result;
			scripts.TryGetValue(sourceFile, out result);
			return result;
		}

		/// <summary>
		/// Gets a script type stored in a specified multi-script assembly.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <returns>The script type.</returns>
		public override Type GetScriptType(PhpSourceFile/*!*/ sourceFile)
		{
			Debug.Assert(sourceFile != null);

			return module.GetType(GetQualifiedScriptTypeName(sourceFile), false, true);
		}

		/// <summary>
		/// Gets a full qualified name of a script type given a sub-namespace.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <returns>The qualified name.</returns>
		public string GetQualifiedScriptTypeName(PhpSourceFile/*!*/ sourceFile)
		{
			Debug.Assert(sourceFile != null);

			return GetQualifiedScriptTypeName(ScriptModule.GetSubnamespace(sourceFile));
		}

		internal bool ScriptExists(FullPath fullPath)
		{
			PhpSourceFile source_file = new PhpSourceFile(Configuration.Application.Compiler.SourceRoot, fullPath);
			return GetScriptType(source_file) != null;
		}
	}

	#endregion
}