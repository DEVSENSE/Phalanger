/*

 Copyright (c) 2004-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Threading;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Serialization;
using PHP.Core.Reflection;
using System.Collections.Generic;
using PHP.Core.AST;

namespace PHP.Core.Emit
{
	/// <summary>
	/// Utilities for emitting PHP classes.
	/// </summary>
	public static class PhpObjectBuilder
	{
		/// <summary>
		/// Used by the <c>WrapperGen</c> to generate the <see cref="PopulateTypeDescMethodName"/> method.
		/// </summary>
		public struct InfoWithAttributes<T> where T : MemberInfo
		{
			public InfoWithAttributes(T info, PhpMemberAttributes attributes)
			{
				this.Info = info;
				this.Attributes = attributes;
			}

			public T Info;
			public PhpMemberAttributes Attributes;
		}

		#region Helper member names

		/// <summary>
		/// Name of the method that initializes (thread) static fields.
		/// </summary>
		/// <remarks>This name does not have the &lt;x&gt; format in order to be a valid C# identifier.</remarks>
		public const string StaticFieldInitMethodName = "__InitializeStaticFields";

		/// <summary>
		/// Name of the method that populates the typedesc corresponding to the type.
		/// </summary>
		/// <remarks>This name does not have the &lt;x&gt; format in order to be a valid C# identifier.</remarks>
		public const string PopulateTypeDescMethodName = "__PopulateTypeDesc";

		/// <summary>
		/// Name of the method that initializes instance fields.
		/// </summary>
		public const string InstanceFieldInitMethodName = "<InitializeInstanceFields>";

		/// <summary>
		/// Name of the field that contains reference to the corresponding <see cref="PhpTypeDesc"/>.
		/// </summary>
		public const string TypeDescFieldName = "<typeDesc>";

		/// <summary>
		/// Name of the field that contains reference to the corresponding <see cref="ClrObject"/>.
		/// </summary>
		public const string ProxyFieldName = "<proxy>";

		#endregion

		#region Constructors

		internal const MethodAttributes DefaultConstructorAttributes = MethodAttributes.Public | MethodAttributes.SpecialName |
			MethodAttributes.RTSpecialName;

		/// <summary>
		/// Attributes used while creating the short constructor of the class.
		/// </summary>
		internal const MethodAttributes ShortConstructorAttributes = MethodAttributes.Public | MethodAttributes.SpecialName |
			MethodAttributes.RTSpecialName;

		/// <summary>
		/// Parameter types of the short constructor.
		/// </summary>
		internal static readonly Type[] ShortConstructorParamTypes = Types.ScriptContext_Bool;

		/// <summary>
		/// Attributes used while creating the long constructor of the class.
		/// </summary>
		internal const MethodAttributes LongConstructorAttributes = MethodAttributes.Public | MethodAttributes.SpecialName |
			MethodAttributes.RTSpecialName;

		/// <summary>
		/// Parameter types of the long constructor.
		/// </summary>
		internal static readonly Type[] LongConstructorParamTypes = Types.ScriptContext_DTypeDesc;

		/// <summary>
		/// Attributes used while creating the deserializing constructor of the class.
		/// </summary>
		internal const MethodAttributes DeserializingConstructorAttributes = MethodAttributes.Family |
			MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;

#if !SILVERLIGHT
		/// <summary>
		/// Parameter types of the deserializing constructor.
		/// </summary>
		internal static readonly Type[] DeserializingConstructorParamTypes = Types.SerializationInfo_StreamingContext;
#endif

		/// <summary>
		/// Emits constructors into a class.
		/// </summary>
		internal static void EmitClassConstructors(PhpType/*!*/ phpType)
		{
#if !SILVERLIGHT
			EmitDeserializingConstructor(phpType);
#endif
			EmitShortConstructor(phpType);
			EmitLongConstructor(phpType);

            EmitFinalizer(phpType);

			// emit CLR-friendly constructors based on the PHP constructor method effective for the type
			if (phpType.IsExported)
				EmitExportedConstructors(phpType);
		}

        /// <summary>
        /// Emit the PhpType finalizer. The finalizer is emitted only if there is __destruct() function
        /// and there is no finalizer in any base class already. The finalizer calls this.Dispose() which
        /// calls __destruct() function directly.
        /// </summary>
        /// <param name="phpType"></param>
        private static void EmitFinalizer(PhpType/*!*/phpType)
        {
            // only if __destruct was now defined in some base class, no need to override existing definition on Finalize
            DRoutine basedestruct;
            DRoutineDesc destruct;
            if ((destruct = phpType.TypeDesc.GetMethod(DObject.SpecialMethodNames.Destruct)) != null && (phpType.Base == null ||
                phpType.Base.GetMethod(DObject.SpecialMethodNames.Destruct, phpType, out basedestruct) == GetMemberResult.NotFound))
            {
                MethodBuilder finalizer_builder = phpType.RealTypeBuilder.DefineMethod("Finalize", MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Family, typeof(void), Type.EmptyTypes);
                
                ILEmitter dil = new ILEmitter(finalizer_builder);

                // exact Finalize() method pattern follows:

                // try
                dil.BeginExceptionBlock();

                // this.Dispose(false)
                dil.Emit(OpCodes.Ldarg_0);
                dil.Emit(OpCodes.Ldc_I4_0);
                dil.Emit(OpCodes.Callvirt, PHP.Core.Emit.Methods.DObject_Dispose);

                // finally
                dil.BeginFinallyBlock();

                // Object.Finalize()
                dil.Emit(OpCodes.Ldarg_0);
                dil.Emit(OpCodes.Call, PHP.Core.Emit.Methods.Object_Finalize);

                dil.EndExceptionBlock();

                dil.Emit(OpCodes.Ret);
            }
        }

#if !SILVERLIGHT
		/// <summary>
		/// Emits deserializing (SerializiationInfo, StreamingContext) constructor.
		/// </summary>
		private static void EmitDeserializingConstructor(PhpType/*!*/ phpType)
		{
			// (SerializationInfo, StreamingContext) constructor
			ConstructorBuilder ctor_builder = phpType.DeserializingConstructorBuilder;

			if (ctor_builder != null)
			{
				ILEmitter cil = new ILEmitter(ctor_builder);

				if (phpType.Base == null) EmitInvokePhpObjectDeserializingConstructor(cil);
				else phpType.Base.EmitInvokeDeserializationConstructor(cil, phpType, null);

				// [ __InitializeStaticFields(context) ]
				cil.Emit(OpCodes.Call, Methods.ScriptContext.GetCurrentContext);

				cil.EmitCall(OpCodes.Call, phpType.StaticFieldInitMethodInfo, null);
				cil.Emit(OpCodes.Ret);
			}
		}
#endif

		/// <summary>
		/// Emits (ScriptContext, bool) constructor.
		/// </summary>
		private static void EmitShortConstructor(PhpType/*!*/ phpType)
		{
			// (ScriptContext,bool) constructor
			ConstructorBuilder ctor_builder = phpType.ShortConstructorBuilder;

			ctor_builder.DefineParameter(1, ParameterAttributes.None, "context");
			ctor_builder.DefineParameter(2, ParameterAttributes.None, "newInstance");
			ILEmitter cil = new ILEmitter(ctor_builder);

			// invoke base constructor
			if (phpType.Base == null) EmitInvokePhpObjectConstructor(cil);
			else phpType.Base.EmitInvokeConstructor(cil, phpType, null);

			// perform fast DObject.<typeDesc> init if we are in its subclass
			if (phpType.Root is PhpType)
			{
				// [ if (GetType() == typeof(self)) this.typeDesc = self.<typeDesc> ]
				cil.Ldarg(FunctionBuilder.ArgThis);
				cil.Emit(OpCodes.Call, Methods.Object_GetType);
				cil.Emit(OpCodes.Ldtoken, phpType.RealTypeBuilder);
				cil.Emit(OpCodes.Call, Methods.GetTypeFromHandle);

				Label label = cil.DefineLabel();
				cil.Emit(OpCodes.Bne_Un_S, label);
                if (true)
                {
                    cil.Ldarg(FunctionBuilder.ArgThis);
                    cil.Emit(OpCodes.Ldsfld, phpType.TypeDescFieldInfo);
                    cil.Emit(OpCodes.Stfld, Fields.DObject_TypeDesc);
                    cil.MarkLabel(label);
                }
			}

			// register this instance for finalization if it introduced the __destruct method
			DRoutine destruct;
			if (phpType.TypeDesc.GetMethod(DObject.SpecialMethodNames.Destruct) != null && (phpType.Base == null ||
				phpType.Base.GetMethod(DObject.SpecialMethodNames.Destruct, phpType, out destruct) == GetMemberResult.NotFound))
			{
				cil.Ldarg(FunctionBuilder.ArgContextInstance);
				cil.Ldarg(FunctionBuilder.ArgThis);

				if (phpType.ProxyFieldInfo != null) cil.Emit(OpCodes.Ldfld, phpType.ProxyFieldInfo);

				cil.Emit(OpCodes.Call, Methods.ScriptContext.RegisterDObjectForFinalization);
			}

			// [ <InitializeInstanceFields>(arg1) ]
			cil.Ldarg(FunctionBuilder.ArgThis);
			cil.Ldarg(FunctionBuilder.ArgContextInstance);
			cil.EmitCall(OpCodes.Call, phpType.Builder.InstanceFieldInit, null);

			// [ __InitializeStaticFields(arg1) ]
			cil.Ldarg(FunctionBuilder.ArgContextInstance);
			cil.EmitCall(OpCodes.Call, phpType.StaticFieldInitMethodInfo, null);

			cil.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Emits (ScriptContext, DTypeDesc) constructor.
		/// </summary>
		private static void EmitLongConstructor(PhpType/*!*/ phpType)
		{
			// (ScriptContext, DTypeDesc) constructor
			ConstructorBuilder ctor_builder = phpType.LongConstructorBuilder;

			ctor_builder.DefineParameter(1, ParameterAttributes.None, "context");
			ctor_builder.DefineParameter(2, ParameterAttributes.None, "caller");

			// [ this(arg1,true) ]
			ILEmitter cil = new ILEmitter(ctor_builder);
			cil.Ldarg(FunctionBuilder.ArgThis);
			cil.Ldarg(FunctionBuilder.ArgContextInstance);
			cil.LdcI4(1);
			cil.Emit(OpCodes.Call, phpType.ShortConstructorInfo);

			if (phpType.ProxyFieldInfo != null)
			{
				// [ <proxy>.InvokeConstructor(args) ]
				cil.Ldarg(FunctionBuilder.ArgThis);
				cil.Emit(OpCodes.Ldfld, phpType.ProxyFieldInfo);
				cil.Ldarg(1);
				cil.Ldarg(2);
				cil.Emit(OpCodes.Call, Methods.DObject_InvokeConstructor);
			}
			else
			{
                // try to find constructor method and call it directly
                // if it is publically visible without any reason to throw a warning in runtime
                
                DRoutineDesc construct = null; // = found constructor; if not null, can be called statically without runtime checks
                bool constructorFound = false;

                // try to find constructor
                for (DTypeDesc type_desc = phpType.TypeDesc; type_desc != null; type_desc = type_desc.Base)
                {
                    construct = type_desc.GetMethod(DObject.SpecialMethodNames.Construct);
                    if (construct == null)
                        construct = type_desc.GetMethod(new Name(type_desc.MakeSimpleName()));

                    if (construct != null)
                    {
                        constructorFound = true;

                        if (!construct.IsPublic || construct.IsStatic || construct.PhpRoutine == null || construct.PhpRoutine.ArgLessInfo == null)
                            construct = null; // invalid constructor found, fall back to dynamic behavior
                        
                        break;
                    }
                }

                // emit constructor call
                if (construct != null)
                {
                    // publically visible not static constructor, can be called statically anywhere

                    // [ __construct( this, context.Stack ) ]
                    cil.Ldarg(FunctionBuilder.ArgThis);                         // this
                    cil.Ldarg(1);
                    cil.Emit(OpCodes.Ldfld, Emit.Fields.ScriptContext_Stack);   // context.Stack
                    cil.Emit(OpCodes.Call, construct.PhpRoutine.ArgLessInfo);   // __construct
                    cil.Emit(OpCodes.Pop);
                }
                else if (!constructorFound)  // there is no ctor at all
                {
                    // [ context.Stack.RemoveFrame() ]
                    cil.Ldarg(1);
                    cil.Emit(OpCodes.Ldfld, Emit.Fields.ScriptContext_Stack);       // context.Stack
                    cil.Emit(OpCodes.Callvirt, Emit.Methods.PhpStack.RemoveFrame);  // .RemoveFrame
                }
                else
                {
                    // constructor should be checked in runtime (various visibility cases)
                    // warnings can be displayed

                    // [ InvokeConstructor(arg2) ]
                    cil.Ldarg(FunctionBuilder.ArgThis);
                    cil.Ldarg(1);
                    cil.Ldarg(2);
                    cil.Emit(OpCodes.Call, Methods.DObject_InvokeConstructor);
                }
			}

			cil.Emit(OpCodes.Ret);
		}

		private static void EmitExportedConstructor(PhpType/*!*/ phpType, ConstructorBuilder/*!*/ ctorStubBuilder,
			PhpMethod phpCtor, ParameterInfo[]/*!*/ parameters)
		{
			// set parameter names and attributes
			if (phpCtor != null)
			{
				ClrStubBuilder.DefineStubParameters(
					ctorStubBuilder,
					phpCtor.Builder.Signature.FormalParams,
					parameters);
			}

			// emit ctor body
			ILEmitter cil = new ILEmitter(ctorStubBuilder);

			// [ this(ScriptContext.CurrentContext ]
			cil.Ldarg(FunctionBuilder.ArgThis);
			cil.EmitCall(OpCodes.Call, Methods.ScriptContext.GetCurrentContext, null);

			LocalBuilder sc_local = cil.DeclareLocal(Types.ScriptContext[0]);
			cil.Stloc(sc_local);
			cil.Ldloc(sc_local);

			cil.LdcI4(1);
			cil.Emit(OpCodes.Call, phpType.ShortConstructorInfo);

			if (phpCtor != null)
			{
				// invoke the PHP ctor method
				ClrStubBuilder.EmitMethodStubBody(
					cil,
					new Place(sc_local),
					parameters,
					new GenericTypeParameterBuilder[0],
					Types.Void,
					phpCtor,
					phpCtor.DeclaringType);
			}
			else cil.Emit(OpCodes.Ret);
		}

		private static void EmitExportedConstructors(PhpType/*!*/ phpType)
		{
			PhpMethod ctor = phpType.GetConstructor() as PhpMethod;

			foreach (StubInfo info in phpType.Builder.ClrConstructorStubs)
			{
				EmitExportedConstructor(
					phpType,
					info.ConstructorBuilder,
					ctor,
					info.Parameters);
			}
		}

		/// <summary>
		/// Defines CLR-friendly constructors based on a PHP &quot;constructor&quot; method.
		/// </summary>
		public static void DefineExportedConstructors(PhpType/*!*/ phpType)
		{
			phpType.Builder.ClrConstructorStubs = new List<StubInfo>();
			PhpMethod ctor = phpType.GetConstructor() as PhpMethod;

			if (ctor == null)
			{
				// the class defines (nor inherits) no constructor -> create a parameter-less CLR constructor
				ConstructorBuilder ctor_builder =
					phpType.RealTypeBuilder.DefineConstructor(
						DefaultConstructorAttributes,
						CallingConventions.Standard,
						Type.EmptyTypes);

				phpType.ClrConstructorInfos = new ConstructorInfo[] { ctor_builder };

				phpType.Builder.ClrConstructorStubs.Add(
					new StubInfo(ctor_builder, new ParameterInfo[0], StubInfo.EmptyGenericParameters, null));
			}
            else if (!ctor.IsAbstract)
            {
                Debug.Assert(!ctor.IsStatic && ctor.Signature.GenericParamCount == 0);

                if (ctor.Builder == null)
                {
                    // contructor not defined in this class
                    phpType.ClrConstructorInfos = new ConstructorInfo[0];
                    return;
                }            

                // infer constructor visibility
                List<ConstructorInfo> ctor_infos = new List<ConstructorInfo>();
                
                MethodAttributes attr = Reflection.Enums.ToMethodAttributes(ctor.RoutineDesc.MemberAttributes);

                foreach (StubInfo info in ClrStubBuilder.DefineMethodExportStubs(
                    ctor,
                    attr,
                    true,
                    delegate(string[] genericParamNames, object[] parameterTypes, object returnType)
                    {
                        // accept all overloads
                        return true;
                    }))
                {
                    phpType.Builder.ClrConstructorStubs.Add(info);

                    // infos are returned in ascending order w.r.t. parameter count
                    ctor_infos.Add(info.ConstructorBuilder);
                }

                phpType.ClrConstructorInfos = ctor_infos.ToArray();
            }
		}

#if !SILVERLIGHT
		/// <summary>
		/// Generates the (<see cref="SerializationInfo"/>, <see cref="StreamingContext"/>) constructor.
		/// </summary>
		/// <param name="typeBuilder">The type builder.</param>
		/// <remarks>
		/// Part of the constructor body - containing a call to parent's deserializing constructor - is generated.
		/// At least a <see cref="OpCodes.Ret"/> has to be emitted to make the constructor complete.
		/// </remarks>
		public static ConstructorBuilder DefineDeserializingConstructor(TypeBuilder typeBuilder)
		{
			ConstructorBuilder ctor_builder = typeBuilder.DefineConstructor(DeserializingConstructorAttributes,
				CallingConventions.Standard, DeserializingConstructorParamTypes);

			// define parameter names
			ctor_builder.DefineParameter(1, ParameterAttributes.None, "info");
			ctor_builder.DefineParameter(2, ParameterAttributes.None, "context");

			// call the base type's deserializing constructor
			ILEmitter il = new ILEmitter(ctor_builder);
			il.Ldarg(FunctionBuilder.ArgThis);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);
			il.Emit(OpCodes.Call, typeBuilder.BaseType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance,
				null, DeserializingConstructorParamTypes, null));

			return ctor_builder;
		}
#endif

		/// <summary>
		/// Generates the (<see cref="ScriptContext"/>, <see cref="DTypeDesc"/>) constructor.
		/// </summary>
		/// <param name="typeBuilder">The type builder.</param>
		/// <param name="shortConstructor">This type's short constructor.</param>
		/// <returns>The constructor builder.</returns>
		/// <remarks>
		/// The entire constructor is generated. See <see cref="PHP.Core.PhpObject(PHP.Core.ScriptContext,DTypeDesc)"/>.
		/// </remarks>
		public static ConstructorBuilder GenerateLongConstructor(TypeBuilder typeBuilder, ConstructorInfo shortConstructor)
		{
			ConstructorBuilder ctor_builder = typeBuilder.DefineConstructor(LongConstructorAttributes,
				CallingConventions.Standard, LongConstructorParamTypes);

			// annotate with EditorBrowsable attribute
#if !SILVERLIGHT // Not available on Silverlight
			ctor_builder.SetCustomAttribute(AttributeBuilders.EditorBrowsableNever);
#endif

			// define parameter names
			ctor_builder.DefineParameter(1, ParameterAttributes.None, "context");
			ctor_builder.DefineParameter(2, ParameterAttributes.None, "caller");

			// call this type's short constructor
			ILEmitter il = new ILEmitter(ctor_builder);
			il.Ldarg(FunctionBuilder.ArgThis);
			il.Ldarg(FunctionBuilder.ArgContextInstance);
			il.LdcI4(1);
			il.Emit(OpCodes.Call, shortConstructor);

			// call PhpObject.InvokeConstructor
			il.Ldarg(FunctionBuilder.ArgThis);
			il.Ldarg(1);
			il.Ldarg(2);
			il.Emit(OpCodes.Call, Methods.DObject_InvokeConstructor);

			il.Emit(OpCodes.Ret);

			return ctor_builder;
		}

		/// <summary>
		/// Defines the (<see cref="ScriptContext"/>) constructor.
		/// </summary>
		/// <param name="typeBuilder">The type builder.</param>
		/// <returns>The constructor builder.</returns>
		/// <remarks>
		/// Part of the constructor body - containing a call to parent's short constructor - is generated.
		/// At least an <see cref="OpCodes.Ret"/> has to be emitted to make the constructor complete.
		/// </remarks>
		public static ConstructorBuilder DefineShortConstructor(TypeBuilder typeBuilder)
		{
			ConstructorBuilder ctor_builder = typeBuilder.DefineConstructor(ShortConstructorAttributes,
				CallingConventions.Standard, ShortConstructorParamTypes);

			// annotate with EditorBrowsable attribute
#if !SILVERLIGHT // Not available on Silverlight
			ctor_builder.SetCustomAttribute(AttributeBuilders.EditorBrowsableNever);
#endif

			// define parameter names
			ctor_builder.DefineParameter(1, ParameterAttributes.None, "context");
			ctor_builder.DefineParameter(2, ParameterAttributes.None, "newInstance");

			// call the base type's short constructor
			ILEmitter il = new ILEmitter(ctor_builder);
			il.Ldarg(FunctionBuilder.ArgThis);
			il.Ldarg(FunctionBuilder.ArgContextInstance);
			il.Ldarg(2);
			il.Emit(OpCodes.Call, typeBuilder.BaseType.GetConstructor(ShortConstructorParamTypes));

			return ctor_builder;
		}

		private static void EmitInvokePhpObjectConstructor(ILEmitter/*!*/ il)
		{
			// [ base(arg1,arg2) ]
			il.Ldarg(FunctionBuilder.ArgThis);
			il.Ldarg(FunctionBuilder.ArgContextInstance);
			il.Ldarg(2);

			il.Emit(OpCodes.Call, Constructors.PhpObject.ScriptContext_Bool);
		}

		private static void EmitInvokePhpObjectDeserializingConstructor(ILEmitter/*!*/ il)
		{
#if SILVERLIGHT // Not available on Silverlight
			Debug.Fail("Deserialization not supported!");
			throw new NotSupportedException("Deserialization not supported!");
#else
			// [ base(arg0, arg1, arg2) ]
			il.Ldarg(FunctionBuilder.ArgThis);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldarg_2);

			il.Emit(OpCodes.Call, Constructors.PhpObject.SerializationInfo_StreamingContext);
#endif
		}

		#endregion

		#region Init field helpers

		/// <summary>
		/// Emits init field helpers (<c>__lastContext</c> field, <c>&lt;InitializeInstanceFields&gt;</c>
		/// method and <c>__InitializeStaticFields</c> into a class.
		/// </summary>
		internal static void EmitInitFieldHelpers(PhpType phpType)
		{
            //
            // <InitializeInstanceFields>
            //

			// <InitializeInstanceFields> method - will contain instance field initialization
			phpType.Builder.InstanceFieldInit = phpType.RealTypeBuilder.DefineMethod(
				InstanceFieldInitMethodName,
#if SILVERLIGHT
				MethodAttributes.Public | MethodAttributes.HideBySig,
#else
				MethodAttributes.Private | MethodAttributes.HideBySig,
#endif
				CallingConventions.Standard,
				Types.Void,
				Types.ScriptContext);
			phpType.Builder.InstanceFieldInitEmitter = new ILEmitter(phpType.Builder.InstanceFieldInit);

            // emit custom body prolog:
            PluginHandler.EmitBeforeBody(phpType.Builder.InstanceFieldInitEmitter, null); 
            

            //
            // <InitializeStaticFields>
            //

			// <InitializeStaticFields> method has already been defined during the analysis phase - will contain (thread)
			// static field initialization
			ILEmitter cil = new ILEmitter(phpType.StaticFieldInitMethodBuilder);

            if (phpType.Builder.HasThreadStaticFields)
			{
				// __lastContext thread-static field - will contain the last SC that inited static fields for this thread
				FieldBuilder last_context = phpType.RealTypeBuilder.DefineField(
					"<lastScriptContext>",
					Types.ScriptContext[0],
					FieldAttributes.Private | FieldAttributes.Static);

				// SILVERLIGHT: Not sure what this does & what would be the right behavior...
#if !SILVERLIGHT 
				last_context.SetCustomAttribute(AttributeBuilders.ThreadStatic);
#endif
                // emit custom body prolog:
                PluginHandler.EmitBeforeBody(cil, null); 

                //
                Label init_needed_label = cil.DefineLabel();
				// [ if (arg0 == __lastContext) ret ]
				cil.Emit(OpCodes.Ldarg_0);
				cil.Emit(OpCodes.Ldsfld, last_context);
				cil.Emit(OpCodes.Bne_Un_S, init_needed_label);
				cil.Emit(OpCodes.Ret);
				// [ __lastContext = arg0 ]
				cil.MarkLabel(init_needed_label);
				cil.Emit(OpCodes.Ldarg_0);
				cil.Emit(OpCodes.Stsfld, last_context);

				// the rest of the method is created when fields are emitted
			}
		}

		#endregion

		#region Type desc population

		/// <summary>
		/// Generates a <c>&lt;PopulateTypeDesc&gt;</c> method that populates a <see cref="DTypeDesc"/>
		/// at runtime (instead of reflecting the class).
		/// </summary>
		/// <param name="phpType">The class representation used in the compiler.</param>
		internal static void GenerateTypeDescPopulation(PhpType phpType)
		{
			MethodBuilder populator = DefinePopulateTypeDescMethod(phpType.RealTypeBuilder);
			ILEmitter il = new ILEmitter(populator);

			// methods
			foreach (KeyValuePair<Name, DRoutineDesc> pair in phpType.TypeDesc.Methods)
			{
				if (!pair.Value.IsAbstract)
				{
					EmitAddMethod(il, pair.Key.ToString(), pair.Value.MemberAttributes,
						pair.Value.PhpMethod.ArgLessInfo);
				}
			}

			// fields
			foreach (KeyValuePair<VariableName, DPropertyDesc> pair in phpType.TypeDesc.Properties)
			{
				PhpField field = pair.Value.PhpField;

				// determine whether we need to add this field
				if (field.Implementor == field.DeclaringPhpType || field.UpgradesVisibility)
				{
					EmitAddProperty(il, phpType.Builder.RealOpenType, pair.Key.ToString(),
						pair.Value.MemberAttributes, field.RealField);
				}
			}

			// constants
			foreach (KeyValuePair<VariableName, DConstantDesc> pair in phpType.TypeDesc.Constants)
			{
				EmitAddConstant(il, pair.Key.ToString(), pair.Value.ClassConstant);
			}

			il.Emit(OpCodes.Ret);
		}

        /// <summary>
        /// Generates a <c>__PopulateTypeDesc</c> method that populates a <see cref="DTypeDesc"/>
        /// at runtime (instead of reflecting the class).
        /// </summary>
        /// <param name="typeBuilder">The target <see cref="TypeBuilder"/>.</param>
        /// <param name="methods">The methods to add to the type desc.</param>
        /// <param name="fields">The fields to add to the type desc.</param>
        /// <param name="constants">The constants to add to the type desc. Together with their value. (Consts are public static literal fields)</param>
        /// <remarks>Used by WrapperGen.</remarks>
        public static void GenerateTypeDescPopulation(TypeBuilder typeBuilder,
            ICollection<InfoWithAttributes<MethodInfo>> methods,
            ICollection<InfoWithAttributes<FieldInfo>> fields,
            ICollection<KeyValuePair<FieldInfo, Object>> constants)
        {
            MethodBuilder populator = DefinePopulateTypeDescMethod(typeBuilder);
            ILEmitter il = new ILEmitter(populator);

            // methods
            if (methods != null)
            {
                foreach (InfoWithAttributes<MethodInfo> info in methods)
                {
                    EmitAddMethod(il, info.Info.Name, info.Attributes, info.Info);
                }
            }

            // fields
            if (fields != null)
            {
                foreach (InfoWithAttributes<FieldInfo> info in fields)
                {
                    EmitAddProperty(il, info.Info.DeclaringType, info.Info.Name, info.Attributes, info.Info);
                }
            }

            // constants
            if (constants != null)
            {
                foreach (var info in constants)
                {
                    EmitAddConstant(il, info.Key.Name, info.Value);
                }
            }
            il.Emit(OpCodes.Ret);
        }

		private static void EmitAddMethod(ILEmitter il, string name, PhpMemberAttributes attributes, MethodInfo argless)
		{
			// [ typeDesc.AddMethod("method", attributes, new RoutineDelegate(method)) ]

			il.Ldarg(0);
			il.Emit(OpCodes.Ldstr, name);
			il.LdcI4((int)attributes);

			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldftn, argless);

			il.Emit(OpCodes.Newobj, Constructors.RoutineDelegate);
			il.Emit(OpCodes.Call, Methods.AddMethod);
		}

		private static void EmitAddProperty(ILEmitter il, Type realOpenType, string name, PhpMemberAttributes attributes,
			FieldInfo field)
		{
			// [ typeDesc.AddProperty("field", attributes, new GetterDelegate(getter), new SetterDelegate(setter)) ]

			il.Ldarg(0);
			il.Emit(OpCodes.Ldstr, name);
			il.LdcI4((int)attributes);

			MethodInfo getter, setter;
			EmitFieldAccessors(
				(TypeBuilder)il.MethodBase.DeclaringType,
				realOpenType,
				field,
				out getter,
				out setter);

			// getter
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldftn, getter);
			il.Emit(OpCodes.Newobj, Constructors.GetterDelegate);

			// setter
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldftn, setter);
			il.Emit(OpCodes.Newobj, Constructors.SetterDelegate);

			il.Emit(OpCodes.Call, Methods.AddProperty);
		}

        private static void EmitAddConstant(ILEmitter il, string name, object value)
        {
            // [ typeDesc.AddConstant("constant", value) ]

            //Debug.Assert(constant.IsStatic);

            il.Ldarg(0);
            il.Emit(OpCodes.Ldstr, name);
            il.LoadLiteralBox(value);  // for non dynamic literal fields
            //il.Emit(OpCodes.Ldsfld, constant);    // for non literal field only !

            il.Emit(OpCodes.Call, Methods.AddConstant);
        }

		private static void EmitAddConstant(ILEmitter/*!*/il, string/*!*/name, ClassConstant/*!*/constant)
        {
            // [ typeDesc.AddConstant("constant", value) ]

            il.Ldarg(0);
            il.Emit(OpCodes.Ldstr, name);

            if (constant.HasValue && constant.RealField.IsLiteral)
            {
                il.LoadLiteralBox(constant.Value);  // for non dynamic literal fields
            }
            else
            {
                il.Emit(OpCodes.Ldsfld, constant.RealField);    // for non literal field only !
            }

            il.Emit(OpCodes.Call, Methods.AddConstant);
        }

		private static void EmitFieldAccessors(TypeBuilder typeBuilder, Type realOpenType, FieldInfo field,
			out MethodInfo getter, out MethodInfo setter)
		{
			// getter
			MethodBuilder getter_builder = typeBuilder.DefineMethod(
				"<^Getter>",
#if SILVERLIGHT
				MethodAttributes.Public | MethodAttributes.Static,
#else
				MethodAttributes.PrivateScope | MethodAttributes.Static,
#endif
				Types.Object[0],
				Types.Object);

			getter_builder.DefineParameter(1, ParameterAttributes.None, "instance");

			PhpFieldBuilder.EmitGetterStub(new ILEmitter(getter_builder), field, realOpenType);

			// setter
			MethodBuilder setter_builder = typeBuilder.DefineMethod(
				"<^Setter>",
#if SILVERLIGHT
				MethodAttributes.Public | MethodAttributes.Static,
#else
				MethodAttributes.PrivateScope | MethodAttributes.Static,
#endif
				Types.Void,
				Types.Object_Object);

			setter_builder.DefineParameter(1, ParameterAttributes.None, "instance");
			setter_builder.DefineParameter(2, ParameterAttributes.None, "value");

			PhpFieldBuilder.EmitSetterStub(new ILEmitter(setter_builder), field, realOpenType);

			getter = getter_builder;
			setter = setter_builder;
		}

		#endregion

		#region Utilities

		/// <summary>
		/// Defines the <c>__InitializeStaticFields</c> static method.
		/// </summary>
		/// <param name="typeBuilder">The <see cref="TypeBuilder"/> to define the method in.</param>
		/// <returns>The <see cref="MethodBuilder"/>.</returns>
		internal static MethodBuilder DefineStaticFieldInitMethod(TypeBuilder typeBuilder)
		{
			MethodBuilder method_builder = typeBuilder.DefineMethod(
				StaticFieldInitMethodName,
				MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard,
				Types.Void,
				Types.ScriptContext);

#if !SILVERLIGHT 
			method_builder.SetCustomAttribute(AttributeBuilders.EditorBrowsableNever);
#endif
			method_builder.DefineParameter(1, ParameterAttributes.None, "context");

			return method_builder;
		}

		/// <summary>
		/// Defines the <c>__PopulateTypeDesc</c> static method.
		/// </summary>
		/// <param name="typeBuilder">The <see cref="TypeBuilder"/> to define the method in.</param>
		/// <returns>The <see cref="MethodBuilder"/>.</returns>
		internal static MethodBuilder DefinePopulateTypeDescMethod(TypeBuilder typeBuilder)
		{
			MethodBuilder method_builder = typeBuilder.DefineMethod(
				PopulateTypeDescMethodName,
#if SILVERLIGHT
				MethodAttributes.Public | MethodAttributes.Static,
#else
				MethodAttributes.Private | MethodAttributes.Static,
#endif
				Types.Void,
				Types.PhpTypeDesc);

			return method_builder;
		}

        #endregion
	}
}
