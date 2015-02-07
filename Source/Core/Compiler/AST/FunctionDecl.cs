/*

 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	#region FormalParam

	/// <summary>
	/// Represents a formal parameter definition.
	/// </summary>
	public sealed class FormalParam : LangElement, IPhpCustomAttributeProvider
	{
		/// <summary>
		/// Name of the argument.
		/// </summary>
		public VariableName Name { get { return name; } }
		private VariableName name;

		/// <summary>
		/// Whether the parameter is &amp;-modified.
		/// </summary>
        public bool PassedByRef { get { return passedByRef; } }
		private bool passedByRef;

		/// <summary>
		/// Whether the parameter is an out-parameter. Set by applying the [Out] attribute.
		/// </summary>
        public bool IsOut { get { return isOut; } }
		private bool isOut;

		/// <summary>
		/// Initial value expression. Can be <B>null</B>.
		/// </summary>
        public Expression InitValue { get { return initValue; } }
		private Expression initValue;

		/// <summary>
		/// Either <see cref="PrimitiveType"/>, <see cref="GenericQualifiedName"/>, or <B>null</B>.
		/// </summary>
        public object TypeHint { get { return typeHint; } }
		private object typeHint;

        /// <summary>Position of <see cref="TypeHint"/> if any.</summary>
        public Position TypeHintPosition { get; internal set; }

		public DType ResolvedTypeHint { get { return resolvedTypeHint; } }
		private DType resolvedTypeHint;

		internal CustomAttributes Attributes { get { return attributes; } }
		private CustomAttributes attributes;

        /// <summary>
		/// Declaring routine.
		/// </summary>
		private PhpRoutine/*!A*/ routine;

		/// <summary>
		/// Index in the <see cref="Signature"/> tuple.
		/// </summary>
		private int index;

		#region Construction

		public FormalParam(Position position, string/*!*/ name, object typeHint, bool passedByRef,
				Expression initValue, List<CustomAttribute> attributes)
			: base(position)
		{
			this.name = new VariableName(name);
			this.typeHint = typeHint;
			this.passedByRef = passedByRef;
			this.initValue = initValue;
			this.attributes = new CustomAttributes(attributes);

			this.resolvedTypeHint = null;
            this.TypeHintPosition = Position.Invalid;
		}

		#endregion

		#region Analysis

		internal void AnalyzeMembers(Analyzer/*!*/ analyzer, PhpRoutine/*!*/ routine, int index)
		{
			this.routine = routine;
			this.index = index;

			PhpType referring_type;
			Scope referring_scope;

			if (routine.IsMethod)
			{
				referring_type = routine.DeclaringPhpType;
				referring_scope = referring_type.Declaration.Scope;
			}
            else if (routine.IsLambdaFunction)
            {
                referring_type = analyzer.CurrentType;
                referring_scope = analyzer.CurrentScope;
            }
            else
            {
                referring_type = null;
                referring_scope = ((PhpFunction)routine).Declaration.Scope;
            }

			attributes.AnalyzeMembers(analyzer, referring_scope);

			resolvedTypeHint = analyzer.ResolveType(typeHint, referring_type, routine, position, false);
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			attributes.Analyze(analyzer, this);

			if (initValue != null)
				initValue = initValue.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

			// adds arguments to local variables table:
			if (!routine.Builder.LocalVariables.AddParameter(name, passedByRef))
			{
				// parameter with the same name specified twice
				analyzer.ErrorSink.Add(Errors.DuplicateParameterName, analyzer.SourceUnit, position, name);
			}

			if (isOut && !passedByRef)
			{
				// out can be used only on by-ref params:
				analyzer.ErrorSink.Add(Errors.OutAttributeOnByValueParam, analyzer.SourceUnit, position, name);
			}
		}

		#endregion

		#region Emission

		/// <summary>
		/// Emits type hint test on the argument if specified.
		/// </summary>
		internal void EmitTypeHintTest(CodeGenerator/*!*/ codeGenerator)
		{
			int real_index = routine.FirstPhpParameterIndex + index;

			// not type hint specified:
			if (typeHint == null) return;

			Debug.Assert(resolvedTypeHint != null);

			ILEmitter il = codeGenerator.IL;
			Label endif_label = il.DefineLabel();

			// IF (DEREF(ARG[argIdx]) is not of hint type) THEN
			il.Ldarg(real_index);
			if (PassedByRef) il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);

			resolvedTypeHint.EmitInstanceOf(codeGenerator, null);
			il.Emit(OpCodes.Brtrue, endif_label);

			// add a branch allowing null values if the argument is optional with null default value (since PHP 5.1.0);
			if (initValue != null && initValue.HasValue && initValue.Value == null)
			{
				// IF (DEREF(ARG[argIdx]) != null) THEN
				il.Ldarg(real_index);
				if (PassedByRef) il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
				il.Emit(OpCodes.Brfalse, endif_label);
			}

			// CALL PhpException.InvalidArgumentType(<param_name>, <class_name>);
			il.Emit(OpCodes.Ldstr, name.ToString());
			il.Emit(OpCodes.Ldstr, resolvedTypeHint.FullName);
			codeGenerator.EmitPhpException(Methods.PhpException.InvalidArgumentType);

			// END IF;
			// END IF;
			il.MarkLabel(endif_label);
		}

		internal void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			attributes.Emit(codeGenerator, this);

			// persists type hint to the [TypeHint] attribute: 
			if (resolvedTypeHint != null)
			{
				ParameterBuilder param_builder = routine.Builder.ParameterBuilders[routine.FirstPhpParameterIndex + index];
				DTypeSpec spec = resolvedTypeHint.GetTypeSpec(codeGenerator.SourceUnit);
				param_builder.SetCustomAttribute(spec.ToCustomAttributeBuilder());
			}
		}

		#endregion

		#region IPhpCustomAttributeProvider Members

		public PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Parameter; } }
		public AttributeTargets AcceptsTargets { get { return AttributeTargets.Parameter; } }

		public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
		{
			return attributes.Count(type, selector);
		}

		public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);

			switch (kind)
			{
				case SpecialAttributes.Out:
					isOut = true;
					break;

				default:
					Debug.Fail("N/A");
					throw null;
			}
		}

		public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);
			routine.Builder.ParameterBuilders[routine.FirstPhpParameterIndex + index].SetCustomAttribute(builder);
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFormalParam(this);
        }
	}

	#endregion

	#region Signature

	public struct Signature
	{
		public bool AliasReturn { get { return aliasReturn; } }
		private readonly bool aliasReturn;

		public List<FormalParam>/*!*/ FormalParams { get { return formalParams; } }
		private readonly List<FormalParam>/*!*/ formalParams;

		public Signature(bool aliasReturn, List<FormalParam>/*!*/ formalParams)
		{
			this.aliasReturn = aliasReturn;
			this.formalParams = formalParams;
		}

		internal void AnalyzeMembers(Analyzer/*!*/ analyzer, PhpRoutine/*!*/ routine)
		{
			int last_mandatory_param_index = -1;
			bool last_param_was_optional = false;
			BitArray alias_mask = new BitArray(formalParams.Count);
			DType[] type_hints = new DType[formalParams.Count];

			for (int i = 0; i < formalParams.Count; i++)
			{
				FormalParam param = formalParams[i];

				param.AnalyzeMembers(analyzer, routine, i);

				alias_mask[i] = param.PassedByRef;
				type_hints[i] = param.ResolvedTypeHint;

				if (param.InitValue == null)
				{
					if (last_param_was_optional)
					{
						analyzer.ErrorSink.Add(Warnings.MandatoryBehindOptionalParam, analyzer.SourceUnit,
							param.Position, param.Name);
					}

					last_mandatory_param_index = i;
					last_param_was_optional = false;
				}
				else
					last_param_was_optional = true;
			}

			routine.Signature.WriteUp(aliasReturn, alias_mask, type_hints, last_mandatory_param_index + 1);
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			foreach (FormalParam param in formalParams)
				param.Analyze(analyzer);
		}

		internal void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			foreach (FormalParam param in formalParams)
				param.Emit(codeGenerator);
		}
	}

	#endregion

	#region FunctionDecl

	/// <summary>
	/// Represents a function declaration.
	/// </summary>
	public sealed class FunctionDecl : Statement, IPhpCustomAttributeProvider, IDeclarationNode, IHasPhpDoc
	{ 
		internal override bool IsDeclaration { get { return true; } }

		public Name Name { get { return name; } }
		private readonly Name name;

		public NamespaceDecl Namespace { get { return ns; } }
		private readonly NamespaceDecl ns;

        public Signature Signature { get { return signature; } }
        private readonly Signature signature;
		private readonly TypeSignature typeSignature;
		private readonly List<Statement>/*!*/ body;
        public List<Statement>/*!*/ Body { get { return body; } }
		private readonly CustomAttributes attributes;
		
		public PhpFunction/*!*/ Function { get { return function; } }
		private readonly PhpFunction/*!*/ function;

		public Position EntireDeclarationPosition { get { return entireDeclarationPosition; } }
		private Position entireDeclarationPosition;

        public ShortPosition HeadingEndPosition { get { return headingEndPosition; } }
        private ShortPosition headingEndPosition;

        public ShortPosition DeclarationBodyPosition { get { return declarationBodyPosition; } }
        private ShortPosition declarationBodyPosition;

		#region Construction

		public FunctionDecl(SourceUnit/*!*/ sourceUnit,
            Position position, Position entireDeclarationPosition, ShortPosition headingEndPosition, ShortPosition declarationBodyPosition,
			bool isConditional, Scope scope, PhpMemberAttributes memberAttributes, string/*!*/ name, NamespaceDecl ns,
			bool aliasReturn, List<FormalParam>/*!*/ formalParams, List<FormalTypeParam>/*!*/ genericParams,
			List<Statement>/*!*/ body, List<CustomAttribute> attributes)
			: base(position)
		{
			Debug.Assert(genericParams != null && name != null && formalParams != null && body != null);

			this.name = new Name(name);
			this.ns = ns;
			this.signature = new Signature(aliasReturn, formalParams);
			this.typeSignature = new TypeSignature(genericParams);
			this.attributes = new CustomAttributes(attributes);
			this.body = body;
			this.entireDeclarationPosition = entireDeclarationPosition;
            this.headingEndPosition = headingEndPosition;
            this.declarationBodyPosition = declarationBodyPosition;

			QualifiedName qn = (ns != null) ? new QualifiedName(this.name, ns.QualifiedName) : new QualifiedName(this.name);
			function = new PhpFunction(qn, memberAttributes, signature, typeSignature, isConditional, scope, sourceUnit, position);
			function.WriteUp(typeSignature.ToPhpRoutineSignature(function));

			function.Declaration.Node = this;
		}

		#endregion

		#region Analysis

		void IDeclarationNode.PreAnalyze(Analyzer/*!*/ analyzer)
		{
			typeSignature.PreAnalyze(analyzer, function);

			if (function.Version.Next != null)
				function.Version.Next.Declaration.Node.PreAnalyze(analyzer);
		}

		void IDeclarationNode.AnalyzeMembers(Analyzer/*!*/ analyzer)
		{
			attributes.AnalyzeMembers(analyzer, function.Declaration.Scope);

			typeSignature.AnalyzeMembers(analyzer, function.Declaration.Scope);
			signature.AnalyzeMembers(analyzer, function);

			// member-analyze the other versions:
			if (function.Version.Next != null)
				function.Version.Next.Declaration.Node.AnalyzeMembers(analyzer);

			function.Declaration.Node = null;
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement/*!*/ Analyze(Analyzer/*!*/ analyzer)
		{
			// functions in incomplete (not emitted) class can't be emitted
			function.Declaration.IsInsideIncompleteClass = analyzer.IsInsideIncompleteClass();

			attributes.Analyze(analyzer, this);
			
			// function is analyzed even if it is unreachable in order to discover more errors at compile-time:
			function.Declaration.IsUnreachable = analyzer.IsThisCodeUnreachable();

			if (function.Declaration.IsUnreachable)
				analyzer.ReportUnreachableCode(position);

			analyzer.EnterFunctionDeclaration(function);

			typeSignature.Analyze(analyzer);
			signature.Analyze(analyzer);

			function.Validate(analyzer.ErrorSink);

            this.Body.Analyze(analyzer);
			
			// validate function and its body:
			function.ValidateBody(analyzer.ErrorSink);

			/*
			if (docComment != null)
				AnalyzeDocComment(analyzer);
			*/
			
			analyzer.LeaveFunctionDeclaration();

			if (function.Declaration.IsUnreachable)
			{
				return EmptyStmt.Unreachable;
			}
			else
			{
				// add entry point if applicable:
				analyzer.SetEntryPoint(function, position);
				return this;
			}
		}

		private void AnalyzeDocComment(Analyzer/*!*/ analyzer)
		{
			// TODO:
			//XmlDocFileBuilder builder = analyzer.Context.Manager.GetDocFileBuilder();
			//if (builder == null) return;

			//string full_name = String.Concat(function.DeclaringType.RealType.FullName, ".", function.ArgLessInfo.Name);

			//builder.WriteFunction(full_name, docComment);
		}

		#endregion

		#region Emission

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("FunctionDecl");

			// marks a sequence point if function is declared here (i.e. is m-decl):
            //Note: this sequence point goes to the function where this function is declared not to this declared function!
			if (!function.IsLambda && function.Declaration.IsConditional)
				codeGenerator.MarkSequencePoint(position.FirstLine, position.FirstColumn, position.LastLine, position.LastColumn + 2);

            // emits attributes on the function itself, its return value, type parameters and regular parameters:
			attributes.Emit(codeGenerator, this);
            signature.Emit(codeGenerator);
			typeSignature.Emit(codeGenerator);

			// prepares code generator for emitting arg-full overload;
			// false is returned when the body should not be emitted:
			if (!codeGenerator.EnterFunctionDeclaration(function)) return;

			// emits the arg-full overload:
			codeGenerator.EmitArgfullOverloadBody(function, body, entireDeclarationPosition, declarationBodyPosition);

			// restores original code generator settings:
			codeGenerator.LeaveFunctionDeclaration();

			// emits function declaration (if needed):
			// ignore s-decl function declarations except for __autoload;
			// __autoload function is declared in order to avoid using callbacks when called:
			if (function.Declaration.IsConditional && !function.QualifiedName.IsAutoloadName)
			{
				Debug.Assert(!function.IsLambda);
				codeGenerator.EmitDeclareFunction(function);
			}
		}

		#endregion

		#region IPhpCustomAttributeProvider Members

		public PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Function; } }

		public AttributeTargets AcceptsTargets
		{
			get { return AttributeTargets.Method | AttributeTargets.ReturnValue; }
		}

		public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
		{
			return attributes.Count(type, selector);
		}

		public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			switch (kind)
			{
				case SpecialAttributes.Export:
					function.Builder.ExportInfo = (ExportAttribute)attribute;
					break;

				default:
					Debug.Fail("N/A");
					throw null;
			}
		}

		public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			if (selector == CustomAttribute.TargetSelectors.Return)
			{
				function.Builder.ReturnParamBuilder.SetCustomAttribute(builder);
			}
			else
			{
				// custom attributes ignored on functions in evals:
				ReflectionUtils.SetCustomAttribute(function.ArgFullInfo, builder);
			}
		}

		#endregion

		public PhpFunction/*!*/ ConvertToLambda(Analyzer/*!*/ analyzer)
		{
			function.ConvertToLambda();

			// perform pre- and member- analyses:
			((IDeclarationNode)this).PreAnalyze(analyzer);
			((IDeclarationNode)this).AnalyzeMembers(analyzer);

			// full analysis is performed later //

			return function;
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFunctionDecl(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc { get; set; }
	}

	#endregion
}
