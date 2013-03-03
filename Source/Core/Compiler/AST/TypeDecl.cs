/*

 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
	#region FormalTypeParam

	public sealed class FormalTypeParam : LangElement, IPhpCustomAttributeProvider
	{
		public Name Name { get { return name; } }
		private readonly Name name;

		/// <summary>
		/// Either <see cref="PrimitiveType"/>, <see cref="GenericQualifiedName"/>, or <B>null</B>.
		/// </summary>
		public object DefaultType { get { return defaultType; } }
		private readonly object defaultType;

		public CustomAttributes Attributes { get { return attributes; } }
		private readonly CustomAttributes attributes;

		private GenericParameter/*! PreAnalyze */ parameter;

        /// <summary>
        /// Singleton instance of an empty <see cref="List&lt;FormalTypeParam&gt;"/>.
        /// </summary>
        public static readonly List<FormalTypeParam>/*!*/EmptyList = new List<FormalTypeParam>();

		#region Construction

		public FormalTypeParam(Position position, Name name, object defaultType, List<CustomAttribute> attributes)
			: base(position)
		{
			this.name = name;
			this.defaultType = defaultType;
			this.attributes = new CustomAttributes(attributes);
		}

		#endregion

		#region Analysis

		internal void PreAnalyze(Analyzer/*!*/ analyzer, GenericParameter/*!*/ parameter)
		{
			this.parameter = parameter;

			PhpRoutine routine = parameter.DeclaringMember as PhpRoutine;
			PhpType type = (routine != null) ? routine.DeclaringType as PhpType : parameter.DeclaringPhpType;

			parameter.WriteUp(analyzer.ResolveType(defaultType, type, routine, position, false));
		}

		internal bool Merge(ErrorSink/*!*/ errors, FormalTypeParam/*!*/ other)
		{
			if (this.name != other.Name)
			{
				PhpType declaring_type = (PhpType)parameter.DeclaringMember;

				errors.Add(Errors.PartialDeclarationsDifferInTypeParameter, declaring_type.Declaration.SourceUnit,
					position, declaring_type.FullName);

				return false;
			}

			attributes.Merge(other.Attributes);

			return true;
		}

		internal void AnalyzeMembers(Analyzer/*!*/ analyzer, Scope referringScope)
		{
			attributes.AnalyzeMembers(analyzer, referringScope);
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			attributes.Analyze(analyzer, this);
		}

		#endregion

		#region Emission

		/// <summary>
		/// Parameters on generic types.
		/// </summary>
		internal void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			attributes.Emit(codeGenerator, this);

			// persists default type to the [TypeHint] attribute: 
			if (parameter.DefaultType != null)
			{
				DTypeSpec spec = parameter.DefaultType.GetTypeSpec(codeGenerator.SourceUnit);
				parameter.SetCustomAttribute(spec.ToCustomAttributeBuilder());
			}
		}

		#endregion

		#region IPhpCustomAttributeProvider Members

		public PhpAttributeTargets AttributeTarget
		{
			get { return PhpAttributeTargets.GenericParameter; }
		}

		public AttributeTargets AcceptsTargets
		{
			get { return AttributeTargets.GenericParameter; }
		}

		public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
		{
			return attributes.Count(type, selector);
		}

		public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			Debug.Fail("N/A");
		}

		public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);
			parameter.SetCustomAttribute(builder);
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFormalTypeParam(this);
        }
	}

	#endregion

	#region TypeSignature

	public struct TypeSignature
	{
		internal List<FormalTypeParam>/*!!*/ TypeParams { get { return typeParams; } }
		private readonly List<FormalTypeParam>/*!!*/ typeParams;

		#region Construction

		public TypeSignature(List<FormalTypeParam>/*!!*/ typeParams)
		{
			Debug.Assert(typeParams != null);
			this.typeParams = typeParams;
		}

		/// <summary>
		/// Creates an array of generic parameters. 
		/// Used by generic types.
		/// </summary>
		internal GenericParameterDesc[]/*!!*/ ToGenericParameters(DMember/*!*/ declaringType)
		{
			int mandatory_generic_param_count;
			return this.ToGenericParameters(declaringType, out mandatory_generic_param_count);
		}

		/// <summary>
		/// Creates a <see cref="PhpRoutineSignature"/> partially initialized with the type parameters of this type signature. 
		/// Used by generic routines.
		/// </summary>
		internal PhpRoutineSignature/*!*/ ToPhpRoutineSignature(DMember/*!*/ declaringRoutine)
		{
			Debug.Assert(declaringRoutine != null);

			int mandatory_generic_param_count;
			GenericParameterDesc[] descs = this.ToGenericParameters(declaringRoutine, out mandatory_generic_param_count);

			GenericParameter[] types = new GenericParameter[descs.Length];
			for (int i = 0; i < descs.Length; i++)
				types[i] = descs[i].GenericParameter;

			return new PhpRoutineSignature(types, mandatory_generic_param_count);
		}

		private GenericParameterDesc[]/*!!*/ ToGenericParameters(DMember/*!*/ declaringMember, out int mandatoryCount)
		{
			Debug.Assert(declaringMember != null);

			if (typeParams.Count == 0)
			{
				mandatoryCount = 0;
				return GenericParameterDesc.EmptyArray;
			}

			GenericParameterDesc[] result = new GenericParameterDesc[typeParams.Count];
			mandatoryCount = 0;
			for (int i = 0; i < typeParams.Count; i++)
			{
				result[i] = new GenericParameter(typeParams[i].Name, i, declaringMember).GenericParameterDesc;
				if (typeParams[i].DefaultType == null)
					mandatoryCount++;
			}
			return result;
		}

		#endregion

		#region Analysis

		internal void PreAnalyze(Analyzer/*!*/ analyzer, PhpType/*!*/ declaringType)
		{
			Debug.Assert(analyzer != null && declaringType != null);

			if (typeParams == null) return;

			Debug.Assert(declaringType.GenericParams.Length == typeParams.Count);

			for (int i = 0; i < typeParams.Count; i++)
				typeParams[i].PreAnalyze(analyzer, declaringType.GetGenericParameter(i));
		}

		internal void PreAnalyze(Analyzer/*!*/ analyzer, PhpRoutine/*!*/ declaringRoutine)
		{
			Debug.Assert(analyzer != null && declaringRoutine != null);

			if (typeParams == null) return;

			Debug.Assert(declaringRoutine.Signature.GenericParamCount == typeParams.Count);

			for (int i = 0; i < typeParams.Count; i++)
				typeParams[i].PreAnalyze(analyzer, declaringRoutine.Signature.GenericParams[i]);
		}

		internal bool Merge(ErrorSink/*!*/ errors, PhpType/*!*/ declaringType, TypeSignature other)
		{
			if (typeParams.Count != other.typeParams.Count)
			{
				errors.Add(Errors.PartialDeclarationsDifferInTypeParameterCount, declaringType.Declaration.SourceUnit,
					declaringType.Declaration.Position, declaringType.FullName);

				return false;
			}

			bool result = true;

			for (int i = 0; i < typeParams.Count; i++)
				result &= typeParams[i].Merge(errors, other.typeParams[i]);

			return result;
		}

		internal void AnalyzeMembers(Analyzer/*!*/ analyzer, Scope referringScope)
		{
			foreach (FormalTypeParam param in typeParams)
				param.AnalyzeMembers(analyzer, referringScope);
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			foreach (FormalTypeParam param in typeParams)
				param.Analyze(analyzer);
		}

		#endregion

		#region Emission

		internal void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			foreach (FormalTypeParam param in typeParams)
				param.Emit(codeGenerator);
		}

		#endregion
	}

	#endregion

	#region TypeDecl

	/// <summary>
	/// Represents a class or an interface declaration.
	/// </summary>
    public sealed class TypeDecl : Statement, IPhpCustomAttributeProvider, IDeclarationNode, IHasPhpDoc
	{
		#region Properties

		internal override bool IsDeclaration { get { return true; } }

		/// <summary>
		/// Name of the class.
		/// </summary>
		public Name Name { get { return name; } }
		private readonly Name name;

        /// <summary>
        /// Position of <see cref="Name"/> in the source code.
        /// </summary>
        public Position NamePosition { get; private set; }

		/// <summary>
		/// Namespace where the class is declared in.
		/// </summary>
		public NamespaceDecl Namespace { get { return ns; } }
		private readonly NamespaceDecl ns;

        /// <summary>
        /// Aliases copied from current scope (global or namespace) which were valid in place of this type declaration.
        /// Used for deferred class declaration in run time, when creating transient compilation unit.
        /// </summary>
        private readonly Dictionary<string, QualifiedName> validAliases;

		/// <summary>
		/// Name of the base class.
		/// </summary>
		private readonly GenericQualifiedName? baseClassName;
        /// <summary>Name of the base class.</summary>
        public GenericQualifiedName? BaseClassName { get { return baseClassName; } }

        /// <summary>Position of <see cref="BaseClassName"/>.</summary>
        public Position BaseClassNamePosition { get; private set; }

		/// <summary>
		/// Implemented interface name indices. 
		/// </summary>
		private readonly List<KeyValuePair<GenericQualifiedName, Position>>/*!*/ implementsList;

        /// <summary>Implemented interface name indices. </summary>
        public List<GenericQualifiedName>/*!*/ ImplementsList { get { return this.implementsList.Select(x => x.Key).ToList(); } }

        /// <summary>Positions of <see cref="ImplementsList"/> elements.</summary>
        public Position[] ImplementsPosition { get { return this.implementsList.Select(x => x.Value).ToArray(); } }

		/// <summary>
		/// Type parameters.
		/// </summary>
		private readonly TypeSignature typeSignature;

		/// <summary>
		/// Member declarations. Partial classes merged to the aggregate has this field <B>null</B>ed.
		/// </summary>
		public List<TypeMemberDecl> Members { get { return members; } }
		private List<TypeMemberDecl> members;

		/// <summary>
		/// Whether the node is partial and has been merged to another partial node (arggregate).
		/// </summary>
		private bool IsPartialMergeResiduum { get { return members == null; } }

		private readonly CustomAttributes attributes;
        /// <summary>Custom attributes applied</summary>
        public CustomAttributes Attributes { get { return attributes; } }

		/// <summary>
		/// Item of the table of classes. Partial classes merged to the aggregate has this field <B>null</B>ed.
		/// </summary>
		public PhpType Type { get { return type; } }
		private PhpType type;

		/// <summary>
		/// Position spanning over the entire declaration including the attributes.
		/// Used for transformation to an eval and for VS integration.
		/// </summary>
		public Position EntireDeclarationPosition { get { return entireDeclarationPosition; } }
		private Position entireDeclarationPosition;

		public ShortPosition DeclarationBodyPosition { get { return declarationBodyPosition; } }
		private ShortPosition declarationBodyPosition;

        private ShortPosition headingEndPosition;
        public ShortPosition HeadingEndPosition { get { return headingEndPosition; } }

        /// <summary>
        /// Code of the class used when declared deferred in Eval.
        /// </summary>
        private string typeDefinitionCode = null;

        /// <summary>Contains value of the <see cref="PartialKeyword"/> property</summary>
        private bool partialKeyword;
        /// <summary>Indicates if type was decorated with partial keyword</summary>
        public bool PartialKeyword { get { return partialKeyword; } }

		#endregion

		#region Construction

		public TypeDecl(SourceUnit/*!*/ sourceUnit,
			Position position, Position entireDeclarationPosition, ShortPosition headingEndPosition, ShortPosition declarationBodyPosition,
			bool isConditional, Scope scope, PhpMemberAttributes memberAttributes, bool isPartial, Name className, Position classNamePosition,
			NamespaceDecl ns, List<FormalTypeParam>/*!*/ genericParams, Tuple<GenericQualifiedName, Position> baseClassName,
			List<KeyValuePair<GenericQualifiedName, Position>>/*!*/ implementsList, List<TypeMemberDecl>/*!*/ members,
			List<CustomAttribute> attributes)
			: base(position)
		{
			Debug.Assert(genericParams != null && implementsList != null && members != null);
            Debug.Assert((memberAttributes & PhpMemberAttributes.Trait) == 0 || (memberAttributes & PhpMemberAttributes.Interface) == 0, "Interface cannot be a trait");

			this.name = className;
            this.NamePosition = classNamePosition;
			this.ns = ns;
			this.typeSignature = new TypeSignature(genericParams);
            if (baseClassName != null)
            {
                this.baseClassName = baseClassName.Item1;
                this.BaseClassNamePosition = baseClassName.Item2;
            }
            this.implementsList = implementsList;
			this.members = members;
			this.attributes = new CustomAttributes(attributes);
			this.entireDeclarationPosition = entireDeclarationPosition;
            this.headingEndPosition = headingEndPosition;
			this.declarationBodyPosition = declarationBodyPosition;
            this.partialKeyword = isPartial;

            // remember current aliases:
            var aliases = (ns != null) ? ns.Aliases : sourceUnit.Aliases;
            if (aliases.Count > 0)
                validAliases = new Dictionary<string, QualifiedName>(aliases);

			// create stuff necessary for inclusion resolving process, other structures are created duirng analysis:
			QualifiedName qn = (ns != null) ? new QualifiedName(name, ns.QualifiedName) : new QualifiedName(name);
			type = new PhpType(qn, memberAttributes, isPartial, typeSignature, isConditional, scope, sourceUnit, position);

            //// add alias for private classes (if not added yet by partial declaration):
            //if (type.IsPrivate)
            //    sourceUnit.AddTypeAlias(qn, this.name);

			// member-analysis needs the node:
			type.Declaration.Node = this;
		}

		#endregion

		#region Pre-analysis

		/// <summary>
		/// Invoked before member-analysis on the primary types.
		/// All types are known at this point.
		/// </summary>
		void IDeclarationNode.PreAnalyze(Analyzer/*!*/ analyzer)
		{
			typeSignature.PreAnalyze(analyzer, type);

			// all types are known:
			DTypeDesc base_type = ResolveBaseType(analyzer);
			List<DTypeDesc> base_interfaces = new List<DTypeDesc>(implementsList.Count);
			ResolveBaseInterfaces(analyzer, base_interfaces);

			// pre-analyze the other versions (include partial types merging):
			if (type.Version.Next != null)
			{
				if (type.Declaration.IsPartial)
				{
                    var nextPartialDecl = (TypeDecl)type.Version.Next.Declaration.Node;
                    nextPartialDecl.PreAnalyzePartialDeclaration(analyzer, this,
                        ref base_type, base_interfaces);

                    // drop the partial type version info:
					type.Version = new VersionInfo(0, null);
				}
				else
					type.Version.Next.Declaration.Node.PreAnalyze(analyzer);
			}

			type.TypeDesc.WriteUpBaseType(base_type);
			type.Builder.BaseInterfaces = base_interfaces;
		}

		private void PreAnalyzePartialDeclaration(Analyzer/*!*/ analyzer, TypeDecl/*!*/ aggregate,
			ref DTypeDesc aggregateBase, List<DTypeDesc>/*!*/ aggregateInterfaces)
		{
            //
            // little hack, change the sourceUnit in order to match the current partial class declaration with the right file and imported namespaces
            //
            var current_sourceUnit = analyzer.SourceUnit;
            analyzer.SourceUnit = this.type.Declaration.SourceUnit;
            //
            //
            //

            try
            {
                bool valid = true;

                if (type.IsInterface != aggregate.type.IsInterface)
                {
                    analyzer.ErrorSink.Add(Errors.IncompatiblePartialDeclarations, type.Declaration.SourceUnit, type.Declaration.Position, type.FullName);
                    analyzer.ErrorSink.Add(Errors.RelatedLocation, aggregate.type.Declaration.SourceUnit, aggregate.type.Declaration.Position);
                    valid = false;
                }

                if (type.Visibility != aggregate.type.Visibility)
                {
                    analyzer.ErrorSink.Add(Errors.ConflictingPartialVisibility, type.Declaration.SourceUnit, type.Declaration.Position, type.FullName);
                    analyzer.ErrorSink.Add(Errors.RelatedLocation, aggregate.type.Declaration.SourceUnit, aggregate.type.Declaration.Position);
                    valid = false;
                }

                // merge base types:
                DTypeDesc base_type = ResolveBaseType(analyzer);
                if (base_type != null)
                {
                    if (aggregateBase != null)
                    {
                        if (!base_type.Type.Equals(aggregateBase.Type)) //Edited by Ðonny Jan 07 2009 - missing "!"?
                        {
                            analyzer.ErrorSink.Add(Errors.PartialDeclarationsDifferInBase, type.Declaration.SourceUnit, type.Declaration.Position, type.FullName);
                            analyzer.ErrorSink.Add(Errors.RelatedLocation, aggregate.type.Declaration.SourceUnit, aggregate.type.Declaration.Position);
                            valid = false;
                        }
                    }
                    else
                    {
                        aggregateBase = base_type;
                    }
                }

                typeSignature.PreAnalyze(analyzer, type);

                // merge generic parameters:
                valid &= aggregate.typeSignature.Merge(analyzer.ErrorSink, this.type, typeSignature);

                // move members to the aggregate:
                members.ForEach(member => member.sourceUnit = analyzer.SourceUnit); // override SourceUnit of the members to match the debug information and imported namespaces properly furing the analysis
                aggregate.members.AddRange(members);
                members = null;

                // move attributes to the aggregate:
                aggregate.attributes.Merge(attributes);

                // merge interfaces:
                ResolveBaseInterfaces(analyzer, aggregateInterfaces);

                // next partial declaration;
                // (if the declaration is erroneous, stop analysis before reporting more messy errors):
                if (valid && type.Version.Next != null)
                {
                    ((TypeDecl)type.Version.Next.Declaration.Node).PreAnalyzePartialDeclaration(analyzer, aggregate,
                        ref aggregateBase, aggregateInterfaces);
                }
            }
            finally
            {
                // cut the AST off the tables:
                type.Declaration.Node = null;
                type = null;

                //
                // change the sourceUnit back
                //
                analyzer.SourceUnit = current_sourceUnit;
            }
		}

		private DTypeDesc ResolveBaseType(Analyzer/*!*/ analyzer)
		{
			if (baseClassName.HasValue)
			{
				DType base_type = analyzer.ResolveTypeName(baseClassName.Value, type, null, position, true);

				if (base_type.IsGenericParameter)
				{
					analyzer.ErrorSink.Add(Errors.CannotDeriveFromTypeParameter, analyzer.SourceUnit, position,
					  base_type.FullName);
					return null;
				}
				else if (base_type.IsIdentityDefinite)
				{
					if (base_type.IsInterface)
					{
						analyzer.ErrorSink.Add(Errors.NonClassExtended, analyzer.SourceUnit, position, base_type.FullName);
						return null;
					}
					else if (base_type.IsFinal)
					{
						analyzer.ErrorSink.Add(Errors.FinalClassExtended, analyzer.SourceUnit, position, base_type.FullName);
						// do not remove the base class to make the further error reports consistent
					}
				}

				return base_type.TypeDesc;
			}
			else
				return null;
		}

		private void ResolveBaseInterfaces(Analyzer/*!*/ analyzer, List<DTypeDesc>/*!*/ interfaces)
		{
			for (int i = 0; i < implementsList.Count; i++)
			{
                DType base_type = analyzer.ResolveTypeName(implementsList[i].Key, type, null, implementsList[i].Value, true);

				if (base_type.IsGenericParameter)
				{
					analyzer.ErrorSink.Add(Errors.CannotDeriveFromTypeParameter, analyzer.SourceUnit, position,
						base_type.FullName);
				}
				else if (base_type.IsIdentityDefinite && !base_type.IsInterface)
				{
					if (type.IsInterface)
						analyzer.ErrorSink.Add(Errors.NonInterfaceExtended, analyzer.SourceUnit, position, base_type.FullName);
					else
						analyzer.ErrorSink.Add(Errors.NonInterfaceImplemented, analyzer.SourceUnit, position, base_type.FullName);
				}
				else
				{
					interfaces.Add(base_type.TypeDesc);
				}
			}
		}

		#endregion

		#region Member Analysis

		/// <summary>
		/// Invoked by analyzer after all files are parsed and before the full analysis takes place.
		/// Invoked only on types directly stored on the compilation unit during parsing,
		/// i.e. invoked only on the primary version and not on the others.
		/// All types and their inheritance relationships are known at this point, partial types has already been merged.
		/// </summary>
		void IDeclarationNode.AnalyzeMembers(Analyzer/*!*/ analyzer)
		{
			attributes.AnalyzeMembers(analyzer, type.Declaration.Scope);
			typeSignature.AnalyzeMembers(analyzer, type.Declaration.Scope);

			// let members add themselves to the type:
			for (int i = 0; i < members.Count; i++)
				members[i].AnalyzeMembers(analyzer, type);

			type.ValidateMembers(analyzer.ErrorSink);

			// analyze members of the other versions:
            if (type.Version.Next != null)
            {
                var nextPartialDecl = type.Version.Next.Declaration.Node;

                // little hack, change the SourceUnit during the analysis to match the partial declaration
                var nextPartialDecl_sourceUnit = ((TypeDecl)nextPartialDecl).Type.Declaration.SourceUnit;
                var current_sourceUnit = analyzer.SourceUnit;

                analyzer.SourceUnit = nextPartialDecl_sourceUnit;
                nextPartialDecl.AnalyzeMembers(analyzer);   // analyze partial class members
                analyzer.SourceUnit = current_sourceUnit;
            }

			// cut the AST off the tables:
			type.Declaration.Node = null;
		}

		#endregion

		#region Analysis

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Statement.Analyze"]/*'/>
		internal override Statement Analyze(Analyzer/*!*/ analyzer)
		{
			// remove classes that has been merged to the aggregate from the further processing:
			if (IsPartialMergeResiduum)
				return EmptyStmt.PartialMergeResiduum;

			// functions in incomplete (not emitted) class can't be emitted
			type.Declaration.IsInsideIncompleteClass = analyzer.IsInsideIncompleteClass();

			// the ClassDecl is fully analyzed even if it will be replaced in the AST by EvalEx
			// and even if it is unreachable in order to discover all possible errors in compile-time

			type.Declaration.IsUnreachable = analyzer.IsThisCodeUnreachable();

			if (type.Declaration.IsUnreachable)
				analyzer.ReportUnreachableCode(position);

			attributes.Analyze(analyzer, this);
			typeSignature.Analyze(analyzer);

			analyzer.EnterTypeDecl(type);

            foreach (var member in members)
            {
                member.EnterAnalyzer(analyzer);
                member.Analyze(analyzer);
                member.LeaveAnalyzer(analyzer);
            }

			analyzer.LeaveTypeDecl();

			AnalyzeDocComments(analyzer);

			// validate the type after all members has been analyzed and validated:
			type.Validate(analyzer.ErrorSink);

			if (type.Declaration.IsUnreachable)
			{
				// only a conditional declaration can be unreachable
				// => not emiting the declaration is ok

				return EmptyStmt.Unreachable;
			}
			else if (!type.IsComplete)
			{
				// mark all functions declared in incomplete class as 'non-compilable'

				// convert incomplete class to an eval if applicable:
				if (analyzer.SourceUnit.CompilationUnit.IsPure && analyzer.CurrentType == null &&
					analyzer.CurrentRoutine == null)
				{
					// error, since there is no place for global code in pure mode:
					analyzer.ErrorSink.Add(Errors.IncompleteClass, analyzer.SourceUnit, position, this.name);
					return this;
				}

				if (analyzer.SourceUnit.CompilationUnit.IsTransient)
				{
					TransientCompilationUnit transient_unit = (TransientCompilationUnit)analyzer.SourceUnit.CompilationUnit;

					// report an error only for synthetic evals as we are 100% sure that the class cannot be completed;
					// note that a synthetic eval can be created even in transient code as some base types could be 
					// declared there conditionally:
					if (transient_unit.EvalKind == EvalKinds.SyntheticEval)
					{
						analyzer.ErrorSink.Add(Errors.IncompleteClass, analyzer.SourceUnit, position, this.name);
						return this;
					}
				}

                // report the warning, incomplete_class
                analyzer.ErrorSink.Add(Warnings.IncompleteClass, analyzer.SourceUnit, position, this.name);

                this.typeDefinitionCode = analyzer.SourceUnit.GetSourceCode(entireDeclarationPosition);
                //// we return an eval
                //EvalEx evalEx = new EvalEx(
                //    entireDeclarationPosition, this.typeDefinitionCode,
                //    (this.Namespace != null && this.Namespace.QualifiedName.Namespaces.Length > 0) ? this.Namespace.QualifiedName : (QualifiedName?)null,
                //    this.validAliases);
                //Statement stmt = new ExpressionStmt(entireDeclarationPosition, evalEx);

                //// this annotation is for the duck-type generation - we need to know the original typedecl
                //evalEx.Annotations.Set<TypeDecl>(this);

                //return stmt;

                // we emit eval
                return this;
			}
			else
			{
				return this;
			}
		}

		private void AnalyzeDocComments(Analyzer/*!*/ analyzer)
		{
			// TODO:
			//XmlDocFileBuilder builder = analyzer.Context.Manager.GetDocFileBuilder();
			//if (builder == null) return;

			//string full_name = type.RealType.FullName;

			//if (docComment != null)
			//  builder.WriteType(full_name, docComment);

			//for (int i = 0; i < members.Count; i++)
			//  members[i].AnalyzeDocComment(analyzer, builder, full_name);
		}

		#endregion

		#region Emission

		internal void EmitDefinition(CodeGenerator/*!*/ codeGenerator)
		{
            if (type.IsComplete)
            {
                Debug.Assert(type.IsComplete, "Incomplete types should be converted to evals.");
                Debug.Assert(type.RealTypeBuilder != null, "A class declared during compilation should have a type builder.");

                attributes.Emit(codeGenerator, this);
                typeSignature.Emit(codeGenerator);

                codeGenerator.EnterTypeDeclaration(type);

                foreach (TypeMemberDecl member_decl in members)
                {
                    member_decl.EnterCodegenerator(codeGenerator);
                    member_decl.Emit(codeGenerator);
                    member_decl.LeaveCodegenerator(codeGenerator);
                }

                // emit stubs for implemented methods & properties that were not declared by this type:
                codeGenerator.EmitGhostStubs(type);

                codeGenerator.LeaveTypeDeclaration();
            }
            else
            {
                Debug.Assert(this.typeDefinitionCode != null);

                // LOAD DynamicCode.Eval(<code>, context, definedVariables, self, includer, source, line, column, evalId)
                
                // wrap Eval into static method
                MethodBuilder method = codeGenerator.IL.TypeBuilder.DefineMethod(
                    string.Format("{0}{1}", ScriptModule.DeclareHelperNane, type.FullName),
                    MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.SpecialName,
                    Types.Void, Types.ScriptContext);

                var il = new ILEmitter(method);
                    
                codeGenerator.EnterLambdaDeclaration(il, false, LiteralPlace.Null, new IndexedPlace(PlaceHolder.Argument, 0), LiteralPlace.Null, LiteralPlace.Null);
                if (true)
                {
                    codeGenerator.EmitEval(
                        EvalKinds.SyntheticEval,
                        new StringLiteral(position, this.typeDefinitionCode, AccessType.Read),
                        position,
                        (this.Namespace != null) ? this.Namespace.QualifiedName : (QualifiedName?)null, this.validAliases);
                    il.Emit(OpCodes.Pop);
                    il.Emit(OpCodes.Ret);
                }
                codeGenerator.LeaveFunctionDeclaration();

                //
                il = codeGenerator.IL;

                type.IncompleteClassDeclareMethodInfo = method;
                type.IncompleteClassDeclarationId = String.Format("{0}${1}:{2}:{3}", type.FullName, unchecked((uint)codeGenerator.SourceUnit.SourceFile.ToString().GetHashCode()), position.FirstLine, position.FirstColumn);

                // sequence point here
                codeGenerator.MarkSequencePoint(position.FirstLine, position.FirstColumn, position.LastLine, position.LastColumn + 2);
                
                if (type.Declaration.IsConditional)
                {
                    // CALL <Declare>.<FullName>(<context>)
                    codeGenerator.EmitLoadScriptContext();
                    il.Emit(OpCodes.Call, method);
                }
                else
                {
                    // if (!<context>.IncompleteTypeDeclared(<id>))
                    //     CALL <Declare>.<FullName>(<context>)
                    var end_if = il.DefineLabel();

                    codeGenerator.EmitLoadScriptContext();
                    il.Emit(OpCodes.Ldstr, type.IncompleteClassDeclarationId);
                    il.Emit(OpCodes.Call, Methods.ScriptContext.IncompleteTypeDeclared);
                    il.Emit(OpCodes.Brtrue, end_if);
                    if (true)
                    {
                        codeGenerator.EmitLoadScriptContext();
                        il.Emit(OpCodes.Call, type.IncompleteClassDeclareMethodInfo);
                    }
                    il.MarkLabel(end_if);
                    il.ForgetLabel(end_if);
                }
            }
		}

		internal void EmitDeclaration(CodeGenerator/*!*/ codeGenerator)
		{
            if (type.IsComplete)
            {
                Debug.Assert(type.IsComplete, "Incomplete types should be converted to evals.");
                Debug.Assert(type.RealTypeBuilder != null, "A class declared during compilation should have a type builder.");

                if (type.Declaration.IsConditional)
                {
                    ILEmitter il = codeGenerator.IL;

                    codeGenerator.MarkSequencePoint(position.FirstLine, position.FirstColumn, position.LastLine, position.LastColumn + 2);

                    // this class was conditionally declared, so we'll emit code that activates it:
                    type.EmitAutoDeclareOnScriptContext(il, codeGenerator.ScriptContextPlace);

                    if (codeGenerator.Context.Config.Compiler.Debug)
                        il.Emit(OpCodes.Nop);
                }
            }
            else
            {
                // declared in emitted Eval
            }
		}

		/// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("ClassDecl");
			EmitDeclaration(codeGenerator);
			EmitDefinition(codeGenerator);
		}

		#endregion

		#region IPhpCustomAttributeProvider Members

		public PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Class; } }

		public AttributeTargets AcceptsTargets
		{
			get
			{
				return ((type.IsInterface) ? AttributeTargets.Interface : AttributeTargets.Class)
					| AttributeTargets.Assembly | AttributeTargets.Module;
			}
		}

		public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
		{
			return attributes.Count(type, selector);
		}

		public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			switch (kind)
			{
				case SpecialAttributes.AttributeUsage:
					type.SetCustomAttributeUsage((AttributeUsageAttribute)attribute);
					break;

				case SpecialAttributes.Export:
					type.Builder.ExportInfo = (ExportAttribute)attribute;
					break;

				default:
					Debug.Fail("N/A");
					throw null;
			}
		}

		public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			type.RealTypeBuilder.SetCustomAttribute(builder);
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitTypeDecl(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc { get; set; }
    }

	#endregion

	#region TypeMemberDecl

	/// <summary>
	/// Represents a member declaration.
	/// </summary>
	public abstract class TypeMemberDecl : LangElement, IPhpCustomAttributeProvider
	{
		public PhpMemberAttributes Modifiers { get { return modifiers; } }
		protected PhpMemberAttributes modifiers;

		public CustomAttributes Attributes { get { return attributes; } }
		protected CustomAttributes attributes;

        #region SourceUnit nesting

        /// <summary>
        /// Overrides current sourceUnit for this TypeMemberDecl. This occurres
        /// when partial class is declared, after preanalysis, when members are
        /// merged into one TypeDecl.
        /// </summary>
        internal SourceUnit sourceUnit = null;

        internal void EnterAnalyzer(Analyzer/*!*/analyzer)
        {
            if (sourceUnit != null)
            {
                if (currentSourceUnit != null)
                    throw new InvalidOperationException("TypeMemberDecl.EnterAnalyzer does not support nesting.");

                currentSourceUnit = analyzer.SourceUnit;
                analyzer.SourceUnit = sourceUnit;
            }
        }
        internal void LeaveAnalyzer(Analyzer/*!*/analyzer)
        {
            if (sourceUnit != null)
            {
                if (currentSourceUnit == null)
                    throw new InvalidOperationException("TypeMemberDecl.EnterAnalyzer was not called before.");

                analyzer.SourceUnit = currentSourceUnit;
                currentSourceUnit = null;
            }
        }
        internal void EnterCodegenerator(CodeGenerator/*!*/ codeGenerator)
        {
            if (sourceUnit != null)
            {
                if (currentSourceUnit != null)
                    throw new InvalidOperationException("TypeMemberDecl.EnterAnalyzer does not support nesting.");

                currentSourceUnit = codeGenerator.SourceUnit;
                codeGenerator.SourceUnit = sourceUnit;
            }
        }
        internal void LeaveCodegenerator(CodeGenerator/*!*/ codeGenerator)
        {
            if (sourceUnit != null)
            {
                if (currentSourceUnit == null)
                    throw new InvalidOperationException("TypeMemberDecl.EnterAnalyzer was not called before.");

                codeGenerator.SourceUnit = currentSourceUnit;
                currentSourceUnit = null;
            }
        }
        private SourceUnit currentSourceUnit = null;

        #endregion

        protected TypeMemberDecl(Position position, List<CustomAttribute> attributes)
			: base(position)
		{
			this.attributes = new CustomAttributes(attributes);
		}

		internal virtual void AnalyzeMembers(Analyzer/*!*/ analyzer, PhpType/*!*/ declaringType)
		{
			attributes.AnalyzeMembers(analyzer, declaringType.Declaration.Scope);
		}

		internal virtual void Analyze(Analyzer/*!*/ analyzer)
		{
			attributes.Analyze(analyzer, this);
		}

#if FALSE //!SILVERLIGHT
		internal abstract void AnalyzeDocComment(Analyzer/*!*/ analyzer, XmlDocFileBuilder/*!*/ builder, string/*!*/ prefix);
#endif

		internal virtual void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			attributes.Emit(codeGenerator, this);
		}

		#region IPhpCustomAttributeProvider Members

		public abstract PhpAttributeTargets AttributeTarget { get; }
		public abstract AttributeTargets AcceptsTargets { get; }

		public abstract void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector);
		public abstract void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector);

		public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
		{
			return attributes.Count(type, selector);
		}

		#endregion
	}

	#endregion

	#region Methods

	/// <summary>
	/// Represents a method declaration.
	/// </summary>
    public sealed class MethodDecl : TypeMemberDecl, IPhpCustomAttributeProvider, IHasPhpDoc
	{
		/// <summary>
		/// Name of the method.
		/// </summary>
		public Name Name { get { return name; } }
		private readonly Name name;

		public Signature Signature { get { return signature; } }
		private readonly Signature signature;

		public TypeSignature TypeSignature { get { return typeSignature; } }
		private readonly TypeSignature typeSignature;

		public List<Statement> Body { get { return body; } }
		private readonly List<Statement> body;

		public List<ActualParam> BaseCtorParams { get { return baseCtorParams; } }
		private List<ActualParam> baseCtorParams;
        
		/// <summary>
		/// Item in the table of methods or a <B>null</B> reference if added to the type yet or an error occured while adding.
		/// </summary>
		private PhpMethod method = null;
		
		public Position EntireDeclarationPosition { get { return entireDeclarationPosition; } }
		private Position entireDeclarationPosition;

        public ShortPosition HeadingEndPosition { get { return headingEndPosition; } }
        private ShortPosition headingEndPosition;

		public ShortPosition DeclarationBodyPosition { get { return declarationBodyPosition; } }
		private ShortPosition declarationBodyPosition;

		#region Construction

        public MethodDecl(Position position, Position entireDeclarationPosition, ShortPosition headingEndPosition, ShortPosition declarationBodyPosition, 
			string name, bool aliasReturn, List<FormalParam>/*!*/ formalParams, List<FormalTypeParam>/*!*/ genericParams, 
			List<Statement> body, PhpMemberAttributes modifiers, List<ActualParam> baseCtorParams, 
			List<CustomAttribute> attributes)
            : base(position, attributes)
        {
            Debug.Assert(genericParams != null && formalParams != null);

            this.modifiers = modifiers;
            this.name = new Name(name);
            this.signature = new Signature(aliasReturn, formalParams);
            this.typeSignature = new TypeSignature(genericParams);
            this.body = body;
            this.baseCtorParams = baseCtorParams;
            this.entireDeclarationPosition = entireDeclarationPosition;
            this.headingEndPosition = headingEndPosition;
            this.declarationBodyPosition = declarationBodyPosition;
        }

		#endregion

		#region Analysis

		internal override void AnalyzeMembers(Analyzer/*!*/ analyzer, PhpType/*!*/ declaringType)
		{
			method = declaringType.AddMethod(name, modifiers, body != null, signature, typeSignature, position,
				analyzer.SourceUnit, analyzer.ErrorSink);

			// method redeclared:
			if (method == null) return;

			method.WriteUp(typeSignature.ToPhpRoutineSignature(method));

			typeSignature.PreAnalyze(analyzer, method);

			base.AnalyzeMembers(analyzer, declaringType);

			typeSignature.AnalyzeMembers(analyzer, declaringType.Declaration.Scope);
			signature.AnalyzeMembers(analyzer, method);
            method.IsDllImport = this.IsDllImport;
            if(method.IsDllImport && Body.Count != 0)
                analyzer.ErrorSink.Add(Warnings.BodyOfDllImportedFunctionIgnored, analyzer.SourceUnit, position);
		}

		internal override void Analyze(Analyzer/*!*/ analyzer)
		{
			// method redeclared:
			if (method == null) return;

			base.Analyze(analyzer);

			PhpType declaring_type = analyzer.CurrentType;

			analyzer.EnterMethodDeclaration(method);

			typeSignature.Analyze(analyzer);
			signature.Analyze(analyzer);

			method.Validate(analyzer.ErrorSink);

			// note, if the declaring type's base is unknown then it cannot be a CLR type;
			ClrType base_clr_type = method.DeclaringType.Base as ClrType;

			if (baseCtorParams != null)
			{
				if (base_clr_type != null)
				{
					AnalyzeBaseCtorCallParams(analyzer, base_clr_type);
				}
				else if (!method.IsConstructor || method.DeclaringType.Base == null || body == null)
				{
					analyzer.ErrorSink.Add(Errors.UnexpectedParentCtorInvocation, analyzer.SourceUnit, position);
					baseCtorParams = null;
				}
				else if (method.DeclaringType.Base.Constructor == null)
				{
					// base class has no constructor, the default parameterless is silently called (and that does nothing);
					// report error, if there are any parameters passed to the parameterless ctor:
					if (baseCtorParams.Count > 0)
						analyzer.ErrorSink.Add(Errors.UnexpectedParentCtorInvocation, analyzer.SourceUnit, position);
					baseCtorParams = null;
				}
				else
				{
					GenericQualifiedName parent_name = new GenericQualifiedName(new QualifiedName(Name.ParentClassName));
					DirectStMtdCall call_expr = new DirectStMtdCall(
                        position, parent_name, Position.Invalid,
                        method.DeclaringType.Base.Constructor.Name, Position.Invalid,
						baseCtorParams, TypeRef.EmptyList);

					body.Insert(0, new ExpressionStmt(position, call_expr));
					baseCtorParams = null;
				}
			}
			else
			{
				// the type immediately extends CLR type with no default ctor, yet there is no call to the base ctor;
				// note, all constructor overloads reflected from the CLR type are visible as we are in a subclass:
				if (method.IsConstructor && base_clr_type != null && !base_clr_type.ClrConstructor.HasParameterlessOverload)
				{
					analyzer.ErrorSink.Add(Errors.ExpectingParentCtorInvocation, analyzer.SourceUnit, position);
				}
			}
            if(method.IsDllImport && !method.IsStatic)
                analyzer.ErrorSink.Add(Errors.DllImportMethodMustBeStatic, analyzer.SourceUnit, position);
            if(method.IsDllImport && method.IsAbstract)
                analyzer.ErrorSink.Add(Errors.DllImportMethodCannotBeAbstract, analyzer.SourceUnit, position);

			if (body != null)
                body.Analyze(analyzer);

			method.ValidateBody(analyzer.ErrorSink);

			analyzer.LeaveMethodDeclaration();

			// add entry point if applicable:
			analyzer.SetEntryPoint(method, position);
		}

		private void AnalyzeBaseCtorCallParams(Analyzer/*!*/ analyzer, ClrType/*!*/ clrBase)
		{
			// we needn't to resolve the ctor here since the base class has to be known CLR type,
			// which has always a known ctor (may be a stub):
			ClrMethod base_ctor = clrBase.ClrConstructor;

			// create non-generic call signature:
			CallSignature call_sig = new CallSignature(baseCtorParams, TypeRef.EmptyList);

			RoutineSignature signature;
			int overload_index = base_ctor.ResolveOverload(analyzer, call_sig, position, out signature);

			if (overload_index == DRoutine.InvalidOverloadIndex)
			{
				analyzer.ErrorSink.Add(Errors.ClassHasNoVisibleCtor, analyzer.SourceUnit, position, clrBase.FullName);
			}
			else if (base_ctor.Overloads[overload_index].MandatoryParamCount != call_sig.Parameters.Count)
			{
				// invalid argument count passed to the base ctor:
				analyzer.ErrorSink.Add(Errors.InvalidArgumentCount, analyzer.SourceUnit, position);
			}

			call_sig.Analyze(analyzer, signature, AST.ExInfoFromParent.DefaultExInfo, true);

			// stores the signature on the type builder:
			method.DeclaringPhpType.Builder.BaseCtorCallSignature = call_sig;
			method.DeclaringPhpType.Builder.BaseCtorCallOverloadIndex = overload_index;

			// we don't need it any more:
			baseCtorParams = null;
		}

#if FALSE //!SILVERLIGHT
		internal override void AnalyzeDocComment(Analyzer/*!*/ analyzer, XmlDocFileBuilder/*!*/ builder, string/*!*/ prefix)
		{
			if (docComment == null) return;

			builder.WriteMethod(String.Concat(prefix, ".", method.Name), docComment);
		}
#endif

		#endregion

        /// <summary>Gets value indicating if this method is decorated with <see cref="System.Runtime.InteropServices.DllImportAttribute"/></summary>
        internal bool IsDllImport {
            get {
                if(Attributes.Attributes  == null) return false;
                foreach(CustomAttribute attr in Attributes.Attributes)
                    if((attr.Type is ClrType) && (attr.Type.RealType.Equals(typeof(System.Runtime.InteropServices.DllImportAttribute))))
                        return true;
                return false;
            }
        }
		
        #region Emission
        
		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("Class.MethodDecl");

			base.Emit(codeGenerator);

			// emit attributes on return value, generic and regular parameters:
			signature.Emit(codeGenerator);
			typeSignature.Emit(codeGenerator);

            if(method.IsDllImport) {
                //TODO: Support for DllImport
                Debug.Assert(false, "DllImport - not supported");
            } else if(!method.IsAbstract)
			{
                // returns immediately if the method is abstract:
				codeGenerator.EnterMethodDeclaration(method);

				// emits the arg-full overload:
				codeGenerator.EmitArgfullOverloadBody(method, body, entireDeclarationPosition, declarationBodyPosition);

				// restores original code generator settings:
				codeGenerator.LeaveMethodDeclaration();
			}
			else
			{
				// static abstract method is non-abstract in CLR => needs to have a body:
				if (method.IsStatic)
				{
					ILEmitter il = new ILEmitter(method.ArgFullInfo);
					il.Emit(OpCodes.Ldstr, method.DeclaringType.FullName);
					il.Emit(OpCodes.Ldstr, method.FullName);
					codeGenerator.EmitPhpException(il, Methods.PhpException.AbstractMethodCalled);
					il.Emit(OpCodes.Ldnull);
					il.Emit(OpCodes.Ret);
				}
			}

			// emits stubs for overridden/implemented methods and export stubs:
			codeGenerator.EmitOverrideAndExportStubs(method);
		}

		#endregion

		#region IPhpCustomAttributeProvider Members

		public override PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Method; } }

		public override AttributeTargets AcceptsTargets
		{
			get
			{
				return (method.IsConstructor ? AttributeTargets.Constructor : AttributeTargets.Method) |
				  AttributeTargets.ReturnValue;
			}
		}

		public override void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			switch (kind)
			{
				case SpecialAttributes.Export:
					Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);
					method.Builder.ExportInfo = (ExportAttribute)attribute;
					break;

				default:
					Debug.Fail("N/A");
					break;
			}
		}

		public override void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			if (selector == CustomAttribute.TargetSelectors.Return)
			{
				method.Builder.ReturnParamBuilder.SetCustomAttribute(builder);
			}
			else
			{
				Debug.Assert(method.ArgLessInfo is MethodBuilder, "PHP methods cannot be dynamic");
				((MethodBuilder)method.ArgFullInfo).SetCustomAttribute(builder);
			}
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitMethodDecl(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc { get; set; }
    }

	#endregion

	#region Fields

	/// <summary>
	/// Represents a field multi-declaration.
	/// </summary>
	/// <remarks>
	/// Is derived from LangElement because we need position to report field_in_interface error.
	/// Else we would have to test ClassType in every FieldDecl and not only in FildDeclList
	/// </remarks>
	public sealed class FieldDeclList : TypeMemberDecl, IPhpCustomAttributeProvider, IHasPhpDoc
	{
		private readonly List<FieldDecl>/*!*/ fields;
        /// <summary>List of fields in this list</summary>
        public List<FieldDecl> Fields/*!*/ { get { return fields; } }


		public FieldDeclList(Position position, PhpMemberAttributes modifiers, List<FieldDecl>/*!*/ fields,
			List<CustomAttribute> attributes)
			: base(position, attributes)
		{
			Debug.Assert(fields != null);

			this.modifiers = modifiers;
			this.fields = fields;
		}

		internal override void AnalyzeMembers(Analyzer/*!*/ analyzer, PhpType/*!*/ declaringType)
		{
			base.AnalyzeMembers(analyzer, declaringType);

			// no fields in interface:
			if (declaringType.IsInterface)
			{
				analyzer.ErrorSink.Add(Errors.FieldInInterface, analyzer.SourceUnit, position);
				return;
			}

			foreach (FieldDecl field in fields)
			{
				PhpField php_field = declaringType.AddField(field.Name, modifiers, field.HasInitVal, field.Position,
					analyzer.SourceUnit, analyzer.ErrorSink);

				field.AnalyzeMember(analyzer, php_field);
			}
		}

		internal override void Analyze(Analyzer/*!*/ analyzer)
		{
			base.Analyze(analyzer);

			foreach (FieldDecl field in fields)
				field.Analyze(analyzer);
		}

#if FALSE //!SILVERLIGHT
		internal override void AnalyzeDocComment(Analyzer/*!*/ analyzer, XmlDocFileBuilder/*!*/ builder, string/*!*/ prefix)
		{
			if (docComment == null) return;
			string xml_comment = docComment; // TODO

			foreach (FieldDecl field in fields)
				field.WriteDocComment(analyzer, builder, prefix, xml_comment);
		}
#endif

		internal override void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("Class.FieldDecl");
			base.Emit(codeGenerator);

			foreach (FieldDecl field in fields)
				field.Emit(codeGenerator);
		}

		#region IPhpCustomAttributeProvider Members

		public override PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Property; } }

		public override AttributeTargets AcceptsTargets
		{
			get { return AttributeTargets.Property | AttributeTargets.Field; }
		}

		public override void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			foreach (FieldDecl field in fields)
				field.ApplyCustomAttribute(kind, attribute, selector);
		}

		public override void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			foreach (FieldDecl field in fields)
				field.EmitCustomAttribute(builder, selector);
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFieldDeclList(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc { get; set; }
	}

	/// <summary>
	/// Represents a field declaration.
	/// </summary>
	public sealed class FieldDecl : LangElement
	{
		/// <summary>
		/// Gets a name of the field.
		/// </summary>
		public VariableName Name { get { return name; } }
		private VariableName name;

		/// <summary>
		/// Initial value of the field represented by compile time evaluated expression.
		/// After analysis represented by Literal or ConstantUse or ArrayEx with constant parameters.
		/// Can be null.
		/// </summary>
		private Expression initializer;
        /// <summary>
        /// Initial value of the field represented by compile time evaluated expression.
        /// After analysis represented by Literal or ConstantUse or ArrayEx with constant parameters.
        /// Can be null.
        /// </summary>
        public Expression Initializer { get { return initializer; } }
		/// <summary>
		/// Field representative, set by member analysis.
		/// </summary>
		public PhpField Field { get { return field; } }
		private PhpField field;

		/// <summary>
		/// Determines whether the field has an initializer.
		/// </summary>
		internal bool HasInitVal { get { return initializer != null; } }

		public FieldDecl(Position position, string/*!*/ name, Expression initVal)
			: base(position)
		{
			this.name = new VariableName(name);
			this.initializer = initVal;
		}

		internal void AnalyzeMember(Analyzer/*!*/ analyzer, PhpField/*!*/ field)
		{
			this.field = field;
		}

		internal void Analyze(Analyzer/*!*/ analyzer)
		{
			// field redeclared:
			if (field == null) return;

			if (initializer != null)
			{
				initializer = initializer.Analyze(analyzer, AST.ExInfoFromParent.DefaultExInfo).Literalize();
			}
		}

		internal void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			codeGenerator.InitializeField(field, initializer);
			codeGenerator.EmitOverrideAndExportStubs(field);
		}

		public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			field.RealFieldBuilder.SetCustomAttribute(builder);
		}

		internal void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			// field redeclared:
			if (field == null) return;

			switch (kind)
			{
				case SpecialAttributes.Export:
					Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);
					field.Builder.ExportInfo = (ExportAttribute)attribute;
					break;

				case SpecialAttributes.AppStatic:
					field.MemberDesc.MemberAttributes |= PhpMemberAttributes.AppStatic;
					break;

				default:
					Debug.Fail("N/A");
					throw null;
			}
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitFieldDecl(this);
        }
	}

	#endregion

	#region Class constants

	/// <summary>
	/// Represents a class constant declaration.
	/// </summary>
	public sealed class ConstDeclList : TypeMemberDecl, IPhpCustomAttributeProvider, IHasPhpDoc
	{
		private readonly List<ClassConstantDecl>/*!*/ constants;
        /// <summary>List of constants in this list</summary>
        public List<ClassConstantDecl>/*!*/ Constants { get { return constants; } }

		public ConstDeclList(Position position, List<ClassConstantDecl>/*!*/ constants, List<CustomAttribute> attributes)
			: base(position, attributes)
		{
			Debug.Assert(constants != null);

			this.constants = constants;

			//class constants never have modifiers
			modifiers = PhpMemberAttributes.Public;
		}

		internal override void AnalyzeMembers(Analyzer/*!*/ analyzer, PhpType/*!*/ declaringType)
		{
			base.AnalyzeMembers(analyzer, declaringType);

			foreach (ClassConstantDecl cd in constants)
			{
				// the value is filled later by full analysis:
				ClassConstant php_constant = declaringType.AddConstant(cd.Name, modifiers, cd.Position, analyzer.SourceUnit,
					analyzer.ErrorSink);

				cd.AnalyzeMember(analyzer, php_constant);
			}
		}

		internal override void Analyze(Analyzer/*!*/ analyzer)
		{
			base.Analyze(analyzer);

			foreach (ClassConstantDecl cd in constants)
				cd.Analyze(analyzer);
		}

#if FALSE //!SILVERLIGHT
		internal override void AnalyzeDocComment(Analyzer/*!*/ analyzer, XmlDocFileBuilder/*!*/ builder, string/*!*/ prefix)
		{
			if (docComment == null) return;
			string xml_comment = docComment; // TODO

			foreach (ClassConstantDecl cd in constants)
				cd.WriteDocComment(analyzer, builder, prefix, xml_comment);
		}
#endif

		internal override void Emit(CodeGenerator codeGenerator)
		{
			base.Emit(codeGenerator);

			foreach (ClassConstantDecl cd in constants)
				cd.Emit(codeGenerator);
		}

		#region IPhpCustomAttributeProvider Members

		public override PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Constant; } }

		public override AttributeTargets AcceptsTargets
		{
			get { return AttributeTargets.Field; }
		}

		public override void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			foreach (ClassConstantDecl cd in constants)
				cd.ApplyCustomAttribute(kind, attribute, selector);
		}

		public override void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			foreach (ClassConstantDecl cd in constants)
				cd.EmitCustomAttribute(builder, selector);
		}

		#endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitConstDeclList(this);
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc { get; set; }
	}

	public sealed class ClassConstantDecl : ConstantDecl
	{
        public override KnownConstant Constant { get { return constant; } }
		internal ClassConstant ClassConstant { get { return constant; } }
		private ClassConstant constant;

		public ClassConstantDecl(Position position, string/*!*/ name, Expression/*!*/ initializer)
			: base(position, name, initializer)
		{
		}

		internal void AnalyzeMember(Analyzer/*!*/ analyzer, ClassConstant/*!*/ constant)
		{
			this.constant = constant;

			// constant redeclared:
			if (constant == null) return;

			// initialize constant so that it has no value:
			this.constant.SetNode(this);
		}

#if FALSE //!SILVERLIGHT
		internal void WriteDocComment(Analyzer/*!*/ analyzer, XmlDocFileBuilder/*!*/ builder, string/*!*/ prefix, string/*!*/ xmlComment)
		{
			builder.WriteField(String.Concat(prefix, ".", name), xmlComment);
		}
#endif

		internal void Emit(CodeGenerator/*!*/ codeGenerator)
		{
			Statistics.AST.AddNode("Class.ConstantDecl");

			codeGenerator.InitializeClassConstant(constant);
			if (constant.IsExported)
			{
				string name = constant.FullName;

				// avoid duplicate export property names
				while (true)
				{
					DPropertyDesc prop_desc = constant.DeclaringPhpType.TypeDesc.GetProperty(new VariableName(name));
					if (prop_desc != null && prop_desc.PhpField.IsExported)
					{
						name = name + "_const";
					}
					else break;
				}

				PropertyBuilder exported_property = ClrStubBuilder.DefineFieldExport(name, constant);
				codeGenerator.EmitConstantExportStub(constant, exported_property);
			}
		}

		internal void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
		{
			// constant redeclared:
			if (constant == null) return;

			switch (kind)
			{
				case SpecialAttributes.Export:
					Debug.Assert(selector == CustomAttribute.TargetSelectors.Default);
					constant.ExportInfo = (ExportAttribute)attribute;
					break;

				default:
					Debug.Fail("N/A");
					break;
			}
		}

		internal void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
		{
			constant.RealFieldBuilder.SetCustomAttribute(builder);
		}

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitClassConstantDecl(this);
        }
	}

	#endregion

    #region Traits

    /// <summary>
    /// Represents class traits usage.
    /// </summary>
    public sealed class TraitsUse : TypeMemberDecl
    {
        #region TraitAdaptation, TraitAdaptationPrecedence, TraitAdaptationAlias

        public abstract class TraitAdaptation : LangElement
        {
            /// <summary>
            /// Name of existing trait member. Its qualified name is optional.
            /// </summary>
            public Tuple<QualifiedName?, Name> TraitMemberName { get; private set; }

            public TraitAdaptation(Position position, Tuple<QualifiedName?, Name> traitMemberName)
                : base(position)
            {
                this.TraitMemberName = traitMemberName;                
            }
        }

        /// <summary>
        /// Trait usage adaptation specifying a member which will be preferred over specified ambiguities.
        /// </summary>
        public sealed class TraitAdaptationPrecedence : TraitAdaptation
        {
            /// <summary>
            /// List of types which member <see cref="TraitAdaptation.TraitMemberName"/>.<c>Item2</c> will be ignored.
            /// </summary>
            public List<QualifiedName>/*!*/IgnoredTypes { get; private set; }

            public TraitAdaptationPrecedence(Position position, Tuple<QualifiedName?, Name> traitMemberName, List<QualifiedName>/*!*/ignoredTypes)
                :base(position, traitMemberName)
            {
                this.IgnoredTypes = ignoredTypes;
            }

            public override void VisitMe(TreeVisitor visitor)
            {
                visitor.VisitTraitAdaptationPrecedence(this);
            }
        }

        /// <summary>
        /// Trait usage adaptation which aliases a trait member.
        /// </summary>
        public sealed class TraitAdaptationAlias : TraitAdaptation
        {
            /// <summary>
            /// Optionally new member visibility attributes.
            /// </summary>
            public PhpMemberAttributes? NewModifier { get; private set; }

            /// <summary>
            /// Optionally new member name. Can be <c>null</c>.
            /// </summary>
            public string NewName { get; private set; }

            public TraitAdaptationAlias(Position position, Tuple<QualifiedName?, Name>/*!*/oldname, string newname, PhpMemberAttributes? newmodifier)
                : base(position, oldname)
            {
                if (oldname == null)
                    throw new ArgumentNullException("oldname");

                this.NewName = newname;
                this.NewModifier = newmodifier;
            }

            public override void VisitMe(TreeVisitor visitor)
            {
                visitor.VisitTraitAdaptationAlias(this);
            }
        }

        #endregion

        /// <summary>
        /// List of trait types to be used.
        /// </summary>
        public List<QualifiedName>/*!*/TraitsList { get { return traitsList; } }
        private readonly List<QualifiedName>/*!*/traitsList;

        /// <summary>
        /// List of trait adaptations modifying names of trait members. Can be <c>null</c> reference.
        /// </summary>
        public List<TraitAdaptation> TraitAdaptationList { get { return traitAdaptationList; } }
        private readonly List<TraitAdaptation> traitAdaptationList;

        public TraitsUse(Position position, List<QualifiedName>/*!*/traitsList, List<TraitAdaptation> traitAdaptationList)
            :base(position, null)
        {
            if (traitsList == null)
                throw new ArgumentNullException("traitsList");

            this.traitsList = traitsList;
            this.traitAdaptationList = traitAdaptationList;
        }

        #region TypeMemberDecl

        public override PhpAttributeTargets AttributeTarget
        {
            get { return PhpAttributeTargets.Types; }
        }

        public override AttributeTargets AcceptsTargets
        {
            get { return (AttributeTargets)0; }
        }

        public override void EmitCustomAttribute(CustomAttributeBuilder builder, CustomAttribute.TargetSelectors selector)
        {
            // nothing
        }

        public override void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
        {
            // nothing
        }

        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitTraitsUse(this);
        }

        #endregion

        internal override void Analyze(Analyzer analyzer)
        {
            // TODO: analyze traits use
        }

        internal override void Emit(CodeGenerator codeGenerator)
        {
            
        }
    }

    #endregion
}
