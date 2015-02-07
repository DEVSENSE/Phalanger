/*

 Copyright (c) 2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Generic;
using PHP.Core.Reflection;

namespace PHP.Core.Emit
{
	internal class StubParameterInfo : ParameterInfo
	{
		public StubParameterInfo(int position, Type/*!*/ type, ParameterAttributes attributes, string name)
		{
			Debug.Assert(type != null);

#if !SILVERLIGHT
			this.ClassImpl = type;
			this.AttrsImpl = attributes;
			this.NameImpl = name;
			this.PositionImpl = position;
#endif
		}
	}

	internal struct StubInfo
	{
		public static GenericTypeParameterBuilder[] EmptyGenericParameters = new GenericTypeParameterBuilder[0];

		private readonly MethodBase methodBase;

		public readonly ParameterInfo[] Parameters;
		public readonly GenericTypeParameterBuilder[] TypeParameters;
		public readonly Type ReturnType;

		public MethodBuilder MethodBuilder { get { return (MethodBuilder)methodBase; } }
		public ConstructorBuilder ConstructorBuilder { get { return (ConstructorBuilder)methodBase; } }

		public StubInfo(MethodBase methodBase, ParameterInfo[] parameters,
			GenericTypeParameterBuilder[] typeParameters, Type returnType)
		{
			Debug.Assert(methodBase is MethodBuilder || methodBase is ConstructorBuilder);

			this.methodBase = methodBase;
			this.Parameters = parameters;
			this.TypeParameters = typeParameters;
			this.ReturnType = returnType;
		}
	}

	internal delegate bool StubSignatureFilter(string[] genericParameterNames, object[] parameterTypes, object returnType);

	/// <summary>
	/// Provides services related to building CLR stubs of PHP methods, fields, and constants.
	/// </summary>
	/// <remarks>
	/// Three areas that make use of CLR stubs have been identified so far:
	/// <list type="1">
	/// <item>Override/implement stubs - <see cref="CodeGenerator.EmitOverrideStubs"/></item>
	/// <item>Dynamic delegate stubs - <see cref="Core.Reflection.ClrDelegateDesc.DelegateStubBuilder"/></item>
	/// <item>Export stubs - <see cref="CodeGenerator.EmitExportStubs"/></item>
	/// </list>
	/// </remarks>
	internal class ClrStubBuilder
	{
		private ILEmitter/*!*/ il;
		private IPlace scriptContextPlace;

		private int paramOffset;

		private LocalBuilder[] referenceLocals;

		public ClrStubBuilder(ILEmitter/*!*/ il, IPlace/*!*/ scriptContextPlace, int paramCount, int paramOffset)
		{
			this.il = il;
			this.scriptContextPlace = scriptContextPlace;
			this.paramOffset = paramOffset;

			this.referenceLocals = new LocalBuilder[paramCount];
		}

		#region EmitLoadClrParameter, EmitStoreClrParameter, EmitConvertReturnValue

		/// <summary>
		/// Emits code that loads a specified parameter on the evaluation stack.
		/// </summary>
		/// <param name="paramInfo">The parameter to load.</param>
		/// <param name="requiredTypeCode">Specifies whether <see cref="PhpReference"/>
		/// (<see cref="PhpTypeCode.PhpReference"/>), <see cref="object"/> (<see cref="PhpTypeCode.Object"/>),
		/// or the most fitting of these two should be loaded.</param>
		public void EmitLoadClrParameter(ParameterInfo/*!*/ paramInfo, PhpTypeCode requiredTypeCode)
		{
			if (paramInfo.IsOut) il.Emit(OpCodes.Ldnull);
			else
			{
				il.Ldarg(paramInfo.Position + paramOffset);

				// dereference ref param
				Type param_type = paramInfo.ParameterType;
				if (param_type.IsByRef)
				{
					param_type = param_type.GetElementType();

					il.Ldind(param_type);
				}

				// convert the parameter to PHP type
				PhpTypeCode type_code = ClrOverloadBuilder.EmitConvertToPhp(
					il,
					param_type/*,
					scriptContextPlace*/);

				il.EmitBoxing(type_code);
			}

			// check whether we have to create a PhpReference
			if (requiredTypeCode == PhpTypeCode.Object ||
				(requiredTypeCode == PhpTypeCode.Unknown && !paramInfo.ParameterType.IsByRef)) return;

			if (paramInfo.ParameterType.IsByRef)
			{
				LocalBuilder ref_local = il.DeclareLocal(Types.PhpReference[0]);

				// remember the PhpReference in a local
				il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
				il.Emit(OpCodes.Dup);
				il.Stloc(ref_local);

				referenceLocals[paramInfo.Position] = ref_local;
			}
			else
			{
				// no reference store-back is necessary
				il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
			}
		}

		/// <summary>
		/// Emits code that stores a <see cref="PhpReference"/>'s value back to a ref/out parameter.
		/// </summary>
		/// <param name="paramInfo">The parameter to store back.</param>
		public void EmitStoreClrParameter(ParameterInfo/*!*/ paramInfo)
		{
			if (paramInfo.ParameterType.IsByRef && referenceLocals[paramInfo.Position] != null)
			{
				il.Ldarg(paramInfo.Position + paramOffset);

				Type param_type = paramInfo.ParameterType.GetElementType();

				// load the new parameter value
				il.Ldloc(referenceLocals[paramInfo.Position]);
				il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);

				// convert it to CLR type
				ClrOverloadBuilder.EmitConvertToClr(
					il,
					PhpTypeCode.Object,
					param_type);

				// store it back
				il.Stind(param_type);
			}
		}

		public void EmitConvertReturnValue(Type/*!*/ returnType, PhpTypeCode expectedTypeCode)
		{
			if (returnType == Types.Void) il.Emit(OpCodes.Pop);
			else
			{
				if (expectedTypeCode == PhpTypeCode.PhpReference)
				{
					// dereference
					il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
					expectedTypeCode = PhpTypeCode.Object;
				}

				ClrOverloadBuilder.EmitConvertToClr(
					il,
					expectedTypeCode,
					returnType);
			}
		}

		#endregion

		#region EmitLoadArgfullParameters, EmitLoadArglessParameters

		private void EmitLoadArgfullParameters(ParameterInfo[]/*!*/ stubParameters,
			Type[]/*!*/ stubTypeParameters, PhpMethod/*!*/ target)
		{
			for (int i = 0; i < target.Signature.GenericParamCount; i++)
			{
				if (i < stubTypeParameters.Length)
				{
					il.Emit(OpCodes.Ldtoken, stubTypeParameters[i]);
					il.Emit(OpCodes.Call, Methods.DTypeDesc_Create);
				}
				else
				{
					// optional type parameter, whose value is not supplied
					il.Emit(OpCodes.Ldsfld, Fields.Arg_DefaultType);
				}
			}

			for (int i = 0; i < target.Signature.ParamCount; i++)
			{
				if (i < stubParameters.Length)
				{
					EmitLoadClrParameter(
						stubParameters[i],
						target.Signature.IsAlias(i) ? PhpTypeCode.PhpReference : PhpTypeCode.Object);
				}
				else
				{
					// optional parameter, whose value is not supplied
					il.Emit(OpCodes.Ldsfld, Fields.Arg_Default);
				}
			}
		}

		private void EmitLoadArglessParameters(ParameterInfo[]/*!*/ stubParameters,
			Type[]/*!*/ stubTypeParameters, PhpMethod/*!*/ target)
		{
			PhpStackBuilder.EmitAddFrame(il, scriptContextPlace, stubTypeParameters.Length, stubParameters.Length,
				delegate(ILEmitter eil, int i)
				{
					il.Emit(OpCodes.Ldtoken, stubTypeParameters[i]);
					il.Emit(OpCodes.Call, Methods.DTypeDesc_Create);
				},
				delegate(ILEmitter eil, int i)
				{
					EmitLoadClrParameter(stubParameters[i], PhpTypeCode.Unknown);
				});
		}

		#endregion

		#region DefineFieldExport

		/// <summary>
		/// Defines a property that &quot;exports;&quot; a given field or constant.
		/// </summary>
		/// <param name="name">The name of the property.</param>
		/// <param name="member">A <see cref="PhpField"/> or <see cref="ClassConstant"/>.</param>
		/// <returns>The export property builder.</returns>
		public static PropertyBuilder/*!*/ DefineFieldExport(string name, DMember/*!*/ member)
		{
			Debug.Assert(member is PhpField || member is ClassConstant);

			DTypeDesc declaring_type_desc = member.DeclaringType.TypeDesc;
			TypeBuilder type_builder = member.DeclaringPhpType.RealTypeBuilder;

			// determine name and type
			Type type = Types.Object[0]; // TODO: field/constant type hints?

			PropertyBuilder prop_builder = type_builder.DefineProperty(
				name,
				Reflection.Enums.ToPropertyAttributes(member.MemberDesc.MemberAttributes),
				type,
				Type.EmptyTypes);

			MethodAttributes accessor_attrs = Reflection.Enums.ToMethodAttributes(member.MemberDesc.MemberAttributes);
			bool changed;

			// define getter
			MethodBuilder getter = type_builder.DefineMethod(
				GetNonConflictingMethodName(declaring_type_desc, "get_" + name, out changed),
				accessor_attrs,
				type,
				Type.EmptyTypes);

			getter.SetCustomAttribute(AttributeBuilders.DebuggerHidden);
			prop_builder.SetGetMethod(getter);

			// generate setter
			if (member is PhpField)
			{
				MethodBuilder setter = type_builder.DefineMethod(
					GetNonConflictingMethodName(declaring_type_desc, "set_" + name, out changed),
					accessor_attrs,
					Types.Void,
					new Type[] { type });

				setter.SetCustomAttribute(AttributeBuilders.DebuggerHidden);
				prop_builder.SetSetMethod(setter);
			}

			return prop_builder;
		}

		/// <summary>
		/// Creates a name based on <paramref name="str"/> that does not clash with any methods in the given type desc.
		/// </summary>
		internal static string/*!*/ GetNonConflictingMethodName(DTypeDesc/*!*/ typeDesc, string/*!*/ str, out bool changed)
		{
			Name name = new Name(str);

			changed = false;
			while (typeDesc.GetMethod(name) != null)
			{
				name.Value += "_";
				changed = true;
			}

			return name.Value;
		}

		#endregion

		#region EmitStubBody, DefineStubParameters, DefineStubTypeParameters

		/// <summary>
		/// Emits stub for one overridden/implemented/exported CLR overload.
		/// </summary>
		/// <param name="il"></param>
		/// <param name="scriptContextPlace"></param>
		/// <param name="stubParameters">The overload parameters.</param>
		/// <param name="stubTypeParameters">The overload type parameters.</param>
		/// <param name="stubReturnType">The overload return type.</param>
		/// <param name="target">The overriding/implementing/exporting method.</param>
		/// <param name="targetType">The type (perhaps constructed) that declared <paramref name="target"/>.</param>
		public static void EmitMethodStubBody(ILEmitter/*!*/ il, IPlace/*!*/ scriptContextPlace,
			ParameterInfo[]/*!*/ stubParameters, Type[]/*!*/ stubTypeParameters,
			Type/*!*/ stubReturnType, PhpMethod/*!*/ target, DType/*!*/ targetType)
		{
			bool stub_is_static = il.MethodBase.IsStatic;

			ClrStubBuilder stub_builder =
				new ClrStubBuilder(il, scriptContextPlace, stubParameters.Length, (stub_is_static ? 0 : 1));

			if (stubParameters.Length >= target.Signature.MandatoryParamCount &&
				stubTypeParameters.Length >= target.Signature.MandatoryGenericParamCount &&
				(target.Properties & RoutineProperties.IsArgsAware) == 0)
			{
				// we can directly call the target argful

				if (!stub_is_static) il.Ldarg(FunctionBuilder.ArgThis);
				scriptContextPlace.EmitLoad(il);

				stub_builder.EmitLoadArgfullParameters(stubParameters, stubTypeParameters, target);

				// invoke the target (virtually if it's not static)
				il.Emit(stub_is_static ? OpCodes.Call : OpCodes.Callvirt,
					DType.MakeConstructed(target.ArgFullInfo, targetType as ConstructedType));
			}
			else
			{
				// we have to take the argless way

				stub_builder.EmitLoadArglessParameters(stubParameters, stubTypeParameters, target);

				// invoke the target's argless
				// TODO: this is not behaving 100% correct, because we're losing virtual dispatch here
				if (stub_is_static) il.Emit(OpCodes.Ldnull);
				else il.Ldarg(FunctionBuilder.ArgThis);

				scriptContextPlace.EmitLoad(il);
				il.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);

				il.Emit(OpCodes.Call, DType.MakeConstructed(target.ArgLessInfo, targetType as ConstructedType));
			}

			// do not keep it on stack needlessly
			if (stubReturnType == Types.Void) il.Emit(OpCodes.Pop);

			// convert ref/out parameters back to CLR type
			for (int i = 0; i < stubParameters.Length; i++)
			{
				stub_builder.EmitStoreClrParameter(stubParameters[i]);
			}

			if (stubReturnType != Types.Void)
			{
				// convert the return parameter back to CLR type
				stub_builder.EmitConvertReturnValue(
					stubReturnType,
					target.Signature.AliasReturn ? PhpTypeCode.PhpReference : PhpTypeCode.Object);
			}

			il.Emit(OpCodes.Ret);
		}

		/// <summary>
		/// Sets attributes of generated override/implement/export stub parameters.
		/// </summary>
		/// <param name="stub">The stub method builder.
		/// </param>
		/// <param name="formalParams">Formal parameters of the implementing PHP method.</param>
		/// <param name="templateParams">Parameters of the overload being overriden/implemented/exported.</param>
		public static void DefineStubParameters(MethodBuilder/*!*/ stub,
			List<PHP.Core.AST.FormalParam> formalParams, ParameterInfo[]/*!*/ templateParams)
		{
			for (int i = 0; i < templateParams.Length; i++)
			{
				string name;

				// take the overriding parameter name if available
				if (formalParams != null && i < formalParams.Count) name = formalParams[i].Name.ToString();
				else name = templateParams[i].Name;

				stub.DefineParameter(i + 1, templateParams[i].Attributes, name);
			}
		}

		/// <summary>
		/// Sets attributes of generated override/implement/export stub parameters.
		/// </summary>
		/// <param name="stub">The stub constructor builder.
		/// </param>
		/// <param name="formalParams">Formal parameters of the implementing PHP method.</param>
		/// <param name="templateParams">Parameters of the overload being overriden/implemented/exported.</param>
		public static void DefineStubParameters(ConstructorBuilder/*!*/ stub,
			List<PHP.Core.AST.FormalParam> formalParams, ParameterInfo[]/*!*/ templateParams)
		{
			for (int i = 0; i < templateParams.Length; i++)
			{
				string name;

				// take the overriding parameter name if available
				if (formalParams != null && i < formalParams.Count) name = formalParams[i].Name.ToString();
				else name = templateParams[i].Name;

				stub.DefineParameter(i + 1, templateParams[i].Attributes, name);
			}
		}

		/// <summary>
		/// Defines generic parameters according to the given template and re-maps relevant parameters.
		/// </summary>
		public static void DefineStubGenericParameters(MethodBuilder/*!*/ stub, Type[]/*!!*/ genericParameters,
			PhpRoutineSignature/*!*/ targetSignature, Type[]/*!!*/ parameters)
		{
			// determine generic parameter names
			string[] generic_param_names = new string[genericParameters.Length];
			for (int j = 0; j < generic_param_names.Length; j++)
			{
				if (j < targetSignature.GenericParamCount)
				{
					generic_param_names[j] = targetSignature.GenericParams[j].Name.ToString();
				}
				else generic_param_names[j] = genericParameters[j].Name;
			}
			GenericTypeParameterBuilder[] generic_params = stub.DefineGenericParameters(generic_param_names);

			// determine generic parameter attributes and constraints
			for (int j = 0; j < generic_params.Length; j++)
			{
				Type template_type = genericParameters[j];

				// attributes
				generic_params[j].SetGenericParameterAttributes(template_type.GenericParameterAttributes);

				// constraints
				Type[] template_constraints = template_type.GetGenericParameterConstraints();

				List<Type> interface_constraints = new List<Type>();
				for (int k = 0; k < template_constraints.Length; k++)
				{
					if (template_constraints[k].IsClass) generic_params[j].SetBaseTypeConstraint(template_constraints[k]);
					else interface_constraints.Add(template_constraints[k]);
				}
				generic_params[j].SetInterfaceConstraints(interface_constraints.ToArray());
			}

			// re-map base method generic parameters to the newly defined generic parameters
			for (int j = 0; j < parameters.Length; j++)
			{
				if (parameters[j].IsGenericParameter && parameters[j].DeclaringMethod != null)
				{
					// method generic parameter
					parameters[j] = generic_params[parameters[j].GenericParameterPosition];
				}
			}
		}

		#endregion

		private static object[] GetStubParameterTypes(
			int paramCount,
			int typeParamCount,
			PhpRoutineSignature/*!*/ signature,
			List<PHP.Core.AST.FormalTypeParam>/*!*/ formalTypeParams)
		{
			object[] parameter_types = new object[paramCount];
			for (int i = 0; i < paramCount; i++)
			{
				DType type_hint = signature.TypeHints[i];
				if (type_hint != null && !type_hint.IsUnknown)
				{
					GenericParameter gen_type_hint = type_hint as GenericParameter;
					if (gen_type_hint != null)
					{
						// this is a generic parameter - declared by either the method or type
						if (gen_type_hint.DeclaringMember is PhpRoutine)
						{
							if (gen_type_hint.Index < typeParamCount)
							{
								// unknown at this point - fixed-up later
								parameter_types[i] = gen_type_hint.Index;
							}
							else
							{
								// default generic parameter
								DType default_type = formalTypeParams[gen_type_hint.Index].DefaultType as DType;
								parameter_types[i] = (default_type == null ? Types.Object[0] : default_type.RealType);
							}
						}
						else parameter_types[i] = gen_type_hint.RealGenericTypeParameterBuilder;
					}
					else parameter_types[i] = type_hint.RealType;
				}
				else parameter_types[i] = Types.Object[0];

				// make it byref if declared with &
				if (signature.AliasMask[i])
				{
					Type type = parameter_types[i] as Type;
					if (type != null) parameter_types[i] = type.MakeByRefType();
					else parameter_types[i] = -((int)parameter_types[i] + 1);
				}

				Debug.Assert(parameter_types[i] != null);
			}

			return parameter_types;
		}

		/// <summary>
		/// Enumerates all export overloads for the given target PHP method.
		/// </summary>
		public static IEnumerable<StubInfo> DefineMethodExportStubs(
			PhpRoutine/*!*/ target,
			MethodAttributes attributes,
			bool defineConstructors,
			StubSignatureFilter/*!*/ signatureFilter)
		{
            Debug.Assert(target.Builder != null);

            Type return_type = Types.Object[0];

            PhpRoutineSignature signature = target.Signature;
			List<AST.FormalParam> formal_params = target.Builder.Signature.FormalParams;
			List<AST.FormalTypeParam> formal_type_params = target.Builder.TypeSignature.TypeParams;

			int gen_sig_count = signature.GenericParamCount - signature.MandatoryGenericParamCount + 1;
			int arg_sig_count = signature.ParamCount - signature.MandatoryParamCount + 1;

			// TODO: return type hints
			// HACK: change return type to void for methods that are apparently event handlers
			if (signature.GenericParamCount == 0 && arg_sig_count == 1 && signature.ParamCount == 2 &&
				(signature.TypeHints[0] == null || signature.TypeHints[0].RealType == Types.Object[0]) &&
				(signature.TypeHints[1] != null && typeof(EventArgs).IsAssignableFrom(signature.TypeHints[1].RealType)))
			{
				return_type = Types.Void;
			}

			for (int gen_sig = 0; gen_sig < gen_sig_count; gen_sig++)
			{
				for (int arg_sig = 0; arg_sig < arg_sig_count; arg_sig++)
				{
					// determine parameter types (except for method mandatory generic parameters)
					object[] parameter_types = GetStubParameterTypes(
						arg_sig + signature.MandatoryParamCount,
						gen_sig + signature.MandatoryGenericParamCount,
						signature,
						formal_type_params);

					// determine generic parameter names
					string[] generic_param_names = new string[target.Signature.MandatoryGenericParamCount + gen_sig];
					for (int i = 0; i < generic_param_names.Length; i++)
					{
						generic_param_names[i] = formal_type_params[i].Name.ToString();
					}

					// are we allowed to generate this signature?
					if (!signatureFilter(generic_param_names, parameter_types, return_type)) continue;

					GenericTypeParameterBuilder[] generic_params = StubInfo.EmptyGenericParameters;
					MethodBase method_base = null;
					MethodBuilder method = null;

					if (!defineConstructors)
					{
						method = target.DeclaringType.RealTypeBuilder.DefineMethod(target.FullName, attributes);

						// determine generic parameters
						if (generic_param_names.Length > 0) generic_params = method.DefineGenericParameters(generic_param_names);

						method_base = method;
					}

					ParameterInfo[] parameters = new ParameterInfo[parameter_types.Length];

					// fill in parameter infos
					Type[] real_parameter_types = new Type[parameters.Length];
					for (int i = 0; i < parameters.Length; i++)
					{
						Type type = parameter_types[i] as Type;

						// generic method parameter fixup
						if (type == null)
						{
							int index = (int)parameter_types[i];
							if (index < 0) type = generic_params[-(index + 1)].MakeByRefType();
							else type = generic_params[index];
						}

						string param_name;
						ParameterAttributes param_attrs;
						if (i < formal_params.Count)
						{
							param_name = formal_params[i].Name.ToString();
							param_attrs = (formal_params[i].IsOut ? ParameterAttributes.Out : ParameterAttributes.None);
						}
						else
						{
							param_name = "args" + (i + 1);
							param_attrs = ParameterAttributes.None;
						}

						parameters[i] = new StubParameterInfo(i, type, param_attrs, param_name);
						real_parameter_types[i] = type;
					}

					if (method != null)
					{
						method.SetParameters(real_parameter_types);
						method.SetReturnType(return_type);

						method.SetCustomAttribute(AttributeBuilders.DebuggerHidden);
					}
					else
					{
						// constructor is never a generic method
						attributes |= MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
						attributes &= ~MethodAttributes.Virtual;

						ConstructorBuilder constructor = target.DeclaringType.RealTypeBuilder.DefineConstructor(
							attributes, CallingConventions.Standard, real_parameter_types);
						constructor.SetCustomAttribute(AttributeBuilders.DebuggerHidden);

						method_base = constructor;
					}

					yield return new StubInfo(method_base, parameters, generic_params, return_type);
				}
			}
		}
	}
}
