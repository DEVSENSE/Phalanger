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

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region LambdaFunctionDecl

        [NodeCompiler(typeof(LambdaFunctionExpr))]
        sealed class LambdaFunctionExprCompiler : ExpressionCompiler<LambdaFunctionExpr>
        {
            private PhpLambdaFunction/*!A*/function;

            #region Analysis

            public override Evaluation Analyze(LambdaFunctionExpr node, Analyzer analyzer, ExInfoFromParent info)
            {
                // construct fake signature containing both - use params and regular params
                var allparams = new List<FormalParam>(node.Signature.FormalParams);
                if (node.UseParams != null)
                    allparams.InsertRange(0, node.UseParams);
                var signature = new Signature(false, allparams);

                //
                function = new PhpLambdaFunction(signature, analyzer.SourceUnit, node.Span);
                function.WriteUp(new TypeSignature(FormalTypeParam.EmptyList).ToPhpRoutineSignature(function));

                SignatureCompiler.AnalyzeMembers(signature, analyzer, function);

                //attributes.Analyze(analyzer, this);

                // ensure 'use' parameters in parent scope:
                if (node.UseParams != null)
                    foreach (var p in node.UseParams)
                        analyzer.CurrentVarTable.Set(p.Name, p.PassedByRef);

                // function is analyzed even if it is unreachable in order to discover more errors at compile-time:
                analyzer.EnterFunctionDeclaration(function);

                //typeSignature.Analyze(analyzer);
                SignatureCompiler.Analyze(signature, analyzer);

                node.Body.Analyze(analyzer);

                // validate function and its body:
                function.ValidateBody(analyzer.ErrorSink);

                analyzer.LeaveFunctionDeclaration();

                return new Evaluation(node);
            }

            #endregion

            #region Emission

            public override bool IsDeeplyCopied(LambdaFunctionExpr node, CopyReason reason, int nestingLevel)
            {
                return false;
            }

            public override PhpTypeCode Emit(LambdaFunctionExpr node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("LambdaFunctionExpr");

                var typeBuilder = codeGenerator.IL.TypeBuilder;

                // define argless and argfull
                this.function.DefineBuilders(typeBuilder);

                //
                codeGenerator.MarkSequencePoint(node.Span);
                if (!codeGenerator.EnterFunctionDeclaration(function))
                    throw new Exception("EnterFunctionDeclaration() failed!");

                codeGenerator.EmitArgfullOverloadBody(function, node.Body, node.EntireDeclarationSpan, node.DeclarationBodyPosition);

                codeGenerator.LeaveFunctionDeclaration();

                // new Closure( <context>, new RoutineDelegate(null,function.ArgLess), <parameters>, <static> )
                codeGenerator.EmitLoadScriptContext();

                var/*!*/il = codeGenerator.IL;
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldftn, function.ArgLessInfo);
                il.Emit(OpCodes.Newobj, Constructors.RoutineDelegate);

                if (node.Signature.FormalParams != null && node.Signature.FormalParams.Length != 0)
                {
                    // array = new PhpArray(<int_count>, <string_count>);
                    il.Emit(OpCodes.Ldc_I4, 0);
                    il.Emit(OpCodes.Ldc_I4, node.Signature.FormalParams.Length);
                    il.Emit(OpCodes.Newobj, Constructors.PhpArray.Int32_Int32);

                    foreach (var p in node.Signature.FormalParams)
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

                if (node.UseParams != null && node.UseParams.Count != 0)
                {
                    // array = new PhpArray(<int_count>, <string_count>);
                    il.Emit(OpCodes.Ldc_I4, 0);
                    il.Emit(OpCodes.Ldc_I4, node.UseParams.Count);
                    il.Emit(OpCodes.Newobj, Constructors.PhpArray.Int32_Int32);

                    foreach (var p in node.UseParams)
                    {
                        // <stack>.SetArrayItem{Ref}
                        il.Emit(OpCodes.Dup);   // PhpArray

                        string variableName = p.Name.Value;

                        il.Emit(OpCodes.Ldstr, variableName);
                        if (p.PassedByRef)
                        {
                            DirectVarUseCompiler.EmitLoadRef(codeGenerator, p.Name);
                            il.Emit(OpCodes.Call, Methods.PhpArray.SetArrayItemRef_String);
                        }
                        else
                        {
                            // LOAD PhpVariable.Copy( <name>, Assigned )
                            DirectVarUseCompiler.EmitLoad(codeGenerator, p.Name);
                            il.LdcI4((int)CopyReason.Assigned);
                            il.Emit(OpCodes.Call, Methods.PhpVariable.Copy);

                            // .SetArrayItemExact( <stack>, <stack>, <hashcode> )
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
            }

            #endregion
        }

        #endregion
    }
}