/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;
using System.IO;

namespace PHP.Core.AST
{
	#region TypeRef

	/// <summary>
	/// Represents a use of a class name in <c>new</c> and <c>instanceof</c> constructs.
	/// </summary>
	public abstract class TypeRef : LangElement
	{
		internal static readonly List<TypeRef>/*!*/ EmptyList = new List<TypeRef>(1);

		public abstract DType ResolvedType { get; }

        /// <summary>
        /// <see cref="ResolvedType"/> or new instance of <see cref="UnknownType"/> if the type was not resolved.
        /// </summary>
        internal DType/*!A*/ResolvedTypeOrUnknown { get { return this.ResolvedType ?? new UnknownType(string.Empty, this); } }

		public List<TypeRef>/*!*/ GenericParams { get { return genericParams; } }
		protected readonly List<TypeRef>/*!*/ genericParams;

        public GenericQualifiedName GenericQualifiedName
        {
            get
            {
                if (this.GenericParams.Count == 0)
                {
                    return new GenericQualifiedName(this.QualifiedName);
                }
                else
                {
                    object[] genericParamNames = new object[this.GenericParams.Count];
                    for (int i = 0; i < genericParamNames.Length; i++)
                        genericParamNames[i] = this.GenericParams[i].GenericQualifiedName;

                    return new GenericQualifiedName(this.QualifiedName, genericParamNames); 
                }
            }
        }

		public TypeRef(Position position, List<TypeRef>/*!*/ genericParams)
			: base(position)
		{
			Debug.Assert(genericParams != null);

			this.genericParams = genericParams;
		}

        /// <summary>
		/// Resolves generic arguments.
		/// </summary>
		/// <returns><B>true</B> iff all arguments are resolvable to types or constructed types (none is variable).</returns>
		internal virtual bool Analyze(Analyzer/*!*/ analyzer)
		{
			bool result = true;

			foreach (TypeRef arg in genericParams)
				result &= arg.Analyze(analyzer);

			return result;
		}

        internal abstract QualifiedName QualifiedName { get; }

		internal abstract void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags);

		/// <summary>
		/// Emits code that loads type descriptors for all generic arguments and a call to 
		/// <see cref="Operators.MakeGenericTypeInstantiation"/>.
		/// </summary>
		internal void EmitMakeGenericInstantiation(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			ILEmitter il = codeGenerator.IL;

			il.EmitOverloadedArgs(Types.DTypeDesc[0], genericParams.Count, Methods.Operators.MakeGenericTypeInstantiation.ExplicitOverloads, delegate(ILEmitter eil, int i)
			{
				genericParams[i].EmitLoadTypeDesc(codeGenerator, flags);
			});

			if (genericParams.Count > 0)
				il.Emit(OpCodes.Call, Methods.Operators.MakeGenericTypeInstantiation.Overload(genericParams.Count));
		}

		/// <summary>
		/// Gets the static type reference or <B>null</B> if the reference cannot be resolved at compile time.
		/// </summary>
		internal abstract object ToStaticTypeRef(ErrorSink/*!*/ errors, SourceUnit/*!*/ sourceUnit);

		internal static object[]/*!!*/ ToStaticTypeRefs(List<TypeRef>/*!*/ typeRefs, ErrorSink/*!*/ errors, SourceUnit/*!*/ sourceUnit)
		{
			if (typeRefs.Count == 0) return ArrayUtils.EmptyObjects;

			object[] result = new object[typeRefs.Count];

			for (int i = 0; i < typeRefs.Count; i++)
			{
				result[i] = typeRefs[i].ToStaticTypeRef(errors, sourceUnit);

				if (result[i] == null)
				{
					errors.Add(Errors.GenericParameterMustBeType, sourceUnit, typeRefs[i].Position);
					result[i] = PrimitiveType.Object;
				}
			}

			return result;
		}

        internal virtual void DumpTo(AstVisitor/*!*/ visitor, TextWriter/*!*/ output)
        {
            output.Write(this.QualifiedName.ToString());
        }
	}

	#endregion

	#region PrimitiveTypeRef

	/// <summary>
	/// Primitive type reference.
	/// </summary>
	public sealed class PrimitiveTypeRef : TypeRef
	{
		public override DType/*!*/ ResolvedType { get { return type; } }
		private PrimitiveType/*!*/ type;
        public PrimitiveType/*!*/ Type { get { return type; } }

		public PrimitiveTypeRef(Position position, PrimitiveType/*!*/ type)
			: base(position, TypeRef.EmptyList)
		{
			this.type = type;
		}

		internal override object ToStaticTypeRef(ErrorSink/*!*/ errors, SourceUnit/*!*/ sourceUnit)
		{
			return type;
		}

		internal override bool Analyze(Analyzer/*!*/ analyzer)
		{
			return true;
		}

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			type.EmitLoadTypeDesc(codeGenerator, ResolveTypeFlags.SkipGenericNameParsing);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitPrimitiveTypeRef(this);
        }

        internal override QualifiedName QualifiedName
        {
            get { return this.Type.QualifiedName; }
        }
	}

	#endregion

	#region DirectTypeRef

	/// <summary>
	/// Direct use of class name.
	/// </summary>
	public sealed class DirectTypeRef : TypeRef
	{
		public QualifiedName ClassName { get { return className; } }
		private QualifiedName className;

		public override DType ResolvedType { get { return resolvedType; } }
		private DType/*! after analysis */ resolvedType;

        internal override QualifiedName QualifiedName
        {
            get { return this.ClassName; }
        }

		internal override object ToStaticTypeRef(ErrorSink/*!*/ errors, SourceUnit/*!*/ sourceUnit)
		{
			return new GenericQualifiedName(className, TypeRef.ToStaticTypeRefs(genericParams, errors, sourceUnit));
		}

		public DirectTypeRef(Position position, QualifiedName className, List<TypeRef>/*!*/ genericParams)
			: base(position, genericParams)
		{
			this.className = className;
		}

        internal static DirectTypeRef/*!*/FromGenericQualifiedName(Position position, GenericQualifiedName genericQualifiedName)
        {
            var genericParams =
                genericQualifiedName.GenericParams.Select<object, TypeRef>(obj =>
                    {
                        if (obj is PrimitiveType)
                            return new PrimitiveTypeRef(Position.Invalid, (PrimitiveType)obj);

                        if (obj is GenericQualifiedName)
                            return FromGenericQualifiedName(Position.Invalid, (GenericQualifiedName)obj);

                        return new PrimitiveTypeRef(Position.Invalid, PrimitiveType.Object);
                    });

            return new DirectTypeRef(position, genericQualifiedName.QualifiedName, genericParams.ToList());
        }

		#region Analysis

		internal override bool Analyze(Analyzer/*!*/ analyzer)
		{
			resolvedType = analyzer.ResolveTypeName(className, analyzer.CurrentType, analyzer.CurrentRoutine, position, false);

			// base call must follow the class name resolution:
			bool args_static = base.Analyze(analyzer);

			if (args_static)
			{
				DTypeDesc[] resolved_arguments = (genericParams.Count > 0) ? new DTypeDesc[genericParams.Count] : DTypeDesc.EmptyArray;
				for (int i = 0; i < genericParams.Count; i++)
					resolved_arguments[i] = genericParams[i].ResolvedType.TypeDesc;

				resolvedType = resolvedType.MakeConstructedType(analyzer, resolved_arguments, position);
			}

			return args_static;
		}

		#endregion

		#region Emission

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			ILEmitter il = codeGenerator.IL;
			Debug.Assert(resolvedType != null);

			// disallow generic parameters on generic type which already has generic arguments:
			resolvedType.EmitLoadTypeDesc(codeGenerator, flags |
					  ((genericParams.Count > 0) ? ResolveTypeFlags.SkipGenericNameParsing : 0));

			// constructed type already emited its generic parameters:
			if (!(resolvedType is ConstructedType))
				EmitMakeGenericInstantiation(codeGenerator, flags);
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitDirectTypeRef(this);
        }
	}

	#endregion

	#region IndirectTypeRef

	/// <summary>
	/// Indirect use of class name (through variable).
	/// </summary>
	public sealed class IndirectTypeRef : TypeRef
	{
		public override DType ResolvedType { get { return null; } }

        /// <summary>
        /// <see cref="VariableUse"/> which value in runtime contains the name of the type.
        /// </summary>
        public VariableUse/*!*/ ClassNameVar { get { return this.classNameVar; } }
        private readonly VariableUse/*!*/ classNameVar;

        internal override QualifiedName QualifiedName
        {
            get { return new QualifiedName(Name.EmptyBaseName); }
        }

		public IndirectTypeRef(Position position, VariableUse/*!*/ classNameVar, List<TypeRef>/*!*/ genericParams)
			: base(position, genericParams)
		{
			Debug.Assert(classNameVar != null && genericParams != null);

			this.classNameVar = classNameVar;
		}

		internal override object ToStaticTypeRef(ErrorSink/*!*/ errors, SourceUnit/*!*/ sourceUnit)
		{
			return null;
		}

		#region Analysis

		internal override bool Analyze(Analyzer/*!*/ analyzer)
		{
			classNameVar.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

			// base call must follow the class name resolve:
			base.Analyze(analyzer);

			// indirect:
			return false;
		}

		#endregion

		#region Emission

		internal override void EmitLoadTypeDesc(CodeGenerator/*!*/ codeGenerator, ResolveTypeFlags flags)
		{
			// disallow generic parameters on generic type which already has generic arguments:
			codeGenerator.EmitLoadTypeDescOperator(null, classNameVar, flags |
				((genericParams.Count > 0) ? ResolveTypeFlags.SkipGenericNameParsing : 0));

			EmitMakeGenericInstantiation(codeGenerator, flags);
		}

		#endregion

        internal override void DumpTo(AstVisitor visitor, TextWriter output)
        {
            output.Write('{');
            this.classNameVar.DumpTo(visitor, output);
            output.Write('}');
        }

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitIndirectTypeRef(this);
        }
	}

	#endregion

	#region NewEx

	/// <summary>
	/// <c>new</c> expression.
	/// </summary>
    public sealed class NewEx : VarLikeConstructUse
	{
		internal override Operations Operation { get { return Operations.New; } }

		internal override bool AllowsPassByReference { get { return true; } }

		private TypeRef/*!*/ classNameRef;
		private CallSignature callSignature;
		private DRoutine constructor;
		private bool runtimeVisibilityCheck;
		private bool typeArgsResolved;
        /// <summary>Type of class being instantiated</summary>
        public TypeRef /*!*/ ClassNameRef { get { return classNameRef; } }
        /// <summary>Call signature of constructor</summary>
        public CallSignature CallSignature { get { return callSignature; } }

		public NewEx(Position position, TypeRef/*!*/ classNameRef, List<ActualParam>/*!*/ parameters)
			: base(position)
		{
			Debug.Assert(classNameRef != null && parameters != null);
			this.classNameRef = classNameRef;
			this.callSignature = new CallSignature(parameters, TypeRef.EmptyList);
		}

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
            Debug.Assert(this.IsMemberOf == null);

			access = info.Access;

			this.typeArgsResolved = classNameRef.Analyze(analyzer);

			DType type = classNameRef.ResolvedType;
			RoutineSignature signature;

			if (typeArgsResolved)
				analyzer.AnalyzeConstructedType(type);

			if (type != null)
			{
				bool error_reported = false;

				// make checks if we are sure about character of the type:
				if (type.IsIdentityDefinite)
				{
					if (type.IsAbstract || type.IsInterface)
					{
						analyzer.ErrorSink.Add(Errors.AbstractClassOrInterfaceInstantiated, analyzer.SourceUnit,
							position, type.FullName);
						error_reported = true;
					}
				}

                // disallow instantiation of Closure
                if (type.RealType == typeof(PHP.Library.SPL.Closure))
                {
                    analyzer.ErrorSink.Add(Errors.ClosureInstantiated, analyzer.SourceUnit, position, type.FullName);
                    error_reported = true;
                }

				// type name resolved, look the constructor up:
				constructor = analyzer.ResolveConstructor(type, position, analyzer.CurrentType, analyzer.CurrentRoutine,
				  out runtimeVisibilityCheck);

				if (constructor.ResolveOverload(analyzer, callSignature, position, out signature) == DRoutine.InvalidOverloadIndex)
				{
					if (!error_reported)
					{
						analyzer.ErrorSink.Add(Errors.ClassHasNoVisibleCtor, analyzer.SourceUnit,
						  position, type.FullName);
					}
				}
			}
			else
			{
				signature = UnknownSignature.Default;
			}

			callSignature.Analyze(analyzer, signature, info, false);

			return new Evaluation(this);
		}

		internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
			return false;
		}

		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("NewEx");

			PhpTypeCode result;

			if (classNameRef.ResolvedType != null && typeArgsResolved)
			{
				// constructor is resolvable (doesn't mean that known) //

				result = classNameRef.ResolvedType.EmitNew(codeGenerator, null, constructor, callSignature, runtimeVisibilityCheck);
			}
			else
			{
				// constructor is unresolvable (a variable is used in type name => type is unresolvable as well) //

				codeGenerator.EmitNewOperator(null, classNameRef, null, callSignature);
				result = PhpTypeCode.Object;
			}

			codeGenerator.EmitReturnValueHandling(this, false, ref result);
			return result;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitNewEx(this);
        }
	}

	#endregion

	#region InstanceOfEx

	/// <summary>
	/// <c>instanceof</c> expression.
	/// </summary>
	public sealed class InstanceOfEx : Expression
	{
		internal override Operations Operation { get { return Operations.InstanceOf; } }

		private Expression/*!*/ expression;
        /// <summary>Expression being tested</summary>
        public Expression /*!*/ Expression { get { return expression; } }
        private TypeRef/*!*/ classNameRef;
        /// <summary>Type to test if <see cref="Expression"/> is of</summary>
        public TypeRef/*!*/ ClassNameRef { get { return classNameRef; } }
		private bool typeArgsResolved;

		public InstanceOfEx(Position position, Expression/*!*/ expression, TypeRef/*!*/ classNameRef)
			: base(position)
		{
			Debug.Assert(expression != null && classNameRef != null);

			this.expression = expression;
			this.classNameRef = classNameRef;
		}

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			expression = expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

			typeArgsResolved = classNameRef.Analyze(analyzer);

			if (typeArgsResolved)
				analyzer.AnalyzeConstructedType(classNameRef.ResolvedType);

			return new Evaluation(this);
		}

		internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
			return false;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("InstanceOfEx");

			// emits load of expression value on the stack:
			codeGenerator.EmitBoxing(expression.Emit(codeGenerator));

			if (classNameRef.ResolvedType != null && typeArgsResolved)
			{
				// type is resolvable (doesn't mean known) //

				classNameRef.ResolvedType.EmitInstanceOf(codeGenerator, null);
			}
			else
			{
				// type is unresolvable (there is some variable or the type is a generic parameter) //

				codeGenerator.EmitInstanceOfOperator(null, classNameRef, null);
			}

			if (access == AccessType.None)
			{
				codeGenerator.IL.Emit(OpCodes.Pop);
				return PhpTypeCode.Void;
			}
			else
			{
				return PhpTypeCode.Boolean;
			}
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitInstanceOfEx(this);
        }
	}

	#endregion

	#region TypeOfEx

	/// <summary>
	/// <c>typeof</c> expression.
	/// </summary>
	public sealed class TypeOfEx : Expression
	{
		internal override Operations Operation { get { return Operations.TypeOf; } }

		public TypeRef/*!*/ ClassNameRef { get { return classNameRef; } }
		private TypeRef/*!*/ classNameRef;

		public bool/*!*/ TypeArgsResolved { get { return typeArgsResolved; } }
		private bool typeArgsResolved;

		public TypeOfEx(Position position, TypeRef/*!*/ classNameRef)
			: base(position)
		{
			Debug.Assert(classNameRef != null);

			this.classNameRef = classNameRef;
		}

		internal override Evaluation Analyze(Analyzer/*!*/ analyzer, ExInfoFromParent info)
		{
			access = info.Access;

			typeArgsResolved = classNameRef.Analyze(analyzer);

			if (typeArgsResolved)
				analyzer.AnalyzeConstructedType(classNameRef.ResolvedType);

			return new Evaluation(this);
		}

		internal override bool IsCustomAttributeArgumentValue
		{
			get
			{
				return classNameRef.ResolvedType != null && typeArgsResolved && classNameRef.ResolvedType.IsDefinite;
			}
		}

		internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
		{
			return false;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("TypeOfEx");

			if (classNameRef.ResolvedType != null && typeArgsResolved)
			{
				// type is resolvable (doesn't mean known) //

				classNameRef.ResolvedType.EmitTypeOf(codeGenerator, null);
			}
			else
			{
				// type is unresolvable (there is some variable or the type is a generic parameter) //

				codeGenerator.EmitTypeOfOperator(null, classNameRef, null);
			}

			if (access == AccessType.None)
			{
				codeGenerator.IL.Emit(OpCodes.Pop);
				return PhpTypeCode.Void;
			}
			else
			{
				return PhpTypeCode.DObject;
			}
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitTypeOfEx(this);
        }
	}

	#endregion	
}
