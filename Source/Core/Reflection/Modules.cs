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
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PHP.Core.Reflection
{
    #region DModule

    /// <remarks>
    /// REFLECTION: Reflection should add reflected items to the compilation unit. 
    /// When ScriptModule is reflected, the scope in the CU's tables is set to arbitrary > 0 value for
    /// conditional items ($mdeclN). N may encode the scope (if useful). Thus it is not necessary to
    /// create any kind of Declaration instances for the reflected items.
    /// </remarks>
    public abstract class DModule
    {
        #region  Fields

        public DAssembly/*!*/ Assembly { get { return assembly; } }
        protected readonly DAssembly/*!*/ assembly;

        public GlobalType/*!*/ GlobalType { get { return globalType; } }
        protected readonly GlobalType/*!*/ globalType;

        #endregion

        #region Construction

        /// <summary>
        /// Used by the loader.
        /// </summary>
        protected DModule(DAssembly/*!*/ assembly)
        {
            this.globalType = new GlobalType(this);
            this.assembly = assembly;
        }

        /// <summary>
        /// Used by <see cref="UnknownModule"/>.
        /// </summary>
        protected DModule()
        {
            this.globalType = new GlobalType(this);
            this.assembly = UnknownAssembly.RuntimeAssembly;
        }

        #endregion

        #region Reflection

        public abstract void Reflect(bool full,
            Dictionary<string, DTypeDesc>/*!*/ types,
            Dictionary<string, DRoutineDesc>/*!*/ functions,
            DualDictionary<string, DConstantDesc>/*!*/ constants);

        #endregion
    }

    #endregion

    #region PhpModule

    public abstract class PhpModule : DModule
    {
        public CompilationUnitBase/*!*/ CompilationUnit
        {
            get
            {
                if (_compilationUnit == null)
                    _compilationUnit = CreateCompilationUnit();

                return _compilationUnit;
            }
        }
        private CompilationUnitBase _compilationUnit;

        protected abstract CompilationUnitBase/*!*/ CreateCompilationUnit();

        /// <summary>
        /// Used by the builder.
        /// </summary>
        protected PhpModule(CompilationUnitBase/*!*/ compilationUnit, PhpAssembly/*!*/ assembly)
            : base(assembly)
        {
            this._compilationUnit = compilationUnit;
        }

        /// <summary>
        /// Used by the loader.
        /// </summary>
        protected PhpModule(DAssembly/*!*/ assembly)
            : base(assembly)
        {
            this._compilationUnit = null; // lazy init or unused
        }
    }

    #endregion

    #region TransientModule

    /// <summary>
    /// Represents a script virtual module.
    /// </summary>
    [DebuggerNonUserCode]
    public class TransientModule : PhpModule
    {
        #region Members

        /// <summary>
        /// Characters separating <see cref="TransientModule.sourcePath"/> and <see cref="TransientModule.Id"/>.
        /// </summary>
        internal static readonly char[]/*!*/IdDelimiters = new[] { '^', '?' };

        public TransientCompilationUnit TransientCompilationUnit { get { return (TransientCompilationUnit)base.CompilationUnit; } }

        public MainRoutineDelegate Main { get { return main; } }
        protected MainRoutineDelegate main;

        public TransientModule ContainingModule { get { return containingModule; } }
        private readonly TransientModule containingModule;

        public int Id { get { return id; } }
        private readonly int id;

        private readonly string sourcePath;

        internal EvalKinds Kind { get { return kind; } }
        private readonly EvalKinds kind;

        /// <summary>
        /// Used by the builder.
        /// </summary>
        public TransientModule(int id, EvalKinds kind, TransientCompilationUnit/*!*/ unit, TransientAssembly/*!*/ scriptAssembly,
            TransientModule containingModule, string sourcePath)
            : base(unit, scriptAssembly)
        {
            Debug.Assert(unit != null && scriptAssembly != null);

            this.id = id;
            this.sourcePath = sourcePath;
            this.kind = kind;
            this.containingModule = containingModule;
        }

        internal string/*!*/ MakeName(string /*!*/ name, bool isSpecialName)
        {
            // <> ensures, the eval-id is removed from the name
            // '?'/'^' distinguish from the file path used by MSA and among regular and special names
            return String.Concat(
                "<",
                    this.sourcePath,
                    isSpecialName ? IdDelimiters[0] : IdDelimiters[1],
                    this.Id.ToString(),
                ">.",
                name);
        }

        private static int ParseEvalId(string/*!*/ name)
        {
            Debug.Assert(name != null && name.Length > 0 && name[0] == '<');

            int id;
            string typename;
            string src;
            ReflectionUtils.ParseTypeId(name, out id, out src, out typename);
            return id;
        }

        internal static int GetEvalId(ApplicationContext/*!*/ applicationContext, MethodBase/*!*/ method)
        {
            // gets [PhpEvalId] attribute defined on the type:
            if (method.DeclaringType != null && applicationContext.IsTransientRealType(method.DeclaringType))
                return ParseEvalId(method.DeclaringType.Namespace);

            if (method.Name.Length > 3 && method.Name[0] == '<' && (method.Name[1] == '?' || method.Name[1] == '*'))
                return ParseEvalId(method.Name);

            return TransientAssembly.InvalidEvalId;
        }

        internal static bool IsSpecialName(string/*!*/ name)
        {
            return name.Length > 1 && name[0] == '<' && name[1] == '*';
        }

        /// <summary>
        /// Gets a string appearing in error messages.
        /// </summary>
        /// <returns>The string.</returns>
        internal string GetErrorString()
        {
            switch (kind)
            {
                case EvalKinds.Assert: return "assert";
                case EvalKinds.ExplicitEval: return "eval";
                case EvalKinds.LambdaFunction: return "runtime-created function";
            }
            return null;
        }

        #endregion

        #region N/A

        protected override CompilationUnitBase CreateCompilationUnit()
        {
            Debug.Fail();
            throw null;
        }

        public override void Reflect(bool full, Dictionary<string, DTypeDesc> types, Dictionary<string, DRoutineDesc> functions, DualDictionary<string, DConstantDesc> constants)
        {
            Debug.Fail();
            throw null;
        }

        #endregion
    }

    #endregion

    #region ClrModule

    /// <summary>
    /// Represents CLR loaded assembly (not a <see cref="System.Reflection.Module"/>).
    /// </summary>
    public sealed class ClrModule : DModule
    {
        public ClrAssembly/*!*/ ClrAssembly { get { return (ClrAssembly)assembly; } }

        #region Construction

        /// <summary>
        /// Called by the loader via <see cref="ClrAssembly"/>.
        /// </summary>
        internal ClrModule(ClrAssembly/*!*/ assembly)
            : base(assembly)
        {

        }

        #endregion

        #region Reflection

        public override void Reflect(bool full,
            Dictionary<string, DTypeDesc>/*!*/ types,
            Dictionary<string, DRoutineDesc>/*!*/ functions,
            DualDictionary<string, DConstantDesc>/*!*/ constants)
        {
            ReflectTypes(assembly.RealAssembly, types);
            ReflectFunctions(assembly.RealAssembly, globalType.TypeDesc, functions);
            ReflectConstants(assembly.RealAssembly, this, constants);
        }

        /// <summary>
        /// Used by CLR modules and PHP pure modules.
        /// </summary>
        internal static void ReflectTypes(Assembly/*!*/ realAssembly, Dictionary<string, DTypeDesc>/*!*/ types)
        {
            // types:
            foreach (Type type in realAssembly.GetTypes())
            {
                if (type.IsVisible)
                {
                    // skip PHP types that were declared conditionally:
                    if (PhpType.IsPhpRealType(type) && PhpType.IsRealConditionalDefinition(type))
                        continue;

                    // converts CLR namespaces and nested types to PHP namespaces:
                    string full_name = QualifiedName.FromClrNotation(type.FullName, true).ToString();

                    DTypeDesc existing;
                    if (types.TryGetValue(full_name, out existing))
                    {
                        ClrTypeDesc existing_clr = existing as ClrTypeDesc;
                        if (existing_clr != null && (existing_clr.GenericOverloads.Count > 0 || type.IsGenericTypeDefinition))
                        {
                            ClrTypeDesc new_clr = DTypeDesc.Create(type) as ClrTypeDesc;
                            if (new_clr != null)
                            {
                                // type is overloaded by the number of generic parameters:
                                existing_clr.AddGenericOverload(new_clr);
                            }
                            else
                            {
                                // do not add, just mark existing with the flag:
                                existing.MemberAttributes |= PhpMemberAttributes.Ambiguous;
                            }
                        }
                        else
                        {
                            // do not add, just mark existing with the flag:
                            existing.MemberAttributes |= PhpMemberAttributes.Ambiguous;
                        }
                    }
                    else
                    {
                        types[full_name] = DTypeDesc.Create(type);
                    }
                }
            }
        }

        internal static void ReflectFunctions(Assembly/*!*/ realAssembly, DTypeDesc/*!*/ declaringType,
            Dictionary<string, DRoutineDesc>/*!*/ functions)
        {
            Debug.Assert(realAssembly != null && declaringType != null && functions != null);

            foreach (Module module in realAssembly.GetModules())
            {
                foreach (MethodInfo real_function in module.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if ((real_function.Attributes & MethodAttributes.FamANDAssem) != 0)    // skip some methods that should not be accessible
                        continue;

                    ReflectFunction(declaringType, real_function, functions);
                }
            }
        }

        internal static void ReflectFunction(DTypeDesc declaringType, MethodInfo real_function, Dictionary<string, DRoutineDesc> functions)
        {
            if (!real_function.IsSpecialName)
            {
                QualifiedName qualified_name = QualifiedName.FromClrNotation(real_function.Name, true);
                string full_name = qualified_name.ToString();

                ClrMethod clr_function = null;

                DRoutineDesc existing;
                if (functions.TryGetValue(full_name, out existing))
                {
                    if (existing.DeclaringType.Equals(declaringType))
                    {
                        Debug.Assert(existing is ClrMethodDesc, "CLR module should contain CLR methods only");

                        // an overload of existing CLR function:
                        clr_function = existing.ClrMethod;
                    }
                    else
                    {
                        // ambiguous:
                        clr_function = null;
                        existing.MemberAttributes |= PhpMemberAttributes.Ambiguous;
                    }
                }
                else
                {
                    // new entry:
                    clr_function = new ClrMethod(qualified_name.Name, declaringType, Enums.GetMemberAttributes(real_function), 1,
                            real_function.ContainsGenericParameters);

                    functions.Add(full_name, clr_function.ClrMethodDesc);
                }

                if (clr_function != null)
                {
                    ClrMethod.Overload overload;
                    clr_function.AddOverload(real_function, out overload);
                }
            }
        }

        internal static void ReflectConstants(Assembly/*!*/ realAssembly, DModule/*!*/ declaringModule,
            DualDictionary<string, DConstantDesc>/*!*/ constants)
        {
            Debug.Assert(realAssembly != null && constants != null);

            foreach (FieldInfo real_field in ReflectionUtils.GetGlobalFields(realAssembly, BindingFlags.Public | BindingFlags.Static))
            {
                if (real_field.IsLiteral && !real_field.IsSpecialName)
                {
                    string full_name = QualifiedName.FromClrNotation(real_field.Name, true).ToString();

                    DConstantDesc existing;
                    if (constants.TryGetValue(full_name, out existing))
                    {
                        // can be already loaded from different module (CRL or CLib):
                        existing.MemberAttributes |= PhpMemberAttributes.Ambiguous;
                    }
                    else
                    {
                        object value = real_field.GetValue(null);
                        DConstantDesc const_desc = new DConstantDesc(declaringModule, PhpMemberAttributes.Public | PhpMemberAttributes.Static, value);
                        constants.Add(full_name, const_desc, false);
                    }
                }
            }
        }

        #endregion
    }

    #endregion

    #region PluginModule

    public sealed class PluginModule : DModule
    {
        public PluginAssembly/*!*/ PluginAssembly { get { return (PluginAssembly)assembly; } }

        #region Construction

        /// <summary>
        /// Called by the loader via <see cref="PluginAssembly"/>.
        /// </summary>
        internal PluginModule(PluginAssembly/*!*/ assembly)
            : base(assembly)
        {

        }

        #endregion

        #region Reflection

        public override void Reflect(bool full,
            Dictionary<string, DTypeDesc>/*!*/ types,
            Dictionary<string, DRoutineDesc>/*!*/ functions,
            DualDictionary<string, DConstantDesc>/*!*/ constants)
        {
            // PluginAssembly does not contain any declarations

            // Let the plugin to modify ApplicationContext
            var attrs = PluginAssemblyAttribute.Reflect(PluginAssembly.RealAssembly);
            if (attrs != null)
                foreach (var plug in attrs)
                {
                    var method = plug.LoaderType.GetMethod(PluginAssembly.LoaderMethod, BindingFlags.Public | BindingFlags.Static, null, PluginAssembly.LoaderMethodParameters, null);
                    if (method != null)
                        method.Invoke(null, new object[] { PluginAssembly.ApplicationContext });
                }
        }

        #endregion
    }

    #endregion

    #region UnknownModule

    public sealed class UnknownModule : DModule
    {
        #region Members

        internal static UnknownModule/*!*/ RuntimeModule;

        static void UnknowModule()
        {
            // the field may be initialized by DTypeDesc .cctor as it is needed for creating predefined type desc:
            if (RuntimeModule == null) RuntimeModule = new UnknownModule();
        }

        internal UnknownModule()
            : base()
        {
        }

        public override void Reflect(bool full,
            Dictionary<string, DTypeDesc>/*!*/ types,
            Dictionary<string, DRoutineDesc>/*!*/ functions,
            DualDictionary<string, DConstantDesc>/*!*/ constants)
        {
            Debug.Fail();
        }

        #endregion
    }

    #endregion

    #region ScriptModule

    /// <summary>
    /// Represents a script virtual module.
    /// </summary>
    public partial class ScriptModule
    {
        #region Statics

        /// <summary>
        /// Main helper argument types.
        /// </summary>
        internal static readonly Type[] MainHelperArgTypes = new Type[] 
    { 
      typeof(ScriptContext),              // context 
      typeof(Dictionary<string, object>), // variables
      typeof(DObject),                    // self
      typeof(DTypeDesc),                  // includer
      typeof(bool)                        // request
    };

        /// <summary>
        /// Value returned from script's Main() method if no return value is specified.
        /// </summary>
        internal const int DefaultMainReturnValue = 1;

        /// <summary>
        /// The name of the main helper method (containing global code). 
        /// </summary>
        internal const string MainHelperName = "<Main>";

        #endregion
    }

    #endregion
}