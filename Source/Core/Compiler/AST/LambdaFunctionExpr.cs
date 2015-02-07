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
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;

using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.AST
{
    #region LambdaFunctionDecl

    /// <summary>
    /// Represents a function declaration.
    /// </summary>
    public sealed class LambdaFunctionExpr : Expression
    {
        //public NamespaceDecl Namespace { get { return ns; } }
        //private readonly NamespaceDecl ns

        internal override Operations Operation
        {
            get { return Operations.Closure; }
        }

        public Signature Signature { get { return signature; } }
        private readonly Signature signature;

        /// <summary>
        /// Parameters specified within <c>use</c> 
        /// </summary>
        public List<FormalParam> UseParams { get { return useParams; } }
        private readonly List<FormalParam> useParams;

        //private readonly TypeSignature typeSignature;
        private readonly List<Statement>/*!*/ body;
        public List<Statement>/*!*/ Body { get { return body; } }
        //private readonly CustomAttributes attributes;

        public Position EntireDeclarationPosition { get { return entireDeclarationPosition; } }
        private Position entireDeclarationPosition;

        public ShortPosition HeadingEndPosition { get { return headingEndPosition; } }
        private ShortPosition headingEndPosition;

        public ShortPosition DeclarationBodyPosition { get { return declarationBodyPosition; } }
        private ShortPosition declarationBodyPosition;

        private PhpMethod/*!*/function;

        #region Construction

        public LambdaFunctionExpr(SourceUnit/*!*/ sourceUnit,
            Position position, Position entireDeclarationPosition, ShortPosition headingEndPosition, ShortPosition declarationBodyPosition,
            Scope scope, NamespaceDecl ns,
            bool aliasReturn, List<FormalParam>/*!*/ formalParams, List<FormalParam> useParams,
            List<Statement>/*!*/ body)
            : base(position)
        {
            Debug.Assert(formalParams != null && body != null);

            //this.ns = ns;
            this.signature = new Signature(aliasReturn, formalParams);
            this.useParams = useParams;
            //this.typeSignature = new TypeSignature(genericParams);
            //this.attributes = new CustomAttributes(attributes);
            this.body = body;
            this.entireDeclarationPosition = entireDeclarationPosition;
            this.headingEndPosition = headingEndPosition;
            this.declarationBodyPosition = declarationBodyPosition;

            //QualifiedName qn = (ns != null) ? new QualifiedName(this.name, ns.QualifiedName) : new QualifiedName(this.name);
            //function = new PhpFunction(qn, memberAttributes, signature, typeSignature, isConditional, scope, sourceUnit, position);
            //function.WriteUp(typeSignature.ToPhpRoutineSignature(function));
            function = null;// new PhpMethod(CLOSURE, __invoke, None, true, signature, typeSignature, sourceUnit, position);
        }

        #endregion

        #region Analysis

        internal override Evaluation Analyze(Analyzer analyzer, ExInfoFromParent info)
        {
            if (function == null)
                throw new NotImplementedException();

            //attributes.Analyze(analyzer, this);

            // function is analyzed even if it is unreachable in order to discover more errors at compile-time:
            analyzer.EnterMethodDeclaration(function);

            //typeSignature.Analyze(analyzer);
            signature.Analyze(analyzer);

            //function.Validate(analyzer.ErrorSink);

            for (int i = 0; i < body.Count; i++)
            {
                body[i] = body[i].Analyze(analyzer);
            }

            // validate function and its body:
            function.ValidateBody(analyzer.ErrorSink);

            /*
            if (docComment != null)
                AnalyzeDocComment(analyzer);
            */

            analyzer.LeaveMethodDeclaration();

            return new Evaluation(this);
        }

        #endregion

        #region Emission

        /// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
        internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
        {
            Statistics.AST.AddNode("LambdaFunctionExpr");

            throw new NotImplementedException();

            //// marks a sequence point if function is declared here (i.e. is m-decl):
            ////Note: this sequence point goes to the function where this function is declared not to this declared function!
            //if (!function.IsLambda && function.Declaration.IsConditional)
            //    codeGenerator.MarkSequencePoint(position.FirstLine, position.FirstColumn, position.LastLine, position.LastColumn + 2);

            //// emits attributes on the function itself, its return value, type parameters and regular parameters:
            //attributes.Emit(codeGenerator, this);
            //signature.Emit(codeGenerator);
            //typeSignature.Emit(codeGenerator);

            //// prepares code generator for emitting arg-full overload;
            //// false is returned when the body should not be emitted:
            //if (!codeGenerator.EnterFunctionDeclaration(function)) return;

            //// emits the arg-full overload:
            //codeGenerator.EmitArgfullOverloadBody(function, body, entireDeclarationPosition, declarationBodyPosition);

            //// restores original code generator settings:
            //codeGenerator.LeaveFunctionDeclaration();

            //// emits function declaration (if needed):
            //// ignore s-decl function declarations except for __autoload;
            //// __autoload function is declared in order to avoid using callbacks when called:
            //if (function.Declaration.IsConditional && !function.QualifiedName.IsAutoloadName)
            //{
            //    Debug.Assert(!function.IsLambda);
            //    codeGenerator.EmitDeclareFunction(function);
            //}
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