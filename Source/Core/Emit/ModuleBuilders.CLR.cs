/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Core.Emit
{
	#region PureModuleBuilder

	public sealed class PureModuleBuilder : PureModule, IPhpModuleBuilder
	{
		public PureCompilationUnit/*!*/ PureCompilationUnit { get { return (PureCompilationUnit)CompilationUnit; } }

		public PhpAssemblyBuilderBase/*!*/ AssemblyBuilder { get { return assemblyBuilder; } }

		public PureAssemblyBuilder/*!*/ PureAssemblyBuilder { get { return assemblyBuilder; } }
		private readonly PureAssemblyBuilder/*!*/ assemblyBuilder;

		public MethodBuilder DeclareHelperBuilder { get { return declareHelperBuilder; } }
		private MethodBuilder declareHelperBuilder;

		public PureModuleBuilder(PureCompilationUnit/*!*/ compilationUnit, PureAssemblyBuilder/*!*/ assemblyBuilder)
			: base(compilationUnit, assemblyBuilder.PureAssembly)
		{
			this.assemblyBuilder = assemblyBuilder;
			DefineBuilders();
		}

		#region Helpers

		public TypeBuilder/*!*/ DefineRealType(string/*!*/ fullName, TypeAttributes attributes)
		{
			return assemblyBuilder.RealModuleBuilder.DefineType(fullName, attributes);
		}

		public MethodInfo/*!*/ DefineRealFunction(string/*!*/ name, MethodAttributes attributes, Type/*!*/ returnType, Type[]/*!*/ parameterTypes)
		{
			Debug.Assert((attributes & MethodAttributes.Static) != 0, "Only static functions can be defined by DefineRealFunction");
			return assemblyBuilder.RealModuleBuilder.DefineGlobalMethod(name, attributes, returnType, parameterTypes);
		}

		public ILEmitter CreateGlobalCodeEmitter()
		{
			// no emitter for global code
			return null;
		}

		internal void DefineBuilders()
		{
			declareHelperBuilder = PureAssemblyBuilder.GlobalTypeEmitter.TypeBuilder.DefineMethod(
				Name.DeclareHelperName.Value, MethodAttributes.Assembly | MethodAttributes.Static,
				Types.Void, new Type[] { typeof(ApplicationContext) });

			// sets the type builder to null, which makes functions and global constants defined globally on the module:
			this.globalType.TypeDesc.DefineBuilder((TypeBuilder)null);
		}

		internal void EmitHelpers()
		{
			EmitDeclareHelper();
		}

		/// <summary>
		/// Emits helper declaring all single-declared functions and classes in the script being built.
		/// </summary>
		/// <remarks>
		/// For each function and class emits a call to <see cref="ApplicationContext.DeclareFunction"/> and 
        /// <see cref="ApplicationContext.DeclareType"/>, respectively, which declares it.
		/// The helper is called as the first instruction of Main helper. 
		/// </remarks>		
		private void EmitDeclareHelper()
		{
			PureCompilationUnit unit = this.PureCompilationUnit;
			ILEmitter il = new ILEmitter(declareHelperBuilder);
			IndexedPlace app_context_place = new IndexedPlace(PlaceHolder.Argument, 0);
            TypeBuilder publicsContainer = null;    // container type for public stubs of global declarations (which are inaccessible from other assemblies)

			foreach (PhpFunction function in unit.GetDeclaredFunctions())
			{
				if (function.IsDefinite)
				{
					app_context_place.EmitLoad(il);

					// NEW RoutineDelegate(<static method>);
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Ldftn, function.ArgLessInfo);
					il.Emit(OpCodes.Newobj, Constructors.RoutineDelegate);

					// LOAD <full name>;
					il.Emit(OpCodes.Ldstr, function.FullName);

					// LOAD <attributes>;
					il.LdcI4((int)function.MemberDesc.MemberAttributes);

                    // LOAD <argfull>
                    if (function.ArgFullInfo != null)
                        CodeGenerator.EmitLoadMethodInfo(
                            il,
                            (function.ArgFullInfo.DeclaringType != null)
                                ? function.ArgFullInfo
                                : EmitPhpFunctionPublicStub(ref publicsContainer, function) // function.ArgFullInfo is real global method not accessible from other assemblies, must be wrapped
                            /*, AssemblyBuilder.DelegateBuilder*/);
                    else
                        il.Emit(OpCodes.Ldnull);
                    
					// CALL <application context>.DeclareFunction(<stub>, <name>, <member attributes>, <argfull>)
					il.Emit(OpCodes.Call, Methods.ApplicationContext.DeclareFunction);
				}
			}

			foreach (PhpType type in unit.GetDeclaredTypes())
			{
				if (type.IsDefinite)
				{
					// CALL <application context>.DeclareType(<type desc>, <name>);
					type.EmitAutoDeclareOnApplicationContext(il, app_context_place);
				}
			}

			foreach (GlobalConstant constant in unit.GetDeclaredConstants())
			{
				if (constant.IsDefinite)
				{
					app_context_place.EmitLoad(il);

					// CALL <application context>.DeclareConstant(<name>, <value>);
					il.Emit(OpCodes.Ldstr, constant.FullName);
                    //il.Emit(OpCodes.Ldsfld, constant.RealField);
                    //if (constant.RealField.FieldType.IsValueType) il.Emit(OpCodes.Box, constant.RealField.FieldType);
                    il.LoadLiteralBox(constant.Value);
					il.Emit(OpCodes.Call, Methods.ApplicationContext.DeclareConstant);
				}
			}

			il.Emit(OpCodes.Ret);

            // complete the publicsContainer type, if created:
            if (publicsContainer != null)
                publicsContainer.CreateType();
		}

        /// <summary>
        /// Emit publically accessible stub that just calls argfull of <paramref name="function"/>.
        /// </summary>
        /// <returns><see cref="MethodInfo"/> of newly created function stub.</returns>
        private MethodInfo/*!*/EmitPhpFunctionPublicStub(ref TypeBuilder publicsContainer, PhpFunction/*!*/function)
        {
            Debug.Assert(function != null);
            Debug.Assert(function.ArgFullInfo != null, "!function.ArgFullInfo");

            if (publicsContainer == null)
            {
                publicsContainer = PureAssemblyBuilder.RealModuleBuilder.DefineType(
                    string.Format("{1}<{0}>",
                        StringUtils.ToClsCompliantIdentifier(Path.ChangeExtension(PureAssemblyBuilder.FileName, "")),
                        QualifiedName.Global.ToString()),
                    TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.SpecialName);
            }

            Type returnType;
            var parameterTypes = function.Signature.ToArgfullSignature(1, out returnType);
            parameterTypes[0] = Types.ScriptContext[0];

            var mi = publicsContainer.DefineMethod(function.GetFullName(), MethodAttributes.Public | MethodAttributes.Static, returnType, parameterTypes);
            var il = new ILEmitter(mi);

            // load arguments
            for (int i = 0; i < parameterTypes.Length; i++)
            {
                if (function.Builder != null)
                    mi.DefineParameter(i + 1, ParameterAttributes.None, function.Builder.ParameterBuilders[i].Name);

                il.Ldarg(i);
            }
            
            // call function.ArgFullInfo
            il.Emit(OpCodes.Call, function.ArgFullInfo);
            
            // .ret
            il.Emit(OpCodes.Ret);

            //
            return mi;
        }

        #endregion
	}

	#endregion

	#region ScriptBuilder

	/// <summary>
	/// Provides means for building scripts.
	/// </summary>
	public sealed partial class ScriptBuilder : ScriptModule, IPhpModuleBuilder
	{
		#region Fields and Properties

		public TypeBuilder ScriptTypeBuilder { get { return (TypeBuilder)scriptInfo.Script; } }
		PhpAssemblyBuilderBase/*!*/ IPhpModuleBuilder.AssemblyBuilder { get { return assemblyBuilder; } }

		public ScriptAssemblyBuilder/*!*/ AssemblyBuilder { get { return assemblyBuilder; } }
		private readonly ScriptAssemblyBuilder/*!*/ assemblyBuilder;

		/// <summary>
		/// Gets the Main helper/Main static method builder.
		/// </summary>
		public MethodBuilder MainHelperBuilder { get { return (MethodBuilder)MainHelper; } }

		/// <summary>
		/// Gets declare helper builder.
		/// </summary>
		internal MethodBuilder DeclareHelperBuilder { get { return declareHelper; } }
		private MethodBuilder declareHelper;

		/// <summary>
		/// Timestamp of the source file when the script builder is created.
		/// </summary>
		public DateTime SourceTimestamp { get { return SourceTimestamp; } }
		private DateTime sourceTimestamp;

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new script builder.
		/// </summary>
		/// <param name="unit">Compilation unit.</param>
		/// <param name="assemblyBuilder">Script assembly builder.</param>
		/// <param name="subnamespace">The script's subnamespace ending with a type delimiter or a <B>null</B> reference.</param>
		/// <returns>New instance.</returns>
		public ScriptBuilder(ScriptCompilationUnit/*!*/ unit, ScriptAssemblyBuilder/*!*/ assemblyBuilder, string subnamespace)
			: base(unit, assemblyBuilder.ScriptAssembly, subnamespace)
		{
			Debug.Assert(unit != null && assemblyBuilder != null);

			this.assemblyBuilder = assemblyBuilder;

			// remembers a timestamp of the source file:
			this.sourceTimestamp = File.GetLastWriteTime(unit.SourceUnit.SourceFile.FullPath);

			DefineBuilders(subnamespace);
		}

		#endregion

		#region Helpers

		public ILEmitter/*!*/ CreateGlobalCodeEmitter()
		{
			return new ILEmitter(MainHelperBuilder);
		}

		public TypeBuilder/*!*/ DefineRealType(string/*!*/ fullName, TypeAttributes attributes)
		{
			return assemblyBuilder.RealModuleBuilder.DefineType(UserTypesNamespace + fullName, attributes);
		}

		public MethodInfo/*!*/ DefineRealFunction(string/*!*/ name, MethodAttributes attributes, Type/*!*/ returnType, Type[]/*!*/ parameterTypes)
		{
			Debug.Assert((attributes & MethodAttributes.Static) != 0, "Only static functions can be defined by DefineRealFunction");
			return ScriptTypeBuilder.DefineMethod(name, attributes, returnType, parameterTypes);
		}

		private void DefineBuilders(string subnamespace)
		{
			// defines script type (implements IPhpScript marking interface):
			TypeBuilder script_builder = assemblyBuilder.RealModuleBuilder.DefineType(
				assemblyBuilder.ScriptAssembly.GetQualifiedScriptTypeName(subnamespace),
				 TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.SpecialName,
				Types.Object[0],
				new Type[] { typeof(IPhpScript) });

			// prevents scripts instantiation:
			script_builder.DefineDefaultConstructor(MethodAttributes.PrivateScope);

            this.scriptInfo = new ScriptInfo(script_builder, DefineMainHelper(script_builder));

			this.declareHelper = DefineDeclareHelper(script_builder);

			// associates the script type builder with the module's global type so that 
			// functions and global constants will be defined on the script type builder:
			this.globalType.TypeDesc.DefineBuilder(script_builder);
		}

		/// <summary>
		/// Defines script type members - helpers and constructors.
		/// </summary>
		private MethodBuilder/*!*/ DefineMainHelper(TypeBuilder/*!*/ builder)
		{
			// public static object <Main>(ScriptContext context,IDictionary variables, DObject self, DTypeDesc includer, bool request);
			MethodBuilder result = builder.DefineMethod(
				MainHelperName,
			MethodAttributes.Public | MethodAttributes.Static | MethodAttributes.SpecialName,
			typeof(object),
				MainHelperArgTypes);

			// gives arguments names (for comfortable debugging):
            result.DefineParameter(1, ParameterAttributes.None, PluginHandler.ConvertParameterName(PhpRoutine.ContextParamName));
            result.DefineParameter(2, ParameterAttributes.None, PluginHandler.ConvertParameterName(PhpRoutine.LocalVariablesTableName));
            result.DefineParameter(3, ParameterAttributes.None, PluginHandler.ConvertParameterName("<self>"));
            result.DefineParameter(4, ParameterAttributes.None, PluginHandler.ConvertParameterName("<includer>"));
            result.DefineParameter(5, ParameterAttributes.None, PluginHandler.ConvertParameterName("<request>"));

			return result;
		}

		private MethodBuilder/*!*/ DefineDeclareHelper(TypeBuilder/*!*/ builder)
		{
			// public static void <Declare>(ScriptContext context);
			MethodBuilder result = builder.DefineMethod(
		DeclareHelperNane,
		MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.SpecialName,
		Types.Void,
				DeclareHelperArgTypes);

			return result;
		}

		internal void EmitHelpers()
		{
			CompilationUnit unit = this.CompilationUnit;
			ILEmitter il = new ILEmitter(DeclareHelperBuilder);
			IndexedPlace script_context_place = new IndexedPlace(PlaceHolder.Argument, 0);

			foreach (PhpFunction function in unit.GetDeclaredFunctions())
			{
				if (function.IsDefinite)
				{
                    CodeGenerator.EmitDeclareFunction(il, script_context_place, function);
				}
			}

			foreach (PhpType type in unit.GetDeclaredTypes())
			{
				if (type.IsDefinite)
				{
					// CALL <context>.DeclareType(<type desc>, <name>);
					type.EmitAutoDeclareOnScriptContext(il, script_context_place);
				}
                else if (!type.IsComplete)
                {
                    if (type.IncompleteClassDeclareMethodInfo != null)
                    {
                        // check whether base class is known at this point of execution,
                        // if so, declare this incomplete class immediately. As PHP does.

                        type.EmitDeclareIncompleteOnScriptContext(il, script_context_place);
                    }
                }
			}

            foreach (GlobalConstant constant in unit.GetDeclaredConstants())
            {
                if (constant.IsDefinite)
                {
                    var field = constant.RealField;
                    Debug.Assert(field != null);
                    Debug.Assert(field.IsStatic);

                    // CALL <context>.DeclareConstant(<name>, <value>);
                    script_context_place.EmitLoad(il);

                    il.Emit(OpCodes.Ldstr, constant.FullName);
                    il.LoadLiteralBox(constant.Value);  //il.Emit(OpCodes.Ldsfld, field);   // const field cannot be referenced in IL
                    il.Emit(OpCodes.Call, Methods.ScriptContext.DeclareConstant);
                }
            }

			il.Emit(OpCodes.Ret);
		}

		internal void Bake()
		{
			scriptInfo = new ScriptInfo(ScriptTypeBuilder.CreateType(), null);
			//FastReflect();
		}

		#endregion

		#region LEGACY: Attributes

		///// <summary>
		///// Adds meta-information about a static script inclusion into the script being built by this script builder.
		///// </summary>
		///// <param name="inclusion">Contains information about the inclusion.</param>
		//public void AddInclusionMetadata(StaticInclusion/*!*/ inclusion)
		//{
		//  // TODO:
		//  // Debug.Assert(!IsTransient(),"Inclusion metadata shouldn't be added in transient scripts.");

		//  //// adds an attribute to the module:
		//  //RelativePath rp = inclusion.Includee.SourceFile.RelativePath;
		//  //CustomAttributeBuilder ca = new CustomAttributeBuilder(Constructors.Includes, new object[] 
		//  //  { rp.Path, rp.Level, inclusion.IsConditional, Reflection.Enums.IsOnceInclusion(inclusion.InclusionType) });

		//  //ScriptTypeBuilder.SetCustomAttribute(ca);
		//}

		///// <summary>
		///// Marks the script type with attributes.
		///// Adds <see cref="PhpEvalIdAttribute"/> is added if the assembly being built is transient.
		///// Adds <see cref="ScriptAttribute"/> otherwise.
		///// </summary>
		//private void AnnotateScriptType()
		//{
		//  // TODO:
		//  //if (IsTransient())
		//  //{ 
		//  //  // eval id of transient code has to be valid:
		//  //  Debug.Assert(evalId!=EvalCompilerManager.InvalidEvalId);

		//  //  CustomAttributeBuilder cab = new CustomAttributeBuilder(Constructors.PhpEvalId,
		//  //    new object[] { evalId }); 

		//  //  ScriptTypeBuilder.SetCustomAttribute(cab);
		//  //}  
		//  //else
		//  //{
		//  //  CustomAttributeBuilder cab = new CustomAttributeBuilder(Constructors.Script, 
		//  //    new object[] { sourceTimestamp.ToFileTime() }); 

		//  //  ScriptTypeBuilder.SetCustomAttribute(cab);
		//  //}    
		//}

		#endregion

        [Flags]
        internal enum ScriptAttributes
        {
            /// <summary>
            /// Time stamp and file name.
            /// </summary>
            Script = 1,

            /// <summary>
            /// List of Scripts that statically include this Script.
            /// </summary>
            ScriptIncluders = 2,

            /// <summary>
            /// List of Scripts that are statically included by this Script.
            /// </summary>
            ScriptIncludees = 4,

            /// <summary>
            /// List of PHP types fully and statically declared by this Script.
            /// </summary>
            ScriptDeclares = 8,

            /// <summary>
            /// All the available info is emitted.
            /// </summary>
            All = Script | ScriptIncluders | ScriptIncludees | ScriptDeclares
        }

        /// <summary>
        /// Emit the Script attribute with includes,includers,relativePath and timeStamp info.
        /// </summary>
        /// <param name="emitAttributes">Specifies single infos to emit.</param>
		internal void SetScriptAttribute(ScriptAttributes emitAttributes)
		{
            // module to resolve type tokens from:
            ModuleBuilder real_builder = this.AssemblyBuilder.RealModuleBuilder;

            // [Script(timeStamp, relativePath)]
            if ((emitAttributes & ScriptAttributes.Script) != 0)
            {
                // construct the [Script] attribute:
                CustomAttributeBuilder cab = new CustomAttributeBuilder(Constructors.Script, new object[] { sourceTimestamp.Ticks, CompilationUnit.RelativeSourcePath });
                ScriptTypeBuilder.SetCustomAttribute(cab);
            }

            // [ScriptIncluders(int[])]
            if ((emitAttributes & ScriptAttributes.ScriptIncluders) != 0 && CompilationUnit.Includers.Count > 0)
            {
                // determine includers type token, remove duplicities:
                int[] includers = ArrayUtils.Unique(Array.ConvertAll(CompilationUnit.Includers.ToArray(), x => real_builder.GetTypeToken(x.Includer.ScriptBuilder.ScriptType).Token)).ToArray();
            
                // construct the [ScriptIncluders] attribute:
                CustomAttributeBuilder cab = new CustomAttributeBuilder(Constructors.ScriptIncluders, new object[] { includers });
                ScriptTypeBuilder.SetCustomAttribute(cab);
            }

            // [ScriptIncludees(int[],byte[])]
            if ((emitAttributes & ScriptAttributes.ScriptIncludees) != 0 && CompilationUnit.Inclusions.Count > 0)
            {
                // determine inclusions type token, group by the token to remove duplicities:
                var inclusionsGroup = ArrayUtils.Group(CompilationUnit.Inclusions.ToArray(), x => real_builder.GetTypeToken(x.Includee.ScriptClassType).Token);
                // determine if single includees are at least once included unconditionally:
                int[] inclusions = new int[inclusionsGroup.Count];
                bool[] inclusionsConditionalFlag = new bool[inclusions.Length];

                int i = 0;
                foreach(var includee in inclusionsGroup)
                {
                    // find any unconditional inclusion to mark this unified inclusion as unconditional
                    inclusionsConditionalFlag[i] = ArrayUtils.LogicalAnd(includee.Value, x => x.IsConditional);
                    //
                    inclusions[i] = includee.Key;
                    ++i;
                }

                // construct the [ScriptIncluders] attribute:
                CustomAttributeBuilder cab = new CustomAttributeBuilder(Constructors.ScriptIncludees, new object[] { inclusions, ScriptIncludeesAttribute.ConvertBoolsToBits(inclusionsConditionalFlag) });
                ScriptTypeBuilder.SetCustomAttribute(cab);
            }

            // [ScriptDeclares(int[])]
            if ((emitAttributes & ScriptAttributes.ScriptDeclares) != 0)
            {
                List<int> declaredTypesToken = new List<int>();

                foreach (PhpType type in CompilationUnit.GetDeclaredTypes())
                {
                    if (type.IsComplete && type.RealType != null)
                    {
                        declaredTypesToken.Add(real_builder.GetTypeToken(type.RealType).Token);
                    }
                }

                if (declaredTypesToken.Count > 0)
                {
                    // construct the [ScriptDeclares] attribute:
                    CustomAttributeBuilder cab = new CustomAttributeBuilder(Constructors.ScriptDeclares, new object[] { declaredTypesToken.ToArray() });
                    ScriptTypeBuilder.SetCustomAttribute(cab);
                }
            }
		}
	}

	#endregion
}
