/*

 Copyright (c) 2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
#if !SILVERLIGHT
    //#define DEBUG_DYNAMIC_STUBS
#endif
#define EMIT_VERIFIABLE_STUBS

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Reflection
{
	#region DPropertyDesc

	[DebuggerNonUserCode]
	public class DPropertyDesc : DMemberDesc
	{
		public DProperty Property { get { return (DProperty)Member; } }
		public PhpField PhpField { get { return (PhpField)Member; } }
		public ClrProperty ClrProperty { get { return (ClrProperty)Member; } }
		public ClrField ClrField { get { return (ClrField)Member; } }

		protected GetterDelegate GetterStub
		{
			get
			{
				if (_getterStub == null) _getterStub = GenerateGetterStub();
				return _getterStub;
			}
		}
		protected GetterDelegate _getterStub = null;

		protected SetterDelegate SetterStub
		{
			get
			{
				if (_setterStub == null) _setterStub = GenerateSetterStub();
				return _setterStub;
			}
		}
		protected SetterDelegate _setterStub = null;


		#region Construction

		/// <summary>
		/// Used by compiler and full-reflect.
		/// </summary>
		internal DPropertyDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes)
			: base(declaringType, memberAttributes)
		{
			Debug.Assert(declaringType != null);
			this._getterStub = null; // to be generated on demand
			this._setterStub = null; // to be generated on demand
		}

		#endregion

		#region Error-throwing setters and getters

        private void EventSetter(object instance, object value)
        {
            PhpException.Throw(
                PhpError.Error,
                string.Format(CoreResources.event_written, DeclaringType.MakeFullName(), MakeFullName()));
        }

        private void MissingSetter(object instance, object value)
        {
            PhpException.Throw(
                PhpError.Error,
                string.Format(CoreResources.readonly_property_written, DeclaringType.MakeFullName(), MakeFullName()));
        }

        private object MissingGetter(object instance)
        {
            PhpException.Throw(
                PhpError.Error,
                string.Format(CoreResources.writeonly_property_read, DeclaringType.MakeFullName(), MakeFullName()));

            return null;
        }

		#endregion

		#region Emission

		internal void EmitSetConversion(ILEmitter/*!*/ il, PhpTypeCode sourceTypeCode, Type/*!*/ targetType)
		{
			LocalBuilder strictness = il.GetTemporaryLocal(typeof(PHP.Core.ConvertToClr.ConversionStrictness));
			if (!ClrOverloadBuilder.EmitConvertToClr(il, sourceTypeCode, targetType, strictness))
			{
				Label label_ok = il.DefineLabel();

				il.Ldloc(strictness);
				il.LdcI4((int)PHP.Core.ConvertToClr.ConversionStrictness.Failed);
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Brfalse, label_ok);

				il.Emit(OpCodes.Ldstr, Property.DeclaringType.FullName);
				il.Emit(OpCodes.Ldstr, Property.FullName);
				il.Emit(OpCodes.Call, Methods.PhpException.PropertyTypeMismatch);

				il.MarkLabel(label_ok, true);
			}
			il.ReturnTemporaryLocal(strictness);
		}

		protected virtual GetterDelegate/*!*/ GenerateGetterStub()
		{
#if SILVERLIGHT
			DynamicMethod stub = new DynamicMethod("<^GetterStub>", Types.Object[0], Types.Object);
#else
			DynamicMethod stub = new DynamicMethod("<^GetterStub>", PhpFunctionUtils.DynamicStubAttributes, CallingConventions.Standard,
				Types.Object[0], Types.Object, this.declaringType.RealType, true);
#endif

			ILEmitter il = new ILEmitter(stub);

			ClrEvent clr_event;
			ClrProperty clr_property;

			Type result_type;

			if ((clr_event = Member as ClrEvent) != null)
			{
                Debug.Assert(!declaringType.RealType.IsValueType, "Value type with ClrEvent not handled! TODO: arg(0) is ClrValue<T>.");

				LocalBuilder temp = il.DeclareLocal(declaringType.RealType);

				il.Ldarg(0);
				il.Emit(OpCodes.Castclass, declaringType.RealType);
				il.Stloc(temp);

				clr_event.EmitGetEventObject(il, new Place(null, Properties.ScriptContext_CurrentContext),
					new Place(temp), true);
			}
			else
			{
				if ((clr_property = Member as ClrProperty) != null)
				{
					// return error-throwing getter if the property is write-only
					if (!clr_property.HasGetter) return new GetterDelegate(MissingGetter);

					if (!clr_property.Getter.IsStatic)
					{
                        ClrOverloadBuilder.EmitLoadInstance(il, IndexedPlace.ThisArg, declaringType.RealType);
//                        il.Emit(OpCodes.Ldarg_0);
						
//                        if (declaringType.RealType.IsValueType) 
//                            il.Emit(OpCodes.Unbox, declaringType.RealType);
//#if EMIT_VERIFIABLE_STUBS
//                        else
//                            il.Emit(OpCodes.Castclass, this.declaringType.RealType);
//#endif
					}

					il.Emit(OpCodes.Call, clr_property.Getter);

					result_type = clr_property.Getter.ReturnType;
				}
				else
				{
					ClrField clr_field = ClrField;

					if (!clr_field.FieldInfo.IsStatic)
					{
                        ClrOverloadBuilder.EmitLoadInstance(il, IndexedPlace.ThisArg, declaringType.RealType);
                        //il.Emit(OpCodes.Ldarg_0);
                        ////il.Emit(OpCodes.Castclass, this.declaringType.RealType);

                        //if (declaringType.RealType.IsValueType) il.Emit(OpCodes.Unbox, declaringType.RealType);
						il.Emit(OpCodes.Ldfld, clr_field.FieldInfo);
					}
					else
					{
						il.Emit(OpCodes.Ldsfld, clr_field.FieldInfo);
					}

					result_type = clr_field.FieldInfo.FieldType;
				}

				il.EmitBoxing(ClrOverloadBuilder.EmitConvertToPhp(il, result_type/*, null*/));
			}

			il.Emit(OpCodes.Ret);

			return (GetterDelegate)stub.CreateDelegate(typeof(GetterDelegate));
		}

		protected virtual SetterDelegate/*!*/ GenerateSetterStub()
		{
			if (Member is ClrEvent) return new SetterDelegate(EventSetter);

#if SILVERLIGHT
			DynamicMethod stub = new DynamicMethod("<^SetterStub>", Types.Void, Types.Object_Object);

            /*DynamicMethod stub = new DynamicMethod("<^SetterStub>", PhpFunctionUtils.DynamicStubAttributes, CallingConventions.Standard,
                Types.Void, Types.Object_Object, this.declaringType.RealType, true);*/
#else
			DynamicMethod stub = new DynamicMethod("<^SetterStub>", PhpFunctionUtils.DynamicStubAttributes, CallingConventions.Standard,
				Types.Void, Types.Object_Object, this.declaringType.RealType, true);
#endif
#if DEBUG_DYNAMIC_STUBS

			// Debugging - save the generated stub to TEMP
			AssemblyName name = new AssemblyName("SetterStub_" + Property.FullName.ToString().Replace(':', '_'));
			AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save, "C:\\Temp");
			ModuleBuilder mb = ab.DefineDynamicModule(name.Name, name.Name + ".dll");
			TypeBuilder tb = mb.DefineType("Stub");
			MethodBuilder meb = tb.DefineMethod(DeclaringType.ToString() + "::" + Property.FullName,
				MethodAttributes.PrivateScope | MethodAttributes.Static, Types.Void, Types.Object_Object);

			ILEmitter il_dbg = new ILEmitter(meb);
			IndexedPlace instance2 = new IndexedPlace(PlaceHolder.Argument, 0);
			IndexedPlace stack = new IndexedPlace(PlaceHolder.Argument, 1);

			ClrProperty clr_property_dbg = Member as ClrProperty;
			if (clr_property_dbg != null && clr_property_dbg.HasSetter)
			{
				if (!clr_property_dbg.Setter.IsStatic)
				{
					il_dbg.Emit(OpCodes.Ldarg_0);
                    if (declaringType.RealType.IsValueType)
                        il_dbg.Emit(OpCodes.Unbox, declaringType.RealType);
#if EMIT_VERIFIABLE_STUBS
                    else
                        il_dbg.Emit(OpCodes.Castclass, declaringType.RealType);
#endif
				}
				il_dbg.Emit(OpCodes.Ldarg_1);
				EmitSetConversion(il_dbg, PhpTypeCode.Object, clr_property_dbg.Setter.GetParameters()[0].ParameterType);
				il_dbg.Emit(OpCodes.Call, clr_property_dbg.Setter);
			}

			il_dbg.Emit(OpCodes.Ret);
			tb.CreateType();
			ab.Save("SetterStub_" + Property.FullName.ToString().Replace(':', '_') + ".dll");
#endif

			ILEmitter il = new ILEmitter(stub);

			ClrProperty clr_property = Member as ClrProperty;
			Type arg_type;

			if (clr_property != null)
			{
				// return error-throwing setter if the property is read-only
				if (!clr_property.HasSetter) return new SetterDelegate(MissingSetter);

				if (!clr_property.Setter.IsStatic)
				{
                    ClrOverloadBuilder.EmitLoadInstance(il, IndexedPlace.ThisArg, declaringType.RealType);
//                    il.Emit(OpCodes.Ldarg_0);

//                    if (declaringType.RealType.IsValueType) 
//                        il.Emit(OpCodes.Unbox, declaringType.RealType);
//#if EMIT_VERIFIABLE_STUBS
//                    else
//                        il.Emit(OpCodes.Castclass, declaringType.RealType);
//#endif

				}

				il.Emit(OpCodes.Ldarg_1);

				arg_type = clr_property.Setter.GetParameters()[0].ParameterType;
				EmitSetConversion(il, PhpTypeCode.Object, arg_type);

				il.Emit(OpCodes.Call, clr_property.Setter);
			}
			else
			{
				ClrField clr_field = ClrField;

				// return error-throwing setter if the field is initonly
				if (clr_field.FieldInfo.IsInitOnly) return new SetterDelegate(MissingSetter);

				if (!clr_field.FieldInfo.IsStatic)
				{
                    ClrOverloadBuilder.EmitLoadInstance(il, IndexedPlace.ThisArg, declaringType.RealType);
                    //il.Emit(OpCodes.Ldarg_0);
                    ////il.Emit(OpCodes.Castclass, this.declaringType.RealType);

                    //if (declaringType.RealType.IsValueType) il.Emit(OpCodes.Unbox, declaringType.RealType);
				}

				il.Emit(OpCodes.Ldarg_1);

				arg_type = clr_field.FieldInfo.FieldType;
				EmitSetConversion(il, PhpTypeCode.Object, arg_type);

				il.Emit((clr_field.FieldInfo.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld), clr_field.FieldInfo);
			}

			il.Emit(OpCodes.Ret);

			return (SetterDelegate)stub.CreateDelegate(typeof(SetterDelegate));
		}

		#endregion

		#region Run-time Operations

		public virtual object Get(DObject instance)
		{
            return GetterStub((instance == null ? null : instance.InstanceObject));
		}

        #region nested class: ClrPrintableValue

        /// <summary>
        /// Get operation used for <see cref="IPhpPrintable"/> operations.
        /// </summary>
        /// <param name="instance">Reference to <c>self</c> instance.</param>
        /// <returns>Value of this property.</returns>
        /// <remarks>Value of CLR properties are wrapped into <see cref="ClrPrintableValue"/> avoiding infinite recursion and displaying values converted to string if necessary.</remarks>
        public virtual object DumpGet(DObject instance)
        {
            var value = this.Get(instance);

            if (this.Member is ClrProperty)
                return new ClrPrintableValue(value);
            else
                return value;
        }

        /// <summary>
        /// Wraps CLR property value to stop recursion and display the value as a string. Same as VisualStudio's Immediate Window.
        /// </summary>
        private class ClrPrintableValue : IPhpPrintable
        {
            #region Fields & Properties

            private readonly object value;

            /// <summary>
            /// Determines whether <see cref="value"/> is primitive type and can be printed as it is. Otherwise the <see cref="value"/> should be evaluated to string to be printed.
            /// </summary>
            private bool OverridePrint { get { return value != null && !PhpVariable.IsPrimitiveType(value.GetType()); } }

            /// <summary>
            /// Converts <see cref="value"/> to string enclosed with { and }.
            /// </summary>
            private string ValueString { get { if (value == null) throw new ArgumentNullException("value"); return string.Format("{{{0}}}", value.ToString()); } }

            #endregion

            #region constructor

            public ClrPrintableValue(object value)
            {
                this.value = value;
            }

            #endregion

            #region IPhpPrintable

            public void Print(System.IO.TextWriter output)
            {
                if (OverridePrint)
                    output.WriteLine(ValueString);
                else
                    PhpVariable.Print(value);
            }

            public void Dump(System.IO.TextWriter output)
            {
                if (OverridePrint)
                    output.WriteLine(ValueString);
                else
                    PhpVariable.Dump(value);
            }

            public void Export(System.IO.TextWriter output)
            {
                if (OverridePrint)
                    output.Write(ValueString);
                else
                    PhpVariable.Export(value);
            }

            #endregion
        }

        #endregion

        /// <summary>
		/// If the property is an unset <see cref="PhpReference"/>, it is returned (no modification takes place),
		/// otherwise <B>null</B> is returned (and the new value is written to the property).
		/// </summary>
		public virtual PhpReference Set(DObject instance, object value)
		{
            SetterStub((instance == null ? null : instance.InstanceObject), value);
			return null;
		}

		public virtual void EnsureInitialized(ScriptContext/*!*/ context)
		{ }

		public override string MakeFullName()
		{
            var knownProperty = this.Member as KnownProperty;
            return (knownProperty != null) ? knownProperty.FullName : LookupFullName();
		}

        private string LookupFullName()
        {
            // TODO:
            // didn't work:
            //return (_setterStub != null) ?
            //    _setterStub.GetUserEntryPoint.Name.Substring("set_".Length) : _getterStub.GetUserEntryPoint.Name.Substring("get_".Length); 

            // brute force:
            foreach (KeyValuePair<VariableName, DPropertyDesc> pair in DeclaringType.Properties)
            {
                if (pair.Value == this) return pair.Key.ToString();
            }

            return "<Unknown>";
        }

		public override string MakeFullGenericName()
		{
			// properties cannot have generic parameters:
			return MakeFullName();
		}

		#endregion
	}

	#endregion

	#region DPhpFieldDesc

    [DebuggerNonUserCode]
    public sealed class DPhpFieldDesc : DPropertyDesc
	{
		#region Construction

		/// <summary>
		/// Used by type population.
		/// </summary>
		internal DPhpFieldDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes,
			GetterDelegate getterStub, SetterDelegate setterStub)
			: base(declaringType, memberAttributes)
		{
			Debug.Assert(declaringType != null && (getterStub != null || setterStub != null));

			this._getterStub = getterStub;
			this._setterStub = setterStub;
		}

		/// <summary>
		/// Used by full reflection.
		/// </summary>
		internal DPhpFieldDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes)
			: base(declaringType, memberAttributes)
		{
			Debug.Assert(declaringType != null);

			// stubs generated on-demand as dynamic methods
		}

		#endregion

		#region Emission (runtime getter/setter stubs)

		protected override GetterDelegate GenerateGetterStub()
		{
#if SILVERLIGHT
			DynamicMethod stub = new DynamicMethod("<^GetterStub>", Types.Object[0], Types.Object);
#else
			DynamicMethod stub = new DynamicMethod("<^GetterStub>", PhpFunctionUtils.DynamicStubAttributes, CallingConventions.Standard,
				Types.Object[0], Types.Object, this.declaringType.RealType, true);
#endif

            Debug.Assert(Member != null, "Populated field does not have a member!");

            if (this.Member is PhpField)
                PhpFieldBuilder.EmitGetterStub(new ILEmitter(stub), PhpField.RealField, declaringType.RealType);
            else if (this.Member is PhpVisibleProperty)
                PhpFieldBuilder.EmitGetterStub(new ILEmitter(stub), ((PhpVisibleProperty)Member).RealProperty, declaringType.RealType);
            else
                throw new NotImplementedException();

			return (GetterDelegate)stub.CreateDelegate(typeof(GetterDelegate));
		}

		protected override SetterDelegate GenerateSetterStub()
		{
			DynamicMethod stub = new DynamicMethod("<^SetterStub>", PhpFunctionUtils.DynamicStubAttributes, CallingConventions.Standard,
				Types.Void, Types.Object_Object, this.declaringType.RealType, true);

            Debug.Assert(Member != null, "Populated field does not have a member!");

            if (this.Member is PhpField)
			    PhpFieldBuilder.EmitSetterStub(new ILEmitter(stub), PhpField.RealField, declaringType.RealType);
            else if (this.Member is PhpVisibleProperty)
                PhpFieldBuilder.EmitSetterStub(new ILEmitter(stub), ((PhpVisibleProperty)Member).RealProperty, declaringType.RealType);
            else
                throw new NotImplementedException();

			return (SetterDelegate)stub.CreateDelegate(typeof(SetterDelegate));
		}

		#endregion

		#region Run-time Operations

		public override PhpReference Set(DObject instance, object value)
		{
			PhpReference reference = value as PhpReference;
			if (reference != null)
			{
				base.Set(instance, reference);
			}
			else
			{
				reference = base.Get(instance) as PhpReference;

				if (reference != null)
				{
					if (!reference.IsSet) return reference;

					reference.Value = value;
					reference.IsSet = true;
				}
				else base.Set(instance, new PhpSmartReference(value));
			}

			return null;
		}

		public override void EnsureInitialized(ScriptContext/*!*/ context)
		{
			if (IsThreadStatic)
			{
				PhpTypeDesc php_desc = (PhpTypeDesc)declaringType;

				if (IsPrivate) php_desc.EnsureThreadStaticFieldsInitialized(context);
				else
				{
					// invoke __InitializeStaticFields on all PHP base classes, since we don't know which
					// of them implements this field
					do
					{
						php_desc.EnsureThreadStaticFieldsInitialized(context);
						php_desc = php_desc.Base as PhpTypeDesc;
					}
					while (php_desc != null);
				}
			}
		}

		#endregion
	}

	#endregion

	#region DProperty

	[DebuggerNonUserCode]
	public abstract class DProperty : DMember
	{
		public sealed override bool IsDefinite { get { return IsIdentityDefinite; } }

		public DPropertyDesc/*!*/ PropertyDesc { get { return (DPropertyDesc)memberDesc; } }

		public abstract MemberInfo RealMember { get; }

		#region Construction

		/// <summary>
		/// Used by subclasses when creating known routines.
		/// </summary>
		public DProperty(DPropertyDesc/*!*/ propertyDesc)
			: base(propertyDesc)
		{
		}

		/// <summary>
		/// Used by subclasses when creating unknown routines.
		/// </summary>
		public DProperty(string/*!*/ fullName)
			: base(null, fullName)
		{
			Debug.Assert(IsUnknown);
		}

		#endregion

		#region Utils

		internal override void ReportAbstractNotImplemented(ErrorSink/*!*/ errors, DType/*!*/ declaringType, PhpType/*!*/ referringType)
		{
			errors.Add(Errors.AbstractPropertyNotImplemented, referringType.Declaration.SourceUnit,
				referringType.Declaration.Position, referringType.FullName, declaringType.MakeFullGenericName(), this.FullName);

			ReportError(errors, Errors.RelatedLocation);
		}

		#endregion

		#region Emission

		internal abstract PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool wantRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck);

		internal abstract AssignmentCallback EmitSet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool isRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck);

		internal abstract void EmitUnset(CodeGenerator/*!*/ codeGenerator, IPlace/*!*/ instance,
			ConstructedType constructedType, bool runtimeVisibilityCheck);

		#endregion
	}

	#endregion

	#region KnownProperty

	[DebuggerNonUserCode]
	public abstract class KnownProperty : DProperty
	{
		public override bool IsUnknown { get { return false; } }

		public VariableName Name { get { return name; } }
		private VariableName name;

		#region Construction

		/// <summary>
		/// Used by subclasses.
		/// </summary>
		protected KnownProperty(DPropertyDesc/*!*/ propertyDesc, VariableName name)
			: base(propertyDesc)
		{
			this.name = name;

			// TODO
		}

		#endregion

		public override string GetFullName()
		{
			return name.Value;
		}

		#region Analysis

		internal override DMemberRef GetImplementationInSuperTypes(DType/*!*/ type, bool searchSupertypes, ref bool inSupertype)
		{
			while (type != null && !type.IsUnknown)
			{
				KnownProperty result = type.GetDeclaredProperty<KnownProperty>(this.Name);
				if (result != null)
				{
					// private members are not visible from subtype:
					if (result.IsPrivate && inSupertype) break;

					return new DMemberRef(result, type);
				}

				if (!searchSupertypes) break;
				inSupertype = true;
				type = type.Base;
			}

			inSupertype = false;
			return null;
		}

		#endregion
	}

	#endregion

	#region UnknownProperty

	public sealed class UnknownProperty : DProperty
	{
		public override bool IsUnknown { get { return true; } }
		public override bool IsIdentityDefinite { get { return false; } }

		public override MemberInfo RealMember { get { return null; } }

		public override DType/*!*/ DeclaringType { get { return declaringType; } }
		private readonly DType/*!*/ declaringType;

		/// <summary>
		/// Used by the compiler for unresolved properties.
		/// </summary>
		public UnknownProperty(DType/*!*/ declaringType, string/*!*/ name)
			: base(name)
		{
			Debug.Assert(declaringType != null && name != null);

			this.declaringType = declaringType;
		}

		public override string GetFullName()
		{
			Debug.Fail("Full name is set by .ctor");
			throw null;
		}

		#region Emission

		internal override PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool wantRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			return codeGenerator.EmitGetStaticPropertyOperator(declaringType, this.FullName, null, wantRef);
		}

		internal override AssignmentCallback EmitSet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool isRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			return codeGenerator.EmitSetStaticPropertyOperator(declaringType, this.FullName, null, isRef);
		}

		internal override void EmitUnset(CodeGenerator/*!*/ codeGenerator, IPlace instance,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			codeGenerator.EmitUnsetStaticPropertyOperator(declaringType, this.FullName, null);
		}

		#endregion
	}

	#endregion

	#region PhpField, PhpFieldBuilder, PhpVisibleProperty

	public sealed class PhpField : KnownProperty, IPhpMember
	{
		#region Properties

		public override bool IsIdentityDefinite { get { return true; } }

		public override MemberInfo RealMember { get { return realField; } }

		public FieldInfo RealField { get { return realField; } }
		public FieldBuilder RealFieldBuilder { get { return (FieldBuilder)realField; } }
		private FieldInfo realField;

		public PropertyInfo ExportedProperty { get { return exportedProperty; } }
		public PropertyBuilder ExportedPropertyBuilder { get { return (PropertyBuilder)exportedProperty; } }
		private PropertyInfo exportedProperty;

		/// <summary>
		/// Should be used internally for validation only. Valid fields doesn't have these attributes.
		/// </summary>
		private new bool IsAbstract { get { return memberDesc.IsAbstract; } }
		private new bool IsFinal { get { return memberDesc.IsFinal; } }

		/// <summary>
		/// Error reporting.
		/// <c>Position.Invalid</c> for reflected PHP fields.
		/// </summary>
		public Position Position { get { return position; } }
		private readonly Position position;

		/// <summary>
		/// Error reporting (for partial classes).
		/// <B>null</B> for reflected PHP fields.
		/// </summary>
		public SourceUnit SourceUnit { get { return sourceUnit; } }
		private readonly SourceUnit sourceUnit;

		public bool HasInitialValue { get { return hasInitialValue; } }
		private readonly bool hasInitialValue;

		/// <summary>
		/// Returns the <see cref="PhpType"/> that implements this field, i.e. provides storage for it.
		/// </summary>
		public PhpType/*!*/ Implementor
		{
			get
			{
				if (implementor == null)
				{
					Debug.Assert(builder != null);

                    PhpField overriding;
					
                    // Find the type, that provides storage for the field
                    if (this.IsStatic || overrides == null ||
                        (overriding = overrides.Member as PhpField) == null || overriding.IsPrivate)
                        implementor = DeclaringPhpType;         // use/create FieldInfo in declaring type
					else
                        implementor = overriding.Implementor;   // use FieldInfo from derivated type
				}
				return implementor;
			}
		}
		private PhpType implementor;

		/// <summary>
		/// Returns <B>true</B> iff this is a public field overriding a protected field.
		/// </summary>
		/// <remarks>Valid after <see cref="DefineBuilders"/>.</remarks>
		public bool UpgradesVisibility { get { return upgradesVisibility; } }
		private bool upgradesVisibility;

		internal DMemberRef Overrides { get { return overrides; } set /* PhpType.Validate */ { overrides = value; } }
		private DMemberRef overrides;

		internal List<DMemberRef> Implements { get { return implements; } }
		private List<DMemberRef> implements;

		internal PhpFieldBuilder Builder { get { return builder; } }
		private readonly PhpFieldBuilder builder;

		/// <summary>
		/// Gets whether the field is exported.
		/// </summary>
		internal bool IsExported { get { return builder.ExportInfo != null || this.DeclaringPhpType.IsExported; } }

		#endregion

		#region Construction

		/// <summary>
		/// Used by compiler.
		/// </summary>
		public PhpField(VariableName name, DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes,
			bool hasInitialValue, SourceUnit/*!*/ sourceUnit, Position position)
			: base(new DPhpFieldDesc(declaringType, memberAttributes), name)
		{
			this.hasInitialValue = hasInitialValue;
			this.position = position;
			this.sourceUnit = sourceUnit;
			this.builder = new PhpFieldBuilder(this);
		}

		/// <summary>
		/// Used by full reflection.
		/// </summary>
		public PhpField(VariableName name, DPropertyDesc/*!*/ fieldDesc, FieldInfo/*!*/ fieldInfo,
			PropertyInfo exportedProperty)
			: base(fieldDesc, name)
		{
			Debug.Assert(fieldDesc is DPhpFieldDesc);

			this.realField = fieldInfo;
			this.exportedProperty = exportedProperty;
			this.hasInitialValue = realField.IsDefined(typeof(PhpHasInitValueAttribute), false);
			this.builder = null;

			this.implementor = DeclaringPhpType;
		}

		/// <summary>
		/// Used by full reflection for fields that are not implemented by their declaring type.
		/// <seealso cref="PhpPublicFieldAttribute"/>
		/// </summary>
		public PhpField(VariableName name, DPropertyDesc/*!*/ fieldDesc, DPropertyDesc/*!*/ implementingFieldDesc,
			bool hasInitialValue, PropertyInfo exportedProperty)
			: base(fieldDesc, name)
		{
			Debug.Assert(fieldDesc is DPhpFieldDesc);

			this.realField = implementingFieldDesc.PhpField.RealField;
			this.exportedProperty = exportedProperty;
			this.hasInitialValue = hasInitialValue;
			this.builder = null;

			this.implementor = implementingFieldDesc.DeclaringType.PhpType;
			this.upgradesVisibility = (IsPublic && implementingFieldDesc.IsProtected);
		}

		#endregion

		#region Utils

		internal override void ReportError(ErrorSink/*!*/ sink, ErrorInfo error)
		{
			if (sourceUnit != null)
				sink.Add(error, SourceUnit, position);
		}

		#endregion

		#region Analysis

		internal override void AddAbstractOverride(DMemberRef/*!*/ abstractProperty)
		{
			if (abstractProperty.Member.DeclaringType.IsInterface)
			{
				if (implements == null)
					implements = new List<DMemberRef>();

				implements.Add(abstractProperty);
			}
			else
				overrides = abstractProperty;
		}

		#endregion

		#region Validation

		internal void Validate(SourceUnit/*!*/ sourceUnit, ErrorSink/*!*/ errors)
		{
			Debug.Assert(position.IsValid);

			// no abstract fields:
			if (IsAbstract)
			{
				errors.Add(Errors.PropertyDeclaredAbstract, SourceUnit, position);
				memberDesc.MemberAttributes &= ~PhpMemberAttributes.Abstract;
			}

			// no final fields:
			if (IsFinal)
			{
				errors.Add(Errors.PropertyDeclaredFinal, SourceUnit, position);
				memberDesc.MemberAttributes &= ~PhpMemberAttributes.Final;
			}
		}

		internal void ValidateOverride(ErrorSink/*!*/ errors, KnownProperty/*!*/ overridden)
		{
			Debug.Assert(sourceUnit != null, "Not applicable on reflected properties");

			// TODO:
			//  string overridden_field_pos = String.Concat(overridden_field.Class.FullSourcePath, overridden_field.Position);

            // static field cannot be made non static
            if (overridden.IsStatic && !this.IsStatic)
            {
                errors.Add(Errors.MakeStaticPropertyNonStatic, SourceUnit, Position,
                  overridden.DeclaringType.FullName, this.Name.ToString(), this.DeclaringType.FullName);
            }

            // decrease visibility is prohibited
            if ((overridden.IsPublic && (this.IsPrivate || this.IsProtected)) ||
                (overridden.IsProtected && (this.IsPrivate)))
            {
                errors.Add(Errors.OverridingFieldRestrictsVisibility, SourceUnit, Position,
                  overridden.DeclaringType.FullName, this.Name.ToString(), overridden.Visibility.ToString().ToLowerInvariant(), this.DeclaringType.FullName);
            }

			//  // field non-staticness non-overridable
			//  if (!overridden_field.FieldModifiers.IsStatic && field.FieldModifiers.IsStatic)
			//  {
			//    classesTable.Errors.Add(Errors.MakeNonStaticFieldStatic, classesTable.SourceFile, field.Position,
			//      overridden_field_class.Name.ToString(), field.FieldName.ToString(), Name.ToString(), overridden_field_pos);
			//  }

			//  // overriding field visibility
			//  if ((overridden_field.FieldModifiers.IsPublic && !field.FieldModifiers.IsPublic)
			//    || (overridden_field.FieldModifiers.IsProtected && !field.FieldModifiers.IsProtected && !field.FieldModifiers.IsPublic))
			//  {
			//    classesTable.Errors.Add(Errors.OverridingFieldRestrictsVisibility, classesTable.SourceFile, field.Position,
			//      Name.ToString(), field.FieldName.ToString(), overridden_field.FieldModifiers.VisibilityToString(), overridden_field_class.Name.ToString(), overridden_field_pos);
			//  }

			//  // protected and public static field overriding
			//  if (overridden_field.FieldModifiers.IsStatic && field.FieldModifiers.IsStatic
			//    && ((overridden_field.FieldModifiers.IsProtected && field.FieldModifiers.IsProtected)
			//    || (overridden_field.FieldModifiers.IsPublic && field.FieldModifiers.IsPublic)))
			//  {
			//    classesTable.Errors.Add(Errors.OverridingStaticFieldByStatic, classesTable.SourceFile, field.Position, overridden_field_class.Name.ToString(),
			//      field.FieldName.ToString(), Name.ToString(), field.FieldModifiers.VisibilityToString(), overridden_field_pos);
			//  }

			//  // protected static field initial value
			//  // we can check only first field overrided in the hierarchy. If the overrided field is protected,
			//  // this field must be public (otherwise previous exception has been thrown), so there is not other
			//  // protected field in the hierarchy above.
			//  if (field.FieldModifiers.IsStatic && field.HasInitValue && field.FieldModifiers.IsPublic &&
			//    overridden_field.FieldModifiers.IsStatic && overridden_field.HasInitValue && overridden_field.FieldModifiers.IsProtected)
			//  {
			//    classesTable.Errors.Add(Errors.OverridingProtectedStaticWithInitValue, classesTable.SourceFile, field.Position,
			//      overridden_field_class.Name.ToString(), field.FieldName.ToString(), Name.ToString(), overridden_field_pos);
			//  }

			//  // if this field increases visibility of inherited field, set class from where is the field accessible from IL
			//  if (field.FieldModifiers.IsPublic && overridden_field.FieldModifiers.IsProtected)
			//    field.SetImplementClass(overridden_field_class);
		}

		#endregion

		#region Emission

		internal void DefineBuilders()
		{
			TypeBuilder type_builder = this.DeclaringPhpType.RealTypeBuilder;

			if (this.Implementor == this.DeclaringPhpType)
			{
				// represent the field as a PhpReference field
				FieldAttributes field_attrs = Enums.ToFieldAttributes(memberDesc.MemberAttributes);

				string name = FullName;
				if (IsExported) name += "#";

				FieldBuilder fb = type_builder.DefineField(name, Types.PhpReference[0], field_attrs);

				// custom attributes implied by member attributes (thread static):
				Enums.DefineCustomAttributes(memberDesc.MemberAttributes, fb);

				// [EditorBrowsable(Never)]
#if !SILVERLIGHT
				fb.SetCustomAttribute(AttributeBuilders.EditorBrowsableNever);
#endif

				// [PhpHasInitValue]
				if (hasInitialValue) fb.SetCustomAttribute(AttributeBuilders.PhpHasInitValue);

				realField = fb;
			}
			else
			{
				PhpField overriden_field = (PhpField)overrides.Member;
				realField = overriden_field.RealField;

				// does this field declaration increase visibility of the field?
				if (IsPublic && overriden_field.IsProtected)
				{
					upgradesVisibility = true;

					// yes, set attribute that marks that the class has a public field declaration that increases
					// visibility of previous declaration
					type_builder.SetCustomAttribute(new CustomAttributeBuilder(
						Constructors.PhpPublicField,
						new object[] { Name.ToString(), IsStatic, hasInitialValue }));
				}
			}

			if (IsExported) exportedProperty = ClrStubBuilder.DefineFieldExport(FullName, this);
		}

		private void AdjustConstructedType(ref ConstructedType constructedType)
		{
			if (constructedType == null) return;

			DTypeDesc implementor_desc = Implementor.TypeDesc;

			// adjust constructed type according to the implementor
			// TODO: this does not work

			while (constructedType.GenericType != implementor_desc)
			{
				constructedType = constructedType.Base as ConstructedType;
				if (constructedType == null) return;
			}
		}

		private PhpTypeCode EmitGetInternal(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool wantRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck, bool setAliasedFlag)
		{
			ILEmitter il = codeGenerator.IL;

			if (IsStatic)
			{
				if (runtimeVisibilityCheck || UpgradesVisibility)
				{
					// let the operator to check the visibility:
					return codeGenerator.EmitGetStaticPropertyOperator(DeclaringType, this.FullName, null, wantRef);
				}

				if (!IsAppStatic) Implementor.EmitThreadStaticInit(codeGenerator, constructedType);

				// retrieve field value
				il.Emit(OpCodes.Ldsfld, DType.MakeConstructed(RealField, constructedType));
				if (wantRef)
				{
					if (setAliasedFlag)
					{
						// set IsAliased to true
						il.Emit(OpCodes.Dup);
						il.Emit(OpCodes.Ldc_I4_1);
						il.EmitCall(OpCodes.Callvirt, Properties.PhpReference_IsAliased.GetSetMethod(), null);
					}

					return PhpTypeCode.PhpReference;
				}
				else
				{
					il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);

					return PhpTypeCode.Object;
				}
			}
			else
			{
				// LOAD Operators.GetObjectFieldDirect[Ref](this,this.<field>,<name>,<type desc>,[<quiet>]);
				codeGenerator.EmitLoadSelf();
				instance.EmitLoad(il);
				il.Emit(OpCodes.Ldfld, DType.MakeConstructed(RealField, constructedType));
				il.Emit(OpCodes.Ldstr, Name.ToString());
				codeGenerator.EmitLoadClassContext();

				if (wantRef)
				{
					il.Emit(OpCodes.Call, Methods.Operators.GetObjectFieldDirectRef);

					return PhpTypeCode.PhpReference;
				}
				else
				{
					il.LoadBool(codeGenerator.ChainBuilder.QuietRead);
					il.Emit(OpCodes.Call, Methods.Operators.GetObjectFieldDirect);

					return PhpTypeCode.Object;
				}
			}
		}

		internal override PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool wantRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			Debug.Assert(IsStatic == (instance == null));

			AdjustConstructedType(ref constructedType);
			return EmitGetInternal(codeGenerator, instance, wantRef, constructedType, runtimeVisibilityCheck, true);
		}

		internal override AssignmentCallback EmitSet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool isRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			Debug.Assert(IsStatic == (instance == null));

			AdjustConstructedType(ref constructedType);

			if (IsStatic)
			{
				// check the visibility at runtime by the operator:
				if (runtimeVisibilityCheck || UpgradesVisibility)
					return codeGenerator.EmitSetStaticPropertyOperator(DeclaringType, this.FullName, null, isRef);

				if (isRef)
				{
					if (!IsAppStatic) Implementor.EmitThreadStaticInit(codeGenerator, constructedType);

					// just write the PhpReference to the field upon assignment
					return delegate(CodeGenerator codeGen, PhpTypeCode stackTypeCode)
					{
						codeGen.IL.Emit(OpCodes.Stsfld, DType.MakeConstructed(RealField, constructedType));
					};
				}
				else
				{
					// read the PhpReference stored in the field
					EmitGetInternal(codeGenerator, null, true, constructedType, false, false);

					// finish the assignment by writing to its Value field
					return delegate(CodeGenerator codeGen, PhpTypeCode stackTypeCode)
					{
						codeGen.IL.Emit(OpCodes.Stfld, Fields.PhpReference_Value);
					};
				}
			}
			else
			{
				// direct access is possible, however, we have to be prepared for actually calling
				// the operator if the field proves to have been unset

				return delegate(CodeGenerator codeGen, PhpTypeCode stackTypeCode)
				{
					ILEmitter il = codeGen.IL;

					codeGen.EmitLoadSelf();
					instance.EmitLoad(il);
					il.Emit(isRef ? OpCodes.Ldflda : OpCodes.Ldfld, DType.MakeConstructed(RealField, constructedType));
					il.Emit(OpCodes.Ldstr, Name.ToString());
					codeGen.EmitLoadClassContext();

					if (isRef)
					{
						// CALL Operators.SetObjectFieldDirectRef(STACK,<target>,ref <field>,<field name>,<type desc>)
						il.Emit(OpCodes.Call, Methods.Operators.SetObjectFieldDirectRef);
					}
					else
					{
						// CALL Operators.SetObjectFieldDirect(STACK,<target>,<field>,<field name>,<type desc>)
						il.Emit(OpCodes.Call, Methods.Operators.SetObjectFieldDirect);
					}
				};
			}
		}

		internal override void EmitUnset(CodeGenerator/*!*/ codeGenerator, IPlace/*!*/ instance,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			ILEmitter il = codeGenerator.IL;

			if (IsStatic)
			{
				// emit error (whether or not the property is visible):
				il.Emit(OpCodes.Ldstr, DeclaringType.FullName);
				il.Emit(OpCodes.Ldstr, this.FullName);
				codeGenerator.EmitPhpException(Methods.PhpException.StaticPropertyUnset);
				return;
			}

			// replace the field with a new PhpSmartReference with IsSet false
			instance.EmitLoad(il);
			il.Emit(OpCodes.Newobj, Constructors.PhpSmartReference.Void);
			il.Emit(OpCodes.Dup);
			
			il.LoadBool(false);
			il.Emit(OpCodes.Callvirt, Properties.PhpReference_IsSet.GetSetMethod());

			il.Emit(OpCodes.Stfld, DType.MakeConstructed(RealField, constructedType));
		}

		#endregion
	}

	internal sealed class PhpFieldBuilder
	{
		public PhpField/*!*/ Field { get { return field; } }
		private PhpField/*!*/ field;

		internal ExportAttribute ExportInfo
		{
			get { return exportInfo; }
			set /* FieldDecl */ { exportInfo = value; }
		}
		private ExportAttribute exportInfo;

		public PhpFieldBuilder(PhpField/*!*/ field)
		{
			this.field = field;
		}

		internal static void EmitGetterStub(ILEmitter/*!*/ il, FieldInfo/*!*/ fieldInfo, Type/*!*/ declaringType)
		{
			if (fieldInfo.IsStatic)
			{
				// [ return <real_field> ]
				il.Emit(OpCodes.Ldsfld, fieldInfo);
			}
			else
			{
				// [ return ((self)<instance>).<real_field> ]
				il.Ldarg(0);
				il.Emit(OpCodes.Castclass, declaringType);
				il.Emit(OpCodes.Ldfld, fieldInfo);
			}
			il.Emit(OpCodes.Ret);
		}

		internal static void EmitSetterStub(ILEmitter/*!*/ il, FieldInfo/*!*/ fieldInfo, Type/*!*/ declaringType)
		{
			Debug.Assert(fieldInfo.FieldType == Types.PhpReference[0]);

			if (fieldInfo.IsStatic)
			{
				// [ <real_field> = (PhpReference)value ]
				il.Ldarg(1);
				il.Emit(OpCodes.Castclass, Types.PhpReference[0]);
				il.Emit(OpCodes.Stsfld, fieldInfo);
			}
			else
			{
				// [ ((self)<instance>).<real_field> = (PhpReference)value ]
				il.Ldarg(0);
				il.Emit(OpCodes.Castclass, declaringType);
				il.Ldarg(1);
				il.Emit(OpCodes.Castclass, Types.PhpReference[0]);
				il.Emit(OpCodes.Stfld, fieldInfo);
			}
			il.Emit(OpCodes.Ret);
		}

        internal static void EmitGetterStub(ILEmitter/*!*/ il, PropertyInfo/*!*/ propertyInfo, Type/*!*/ declaringType)
        {
            var getter = propertyInfo.GetGetMethod(true);

            if (getter == null)
            {
                il.Emit(OpCodes.Ldstr, declaringType.Name);
                il.Emit(OpCodes.Ldstr, "get_" + propertyInfo.Name);
                il.Emit(OpCodes.Call, Methods.PhpException.UndefinedMethodCalled);
                il.Emit(OpCodes.Ldnull);

                il.Emit(OpCodes.Ret);
                return;
            }

            if (getter.IsStatic)
            {
                // [ return getter() ]
                il.Emit(OpCodes.Call, getter);
            }
            else
            {
                // [ return ((self)<instance>).getter() ]
                il.Ldarg(0);
                il.Emit(OpCodes.Castclass, declaringType);
                il.Emit(OpCodes.Call, getter);
            }

            // box
            il.EmitBoxing(PhpTypeCodeEnum.FromType(getter.ReturnType));

            //
            il.Emit(OpCodes.Ret);
        }

        internal static void EmitSetterStub(ILEmitter/*!*/ il, PropertyInfo/*!*/ propertyInfo, Type/*!*/ declaringType)
        {
            var setter = propertyInfo.GetSetMethod(/*false*/);

            if (setter == null)
            {
                il.Emit(OpCodes.Ldstr, declaringType.Name);
                il.Emit(OpCodes.Ldstr, "set_" + propertyInfo.Name);
                il.Emit(OpCodes.Call, Methods.PhpException.UndefinedMethodCalled);  // CoreResources.readonly_property_written
                
                il.Emit(OpCodes.Ret);
                return;
            }

            var parameters = setter.GetParameters();
            Debug.Assert(parameters.Length == 1 /*&& parameters[0].ParameterType == Types.PhpReference[0]*/);

            if (!setter.IsStatic)
            {
                // [ ((self)<instance>). ]
                il.Ldarg(0);
                il.Emit(OpCodes.Castclass, declaringType);
            }
            
            // [ setter((object)value) ]
            il.Ldarg(1);
            il.Emit(OpCodes.Castclass, parameters[0].ParameterType);
            il.Emit(OpCodes.Call, setter);
            
            //
            il.Emit(OpCodes.Ret);
        }
	}

    /// <summary>
    /// Used by full reflection for class properties marked with [PhpVisible] attribute.
    /// </summary>
    public sealed class PhpVisibleProperty : KnownProperty
    {
        #region Fields and Properties

        public override bool IsIdentityDefinite { get { return true; } }
        public override MemberInfo RealMember { get { return realProperty; } }

        public PropertyInfo/*!*/RealProperty { get { return realProperty; } }
        private PropertyInfo/*!*/realProperty;

        #endregion

        #region Construction

        /// <summary>
        /// Used by full reflection.
        /// </summary>
        public PhpVisibleProperty(VariableName name, DPropertyDesc/*!*/ fieldDesc, PropertyInfo/*!*/ propertyInfo)
            : base(fieldDesc, name)
        {
            Debug.Assert(fieldDesc is DPhpFieldDesc);
            Debug.Assert(propertyInfo != null);

            this.realProperty = propertyInfo;
        }

        #endregion

        #region Emission (during compilation)

        /// <summary>
        /// Emit (stack_top dup).IsAliased = true;
        /// </summary>
        /// <param name="il"></param>
        private void EmitIsAliased(ILEmitter/*!*/il)
        {
            // set IsAliased to true
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldc_I4_1);
            il.EmitCall(OpCodes.Callvirt, Properties.PhpReference_IsAliased.GetSetMethod(), null);
        }

        internal override PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool wantRef,
            ConstructedType constructedType, bool runtimeVisibilityCheck)
        {
            Debug.Assert(IsStatic == (instance == null));

            ILEmitter il = codeGenerator.IL;
            var getter = RealProperty.GetGetMethod();

            if (getter == null)
                throw new MissingMethodException(string.Format("'{0}.get_{1}' not implemented!", RealProperty.DeclaringType.Name, RealProperty.Name));

            // <this>.
            if (!IsStatic)
                instance.EmitLoad(il);

            // getter()
            il.Emit(OpCodes.Call, getter);
            
            // handle references
            if (wantRef)
            {
                // make reference
                if (Types.PhpReference[0].IsAssignableFrom(getter.ReturnType))
                {
                    EmitIsAliased(il);
                }
                else
                {
                    throw new NotImplementedException();
                }
                //
                return PhpTypeCode.PhpReference;
            }
            else
            {
                // dereference
                if (Types.PhpReference[0].IsAssignableFrom(getter.ReturnType))
                {
                    EmitIsAliased(il);
                    il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
                }
                else
                {
                    il.EmitBoxing(PhpTypeCodeEnum.FromType(getter.ReturnType));
                }
                //
                return PhpTypeCode.Object;
            }
        }

        internal override AssignmentCallback EmitSet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool isRef,
            ConstructedType constructedType, bool runtimeVisibilityCheck)
        {
            ILEmitter il = codeGenerator.IL;
            var setter = RealProperty.GetSetMethod();

            if (setter == null)
                throw new MissingMethodException(string.Format("'{0}.set_{1}' not implemented!", RealProperty.DeclaringType.Name, RealProperty.Name));

            // <this>.
            if (!IsStatic)
                instance.EmitLoad(il);

            // setter()
            return delegate(CodeGenerator codeGen, PhpTypeCode stackTypeCode)
            {
                var parameters = setter.GetParameters();

                if (isRef && parameters[0].ParameterType != Types.PhpReference[0])
                {
                    // .setter(<stack>.Value)
                    codeGen.IL.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
                    codeGen.IL.Emit(OpCodes.Call, setter);
                }
                else if (!isRef && parameters[0].ParameterType == Types.PhpReference[0])
                {
                    // .getter().Value = <stack>
                    codeGen.IL.Emit(OpCodes.Call, RealProperty.GetGetMethod());
                    codeGen.IL.Emit(OpCodes.Stfld, Fields.PhpReference_Value);
                }
                else
                {
                    // .setter(<stack>)
                    codeGen.IL.Emit(OpCodes.Call, setter);
                }
            };
        }

        internal override void EmitUnset(CodeGenerator/*!*/ codeGenerator, IPlace/*!*/ instance, ConstructedType constructedType, bool runtimeVisibilityCheck)
        {
            ILEmitter il = codeGenerator.IL;

            if (IsStatic)
            {
                // emit error (whether or not the property is visible):
                il.Emit(OpCodes.Ldstr, DeclaringType.FullName);
                il.Emit(OpCodes.Ldstr, this.FullName);
                codeGenerator.EmitPhpException(Methods.PhpException.StaticPropertyUnset);
                return;
            }

            throw new NotImplementedException();
        }

        #endregion
    }

	#endregion

	#region ClrPropertyBase

	[DebuggerNonUserCode]
	public abstract class ClrPropertyBase : KnownProperty
	{
		public override bool IsIdentityDefinite { get { return true; } }

		#region Construction

		/// <summary>
		/// Used by full-reflect.
		/// </summary>
		public ClrPropertyBase(DPropertyDesc/*!*/ propertyDesc, VariableName name)
			: base(propertyDesc, name)
		{
		}

		#endregion
	}

	#endregion

	#region ClrProperty

	[DebuggerNonUserCode]
	public sealed class ClrProperty : ClrPropertyBase
	{
		public override MemberInfo/*!*/ RealMember { get { return realProperty; } }

		public PropertyInfo/*!*/ RealProperty { get { return realProperty; } }
		private PropertyInfo/*!*/ realProperty;

		public bool HasGetter { get { return hasGetter; } }
		private readonly bool hasGetter;

		public bool HasSetter { get { return hasSetter; } }
		private readonly bool hasSetter;

		public MethodInfo Getter { get { return (hasGetter) ? realProperty.GetGetMethod(true) : null; } }
		public MethodInfo Setter { get { return (hasSetter) ? realProperty.GetSetMethod(true) : null; } }

		#region Construction

		/// <summary>
		/// Used by full-reflect.
		/// </summary>
		public ClrProperty(VariableName name, ClrTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes,
			PropertyInfo/*!*/ realProperty, bool hasGetter, bool hasSetter)
			: base(new DPropertyDesc(declaringType, memberAttributes), name)
		{
			Debug.Assert(realProperty != null);

			this.realProperty = realProperty;
			this.hasGetter = hasGetter;
			this.hasSetter = hasSetter;
		}

		#endregion

		#region Analysis: Overrides

		internal override void AddAbstractOverride(DMemberRef/*!*/ abstractMethod)
		{
			// nop, we don't need to maintain information about abstract overrides
		}

		#endregion

		#region Emission

		internal override PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool wantRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			Debug.Assert(IsStatic == (instance == null));
			Debug.Assert(hasGetter, "TODO");

			ILEmitter il = codeGenerator.IL;

			if (IsStatic)
			{
				if (runtimeVisibilityCheck)
				{
					// let the operator to check the visibility:
					return codeGenerator.EmitGetStaticPropertyOperator(DeclaringType, this.FullName, null, wantRef);
				}
			}
			else
			{
				instance.EmitLoad(il);
			}

			MethodInfo getter = this.Getter;

			il.Emit(getter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, getter);

			PhpTypeCode result = ClrOverloadBuilder.EmitConvertToPhp(il, getter.ReturnType/*, codeGenerator.ScriptContextPlace*/);

			codeGenerator.EmitReferenceDereference(ref result, wantRef);
			return result;
		}

		internal override AssignmentCallback EmitSet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool isRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			Debug.Assert(IsStatic == (instance == null));

			//Debug.Assert(hasSetter, "TODO");
            if (!hasSetter)
            {
                throw new CompilerException(
                    new ErrorInfo(
                           0,
                           "readonly_property_written",
                           ErrorSeverity.Error),
                    new string[]{DeclaringType.FullName, this.Name.Value}
                    );
            }

            //
            
			if (IsStatic)
			{
				// check the visibility at runtime by the operator:
				if (runtimeVisibilityCheck)
					return codeGenerator.EmitSetStaticPropertyOperator(DeclaringType, this.FullName, null, isRef);
			}
			else
			{
				// load target instance:
				instance.EmitLoad(codeGenerator.IL);
			}

			return delegate(CodeGenerator codeGen, PhpTypeCode stackTypeCode)
			{
				MethodInfo setter = this.Setter;

				// TODO: can we get different PhpTypeCode?
				ILEmitter il = codeGen.IL;
				PropertyDesc.EmitSetConversion(il, stackTypeCode, setter.GetParameters()[0].ParameterType);

				il.Emit(setter.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, setter);
			};
		}

		internal override void EmitUnset(CodeGenerator codeGenerator/*!*/, IPlace/*!*/ instance,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			// TODO:
		}

		#endregion
	}

	#endregion

	#region ClrField

	[DebuggerNonUserCode]
	public sealed class ClrField : ClrPropertyBase
	{
		public override MemberInfo/*!*/ RealMember { get { return fieldInfo; } }

		public FieldInfo/*!*/ FieldInfo { get { return fieldInfo; } }
		private readonly FieldInfo/*!*/ fieldInfo;

		#region Construction

		/// <summary>
		/// Used by full-reflect.
		/// </summary>
		public ClrField(VariableName/*!*/ name, ClrTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes,
			FieldInfo/*!*/ fieldInfo)
			: base(new DPropertyDesc(declaringType, memberAttributes), name)
		{
			this.fieldInfo = fieldInfo;
		}

		#endregion

		#region Emission

		internal override PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool wantRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			Debug.Assert(IsStatic == (instance == null));

			ILEmitter il = codeGenerator.IL;

			if (IsStatic)
			{
				if (runtimeVisibilityCheck)
				{
					// let the operator to check the visibility:
					return codeGenerator.EmitGetStaticPropertyOperator(DeclaringType, this.FullName, null, wantRef);
				}
				il.Emit(OpCodes.Ldsfld, fieldInfo);
			}
			else
			{
				instance.EmitLoad(il);
				il.Emit(OpCodes.Ldfld, fieldInfo);
			}

			PhpTypeCode result = ClrOverloadBuilder.EmitConvertToPhp(il, fieldInfo.FieldType/*, codeGenerator.ScriptContextPlace*/);

			codeGenerator.EmitReferenceDereference(ref result, wantRef);
			return result;
		}

		internal override AssignmentCallback EmitSet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool isRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			Debug.Assert(IsStatic == (instance == null));

			if (IsStatic)
			{
				// check the visibility at runtime by the operator:
				if (runtimeVisibilityCheck)
					return codeGenerator.EmitSetStaticPropertyOperator(DeclaringType, this.FullName, null, isRef);
			}
			else
			{
				// load target instance:
				instance.EmitLoad(codeGenerator.IL);
			}

			return delegate(CodeGenerator codeGen, PhpTypeCode stackTypeCode)
			{
				// TODO: can we get different PhpTypeCode?
				ILEmitter il = codeGen.IL;
				PropertyDesc.EmitSetConversion(il, stackTypeCode, fieldInfo.FieldType);

				il.Emit(IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo);
			};
		}

		internal override void EmitUnset(CodeGenerator/*!*/ codeGenerator, IPlace/*!*/ instance,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			// TODO:
		}

		#endregion
	}

	#endregion

	#region ClrEvent

	[DebuggerNonUserCode]
	public sealed class ClrEvent : ClrPropertyBase
	{
		public override MemberInfo/*!*/ RealMember { get { return realEvent; } }

		public EventInfo/*!*/ RealEvent { get { return realEvent; } }
		private EventInfo/*!*/ realEvent;

		public Type/*!*/ HandlerType { get { return realEvent.EventHandlerType; } }

		public bool HasAddMethod { get { return hasAddMethod; } }
		private readonly bool hasAddMethod;

		public bool HasRemoveMethod { get { return hasRemoveMethod; } }
		private readonly bool hasRemoveMethod;

		public MethodInfo AddMethod { get { return (hasAddMethod) ? realEvent.GetAddMethod(true) : null; } }
		public MethodInfo RemoveMethod { get { return (hasRemoveMethod) ? realEvent.GetRemoveMethod(true) : null; } }

		#region Construction

		/// <summary>
		/// Used by full-reflect.
		/// </summary>
		public ClrEvent(VariableName name, ClrTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes,
			EventInfo/*!*/ realEvent, bool hasAddMethod, bool hasRemoveMethod)
			: base(new DPropertyDesc(declaringType, memberAttributes), name)
		{
			this.realEvent = realEvent;
			this.hasAddMethod = hasAddMethod;
			this.hasRemoveMethod = hasRemoveMethod;
		}

		#endregion

		#region Analysis: Overrides

		internal override void AddAbstractOverride(DMemberRef/*!*/ abstractMethod)
		{
			// nop, we don't need to maintain information about abstract overrides
		}

		#endregion

		#region Emission

		internal override PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool wantRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			Debug.Assert(IsStatic == (instance == null));

			ILEmitter il = codeGenerator.IL;

			if (IsStatic && runtimeVisibilityCheck)
			{
				// let the operator to check the visibility:
				return codeGenerator.EmitGetStaticPropertyOperator(DeclaringType, this.FullName, null, wantRef);
			}

			EmitGetEventObject(il, codeGenerator.ScriptContextPlace, instance, false);

			if (wantRef) il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
			return (wantRef ? PhpTypeCode.PhpReference : PhpTypeCode.DObject);
		}

		internal void EmitGetEventObject(ILEmitter/*!*/ il, IPlace/*!*/ contextPlace, IPlace instance, bool dynamicStub)
		{
			// [ ClrEventObject<handlerType>.Wrap(<SC>, <event name>, <addMethod>, <removeMethod>) ]

			contextPlace.EmitLoad(il);
			il.Emit(OpCodes.Ldstr, FullName);

			ConstructorInfo hook_ctor = typeof(Library.EventClass<>.HookDelegate).MakeGenericType(HandlerType).
				GetConstructor(Types.DelegateCtorArgs);

			// create delegates to the add and remove methods
			EmitLoadAccessorDelegate(il, hook_ctor, instance, dynamicStub, AddMethod);
			EmitLoadAccessorDelegate(il, hook_ctor, instance, dynamicStub, RemoveMethod);

			MethodInfo wrap_method = typeof(Library.EventClass<>).MakeGenericType(HandlerType).GetMethod("Wrap");
			il.Emit(OpCodes.Call, wrap_method);
		}

		private void EmitLoadAccessorDelegate(ILEmitter/*!*/ il, ConstructorInfo/*!*/ delegateCtor,
			IPlace instance, bool dynamicStub, MethodInfo accessor)
		{
			if (accessor != null)
			{
				if (instance != null) instance.EmitLoad(il); else il.Emit(OpCodes.Ldnull);
				if (!dynamicStub && accessor.IsVirtual)
				{
					instance.EmitLoad(il);
					il.Emit(OpCodes.Ldvirtftn, accessor);
				}
				else il.Emit(OpCodes.Ldftn, accessor);

				il.Emit(OpCodes.Newobj, delegateCtor);
			}
			else il.Emit(OpCodes.Ldnull);
		}

		internal override AssignmentCallback EmitSet(CodeGenerator/*!*/ codeGenerator, IPlace instance, bool isRef,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			Debug.Fail();
			return null;
		}

		internal override void EmitUnset(CodeGenerator/*!*/ codeGenerator, IPlace/*!*/ instance,
			ConstructedType constructedType, bool runtimeVisibilityCheck)
		{
			Debug.Fail();
		}

		#endregion
	}

	#endregion
}
