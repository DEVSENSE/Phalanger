/*

 Copyright (c) 2007- DEVSENSE
 Copyright (c) 2003-2006 Martin Maly, Ladislav Prosek, Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;

using PHP.Core.AST;
using FcnParam = System.Tuple<System.Collections.Generic.List<PHP.Core.AST.TypeRef>, System.Collections.Generic.List<PHP.Core.AST.ActualParam>, System.Collections.Generic.List<PHP.Core.AST.Expression>>;

namespace PHP.Core.Parsers
{
	#region Helpers
    
    /// <summary>
    /// Sink for specific language elements.
    /// Methods of this interface are called by the parser.
    /// In this way implementers are notified about declarations already during parsing,
    /// note root AST is not available at this time.
    /// </summary>
	public interface IReductionsSink
	{
		void InclusionReduced(Parser/*!*/ parser, IncludingEx/*!*/ decl);
		void FunctionDeclarationReduced(Parser/*!*/ parser, FunctionDecl/*!*/ decl);
		void TypeDeclarationReduced(Parser/*!*/ parser, TypeDecl/*!*/ decl);
		void GlobalConstantDeclarationReduced(Parser/*!*/ parser, GlobalConstantDecl/*!*/ decl);
        void NamespaceDeclReduced(Parser/*!*/ parser, NamespaceDecl/*!*/ decl);
        void LambdaFunctionReduced(Parser/*!*/ parser, LambdaFunctionExpr/*!*/ decl);
	}

    // Due to a MCS bug, it has to be in the other partial class in generated (Generated/Parser.cs)
    // .. uncomment the following once it is fixed!

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Explicit)]
	public partial struct SemanticValueType
	{
		public override string ToString()
		{
			if (Object != null) return Object.ToString();
			if (Offset != 0) return String.Format("[{0}-{1}]", Offset, Integer);
			if (Double != 0.0) return Double.ToString();
			return Integer.ToString();
		}
	}

	#endregion

	public partial class Parser
	{
		#region Reductions Sinks

        public static readonly IReductionsSink/*!*/ NullReductionSink = new _NullReductionsSink();

		private sealed class _NullReductionsSink : IReductionsSink
		{
			void IReductionsSink.InclusionReduced(Parser/*!*/ parser, IncludingEx/*!*/ incl)
			{
			}

			void IReductionsSink.FunctionDeclarationReduced(Parser/*!*/ parser, FunctionDecl/*!*/ decl)
			{
			}

			void IReductionsSink.TypeDeclarationReduced(Parser/*!*/ parser, TypeDecl/*!*/ decl)
			{
			}

			void IReductionsSink.GlobalConstantDeclarationReduced(Parser/*!*/ parser, GlobalConstantDecl/*!*/ decl)
			{
			}

            void IReductionsSink.NamespaceDeclReduced(Parser parser, NamespaceDecl decl)
            {
            }

            void IReductionsSink.LambdaFunctionReduced(Parser parser, LambdaFunctionExpr decl)
            {
            }
        }

		public sealed class ReductionsCounter : IReductionsSink
		{
			public int InclusionCount { get { return _inclusionCount; } }
			private int _inclusionCount = 0;

			public int FunctionCount { get { return _functionCount; } }
			private int _functionCount = 0;

			public int TypeCount { get { return _typeCount; } }
			private int _typeCount = 0;

			public int ConstantCount { get { return _constantCount; } }
			private int _constantCount = 0;

            void IReductionsSink.InclusionReduced(Parser/*!*/ parser, IncludingEx/*!*/ incl)
			{
				_inclusionCount++;
			}

			void IReductionsSink.FunctionDeclarationReduced(Parser/*!*/ parser, FunctionDecl/*!*/ decl)
			{
				_functionCount++;
			}

			void IReductionsSink.TypeDeclarationReduced(Parser/*!*/ parser, TypeDecl/*!*/ decl)
			{
				_typeCount++;
			}

			void IReductionsSink.GlobalConstantDeclarationReduced(Parser/*!*/ parser, GlobalConstantDecl/*!*/ decl)
			{
				_constantCount++;
			}

            void IReductionsSink.NamespaceDeclReduced(Parser parser, NamespaceDecl decl)
            {
            }

            void IReductionsSink.LambdaFunctionReduced(Parser parser, LambdaFunctionExpr decl)
            {
            }
		}

		#endregion
        
        protected sealed override int EofToken
		{
			get { return (int)Tokens.EOF; }
		}

		protected sealed override int ErrorToken
		{
			get { return (int)Tokens.ERROR; }
		}

        protected override bool IsDocCommentToken(int tok)
        {
            return (tok == (int)Tokens.T_DOC_COMMENT);
        }

        protected override Text.Span CombinePositions(Text.Span first, Text.Span last)
        {
            if (last.IsValid)
            {
                if (first.IsValid)
                    return Text.Span.Combine(first, last);
                else
                    return last;
            }
            else
                return first;
        }

        protected override Text.Span InvalidPosition
        {
            get
            {
                return Text.Span.Invalid;
            }
        }

		private Scanner scanner;
		private LanguageFeatures features;

		public ErrorSink ErrorSink { get { return errors; } }
		private ErrorSink errors;

		public SourceUnit SourceUnit { get { return sourceUnit; } }
		private SourceUnit sourceUnit;

		private IReductionsSink/*!*/reductionsSink;
		private bool unicodeSemantics;
		private TextReader reader;
		private Scope currentScope;

		public bool AllowGlobalCode { get { return allowGlobalCode; } set { allowGlobalCode = value; } }
		private bool allowGlobalCode;

		/// <summary>
		/// The root of AST.
		/// </summary>
		private GlobalCode astRoot;

		private const int strBufSize = 100;

        private NamespaceDecl currentNamespace;
        private bool IsInGlobalNamespace { get { return currentNamespace == null || currentNamespace.QualifiedName.Namespaces.Length == 0; } }
        private string CurrentNamespaceName { get { return IsInGlobalNamespace ? string.Empty : currentNamespace.QualifiedName.ToString(); } }

        /// <summary>
        /// Special names not namespaced. These names will not be translated using aliases and current namespace.
        /// The list is dynamically extended during parsing with generic arguments.
        /// </summary>
        private readonly HashSet<string>/*!*/reservedTypeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Name.SelfClassName.Value,
            Name.StaticClassName.Value,
            Name.ParentClassName.Value,
        };

		// stack of string buffers; used when processing encaps strings
        private readonly Stack<PhpStringBuilder> strBufStack = new Stack<PhpStringBuilder>(100);

		public Parser()
		{
		}

		private new bool Parse()
		{
			return false;
		}

        public GlobalCode Parse(SourceUnit/*!*/ sourceUnit,
            TextReader/*!*/ reader, ErrorSink/*!*/ errors,
			IReductionsSink reductionsSink, Lexer.LexicalStates initialLexicalState,
			LanguageFeatures features, int positionShift = 0)
		{
			Debug.Assert(reader != null && errors != null);

			// initialization:
            this.sourceUnit = sourceUnit;
            this.errors = errors;
            this.features = features;
            this.reader = reader;
            this.reductionsSink = reductionsSink ?? NullReductionSink;
            InitializeFields();

            this.scanner = new Scanner(reader, sourceUnit, errors, (reductionsSink as ICommentsSink) ?? (sourceUnit as ICommentsSink), features, positionShift);
			this.scanner.CurrentLexicalState = initialLexicalState;
			this.currentScope = new Scope(1); // starts assigning scopes from 2 (1 is reserved for prepended inclusion)

			this.unicodeSemantics = (features & LanguageFeatures.UnicodeSemantics) != 0;

			base.Scanner = this.scanner;
			base.Parse();

            GlobalCode result = astRoot;

			// clean and let GC collect unused AST and other stuff:
			ClearFields();

			return result;
		}

		private void InitializeFields()
		{
			strBufStack.Clear();
            //docCommentStack = null;
			condLevel = 0;

            Debug.Assert(sourceUnit != null);

            if (sourceUnit.CurrentNamespace.HasValue && sourceUnit.CurrentNamespace.Value.Namespaces.Length > 0)
            {   // J: inject current namespace from sourceUnit:
                this.currentNamespace = new AST.NamespaceDecl(default(Text.Span), sourceUnit.CurrentNamespace.Value.ToStringList(), true);

                // add aliases into the namespace:
                if (sourceUnit.Aliases.Count > 0)
                    foreach (var alias in sourceUnit.Aliases)
                        this.currentNamespace.Aliases.Add(alias.Key, alias.Value);
            }
            else
            {
                this.currentNamespace = null;
            }
		}

		private void ClearFields()
		{
			scanner = null;
			features = 0;
			errors = null;
			sourceUnit = null;
			reductionsSink = null;
			astRoot = null;
			reader = null;
		}

        #region Conditional Code, Scope

		private int condLevel;

		private void EnterConditionalCode()
		{
			condLevel++;
		}

		private void LeaveConditionalCode()
		{
			Debug.Assert(condLevel > 0);
			condLevel--;
		}

		public bool IsCurrentCodeConditional
		{
			get
			{
				return condLevel > 0;
			}
		}

		public bool IsCurrentCodeOneLevelConditional
		{
			get
			{
				return condLevel > 1;
			}
		}

		internal Scope GetScope()
		{
			currentScope.Increment();
			return currentScope;
		}

		#endregion Conditional Code

		#region Complex Productions

        private Expression CreateConcatExOrStringLiteral(Text.Span p, List<Expression> exprs, bool trimEoln)
		{
			PhpStringBuilder encapsed_str = strBufStack.Pop();
			
			if (trimEoln)
                encapsed_str.TrimEoln();

			if (exprs.Count > 0)
			{
                if (encapsed_str.Length > 0)
                    exprs.Add(encapsed_str.CreateLiteral());

				return new ConcatEx(p, exprs);
			}
            else
            {
                return encapsed_str.CreateLiteral();
            }
		}

        private VariableUse CreateStaticFieldUse(Text.Span span, CompoundVarUse/*!*/ className, CompoundVarUse/*!*/ field)
        {
            return CreateStaticFieldUse(span, new IndirectTypeRef(span, className, TypeRef.EmptyList), field);
        }
        private VariableUse CreateStaticFieldUse(Text.Span span, GenericQualifiedName/*!*/ className, Text.Span classNameSpan, CompoundVarUse/*!*/ field)
        {
            return CreateStaticFieldUse(span, DirectTypeRef.FromGenericQualifiedName(classNameSpan, className), field);
        }
		private VariableUse CreateStaticFieldUse(Text.Span span, TypeRef/*!*/ typeRef, CompoundVarUse/*!*/ field)
		{
			DirectVarUse dvu;
			IndirectVarUse ivu;

			if ((dvu = field as DirectVarUse) != null)
			{
                return new DirectStFldUse(span, typeRef, dvu.VarName, field.Span);
			}
			else if ((ivu = field as IndirectVarUse) != null)
			{
                return new IndirectStFldUse(span, typeRef, ivu.VarNameEx);
			}
			else
			{
				ItemUse iu = (ItemUse)field;
                iu.Array = CreateStaticFieldUse(iu.Array.Span, typeRef, (CompoundVarUse)iu.Array);
				return iu;
			}
		}

		private ForeachStmt/*!*/ CreateForeachStmt(Text.Span pos, Expression/*!*/ enumeree, ForeachVar/*!*/ var1,
		  Text.Span pos1, ForeachVar var2, Statement/*!*/ body)
		{
			ForeachVar key, value;
			if (var2 != null)
			{
				key = var1;
				value = var2;

				if (key.Alias)
				{
					errors.Add(Errors.KeyAlias, SourceUnit, pos1);
					key.Alias = false;
				}
			}
			else
			{
				key = null;
				value = var1;
			}

			return new ForeachStmt(pos, enumeree, key, value, body);
		}

		private void CheckVariableUse(Text.Span span, object item)
		{
			if (item as VariableUse == null)
			{
                errors.Add(FatalErrors.CheckVarUseFault, SourceUnit, span);
                throw new CompilerException();
			}
		}

        private FcnParam/*!*/ CreateFcnParam(FcnParam/*!*/fcnParam, Expression/*!*/arrayDereference)
        {
            var arrayKeyList = fcnParam.Item3;
            if (arrayKeyList == null)
                arrayKeyList = new List<Expression>(1);

            arrayKeyList.Add(arrayDereference);

            return new FcnParam(fcnParam.Item1, fcnParam.Item2, arrayKeyList);
        }

        private static VarLikeConstructUse/*!*/ CreateFcnArrayDereference(Text.Span pos, VarLikeConstructUse/*!*/varUse, List<Expression> arrayKeysExpression)
        {
            if (arrayKeysExpression != null && arrayKeysExpression.Count > 0)
            {
                // wrap fcnCall into ItemUse
                foreach (var keyExpr in arrayKeysExpression)
                    varUse = new ItemUse(pos, varUse, keyExpr, true);
            }

            return varUse;
        }

        private static VarLikeConstructUse/*!*/ DereferenceFunctionArrayAccess(VarLikeConstructUse/*!*/varUse)
        {
            ItemUse itemUse;
            while ((itemUse = varUse as ItemUse) != null && itemUse.IsFunctionArrayDereferencing)
                varUse = itemUse.Array;

            return varUse;
        }

		private static VarLikeConstructUse/*!*/ CreateVariableUse(Text.Span pos, VarLikeConstructUse/*!*/ variable, VarLikeConstructUse/*!*/ property,
                                                           FcnParam parameters, VarLikeConstructUse chain)
		{
			if (parameters != null)
			{
				if (property is ItemUse)
				{
					property.IsMemberOf = variable;
                    property = new IndirectFcnCall(pos, property, (List<ActualParam>)parameters.Item2, (List<TypeRef>)parameters.Item1);
				}
				else
				{
					DirectVarUse direct_use;
					if ((direct_use = property as DirectVarUse) != null)
					{
						QualifiedName method_name = new QualifiedName(new Name(direct_use.VarName.Value), Name.EmptyNames);
                        property = new DirectFcnCall(pos, method_name, null, property.Span, (List<ActualParam>)parameters.Item2, (List<TypeRef>)parameters.Item1);
					}
					else
					{
						IndirectVarUse indirect_use = (IndirectVarUse)property;
                        property = new IndirectFcnCall(pos, indirect_use.VarNameEx, (List<ActualParam>)parameters.Item2, (List<TypeRef>)parameters.Item1);
					}

                    property.IsMemberOf = variable;
                }

                // wrap into ItemUse
                property = CreateFcnArrayDereference(pos, property, parameters.Item3);
            }
			else
			{
				property.IsMemberOf = variable;
			}

			if (chain != null)
			{
				// finds the first variable use in the chain and connects it to the property

				VarLikeConstructUse first_in_chain = chain;
                for (;;)
                {
                    first_in_chain = DereferenceFunctionArrayAccess(first_in_chain);

                    if (first_in_chain.IsMemberOf != null)
                        first_in_chain = first_in_chain.IsMemberOf;
                    else
                        break;
                }

				first_in_chain.IsMemberOf = property;
				return chain;
			}
			else
			{
				return property;
			}
		}

        private static VarLikeConstructUse/*!*/ CreatePropertyVariable(Text.Span pos, CompoundVarUse/*!*/ property, FcnParam parameters)
		{
			if (parameters != null)
			{
				DirectVarUse direct_use;
				IndirectVarUse indirect_use;
                VarLikeConstructUse fcnCall;

				if ((direct_use = property as DirectVarUse) != null)
				{
					QualifiedName method_name = new QualifiedName(new Name(direct_use.VarName.Value), Name.EmptyNames);
                    fcnCall = new DirectFcnCall(pos, method_name, null, property.Span, (List<ActualParam>)parameters.Item2, (List<TypeRef>)parameters.Item1);
				}
                else if ((indirect_use = property as IndirectVarUse) != null)
                {
                    fcnCall = new IndirectFcnCall(pos, indirect_use.VarNameEx, (List<ActualParam>)parameters.Item2, (List<TypeRef>)parameters.Item1);
                }
                else
                {
                    fcnCall = new IndirectFcnCall(pos, (ItemUse)property, (List<ActualParam>)parameters.Item2, (List<TypeRef>)parameters.Item1);
                }

                // wrap fcnCall into ItemUse
                fcnCall = CreateFcnArrayDereference(pos, fcnCall, parameters.Item3);

                return fcnCall;
			}
			else
			{
				return property;
			}
		}

		private static VarLikeConstructUse/*!*/ CreatePropertyVariables(VarLikeConstructUse chain, VarLikeConstructUse/*!*/ member)
		{
            // dereference function array access:
            var element = DereferenceFunctionArrayAccess(member);
            
            // 
            if (chain != null)
			{
                IndirectFcnCall ifc = element as IndirectFcnCall;

				if (ifc != null && ifc.NameExpr as ItemUse != null)
				{
					// we know that FcNAme is VLCU and not Expression, because chain is being parsed:
					((VarLikeConstructUse)ifc.NameExpr).IsMemberOf = chain;
				}
				else
				{
                    element.IsMemberOf = chain;
				}
			}
			else
			{
                element.IsMemberOf = null;
			}

			return member;
		}

        private DirectFcnCall/*!*/ CreateDirectFcnCall(Text.Span pos, QualifiedName qname, Text.Span qnamePosition, List<ActualParam> args, List<TypeRef> typeArgs)
        {
            QualifiedName? fallbackQName;

            TranslateFallbackQualifiedName(ref qname, out fallbackQName);
            return new DirectFcnCall(pos, qname, fallbackQName, qnamePosition, args, typeArgs);
        }

        private GlobalConstUse/*!*/ CreateGlobalConstUse(Text.Span pos, QualifiedName qname)
        {
            QualifiedName? fallbackQName;

            if (qname.IsSimpleName && (qname == QualifiedName.Null || qname == QualifiedName.True || qname == QualifiedName.False))
            {
                // special global consts
                fallbackQName = null;
                qname.IsFullyQualifiedName = true;
            }
            else
            {
                TranslateFallbackQualifiedName(ref qname, out fallbackQName);
            }
            
            return new GlobalConstUse(pos, qname, fallbackQName);
        }

        /// <summary>
        /// Process <paramref name="qname"/>. Ensure <paramref name="qname"/> will be fully qualified.
        /// Outputs <paramref name="fallbackQName"/> which should be used if <paramref name="qname"/> does not refer to any existing entity.
        /// </summary>
        /// <param name="qname"></param>
        /// <param name="fallbackQName"></param>
        /// <remarks>Used for handling global function call and global constant use.
        /// In PHP entity in current namespace is tried first, then it falls back to global namespace.</remarks>
        private void TranslateFallbackQualifiedName(ref QualifiedName qname, out QualifiedName? fallbackQName)
        {
            qname = TranslateNamespace(qname);

            if (!qname.IsFullyQualifiedName && qname.IsSimpleName &&
                !IsInGlobalNamespace && !sourceUnit.HasImportedNamespaces &&
                !reservedTypeNames.Contains(qname.Name.Value))
            {
                // "\foo"
                fallbackQName = new QualifiedName(qname.Name) { IsFullyQualifiedName = true };

                // "namespace\foo"
                qname = new QualifiedName(qname.Name, currentNamespace.QualifiedName.Namespaces) { IsFullyQualifiedName = true };
            }
            else
            {
                fallbackQName = null;
                qname.IsFullyQualifiedName = true;  // just ensure
            }
        }

		private Expression/*!*/ CheckInitializer(Text.Span pos, Expression/*!*/ initializer)
		{
			if (initializer is ArrayEx)
			{
				errors.Add(Errors.ArrayInClassConstant, SourceUnit, pos);
				return new NullLiteral(pos);
			}

			return initializer;
		}

		private PhpMemberAttributes CheckPrivateType(Text.Span pos)
		{
			if (currentNamespace != null)
			{
				errors.Add(Errors.PrivateClassInGlobalNamespace, SourceUnit, pos);
				return PhpMemberAttributes.None;
			}

			return PhpMemberAttributes.Private;
		}

		private int CheckPartialType(Text.Span pos)
		{
			if (IsCurrentCodeConditional)
			{
				errors.Add(Errors.PartialConditionalDeclaration, SourceUnit, pos);
				return 0;
			}

			if (sourceUnit.IsTransient)
			{
				errors.Add(Errors.PartialTransientDeclaration, SourceUnit, pos);
				return 0;
			}

			if (!sourceUnit.IsPure)
			{
				errors.Add(Errors.PartialImpureDeclaration, SourceUnit, pos);
				return 0;
			}

			return 1;
		}

		private Statement/*!*/ CheckGlobalStatement(Statement/*!*/ statement)
		{
			if (sourceUnit.IsPure && !allowGlobalCode)
			{
				if (!statement.SkipInPureGlobalCode())
					errors.Add(Errors.GlobalCodeInPureUnit, SourceUnit, statement.Span);

				return EmptyStmt.Skipped;
			}

			return statement;
		}

		/// <summary>
		/// Checks whether a reserved class name is used in generic qualified name.
		/// </summary>
		private void CheckReservedNamesAbsence(Tuple<GenericQualifiedName, Text.Span> genericName)
		{
            if (genericName != null)
                CheckReservedNamesAbsence(genericName.Item1, genericName.Item2);
		}

        private void CheckReservedNamesAbsence(GenericQualifiedName genericName, Text.Span span)
        {
            if (genericName.QualifiedName.IsReservedClassName)
            {
                errors.Add(Errors.CannotUseReservedName, SourceUnit, span, genericName.QualifiedName.Name);
            }

            if (genericName.GenericParams != null)
                CheckReservedNamesAbsence(genericName.GenericParams, span);
        }

        private void CheckReservedNamesAbsence(object[] staticTypeRefs, Text.Span span)
        {
            foreach (object static_type_ref in staticTypeRefs)
                if (static_type_ref is GenericQualifiedName)
                    CheckReservedNamesAbsence((GenericQualifiedName)static_type_ref, span);
        }

        private void CheckReservedNamesAbsence(List<Tuple<GenericQualifiedName, Text.Span>> genericNames)
		{
            if (genericNames != null)
            {
                int count = genericNames.Count;
                for (int i = 0; i < count; i++)
                    CheckReservedNamesAbsence(genericNames[i].Item1, genericNames[i].Item2);
            }
		}

		private bool CheckReservedNameAbsence(Name typeName, Text.Span span)
		{
            if (typeName.IsReservedClassName)
            {
                errors.Add(Errors.CannotUseReservedName, SourceUnit, span, typeName);
                return false;
            }

            return true;
		}

        private void CheckTypeNameInUse(Name typeName, Text.Span span)
        {
            if (CurrentScopeAliases.ContainsKey(typeName.Value) || reservedTypeNames.Contains(typeName.Value))
                errors.Add(FatalErrors.ClassAlreadyInUse, SourceUnit, span, CurrentNamespaceName + typeName.Value);
        }

        /// <summary>
        /// Check is given <paramref name="declarerName"/> and its <paramref name="typeParams"/> are without duplicity.
        /// </summary>
        /// <param name="typeParams">Generic type parameters.</param>
        /// <param name="declarerName">Type name.</param>
		private void CheckTypeParameterNames(List<FormalTypeParam> typeParams, string/*!*/declarerName)
		{
			if (typeParams == null) return;

			for (int i = 0; i < typeParams.Count; i++)
			{
				if (typeParams[i].Name.Equals(declarerName))
				{
					ErrorSink.Add(Errors.GenericParameterCollidesWithDeclarer, SourceUnit, typeParams[i].Span, declarerName);
				}
                else if (CurrentScopeAliases.ContainsKey(typeParams[i].Name.Value))
                {
                    ErrorSink.Add(Errors.GenericAlreadyInUse, SourceUnit, typeParams[i].Span, typeParams[i].Name.Value);
                }
                else
                {
                    for (int j = 0; j < i; j++)
                    {
                        if (typeParams[j].Name.Equals(typeParams[i].Name))
                            errors.Add(Errors.DuplicateGenericParameter, SourceUnit, typeParams[i].Span);
                    }
                }
			}
		}

		private CustomAttribute.TargetSelectors IdentifierToTargetSelector(Text.Span span, string/*!*/ identifier)
		{
			if (identifier.EqualsOrdinalIgnoreCase("assembly"))
				return CustomAttribute.TargetSelectors.Assembly;

			if (identifier.EqualsOrdinalIgnoreCase("module"))
				return CustomAttribute.TargetSelectors.Module;

            if (identifier.EqualsOrdinalIgnoreCase("return"))
                return CustomAttribute.TargetSelectors.Return;

			errors.Add(Errors.InvalidAttributeTargetSelector, SourceUnit, span, identifier);
			return CustomAttribute.TargetSelectors.Default;
		}

		private List<CustomAttribute>/*!*/CustomAttributes(List<CustomAttribute>/*!*/ attrs, CustomAttribute.TargetSelectors targetSelector)
		{
			for (int i = 0; i < attrs.Count; i++)
				attrs[i].TargetSelector = targetSelector;

			return attrs;
		}

		#endregion

        //#region Imports

        ///// <summary>
        ///// Import of a particular type or function.
        ///// </summary>
        //public void AddImport(Position position, DeclarationKind kind, List<string>/*!*/ names, string aliasName)
        //{
        //    QualifiedName qn = new QualifiedName(names, true, true);
        //    Name alias = (aliasName != null) ? new Name(aliasName) : qn.Name;

        //    switch (kind)
        //    {
        //        case DeclarationKind.Type:
        //            if (!sourceUnit.AddTypeAlias(qn, alias))
        //                errors.Add(Errors.ConflictingTypeAliases, SourceUnit, position);
        //            break;

        //        case DeclarationKind.Function:
        //            if (!sourceUnit.AddFunctionAlias(qn, alias))
        //                errors.Add(Errors.ConflictingFunctionAliases, SourceUnit, position);
        //            break;

        //        case DeclarationKind.Constant:
        //            if (!sourceUnit.AddConstantAlias(qn, alias))
        //                errors.Add(Errors.ConflictingConstantAliases, SourceUnit, position);
        //            break;
        //    }
        //}

        ///// <summary>
        ///// Import of a namespace with a qualified name.
        ///// </summary>
        //public void AddImport(List<string>/*!*/ namespaceNames)
        //{
        //    sourceUnit.AddImportedNamespace(new QualifiedName(namespaceNames, false, true));
        //}

        ///// <summary>
        ///// Import of a namespace with a simple name.
        ///// </summary>
        //public void AddImport(string namespaceName)
        //{
        //    sourceUnit.AddImportedNamespace(new QualifiedName(Name.EmptyBaseName, new Name[] { new Name(namespaceName) }));
        //}
        public void AddImport(QualifiedName namespaceName)
        {
            if (sourceUnit.IsPure)
            {
                ErrorSink.Add(Warnings.ImportDeprecated, SourceUnit, this.yypos);   // deprecated statement

                sourceUnit.ImportedNamespaces.Add(namespaceName);
            }
            else
            {
                ErrorSink.Add(Errors.ImportOnlyInPureMode, sourceUnit, this.yypos); // does actually not happen, since T_IMPORT is not recognized outside Pure mode at all
            }
        }

        //#endregion

        #region aliases (use_statement)

        /// <summary>
        /// Dictionary of PHP aliases for the current scope.
        /// </summary>
        private Dictionary<string, QualifiedName>/*!*/ CurrentScopeAliases
        {
            get
            {
                return (currentNamespace != null) ? currentNamespace.Aliases : this.sourceUnit.Aliases;
            }
        }

        /// <summary>
        /// Add PHP alias (through <c>use</c> keyword).
        /// </summary>
        /// <param name="fullQualifiedName">Fully qualified aliased name.</param>
        private void AddAlias(QualifiedName fullQualifiedName)
        {
            AddAlias(fullQualifiedName, fullQualifiedName.Name.Value);
        }

        /// <summary>
        /// Add PHP alias (through <c>use</c> keyword).
        /// </summary>
        /// <param name="fullQualifiedName">Fully qualified aliased name.</param>
        /// <param name="alias">If not null, represents the alias name. Otherwise the last component from <paramref name="fullQualifiedName"/> is used.</param>
        private void AddAlias(QualifiedName fullQualifiedName, string/*!*/alias)
        {
            Debug.Assert(!string.IsNullOrEmpty(fullQualifiedName.Name.Value));
            Debug.Assert(!string.IsNullOrEmpty(alias));
            Debug.Assert(fullQualifiedName.IsFullyQualifiedName);

            //if (sourceUnit.CompilationUnit.IsTransient)   // J: supported, as it is in PHP. Simply within this transient sourceUnit
            //    throw new NotImplementedException("Adding an alias from within eval is not supported.");
            
            // check if it aliases itself:
            QualifiedName qualifiedAlias = new QualifiedName(
                new Name(alias),
                (currentNamespace != null) ? currentNamespace.QualifiedName : new QualifiedName(Name.EmptyBaseName));

            if (fullQualifiedName == qualifiedAlias) return;    // ignore
            
            // add the alias:
            
            // check for alias duplicity:
            if (!CurrentScopeAliases.ContainsKey(alias) && !reservedTypeNames.Contains(alias))
            {
                // TODO: check if there is no conflict with some class declaration (this should be in runtime ... but this overriding looks like useful features)

                // add alias into the scope
                CurrentScopeAliases.Add(alias, fullQualifiedName);
            }
            else
            {
                errors.Add(FatalErrors.AliasAlreadyInUse, this.sourceUnit, this.yypos, fullQualifiedName.NamespacePhpName, alias);
            }
        }

        private void ReserveTypeNames(List<FormalTypeParam> typeParams)
        {
            if (typeParams == null) return;
            foreach (var param in typeParams)
                reservedTypeNames.Add(param.Name.Value);
        }
        private void UnreserveTypeNames(List<FormalTypeParam> typeParams)
        {
            if (typeParams == null) return;
            foreach (var param in typeParams)
                reservedTypeNames.Remove(param.Name.Value);
        }

        /// <summary>
        /// Transforms each item of <paramref name="qnameList"/> using <see cref="TranslateAny(QualifiedName)"/> function.
        /// </summary>
        /// <param name="qnameList">List of qualified names.</param>
        /// <returns>Reference to <paramref name="qnameList"/>.</returns>
        private List<QualifiedName> TranslateAny(List<QualifiedName> qnameList)
        {
            for (int i = 0; i < qnameList.Count; i++)
                qnameList[i] = TranslateAny(qnameList[i]);
            
            return qnameList;
        }

        /// <summary>
        /// Translate the name using defined aliases. Any first part of the <see cref="QualifiedName"/> will be translated.
        /// </summary>
        /// <param name="qname">The name to translate.</param>
        /// <returns>Translated qualified name.</returns>
        /// <remarks>Fully qualified names are not translated.</remarks>
        private QualifiedName TranslateAny(QualifiedName qname)
        {
            if (qname.IsFullyQualifiedName) return qname;

            // skip special names:
            if (qname.IsSimpleName)
            {
                if (reservedTypeNames.Contains(qname.Name.Value))
                    return qname;
            }

            // return the alias if found:
            return TranslateAlias(qname);
        }

        /// <summary>
        /// Translate the name using defined aliases. Only namespace part of the <see cref="QualifiedName"/> will be translated. The <see cref="QualifiedName.Name"/> part will not.
        /// </summary>
        /// <param name="qname">The name to translate.</param>
        /// <returns>Translated qualified name.</returns>
        /// <remarks>Fully qualified names are not translated.</remarks>
        private QualifiedName TranslateNamespace(QualifiedName qname)
        {
            if (qname.IsFullyQualifiedName)
            {
                return qname;
            }

            if (qname.IsSimpleName)
            {
                // no namespace part, return not fully qualified simple name (function or constant), has to be handled during analysis:
                return qname;
            }
            else
            {
                return TranslateAlias(qname);
            }
        }

        /// <summary>
        /// Translate first part of given <paramref name="qname"/> into aliased <see cref="QualifiedName"/>.
        /// If no such alias is found, return original <paramref name="qname"/>.
        /// </summary>
        /// <param name="qname">Name which first part has tobe translated.</param>
        /// <returns>Translated <see cref="QualifiedName"/>.</returns>
        /// <remarks>Always returns fully qualified name.</remarks>
        private QualifiedName TranslateAlias(QualifiedName qname)
        {
            Debug.Assert(!qname.IsFullyQualifiedName);

            return QualifiedName.TranslateAlias(
                qname,
                CurrentScopeAliases,
                (IsInGlobalNamespace || sourceUnit.HasImportedNamespaces) ? (QualifiedName?)null : currentNamespace.QualifiedName);  // do not use current namespace, if there are imported namespace ... will be resolved later
        }

        #endregion

        #region PHPDocBlock

        protected object CreatePHPDocBlockStmt(object/*!*/doccomment)
        {
            Debug.Assert(doccomment is PHPDocBlock);
            return new PHPDocStmt(yypos, (PHPDocBlock)doccomment);
        }

        protected object SetPHPDoc(object obj, object phpdoc)
        {
            Debug.Assert(obj is LangElement);
            if (!object.ReferenceEquals(phpdoc, null))
            {
                Debug.Assert(phpdoc is PHPDocBlock);
                ((LangElement)obj).SetPHPDoc((PHPDocBlock)phpdoc);
            }

            return obj;
        }

        #endregion

        #region Helpers

        private static readonly List<Statement> emptyStatementList = new List<Statement>();
        private static readonly List<Tuple<GenericQualifiedName, Text.Span>> emptyGenericQualifiedNamePositionList = new List<Tuple<GenericQualifiedName, Text.Span>>();
		private static readonly List<FormalParam> emptyFormalParamListIndex = new List<FormalParam>();
		private static readonly List<ActualParam> emptyActualParamListIndex = new List<ActualParam>();
		private static readonly List<Expression> emptyExpressionListIndex = new List<Expression>();
		private static readonly List<Item> emptyItemListIndex = new List<Item>();
		private static readonly List<NamedActualParam> emptyNamedActualParamListIndex = new List<NamedActualParam>();
		private static readonly List<FormalTypeParam> emptyFormalTypeParamList = new List<FormalTypeParam>();
		private static readonly List<TypeRef> emptyTypeRefList = new List<TypeRef>();

        private static List<T>/*!*/ListAdd<T>(object list, object item)
        {
            Debug.Assert(list is List<T>);
            //Debug.Assert(item is T);

            var tlist = (List<T>)list;
            
            if (item is T)
            {
                tlist.Add((T)item);
            }
            else if (item != null)
            {
                Debug.Assert(item is List<T>);
                tlist.AddRange((List<T>)item);
            }

            return tlist;
        }

        private static object/*!*/StatementListAdd(object/*!*/listObj, object itemObj)
        {
            Debug.Assert(listObj is List<Statement>);

            if (!object.ReferenceEquals(itemObj, null))
            {
                Debug.Assert(itemObj is Statement);

                var list = (List<Statement>)listObj;
                var stmt = (Statement)itemObj;

                NamespaceDecl nsitem;
                
                // little hack when appending statement after simple syntaxed namespace:

                // namespace A;
                // foo();   // <-- add this statement into namespace A

                if (list.Count != 0 && (nsitem = list.Last() as NamespaceDecl) != null && nsitem.IsSimpleSyntax && !(stmt is NamespaceDecl))
                {
                    // adding a statement after simple namespace declaration => add the statement into the namespace:
                    StatementListAdd(nsitem.Statements, stmt);
                    //nsitem.UpdatePosition(Text.Span.CombinePositions(nsitem.Span, ((Statement)item).Span));
                }
                else
                {
                    if (list.Count != 0)
                    {
                        var last = list.Last();
                        if (last.GetType() == typeof(PHPDocStmt))
                            stmt.SetPHPDoc(((PHPDocStmt)last).PHPDoc);
                    }

                    list.Add(stmt);
                }
            }

            //
            return listObj;
        }

        private static void ListPrepend<T>(object/*!*/list, object/*!*/item)
        {
            Debug.Assert(list != null);
            Debug.Assert(item != null);
            Debug.Assert(item is T);
                
            var tlist = (List<T>)list;

            // little hack when prepending simple syntaxed namespace before some statements:
            
            // namespace A;  // == {item}
            // stmt1; stmt2; // <-- add these stamenents into namespace A
            // namespace B;

            NamespaceDecl nsitem = item as NamespaceDecl;
            if (nsitem != null && nsitem.IsSimpleSyntax)
            {
                Debug.Assert(list is List<Statement>);
                var slist = (List<Statement>)list;

                // move statements into nsitem namespace:
                int i = 0;  // find how many Statements from tlist move to nsitem.Statements
                while (i < slist.Count && !(slist[i] is NamespaceDecl))
                    ++i;

                if (i > 0)
                {
                    nsitem.Statements.AddRange(slist.Take(i));
                    //nsitem.UpdatePosition(Text.Span.CombinePositions(nsitem.Span, slist[i - 1].Span));
                    tlist.RemoveRange(0, i);
                }
            }
                
            // prepend the item:
            tlist.Insert(0, (T)item);
        }

		private static List<T>/*!*/NewList<T>(T item)
		{
			return new List<T>(1){ item };
        }

		private static List<T>/*!*/NewList<T>(object item)
		{
            return NewList<T>((T)item);
		}

        private static int GetHeadingEnd(Text.Span lastNonBodySymbolPosition)
        {
            return lastNonBodySymbolPosition.End;
        }

        private static int GetBodyStart(Text.Span bodyPosition)
        {
            return bodyPosition.Start;
        }

        /// <summary>
        /// Handles token that is not valid PHP class/namespace name token in PHP,
        /// but can be used from referenced C# library.
        /// </summary>
        /// <param name="span">Token position.</param>
        /// <param name="token">Token text.</param>
        /// <returns>Text of the token.</returns>
        private string CSharpNameToken(Text.Span span, string token)
        {
            // get token string:
            //string token = this.scanner.GetTokenString(position);

            if (token == null)
                throw new ArgumentNullException("token");
            
            // report syntax error if C# names are not allowed
            if ((this.features & LanguageFeatures.CSharpTypeNames) == 0)
            {
                this.ErrorSink.Add(FatalErrors.SyntaxError, this.SourceUnit, span, CoreResources.GetString("unexpected_token", token));
            }

            //
            return token;
        }

		#endregion
	}
}