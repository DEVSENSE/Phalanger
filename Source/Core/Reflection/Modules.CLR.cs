/*

 Copyright (c) 2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Threading;
using PHP.Core.Emit;
using System.Reflection.Emit;

namespace PHP.Core.Reflection
{
	#region PureModule

	public class PureModule : PhpModule
	{
		#region Construction

		/// <summary>
		/// Called by the loader. The module can be loaded to <see cref="PureAssembly"/> or 
		/// <see cref="PhpLibraryAssembly"/>.
		/// </summary>
		internal PureModule(DAssembly/*!*/ assembly)
			: base(assembly)
		{
		}

		/// <summary>
		/// Called by the builder.
		/// </summary>
		internal PureModule(PureCompilationUnit/*!*/ compilationUnit, PureAssembly/*!*/ assembly)
			: base(compilationUnit, assembly)
		{
		}

		protected override CompilationUnitBase/*!*/ CreateCompilationUnit()
		{
			Debug.Fail("Compilation unit is never created for a loaded pure module.");
			throw null;
		}

		#endregion

		#region Reflection

		private bool AutoPopulate()
		{
			Type global_type = this.Assembly.RealModule.GetType(QualifiedName.Global.Name.Value);
			if (global_type != null)
			{
				MethodInfo declare_helper = global_type.GetMethod(Name.DeclareHelperName.Value,
					BindingFlags.NonPublic | BindingFlags.Static);

				if (declare_helper != null)
				{
					try
					{
						declare_helper.Invoke(null, new object[] { this.assembly.ApplicationContext });
						return true;
					}
					catch (Exception)
					{
					}
				}
			}

			return false;
		}

		public override void Reflect(bool full,
			Dictionary<string, DTypeDesc>/*!*/ types,
			Dictionary<string, DRoutineDesc>/*!*/ functions,
			DualDictionary<string, DConstantDesc>/*!*/ constants)
		{
			if (AutoPopulate()) return;

			// types:
			ClrModule.ReflectTypes(this.Assembly.RealAssembly, types);

			// TODO:

			// functions and global constants:
			foreach (Module module in this.Assembly.RealAssembly.GetModules())
			{
				//ClrTypeDesc.ReflectMethods(module.GetMethods(), globalType.TypeDesc,
				//	delegate(Name name, ClrMethodDesc/*!*/ function) { functions.Add(name.Value, function); });
			}
		}

		#endregion
	}

	#endregion

	#region ScriptModule

	/// <summary>
	/// Represents a script virtual module.
	/// </summary>
	public partial class ScriptModule : PhpModule
	{
		#region Statics

		/// <summary>
		/// Declare functions helper argument types.
		/// </summary>
		internal static readonly Type[] DeclareHelperArgTypes = Types.ScriptContext;

		/// <summary>
		/// The name of the class containing script functions and helpers.
		/// </summary>
		internal const string ScriptTypeName = "<Script>";

		internal const string DeclareHelperNane = "<Declare>";

		/// <summary>
		/// Value returned from skipped include construct.
		/// </summary>
		internal const bool SkippedIncludeReturnValue = true;

		#endregion

		#region Fields and properties

		public new CompilationUnit CompilationUnit { get { return (CompilationUnit)base.CompilationUnit; } }

		/// <summary>
		/// A script assembly where the script belongs to.
		/// </summary>
		public ScriptAssembly ScriptAssembly { get { return (ScriptAssembly)assembly; } }

		/// <summary>
		/// Gets a namespace where user classes and interfaces are declared in.
		/// Ends with a <see cref="Type.Delimiter"/> (a dot) for convenience.
		/// </summary>
		public string UserTypesNamespace { get { return userTypesNamespace; } }
		private string userTypesNamespace;

		/// <summary>
		/// Gets the script type.
		/// </summary>
        public Type ScriptType
        {
            get
            {
                return scriptInfo.Script;
            }
            //internal set
            //{
            //    scriptInfo = new ScriptInfo(value);
            //}
        }

        /// <summary>
        /// Gets the script info containing the type and Main delegate.
        /// </summary>
        public ScriptInfo ScriptInfo { get { return scriptInfo; } }
        protected ScriptInfo scriptInfo;

        /// <summary>
        /// MainHelper method, which is called when inclusion is made.
        /// </summary>
        public MethodInfo MainHelper { get { return ScriptInfo.MainHelper; } /*set { ScriptInfo.MainHelper = value; }*/ }

        /// <summary>
        /// Gets the relative path of the source file represented by this ScriptModule.
        /// </summary>
        public string RelativeSourcePath { get { return relativeSourcePath; } }
        private readonly string relativeSourcePath;

		#endregion

		#region Construction

		/// <summary>
		/// Used by the builder.
		/// </summary>
		public ScriptModule(CompilationUnit/*!*/ unit, ScriptAssembly/*!*/ scriptAssembly, string subnamespace)
			: base(unit, scriptAssembly)
		{
			Debug.Assert(unit != null && scriptAssembly != null);

			this.userTypesNamespace = subnamespace;
			this.scriptInfo = null;
            this.relativeSourcePath = unit.RelativeSourcePath;
		}

		/// <summary>
		/// Used by the loader.
		/// </summary>
        /// <param name="relativeSourcePath">Path to the source file which is represented by this script.</param>
        /// <param name="scriptType">Type which represents script. Implements IPhpScript interface.</param>
        /// <param name="scriptAssembly">Script assembly this script module belongs to.</param>
        /// <param name="subnamespace">Namespace where all types statically declared in this script are contained.</param>
        public ScriptModule(string/*!*/ relativeSourcePath, Type/*!*/ scriptType, ScriptAssembly/*!*/ scriptAssembly, string subnamespace)
			: base(scriptAssembly)
		{
			Debug.Assert(scriptAssembly != null);

			this.scriptInfo = new ScriptInfo(scriptType);
			this.userTypesNamespace = subnamespace;
            this.relativeSourcePath = relativeSourcePath;

			//FastReflect();
		}


		/// <summary>
		/// This method is used when module is reflected (and is not intended
		/// to be compiled).
		/// </summary>
		/// <returns>Returns new instance of ReflectedCompilationUnit.</returns>
		protected override CompilationUnitBase/*!*/ CreateCompilationUnit()
		{
			return new ReflectedCompilationUnit(this);
		}

		#endregion

		#region Getters

		public virtual object[] GetInclusionAttributes()
		{
			return ScriptType.GetCustomAttributes(typeof(IncludesAttribute), false);
		}

		/// <summary>
		/// Translates a source path to a namespace using the current source root from configuration.
		/// </summary>
		/// <returns>The namespace optionally ending with a dot.</returns>
		public static string/*!*/ GetSubnamespace(RelativePath rp, bool appendDot)
		{
			StringBuilder result = new StringBuilder(rp.Path.Length);
			
			result.Append('<');
			
			// external script's file which can be reached via relative path:
			if (rp.Level > 0)
			{
				result.Append("$L");
				result.Append((char)(rp.Level + 'a'));
			}

			for (int i = 0; i < rp.Path.Length; i++)
			{
				switch (rp.Path[i])
				{
					case '\\': result.Append('/'); break;
					case '$': result.Append("$$"); break;
					case '[': result.Append("$A"); break;
					case ']': result.Append("$B"); break;
					case '+': result.Append("$C"); break;
					case ',': result.Append("$D"); break;
					case '&': result.Append("$E"); break;
					case '#': result.Append("$F"); break;
					case '`': result.Append("$G"); break;
					default: result.Append(rp.Path[i]); break;
				}
			}
			
			result.Append('>');
			if (appendDot) result.Append('.');
			return result.ToString();
		}


		/// <summary>
		/// Translates namespace name to file path.
		/// (Decodes namespace names encoded using <see cref="GetSubnamespace"/> method).
		/// </summary>
		public static RelativePath GetPathFromSubnamespace(string/*!*/ subnamespace)
		{
			if (subnamespace == null)
				throw new ArgumentNullException("subnamespace");

			if (subnamespace.Length < 2 || subnamespace[0] != '<')
				throw new ArgumentException();

			try
			{
				StringBuilder result = new StringBuilder(subnamespace.Length);
				sbyte level = 0;

				int i = 1; 
				while (subnamespace[i] != '>')
				{
					if (subnamespace[i] == '$')
					{
						i++;
						
						Debug.Assert(i < subnamespace.Length);
						
						switch (subnamespace[i])
						{
							case '$': result.Append('$'); break;
							case 'A': result.Append('['); break;
							case 'B': result.Append(']'); break;
							case 'C': result.Append('+'); break;
							case 'D': result.Append(','); break;
							case 'E': result.Append('&'); break;
							case 'F': result.Append('#'); break;
							case 'G': result.Append('`'); break;
							
							case 'L':
								level = (sbyte)(subnamespace[++i] - 'a');
								break;
								
							default: 
								throw new ArgumentException(); 
						}
					}
					else
						result.Append(subnamespace[i]);
					
					i++;
				}

				return new RelativePath(level, result.ToString());
			}
			catch
			{
				throw new ArgumentException();
			}
		}

		#endregion

		#region Reflection

		/// <summary>
		/// Reflect types, functions and constants in compiled CU
		/// </summary>
        /// <param name="full">Not used.</param>
        /// <param name="functions">Will contain reflected functions.</param>
        /// <param name="types">Will contain reflected classes.</param>
        /// <param name="constants">Not used.</param>
		public override void Reflect(bool full,
			Dictionary<string, DTypeDesc>/*!*/ types,
			Dictionary<string, DRoutineDesc>/*!*/ functions,
			DualDictionary<string, DConstantDesc>/*!*/ constants)
		{
            // pairs of <assembly, list of namespaces>
            // each namespace represents relative path to the script containing functions/PHP classes
            var reachedScripts = new Dictionary<Type, bool>();

            CollectIncludees(ScriptType, reachedScripts);

            // reflect functions/classes from reachedScripts
            foreach (var scriptType in reachedScripts.Keys)
            {
                Debug.Assert(scriptType.Name == ScriptModule.ScriptTypeName);

                ReflectScriptTypeFunctions(scriptType, functions);
                ReflectScriptTypeClasses(scriptType, types);
                ReflectScriptTypeConstants(scriptType, constants);
            }
		}

        /// <summary>
        /// Collect scripts to reflect from.
        /// </summary>
        /// <param name="scriptType">The Script type to collect.</param>
        /// <param name="reachedScripts">Scripts reached using static inclusions. Used also as recursion prevention.</param>
        private static void CollectIncludees(Type scriptType, Dictionary<Type,bool> reachedScripts)
        {
            Debug.Assert(scriptType.Name == ScriptModule.ScriptTypeName);
            
            // recursion prevention
            if (reachedScripts.ContainsKey(scriptType)) return;
            reachedScripts.Add(scriptType, false);
              
            //
            // reflect statically included script types recursively
            //
            var sa = ScriptIncludeesAttribute.Reflect(scriptType);
            if (sa != null && sa.Inclusions.Length > 0)
            {
                var module = scriptType.Module;
                var conditionalFlags = sa.InclusionsConditionalFlags;
                // recursively reflect statically included scripts
                for (int i = 0; i < sa.Inclusions.Length; ++i )
                    if (!conditionalFlags[i])   // allow reflecting of included script only if it is not conditional include
                    {
                        Type includedScript = module.ResolveType(sa.Inclusions[i]);

                        if (includedScript != null)
                            CollectIncludees(includedScript, reachedScripts);
                    }
            }
        }

        /// <summary>
        /// Reflect PHP classes declared statically in the given script <c>type</c>.
        /// </summary>
        /// <param name="scriptType">Script type to reflect.</param>
        /// <param name="types">List of types to reflect to.</param>
        private static void ReflectScriptTypeClasses(Type scriptType,
            Dictionary<string, DTypeDesc>/*!*/ types)
        {
            ScriptDeclaresAttribute script_declares = ScriptDeclaresAttribute.Reflect(scriptType);
            if (script_declares == null) return;

            var module = scriptType.Module;

            foreach (var typeToken in script_declares.DeclaredTypes)
            {
                Type type = module.ResolveType(typeToken);

                // reflect PHP class, skip PHP types that were declared conditionally
                if (PhpType.IsPhpRealType(type) && !PhpType.IsRealConditionalDefinition(type))
                {
                    // converts CLR namespaces and nested types to PHP namespaces:
                    string full_name = QualifiedName.FromClrNotation(type.FullName, true).ToString();

                    // Creating PhpTypeDesc with cache lookup since this type can be in the cache already:
                    // Also force PHP type, because we already checked PhpType.IsPhpRealType(type)
                    PhpTypeDesc phpType = (PhpTypeDesc)DTypeDesc.Create(type.TypeHandle);

                    DTypeDesc existing;
                    if (types.TryGetValue(full_name, out existing))
                    {
                        // TODO (TP): can be generic overload!!
                        existing.MemberAttributes |= PhpMemberAttributes.Ambiguous;
                    }
                    types.Add(full_name, phpType);
                }
            }            
        }

		/// <summary>
		///	reflect global functions in &lt;Script&gt; class
		/// TODO: consider merging with PhpTypeDesc.FullReflectMethods
		/// </summary>
		/// <param name="functions">Dictionary for functions</param>
        /// <param name="scriptType">The type to reflect from.</param>
        private static void ReflectScriptTypeFunctions(Type scriptType, Dictionary<string, DRoutineDesc> functions)
		{
            MethodInfo[] real_methods = scriptType.GetMethods(DTypeDesc.MembersReflectionBindingFlags);
			Dictionary<string, MethodInfo> argless_stubs = new Dictionary<string, MethodInfo>(real_methods.Length / 3);

			// first pass - fill argless_stubs
			for (int i = 0; i < real_methods.Length; i++)
			{
				MethodInfo info = real_methods[i];
				if ((info.Attributes & MethodAttributes.MemberAccessMask) != MethodAttributes.PrivateScope)
				{
					if (PhpFunctionUtils.IsArglessStub(info, null))
					{
						argless_stubs.Add(info.Name, info);
						real_methods[i] = null;
					}
				}
				else real_methods[i] = null; // expunge private scope methods
			}

			// second pass - match argfulls
            foreach (MethodInfo info in real_methods)
            {
                // argfull detection:
                if (info == null || !PhpFunctionUtils.IsArgfullOverload(info, null))
                    continue;

                // TODO: namespaces!!

                if (functions.ContainsKey(info.Name))
                    throw new InvalidOperationException("Function '" + info.Name + "' redeclaration.");   // compilation unit contains more functions with the same name

                // find the argless:
                MethodInfo argless_info = null;
                if (!argless_stubs.TryGetValue(info.Name, out argless_info))
                {
                    // argless has to be generated on-the-fly
                    throw new NotImplementedException("Generating argless stubs for imported PHP types is not yet implemented");
                }

                if (!PhpFunctionUtils.IsRealConditionalDefinition(info.Name))
                {
                    Name name = new Name(info.Name);
                    DRoutineDesc func_desc;
                    PhpMemberAttributes attrs = Enums.GetMemberAttributes(info);

                    // this method has not been populated -> create a new PhpRoutineDesc
                    func_desc = new PhpRoutineDesc(attrs, (RoutineDelegate)Delegate.CreateDelegate(Types.RoutineDelegate, argless_info), true);
                    functions.Add(info.Name, func_desc);

                    //
                    if (func_desc.Member == null)
                    {
                        PhpFunction func = new PhpFunction(new QualifiedName(name), (PhpRoutineDesc)func_desc, info, argless_info);
                        func.WriteUp(PhpRoutineSignature.FromArgfullInfo(func, info));
                        func_desc.Member = func;
                    }
                }
            }
		}

        /// <summary>
        /// Reflect global constants in &lt;script&gt; class.
        /// </summary>
        /// <param name="scriptType">The type representing single script.</param>
        /// <param name="constants">Dictionary for constants.</param>
        private void ReflectScriptTypeConstants(Type scriptType, DualDictionary<string, DConstantDesc>/*!*/ constants)
        {
            foreach (var field in scriptType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                // TODO: namespaces!!

                GlobalConstant constant = new GlobalConstant(this, QualifiedName.FromClrNotation(field.Name, true), field);
                constant.SetValue(Convert.ClrLiteralToPhpLiteral(field.GetValue(null)));
                constants.Add(field.Name, constant.ConstantDesc, false);
            }            
        }

		#endregion
	}

	#endregion
}
