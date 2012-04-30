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

        private PhpLambdaFunction/*!*/function;

        #region Construction

        public LambdaFunctionExpr(SourceUnit/*!*/ sourceUnit,
            Position position, Position entireDeclarationPosition, ShortPosition headingEndPosition, ShortPosition declarationBodyPosition,
            Scope scope, NamespaceDecl ns,
            bool aliasReturn, List<FormalParam>/*!*/ formalParams, List<FormalParam> useParams,
            List<Statement>/*!*/ body)
            : base(position)
        {
            Debug.Assert(formalParams != null && body != null);

            // inject use parameters at the begining of formal parameters
            if (useParams != null && useParams.Count > 0)
                formalParams.InsertRange(0, useParams);
            
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
            function = new PhpLambdaFunction(this.signature, sourceUnit, position);
            function.WriteUp(new TypeSignature(FormalTypeParam.EmptyList).ToPhpRoutineSignature(function));
        }

        #endregion

        #region Analysis

        internal override Evaluation Analyze(Analyzer analyzer, ExInfoFromParent info)
        {
            signature.AnalyzeMembers(analyzer, function);

            //attributes.Analyze(analyzer, this);

            // ensure 'use' parameters in parent scope:
            if (this.useParams != null)
                foreach (var p in this.useParams)
                    analyzer.CurrentVarTable.Set(p.Name, p.PassedByRef);

            // function is analyzed even if it is unreachable in order to discover more errors at compile-time:
            analyzer.EnterFunctionDeclaration(function);

            //typeSignature.Analyze(analyzer);
            signature.Analyze(analyzer);

            this.Body.Analyze(analyzer);
            
            // validate function and its body:
            function.ValidateBody(analyzer.ErrorSink);

            analyzer.LeaveFunctionDeclaration();

            return new Evaluation(this);
        }

        #endregion

        #region Emission

        internal override bool IsDeeplyCopied(CopyReason reason, int nestingLevel)
        {
            return false;
        }

        /// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
        internal override PhpTypeCode Emit(CodeGenerator/*!*/ codeGenerator)
        {
            Statistics.AST.AddNode("LambdaFunctionExpr");
            
            var typeBuilder = codeGenerator.IL.TypeBuilder;

            // define argless and argfull
            this.function.DefineBuilders(typeBuilder);

            //
            codeGenerator.MarkSequencePoint(position.FirstLine, position.FirstColumn, position.LastLine, position.LastColumn + 2);
            if (!codeGenerator.EnterFunctionDeclaration(function))
                throw new Exception("EnterFunctionDeclaration() failed!");

            codeGenerator.EmitArgfullOverloadBody(function, body, entireDeclarationPosition, declarationBodyPosition);
            
            codeGenerator.LeaveFunctionDeclaration();

            // new Closure( <context>, new RoutineDelegate(null,function.ArgLess), <parameters>, <static> )
            codeGenerator.EmitLoadScriptContext();

            var/*!*/il = codeGenerator.IL;
            il.Emit(OpCodes.Ldnull);
            il.Emit(OpCodes.Ldftn, function.ArgLessInfo);
            il.Emit(OpCodes.Newobj, Constructors.RoutineDelegate);

            if (signature.FormalParams != null && signature.FormalParams.Count > 0)
            {
                // array = new PhpArray(<int_count>, <string_count>);
                il.Emit(OpCodes.Ldc_I4, 0);
                il.Emit(OpCodes.Ldc_I4, signature.FormalParams.Count);
                il.Emit(OpCodes.Newobj, Constructors.PhpArray.Int32_Int32);

                foreach (var p in signature.FormalParams)
                {
                    // CALL array.SetArrayItem("&$name", "<required>" | "<optional>");
                    il.Emit(OpCodes.Dup);   // PhpArray

                    string keyValue = string.Format("{0}${1}", p.PassedByRef ? "&" : null, p.Name.Value);

                    il.Emit(OpCodes.Ldstr, keyValue);
                    il.Emit(OpCodes.Ldstr, (p.InitValue != null) ? "<optional>" : "<required>");
                    il.LdcI4(IntStringKey.StringKeyToArrayIndex(keyValue));

                    il.Emit(OpCodes.Call, Methods.PhpArray.SetArrayItemExact_String);
                }
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }

            if (useParams != null & useParams.Count > 0)
            {
                // array = new PhpArray(<int_count>, <string_count>);
                il.Emit(OpCodes.Ldc_I4, 0);
                il.Emit(OpCodes.Ldc_I4, useParams.Count);
                il.Emit(OpCodes.Newobj, Constructors.PhpArray.Int32_Int32);

                foreach (var p in useParams)
                {
                    // CALL array.SetArrayItem("&$name", "<required>" | "<optional>");
                    il.Emit(OpCodes.Dup);   // PhpArray

                    string variableName = p.Name.Value;

                    il.Emit(OpCodes.Ldstr, variableName);
                    if (p.PassedByRef)
                    {
                        DirectVarUse.EmitLoadRef(codeGenerator, p.Name);
                        il.Emit(OpCodes.Call, Methods.PhpArray.SetArrayItemRef_String);
                    }
                    else
                    {
                        DirectVarUse.EmitLoad(codeGenerator, p.Name);
                        il.LdcI4(IntStringKey.StringKeyToArrayIndex(variableName));
                        il.Emit(OpCodes.Call, Methods.PhpArray.SetArrayItemExact_String);
                    }
                }
            }
            else
            {
                il.Emit(OpCodes.Ldnull);
            }

            il.Emit(OpCodes.Newobj, typeof(PHP.Library.SPL.Closure).GetConstructor(new Type[] { typeof(ScriptContext), typeof(RoutineDelegate), typeof(PhpArray), typeof(PhpArray) }));
             
            return PhpTypeCode.Object;
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