/*

 Copyright (c) 2007- DEVSENSE
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

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
	#region FormalParam

	/// <summary>
	/// Represents a formal parameter definition.
	/// </summary>
    [Serializable]
	public sealed class FormalParam : LangElement
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
        public bool IsOut { get { return isOut; } internal set { isOut = value; } }
		private bool isOut;

		/// <summary>
		/// Initial value expression. Can be <B>null</B>.
		/// </summary>
        public Expression InitValue { get { return initValue; } internal set { initValue = value; } }
		private Expression initValue;

		/// <summary>
		/// Either <see cref="PrimitiveTypeName"/>, <see cref="GenericQualifiedName"/>, or <B>null</B>.
		/// </summary>
        public object TypeHint { get { return typeHint; } }
		private object typeHint;

        /// <summary>Position of <see cref="TypeHint"/> if any.</summary>
        public Text.Span TypeHintPosition { get; internal set; }

		/// <summary>
        /// Gets collection of CLR attributes annotating this statement.
        /// </summary>
        public CustomAttributes Attributes
        {
            get { return this.GetCustomAttributes(); }
            set { this.SetCustomAttributes(value); }
        }

        #region Construction

		public FormalParam(Text.Span span, string/*!*/ name, object typeHint, bool passedByRef,
				Expression initValue, List<CustomAttribute> attributes)
            : base(span)
		{
            Debug.Assert(typeHint == null || typeHint is PrimitiveTypeName || typeHint is GenericQualifiedName);

			this.name = new VariableName(name);
			this.typeHint = typeHint;
			this.passedByRef = passedByRef;
			this.initValue = initValue;
            if (attributes != null && attributes.Count != 0)
                this.Attributes = new CustomAttributes(attributes);

			this.TypeHintPosition = Text.Span.Invalid;
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

    [Serializable]
    public struct Signature
	{
		public bool AliasReturn { get { return aliasReturn; } }
		private readonly bool aliasReturn;

		public FormalParam[]/*!*/ FormalParams { get { return formalParams; } }
		private readonly FormalParam[]/*!*/ formalParams;

		public Signature(bool aliasReturn, IList<FormalParam>/*!*/ formalParams)
		{
			this.aliasReturn = aliasReturn;
			this.formalParams = formalParams.AsArray();
		}
	}

	#endregion

	#region FunctionDecl

	/// <summary>
	/// Represents a function declaration.
	/// </summary>
    [Serializable]
    public sealed class FunctionDecl : Statement, IHasSourceUnit
	{ 
		internal override bool IsDeclaration { get { return true; } }

		public Name Name { get { return name; } }
		private readonly Name name;

		public NamespaceDecl Namespace { get { return ns; } }
		private readonly NamespaceDecl ns;

        public Signature Signature { get { return signature; } }
        private readonly Signature signature;

        public TypeSignature TypeSignature { get { return typeSignature; } }
		private readonly TypeSignature typeSignature;

        public Statement[]/*!*/ Body { get { return body; } }
        private readonly Statement[]/*!*/ body;

        /// <summary>
        /// Gets value indicating whether the function is declared conditionally.
        /// </summary>
        public bool IsConditional { get; private set; }

        /// <summary>
        /// Gets function declaration attributes.
        /// </summary>
        public PhpMemberAttributes MemberAttributes { get; private set; }

        internal Scope Scope { get; private set; }
        public SourceUnit/*!*/ SourceUnit { get; private set; }
        
        /// <summary>
        /// Gets collection of CLR attributes annotating this statement.
        /// </summary>
        public CustomAttributes Attributes
        {
            get { return this.GetCustomAttributes(); }
            set { this.SetCustomAttributes(value); }
        }

        public Text.Span EntireDeclarationPosition { get { return entireDeclarationPosition; } }
        private Text.Span entireDeclarationPosition;

        public int HeadingEndPosition { get { return headingEndPosition; } }
        private int headingEndPosition;

        public int DeclarationBodyPosition { get { return declarationBodyPosition; } }
        private int declarationBodyPosition;

		#region Construction

		public FunctionDecl(SourceUnit/*!*/ sourceUnit,
            Text.Span span, Text.Span entireDeclarationPosition, int headingEndPosition, int declarationBodyPosition,
			bool isConditional, Scope scope, PhpMemberAttributes memberAttributes, string/*!*/ name, NamespaceDecl ns,
			bool aliasReturn, List<FormalParam>/*!*/ formalParams, List<FormalTypeParam>/*!*/ genericParams,
			IList<Statement>/*!*/ body, List<CustomAttribute> attributes)
			: base(span)
		{
			Debug.Assert(genericParams != null && name != null && formalParams != null && body != null);

			this.name = new Name(name);
			this.ns = ns;
			this.signature = new Signature(aliasReturn, formalParams);
			this.typeSignature = new TypeSignature(genericParams);
			if (attributes != null && attributes.Count != 0)
                this.Attributes = new CustomAttributes(attributes);
			this.body = body.AsArray();
			this.entireDeclarationPosition = entireDeclarationPosition;
            this.headingEndPosition = headingEndPosition;
            this.declarationBodyPosition = declarationBodyPosition;
            this.IsConditional = isConditional;
            this.MemberAttributes = memberAttributes;
            this.Scope = scope;
            this.SourceUnit = sourceUnit;
		}

		#endregion

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
        public PHPDocBlock PHPDoc
        {
            get { return this.GetPHPDoc(); }
            set { this.SetPHPDoc(value); }
        }
	}

	#endregion
}
