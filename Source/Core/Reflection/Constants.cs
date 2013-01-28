/*

 Copyright (c) 2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;

using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using PHP.Core.Parsers;
using PHP.Core.Emit;

namespace PHP.Core.Reflection
{
	#region DConstantDesc

	public sealed class DConstantDesc : DMemberDesc
	{
		/// <summary>
		/// Written-up by the analyzer if the value is evaluable (literals only).
		/// </summary>
		public object LiteralValue
		{
			get
			{
                Debug.Assert(!ValueIsDeferred, "This constant's literal value cannot be accessed directly. You have to read its realField in runtime after you initialize static fields.");
				return literalValue;
			}
			internal /* friend DConstant */ set
			{
				Debug.Assert(value is int || value is string || value == null || value is bool || value is double || value is long);
				this.literalValue = value;
                this.ValueIsDeferred = false;
			}
		}
		private object literalValue;

        public GlobalConstant GlobalConstant { get { return (GlobalConstant)Member; } }
		public ClassConstant ClassConstant { get { return (ClassConstant)Member; } }

		#region Construction

		/// <summary>
		/// Used by compiler for global constants.
		/// </summary>
		public DConstantDesc(DModule/*!*/ declaringModule, PhpMemberAttributes memberAttributes, object literalValue)
			: base(declaringModule.GlobalType.TypeDesc, memberAttributes | PhpMemberAttributes.Static)
		{
			Debug.Assert(declaringModule != null);
			this.literalValue = literalValue;
		}

		/// <summary>
		/// Used by compiler for class constants.
		/// </summary>
		public DConstantDesc(DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes, object literalValue)
			: base(declaringType, memberAttributes | PhpMemberAttributes.Static)
		{
			Debug.Assert(declaringType != null);
			this.literalValue = literalValue;
		}

		#endregion

		public override string MakeFullName()
		{
			Debug.Fail();
			return null;
		}

		public override string MakeFullGenericName()
		{
			Debug.Fail();
			return null;
        }

        #region Run-Time Operations

        /// <summary>
        /// <c>True</c> if value of this constant is deferred to runtime; hence it must be read from corresponding static field every time.
        /// </summary>
        internal bool ValueIsDeferred { get; set; }

        /// <summary>
        /// Read value of this constant.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public object GetValue(ScriptContext/*!*/ context)
        {
            if (ValueIsDeferred)
            {
                if (Member.GetType() == typeof(ClassConstant) && DeclaringType.GetType() == typeof(PhpTypeDesc))
                {
                    ((PhpTypeDesc)DeclaringType).EnsureThreadStaticFieldsInitialized(context);
                    return ((ClassConstant)Member).GetValue();
                }

                if (memberAttributes.GetType() == typeof(GlobalConstant))
                {
                    // TODO: initialize deferred global constant
                    return ((ClassConstant)Member).GetValue();
                }

                Debug.Fail("Uncaught constant type.");
            }

            //
            return this.LiteralValue;
        }

        #endregion
    }

	#endregion

	#region DConstant

	public abstract class DConstant : DMember
	{
		public sealed override bool IsDefinite { get { return IsIdentityDefinite; } }

		public DConstantDesc/*!*/ ConstantDesc { get { return (DConstantDesc)memberDesc; } }

		/// <summary>
		/// Whether the value of the constant is known and stored in the constant-desc.
		/// </summary>
		public abstract bool HasValue { get; }

		/// <summary>
		/// Constant value. Valid only if <see cref="HasValue"/> is <B>true</B>.
		/// </summary>
		public object Value { get { return ConstantDesc.LiteralValue; } }


		#region Construction

		/// <summary>
		/// Used by known constant subclasses.
		/// </summary>
		public DConstant(DConstantDesc/*!*/ constantDesc)
			: base(constantDesc)
		{
			Debug.Assert(constantDesc != null);
		}

		/// <summary>
		/// Used by unknown constants subclasses.
		/// </summary>
		public DConstant(string/*!*/ fullName)
			: base(null, fullName)
		{
			Debug.Assert(IsUnknown);
		}

		#endregion

		internal virtual void ReportCircularDefinition(ErrorSink/*!*/ errors)
		{
			Debug.Fail();
		}

		internal abstract PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
            bool runtimeVisibilityCheck, string fallbackName);
	}

	#endregion

	#region UnknownClassConstant, UnknownGlobalConstant

	public sealed class UnknownClassConstant : DConstant
	{
		public override bool IsUnknown { get { return true; } }
		public override bool IsIdentityDefinite { get { return false; } }
		public override bool HasValue { get { return false; } }

		public override DType/*!*/ DeclaringType { get { return declaringType; } }
		private readonly DType/*!*/ declaringType;

		public UnknownClassConstant(DType/*!*/ declaringType, string/*!*/ fullName)
			: base(fullName)
		{
			Debug.Assert(fullName != null);
			this.declaringType = declaringType;
		}

		public override string GetFullName()
		{
			return FullName;
		}

		internal override PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
			bool runtimeVisibilityCheck, string fallbackName)
		{
            Debug.Assert(fallbackName == null);

            codeGenerator.EmitGetConstantValueOperator(declaringType, this.FullName, null);
			return PhpTypeCode.Object;
		}
	}

	public sealed class UnknownGlobalConstant : DConstant
	{
		public override bool IsUnknown { get { return true; } }
		public override bool IsIdentityDefinite { get { return false; } }
		public override bool HasValue { get { return false; } }

		public UnknownGlobalConstant(string/*!*/ fullName)
			: base(fullName)
		{
			Debug.Assert(fullName != null);
		}

		public override string GetFullName()
		{
			return FullName;
		}

		internal override PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
            bool runtimeVisibilityCheck, string fallbackName)
		{
			codeGenerator.EmitGetConstantValueOperator(null, this.FullName, fallbackName);
			return PhpTypeCode.Object;
		}
	}

	#endregion

	#region KnownConstant

	public abstract class KnownConstant : DConstant, IPhpMember
	{
		public sealed override bool IsUnknown { get { return false; } }

		/// <summary>
		/// Whether the value of the constant is known and stored in the constant-desc.
		/// </summary>
        public sealed override bool HasValue { get { return node == null && !ConstantDesc.ValueIsDeferred; } }

		/// <summary>
		/// Real storage of the constant (a field).
		/// </summary>
		public FieldInfo RealField { get { return realField; } }
		public FieldBuilder RealFieldBuilder { get { return (FieldBuilder)realField; } }
		protected FieldInfo realField;

        private Func<object>/*!*/GetterStub { get { if (getterStub == null) GenerateGetterStub(); return getterStub; } }
        private Func<object>/*!*/getterStub = null;

		/// <summary>
		/// AST node representing the constant. Used for evaluation only.
		/// </summary>
		internal AST.ConstantDecl Node { get { return node; } }
		private AST.ConstantDecl node;

		public abstract Position Position { get; }
		public abstract SourceUnit SourceUnit { get; }

		internal ExportAttribute ExportInfo { get { return exportInfo; } set { exportInfo = value; } }
		internal /* protected */ ExportAttribute exportInfo;

		/// <summary>
		/// Gets whether the constant is exported.
		/// </summary>
		internal abstract bool IsExported { get; }

		public KnownConstant(DConstantDesc/*!*/ constantDesc)
			: base(constantDesc)
		{
			Debug.Assert(constantDesc != null);
			this.node = null;
		}

		internal void SetValue(object value)
		{
			this.ConstantDesc.LiteralValue = value;
			this.node = null;
		}

		internal void SetNode(AST.ConstantDecl/*!*/ node)
		{
			this.ConstantDesc.LiteralValue = null;
			this.node = node;
		}

        private void GenerateGetterStub()
        {
            Debug.Assert(this.realField != null);
            Debug.Assert(this.realField.FieldType == typeof(object));

            DynamicMethod stub = new DynamicMethod("<^GetterStub>", this.realField.FieldType, Type.EmptyTypes, true);
            ILEmitter il = new ILEmitter(stub);

            il.Emit(OpCodes.Ldsfld, this.realField);
            il.Emit(OpCodes.Ret);
            this.getterStub = (Func<object>)stub.CreateDelegate(typeof(Func<object>));
        }

		#region Emission

		internal override PhpTypeCode EmitGet(CodeGenerator/*!*/ codeGenerator, ConstructedType constructedType,
            bool runtimeVisibilityCheck, string fallbackName)
		{
			ILEmitter il = codeGenerator.IL;

			if (HasValue)
			{
				il.LoadLiteral(Value);
                return PhpTypeCodeEnum.FromObject(Value);
			}
			else
			{
				Debug.Assert(realField != null);

                il.Emit(OpCodes.Ldsfld, DType.MakeConstructed(realField, constructedType));
                return PhpTypeCodeEnum.FromType(realField.FieldType);
			}
		}

		#endregion

        #region Run-Time Operations

        internal object GetValue()
        {
            Debug.Assert(realField != null);
            return GetterStub();
        }

        #endregion
	}

	#endregion

	#region GlobalConstant

	/// <summary>
	/// Pure mode global constants, namespace constants, CLR constants, library constants.
	/// </summary>
	public sealed class GlobalConstant : KnownConstant, IDeclaree
	{
		#region Statics

		public static readonly GlobalConstant Null;
		public static readonly GlobalConstant False;
		public static readonly GlobalConstant True;

        public static readonly GlobalConstant PhpIntSize;
        public static readonly GlobalConstant PhpIntMax;

		static GlobalConstant()
		{
			if (UnknownModule.RuntimeModule == null) UnknownModule.RuntimeModule = new UnknownModule();

            Null = new GlobalConstant(QualifiedName.Null, Fields.PhpVariable_LiteralNull);
			Null.SetValue(null);
            False = new GlobalConstant(QualifiedName.False, Fields.PhpVariable_LiteralFalse);
			False.SetValue(false);
            True = new GlobalConstant(QualifiedName.True, Fields.PhpVariable_LiteralTrue);
			True.SetValue(true);

            PhpIntSize = new GlobalConstant(new QualifiedName(new Name("PHP_INT_SIZE")), typeof(PhpVariable).GetField("LiteralIntSize"));
            PhpIntSize.SetValue(PhpVariable.LiteralIntSize);
            PhpIntMax = new GlobalConstant(new QualifiedName(new Name("PHP_INT_MAX")), typeof(int).GetField("MaxValue"));
            PhpIntMax.SetValue(int.MaxValue);
		}

		#endregion

		#region Properties

		public override bool IsIdentityDefinite
		{
			get { return declaration == null || !declaration.IsConditional; }
		}

		public IPhpModuleBuilder DeclaringModuleBuilder { get { return (IPhpModuleBuilder)DeclaringModule; } }

		/// <summary>
		/// Note: the base name is case-sensitive.
		/// </summary>
		public QualifiedName QualifiedName { get { return qualifiedName; } }
		private readonly QualifiedName qualifiedName;

		public Declaration Declaration { get { return declaration; } }
		private Declaration declaration;

		public VersionInfo Version { get { return version; } set { version = value; } }
		private VersionInfo version;

		public override Position Position { get { return declaration.Position; } }
		public override SourceUnit SourceUnit { get { return declaration.SourceUnit; } }

        /// <summary>
        /// If constant defined within &lt;script&gt; type, remember its builder to define constant field there.
        /// In case of pure or transient module, this is null. If this is null, the constant is declared in as CLR global.
        /// </summary>
        private TypeBuilder scriptTypeBuilder = null;

        /// <summary>
        /// Name of the extension where this global constant was defined.
        /// </summary>
        public string Extension
        {
            get
            {
                PhpLibraryModule libraryModule = DeclaringModule as PhpLibraryModule;

                if (libraryModule != null)
                    return libraryModule.GetImplementedExtension(realField.DeclaringType);
                else
                    return null;
            }
        }

		internal override bool IsExported
		{
			get
			{
				return exportInfo != null;
			}
		}

		#endregion

		#region Construction

        /// <summary>
        /// Used for constants created by run-time, but with known declaring module
        /// </summary>
        internal GlobalConstant(DModule/*!*/ declaringModule, QualifiedName qualifiedName, FieldInfo info)
            : base(new DConstantDesc(declaringModule, PhpMemberAttributes.None, null))
        {
            this.realField = info;
            this.qualifiedName = qualifiedName;
        }


		/// <summary>
		/// Used for constants created by run-time.
		/// </summary>
		internal GlobalConstant(QualifiedName qualifiedName, FieldInfo info)
			: base(new DConstantDesc(UnknownModule.RuntimeModule, PhpMemberAttributes.None, null))
		{
			this.realField = info;
            this.qualifiedName = qualifiedName;
		}

		/// <summary>
		/// Used by compiler.
		/// </summary>
		public GlobalConstant(QualifiedName qualifiedName, PhpMemberAttributes memberAttributes,
            SourceUnit/*!*/ sourceUnit, bool isConditional, Scope scope, Position position)
			: base(new DConstantDesc(sourceUnit.CompilationUnit.Module, memberAttributes, null))
		{
            Debug.Assert(sourceUnit != null);

			this.qualifiedName = qualifiedName;
			this.declaration = new Declaration(sourceUnit, this, false, isConditional, scope, position);
            //this.origin = origin;

            if (sourceUnit.CompilationUnit is ScriptCompilationUnit)    // J: place the constant into <script> type so it can be reflected properly
                scriptTypeBuilder = ((ScriptCompilationUnit)sourceUnit.CompilationUnit).ScriptBuilder.ScriptTypeBuilder;
		}

		#endregion

		public override string GetFullName()
		{
			return qualifiedName.ToString();
		}

		internal override void ReportCircularDefinition(ErrorSink/*!*/ errors)
		{
			errors.Add(Errors.CircularConstantDefinitionGlobal, SourceUnit, Position, FullName);
		}

		public void ReportRedeclaration(ErrorSink/*!*/ errors)
		{
			errors.Add(FatalErrors.ConstantRedeclared, SourceUnit, Position, FullName);
		}

		internal void DefineBuilders()
		{
			if (realField == null)
			{
                // resolve attributes
                FieldAttributes field_attrs = Enums.ToFieldAttributes(memberDesc.MemberAttributes);
                field_attrs |= FieldAttributes.Literal;

                Debug.Assert((field_attrs & FieldAttributes.Static) != 0);

                // convert name to CLR notation:
                var clrName = qualifiedName.ToClrNotation(0, 0);

                // type
                Type type = Types.Object[0];
                if (this.HasValue && this.Value != null)
                    type = this.Value.GetType();

                // define public static const field:
                if (scriptTypeBuilder != null)  // const in SSA or MSA
                {
                    realField = scriptTypeBuilder.DefineField(clrName, type, field_attrs);
                }
                else // const in Pure or Transient
                {
                    ModuleBuilder module_builder = this.DeclaringModuleBuilder.AssemblyBuilder.RealModuleBuilder;

                    // represent the class constant as a static initonly field

                    realField = ReflectionUtils.DefineGlobalField(module_builder, clrName, type, field_attrs);
                }

                Debug.Assert(realField != null);

                // set value
                if (this.HasValue)
                    ((FieldBuilder)realField).SetConstant(this.Value);
			}
		}

		internal DConstantDesc Bake()
		{
			// TODO: rereflection
			return this.ConstantDesc;
		}
	}

	#endregion

	#region ClassConstant

	public sealed class ClassConstant : KnownConstant
	{
		public override bool IsIdentityDefinite { get { return true; } }

		public VariableName Name { get { return name; } }
		private readonly VariableName name;

		/// <summary>
		/// Error reporting.
		/// <see cref="ShortPosition.Invalid"/> for reflected PHP methods.
		/// </summary>
		public override Position Position { get { return position; } }
		private readonly Position position;

		/// <summary>
		/// Error reporting (for partial classes).
		/// <B>null</B> for reflected PHP methods.
		/// </summary>
		public override SourceUnit SourceUnit { get { return sourceUnit; } }
		private SourceUnit sourceUnit;

		internal override bool IsExported
		{
			get
			{ return exportInfo != null || this.DeclaringPhpType.IsExported; }
		}

		#region Construction

		/// <summary>
		/// Used by compiler.
		/// </summary>
		public ClassConstant(VariableName name, DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes,
			SourceUnit/*!*/ sourceUnit, Position position)
			: base(new DConstantDesc(declaringType, memberAttributes, null))
		{
			Debug.Assert(declaringType != null);

			this.name = name;
			this.position = position;
			this.sourceUnit = sourceUnit;
		}

		/// <summary>
		/// Used by full-reflect (CLR).
		/// </summary>
		public ClassConstant(VariableName name, DTypeDesc/*!*/ declaringType, PhpMemberAttributes memberAttributes)
			: base(new DConstantDesc(declaringType, memberAttributes, null))
		{
			Debug.Assert(declaringType != null);

			this.name = name;
		}

		/// <summary>
		/// Used by full-reflect (PHP).
		/// </summary>
		public ClassConstant(VariableName name, DConstantDesc/*!*/ constantDesc, FieldInfo/*!*/ fieldInfo)
			: base(constantDesc)
		{
			this.name = name;
			this.realField = fieldInfo;
		}

		#endregion

		public override string GetFullName()
		{
			return name.Value;
		}

		internal override void ReportCircularDefinition(ErrorSink/*!*/ errors)
		{
			errors.Add(Errors.CircularConstantDefinitionClass, SourceUnit, Position, DeclaringType.FullName, FullName);
		}

		/// <summary>
		/// Checks whether a specified name is valid constant name. 
		/// </summary>
		/// <param name="name">The constant name.</param>
		/// <seealso cref="PhpVariable.IsValidName"/>
		public static bool IsValidName(string name)
		{
			return PhpVariable.IsValidName(name);
		}

		public void Validate(ErrorSink/*!*/ errors)
		{
			// nop
		}

		internal void DefineBuilders()
		{
			if (realField == null)
			{
				TypeBuilder type_builder = this.DeclaringPhpType.RealTypeBuilder;

				// represent the class constant as a static initonly field
				FieldAttributes field_attrs = Enums.ToFieldAttributes(memberDesc.MemberAttributes);
                Type field_type = Types.Object[0];
                if (this.HasValue)
                {
                    var value = this.Value;
                    if (value == null || value is int || value is double || value is string || value is long || value is bool)
                    {
                        if (value != null)
                            field_type = value.GetType();

                        field_attrs |= FieldAttributes.Literal;
                    }
                    else
                    {
                        field_attrs |= FieldAttributes.InitOnly;
                    }
                }
                
				string name = FullName;
				if (IsExported) name += "#";

				FieldBuilder fb = type_builder.DefineField(name, field_type, field_attrs);

				// [EditorBrowsable(Never)] for user convenience - not on silverlight
                // [ThreadStatic] for deferred constants
#if !SILVERLIGHT
				if (IsExported)
					fb.SetCustomAttribute(AttributeBuilders.EditorBrowsableNever);
                if (!this.HasValue) // constant initialized for every request separatelly (same as static PHP field)
                    fb.SetCustomAttribute(AttributeBuilders.ThreadStatic);
#endif

				realField = fb;
			}
        }

        #region Emission

        internal override PhpTypeCode EmitGet(CodeGenerator codeGenerator, ConstructedType constructedType, bool runtimeVisibilityCheck, string fallbackName)
        {
            if (!HasValue)
            {
                // __InitializeStaticFields to ensure, this deferred constant has been initialized (same as thread static field):
                DeclaringPhpType.EmitThreadStaticInit(codeGenerator, constructedType);
            }

            return base.EmitGet(codeGenerator, constructedType, runtimeVisibilityCheck, fallbackName);
        }

        #endregion
    }

	#endregion
}
