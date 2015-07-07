/*

 Copyright (c) 2012 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;

using PHP.Core.Parsers;

namespace PHP.Core.AST
{
    #region LambdaFunctionDecl

    /// <summary>
    /// Represents a function declaration.
    /// </summary>
    [Serializable]
    public sealed class LambdaFunctionExpr : Expression, IHasSourceUnit, IDeclarationElement
    {
        /// <summary>
        /// Gets namespace containing this lambda expression. Can be <c>null</c>.
        /// </summary>
        public NamespaceDecl Namespace { get { return ns; } }
        private readonly NamespaceDecl ns;

        public override Operations Operation
        {
            get { return Operations.Closure; }
        }

        /// <summary>
        /// <see cref="PHPDocBlock"/> instance or <c>null</c> reference.
        /// </summary>
        public PHPDocBlock PHPDoc
        {
            get { return this.GetPHPDoc(); }
            set { this.SetPHPDoc(value); }
        }

        public Signature Signature { get { return signature; } }
        private readonly Signature signature;

        /// <summary>
        /// Parameters specified within <c>use</c> 
        /// </summary>
        public List<FormalParam> UseParams { get { return useParams; } }
        private readonly List<FormalParam> useParams;

        //private readonly TypeSignature typeSignature;
        public Statement[]/*!*/ Body { get { return body; } }
        private readonly Statement[]/*!*/ body;
        //private readonly CustomAttributes attributes;

        public Text.Span EntireDeclarationSpan { get { return entireDeclarationSpan; } }
        private readonly Text.Span entireDeclarationSpan;

        public int HeadingEndPosition { get { return headingEndPosition; } }
        private readonly int headingEndPosition;

        public int DeclarationBodyPosition { get { return declarationBodyPosition; } }
        private readonly int declarationBodyPosition;

        /// <summary>
        /// Gets the source file <see cref="SourceUnit"/>. Cannot be <c>null</c>.
        /// </summary>
        public SourceUnit/*!*/SourceUnit { get { return this.sourceUnit; } }
        private readonly SourceUnit/*!*/sourceUnit;

        #region Construction

        public LambdaFunctionExpr(SourceUnit/*!*/ sourceUnit,
            Text.Span span, Text.Span entireDeclarationPosition, int headingEndPosition, int declarationBodyPosition,
            Scope scope, NamespaceDecl ns,
            bool aliasReturn, List<FormalParam>/*!*/ formalParams, List<FormalParam> useParams,
            IList<Statement>/*!*/ body)
            : base(span)
        {
            Debug.Assert(formalParams != null && body != null);
            Debug.Assert(sourceUnit != null);

            this.sourceUnit = sourceUnit;
            
            this.ns = ns;
            this.signature = new Signature(aliasReturn, formalParams);
            this.useParams = useParams;
            //this.typeSignature = new TypeSignature(genericParams);
            //this.attributes = new CustomAttributes(attributes);
            this.body = body.AsArray();
            this.entireDeclarationSpan = entireDeclarationPosition;
            this.headingEndPosition = headingEndPosition;
            this.declarationBodyPosition = declarationBodyPosition;
        }

        #endregion

        /// <summary>
        /// Call the right Visit* method on the given Visitor object.
        /// </summary>
        /// <param name="visitor">Visitor to be called.</param>
        public override void VisitMe(TreeVisitor visitor)
        {
            visitor.VisitLambdaFunctionExpr(this);
        }
    }

    #endregion
}