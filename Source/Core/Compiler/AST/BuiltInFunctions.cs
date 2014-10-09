/*

 Copyright (c) 2013 DEVSENSE
 
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region IncludingEx

        [NodeCompiler(typeof(IncludingEx))]
        sealed class IncludingExCompiler : ExpressionCompiler<IncludingEx>, IIncludingExCompiler
        {
            #region IIncludingExCompiler

            /// <summary>
            /// Static inclusion info or <B>null</B> reference if target cannot be determined statically.
            /// Set during inclusion graph building, before the analysis takes place.
            /// </summary>
            public StaticInclusion Inclusion { get { return inclusion; } /* CompilationUnit */ set { inclusion = value; } }
            private StaticInclusion inclusion;

            /// <summary>
            /// Set during inclusion graph building, before the analysis takes place.
            /// </summary>
            public Characteristic Characteristic { get { return characteristic; } set { characteristic = value; } }
            private Characteristic characteristic;

            #endregion

            public override Evaluation Analyze(IncludingEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                // if the expression should be emitted:
                if (characteristic == Characteristic.Dynamic || characteristic == Characteristic.StaticArgEvaluated)
                {
                    node.Target = node.Target.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                }

                analyzer.AddCurrentRoutineProperty(RoutineProperties.ContainsInclude);

                analyzer.CurrentScope = node.Scope;

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(IncludingEx node, CodeGenerator codeGenerator)
            {
                PhpTypeCode result;

                // emits inclusion and Main() call:
                if (inclusion != null)
                    result = EmitStaticInclusion(node, codeGenerator);
                else
                    result = EmitDynamicInclusion(node, codeGenerator);

                // return value conversion:
                codeGenerator.EmitReturnValueHandling(node, false, ref result);

                return result;
            }

            /// <summary>
            /// Emits a static inclusion.
            /// </summary>
            private PhpTypeCode EmitStaticInclusion(IncludingEx node, CodeGenerator/*!*/ codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                Label endif_label = il.DefineLabel();
                Label else_label = il.DefineLabel();
                MethodInfo method;

                // if the expression should be emitted:
                if (characteristic == Characteristic.StaticArgEvaluated)
                {
                    if (!(node.Target is StringLiteral || node.Target is BinaryStringLiteral))
                    {
                        // emits expression evaluation and ignores the result:
                        node.Target.Emit(codeGenerator);
                        il.Emit(OpCodes.Pop);
                    }
                }

                if (characteristic == Characteristic.StaticAutoInclusion)
                {
                    // calls the Main routine only if this script is the main one:
                    il.Ldarg(ScriptBuilder.ArgIsMain);
                }
                else
                {
                    RelativePath relativePath = new RelativePath(inclusion.Includee.RelativeSourcePath);    // normalize the relative path

                    // CALL context.StaticInclude(<relative included script source path>,<this script type>,<inclusion type>);
                    codeGenerator.EmitLoadScriptContext();
                    il.Emit(OpCodes.Ldc_I4, (int)relativePath.Level);
                    il.Emit(OpCodes.Ldstr, relativePath.Path);
                    il.Emit(OpCodes.Ldtoken, inclusion.Includee.ScriptClassType);
                    il.LoadLiteral(node.InclusionType);
                    il.Emit(OpCodes.Call, Methods.ScriptContext.StaticInclude);
                }

                // IF (STACK)
                il.Emit(OpCodes.Brfalse, else_label);
                if (true)
                {
                    // emits a call to the main helper of the included script:
                    method = inclusion.Includee.MainHelper;

                    // CALL <Main>(context, variables, self, includer, false):
                    codeGenerator.EmitLoadScriptContext();
                    codeGenerator.EmitLoadRTVariablesTable();
                    codeGenerator.EmitLoadSelf();
                    codeGenerator.EmitLoadClassContext();
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Call, method);

                    il.Emit(OpCodes.Br, endif_label);
                }

                // ELSE

                il.MarkLabel(else_label);
                if (true)
                {
                    // LOAD <PhpScript.SkippedIncludeReturnValue>;                          
                    il.LoadLiteral(ScriptModule.SkippedIncludeReturnValue);
                    il.Emit(OpCodes.Box, ScriptModule.SkippedIncludeReturnValue.GetType());
                }

                il.MarkLabel(endif_label);
                // END IF 

                return PhpTypeCode.Object;
            }

            /// <summary>
            /// Emits dynamic inclusion.
            /// </summary>
            private PhpTypeCode EmitDynamicInclusion(IncludingEx node, CodeGenerator/*!*/ codeGenerator)
            {
                // do not generate dynamic auto inclusions:
                if (InclusionTypesEnum.IsAutoInclusion(node.InclusionType))
                    return PhpTypeCode.Void;

                ILEmitter il = codeGenerator.IL;

                // CALL context.DynamicInclude(<file name>,<relative includer source path>,variables,self,includer);
                codeGenerator.EmitLoadScriptContext();
                codeGenerator.EmitConversion(node.Target, PhpTypeCode.String);
                il.Emit(OpCodes.Ldstr, codeGenerator.SourceUnit.SourceFile.RelativePath.ToString());
                codeGenerator.EmitLoadRTVariablesTable();
                codeGenerator.EmitLoadSelf();
                codeGenerator.EmitLoadClassContext();
                il.LoadLiteral(node.InclusionType);
                il.Emit(OpCodes.Call, Methods.ScriptContext.DynamicInclude);

                return PhpTypeCode.Object;
            }
        }

        #endregion

        #region IssetEx

        [NodeCompiler(typeof(IssetEx))]
        sealed class IssetExCompiler : ExpressionCompiler<IssetEx>
        {
            public override Evaluation Analyze(IssetEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                var vars = node.VarList;
                for (int i = 0; i < vars.Count; i++)
                    vars[i].Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(IssetEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.None || access == AccessType.Read);
                Statistics.AST.AddNode("IssetEx");
                ILEmitter il = codeGenerator.IL;

                codeGenerator.ChainBuilder.Create();
                codeGenerator.ChainBuilder.QuietRead = true;

                var vars = node.VarList;

                if (vars.Count == 1)
                {
                    codeGenerator.EmitBoxing(VariableUseHelper.EmitIsset(vars[0], codeGenerator, false));

                    // Compare the result with "null"
                    il.CmpNotNull();
                }
                else
                {
                    // Define labels 
                    Label f_label = il.DefineLabel();
                    Label x_label = il.DefineLabel();

                    // Get first variable
                    codeGenerator.EmitBoxing(VariableUseHelper.EmitIsset(vars[0], codeGenerator, false));

                    // Compare the result with "null"
                    il.CmpNotNull();

                    // Process following variables and include branching
                    for (int i = 1; i < vars.Count; i++)
                    {
                        il.Emit(OpCodes.Brfalse, f_label);
                        codeGenerator.EmitBoxing(VariableUseHelper.EmitIsset(vars[i], codeGenerator, false));

                        // Compare the result with "null"
                        codeGenerator.IL.CmpNotNull();
                    }

                    il.Emit(OpCodes.Br, x_label);
                    il.MarkLabel(f_label, true);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.MarkLabel(x_label, true);
                }

                codeGenerator.ChainBuilder.End();

                if (access == AccessType.None)
                {
                    il.Emit(OpCodes.Pop);
                    return PhpTypeCode.Void;
                }

                return PhpTypeCode.Boolean;
            }
        }

        #endregion

        #region EmptyEx

        [NodeCompiler(typeof(EmptyEx))]
        sealed class EmptyExCompiler : ExpressionCompiler<EmptyEx>
        {
            public override Evaluation Analyze(EmptyEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                var expression = node.Expression;
                var evaluation = expression
                    .Analyze(analyzer, ExInfoFromParent.DefaultExInfo)
                    .Evaluate(node, out expression);
                node.Expression = expression;

                return evaluation;
            }

            public override object Evaluate(EmptyEx node, object value)
            {
                return !Convert.ObjectToBoolean(value);
            }

            /// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
            /// <param name="node">Instance.</param>
            /// <remarks>
            /// Nothing is expected on the evaluation stack. The result value is left on the
            /// evaluation stack.
            /// </remarks>
            public override PhpTypeCode Emit(EmptyEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.Read || access == AccessType.None);
                Statistics.AST.AddNode("EmptyEx");

                var variable = node.Expression as VariableUse;

                //
                codeGenerator.ChainBuilder.Create();

                if (variable != null)
                {
                    // legacy isset behaviour (before PHP 5.5)
                    codeGenerator.ChainBuilder.QuietRead = true;

                    // call EmitIsset in order to evaluate the variable quietly
                    codeGenerator.EmitBoxing(variable.EmitIsset(codeGenerator, true));
                    codeGenerator.IL.Emit(OpCodes.Call, Methods.PhpVariable.IsEmpty);
                }
                else
                {
                    codeGenerator.EmitObjectToBoolean(node.Expression, true);
                }

                //
                codeGenerator.ChainBuilder.End();


                if (access == AccessType.None)
                {
                    codeGenerator.IL.Emit(OpCodes.Pop);
                    return PhpTypeCode.Void;
                }

                return PhpTypeCode.Boolean;
            }

            public override bool IsDeeplyCopied(EmptyEx node, CopyReason reason, int nestingLevel)
            {
                return false;
            }
        }

        #endregion

        #region EvalEx, AssertEx

        [NodeCompiler(typeof(EvalEx), Singleton = true)]
        sealed class EvalExCompiler : ExpressionCompiler<EvalEx>
        {
            #region Analysis

            public override Evaluation Analyze(EvalEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                node.Code = node.Code.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                analyzer.AddCurrentRoutineProperty(RoutineProperties.ContainsEval);

                return new Evaluation(node);
            }

            #endregion

            #region Emission

            public override PhpTypeCode Emit(EvalEx node, CodeGenerator codeGenerator)
            {
                // not emitted in release mode:
                Debug.Assert(access == AccessType.None || access == AccessType.Read || access == AccessType.ReadRef);
                Debug.Assert(codeGenerator.RTVariablesTablePlace != null, "Function should have variables table.");
                Statistics.AST.AddNode("EvalEx");

                PhpTypeCode result = codeGenerator.EmitEval(EvalKinds.ExplicitEval, node.Code, node.Span, null, null);
                
                // handles return value according to the access type:
                codeGenerator.EmitReturnValueHandling(node, false, ref result);
                return result;
            }

            #endregion
        }

        [NodeCompiler(typeof(AssertEx))]
        sealed class AssertExCompiler : ExpressionCompiler<AssertEx>
        {
            /// <summary>
            /// Contains the code string literal that has been inlined.
            /// </summary>
            private string _inlinedCode;

            #region Analysis

            public override Evaluation Analyze(AssertEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                // assertion:
                if (analyzer.Context.Config.Compiler.Debug)
                {
                    Evaluation code_evaluation = node.CodeEx.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
                    //Evaluation desc_evaluation = node.DescriptionEx.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

                    // string parameter is parsed and converted to an expression:
                    if (code_evaluation.HasValue)
                    {
                        _inlinedCode = Convert.ObjectToString(code_evaluation.Value);
                        if (!string.IsNullOrEmpty(_inlinedCode))
                        {
                            const string prefix = "return ";

                            // the position of the last character before the parsed string:
                            var statements = analyzer.BuildAst(node.CodeEx.Span.Start - prefix.Length + 1, String.Concat(prefix, _inlinedCode, ";"));

                            // code is unevaluable:
                            if (statements == null)
                                return new Evaluation(node, true);

                            if (statements.Length > 1)
                                analyzer.ErrorSink.Add(Warnings.MultipleStatementsInAssertion, analyzer.SourceUnit, node.Span);

                            Debug.Assert(statements.Length > 0 && statements[0] is JumpStmt);

                            node.CodeEx = ((JumpStmt)statements[0]).Expression;
                        }
                        else
                        {
                            // empty assertion:
                            return new Evaluation(node, true);
                        }
                    }
                    else
                    {
                        node.CodeEx = code_evaluation.Expression;
                        analyzer.AddCurrentRoutineProperty(RoutineProperties.ContainsEval);
                    }

                    //
                    node.CodeEx = node.CodeEx.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                    
                    return new Evaluation(node);
                }
                else
                {
                    // replace with "true" value in release mode:
                    return new Evaluation(node, true);
                }
            }

            #endregion

            #region Emission

            public override PhpTypeCode Emit(AssertEx node, CodeGenerator codeGenerator)
            {
                // not emitted in release mode:
                Debug.Assert(codeGenerator.Context.Config.Compiler.Debug, "Assert should be cut off in release mode.");
                Debug.Assert(access == AccessType.None || access == AccessType.Read || access == AccessType.ReadRef);
                Debug.Assert(_inlinedCode != null || codeGenerator.RTVariablesTablePlace != null, "Function should have variables table.");
                Statistics.AST.AddNode("AssertEx");

                ILEmitter il = codeGenerator.IL;
                PhpTypeCode result;

                if (_inlinedCode != null)
                {
                    Label endif_label = il.DefineLabel();
                    Label else_label = il.DefineLabel();

                    // IF DynamicCode.PreAssert(context) THEN
                    codeGenerator.EmitLoadScriptContext();
                    il.Emit(OpCodes.Call, Methods.DynamicCode.PreAssert);
                    il.Emit(OpCodes.Brfalse, else_label);
                    if (true)
                    {
                        // LOAD <evaluated assertion>;
                        codeGenerator.EmitBoxing(((Expression)node.CodeEx).Emit(codeGenerator));

                        // CALL DynamicCode.PostAssert(context);
                        codeGenerator.EmitLoadScriptContext();
                        il.Emit(OpCodes.Call, Methods.DynamicCode.PostAssert);

                        // LOAD bool CheckAssertion(STACK, <inlined code>, context, <source path>, line, column);
                        var position = new Text.TextPoint(codeGenerator.SourceUnit.LineBreaks, node.Span.Start);
                        il.Emit(OpCodes.Ldstr, _inlinedCode);
                        codeGenerator.EmitLoadScriptContext();
                        il.Emit(OpCodes.Ldstr, codeGenerator.SourceUnit.SourceFile.RelativePath.ToString());
                        il.LdcI4(position.Line);
                        il.LdcI4(position.Column);
                        codeGenerator.EmitLoadNamingContext();
                        il.Emit(OpCodes.Call, Methods.DynamicCode.CheckAssertion);

                        // GOTO END IF;
                        il.Emit(OpCodes.Br, endif_label);
                    }
                    // ELSE
                    il.MarkLabel(else_label);
                    if (true)
                    {
                        // LOAD true;
                        il.Emit(OpCodes.Ldc_I4_1);
                    }
                    // END IF;
                    il.MarkLabel(endif_label);

                    result = PhpTypeCode.Object;
                }
                else
                {
                    result = codeGenerator.EmitEval(EvalKinds.Assert, node.CodeEx, node.Span, null, null);
                }

                // handles return value according to the access type:
                codeGenerator.EmitReturnValueHandling(node, false, ref result);
                return result;
            }

            #endregion
        }

        #endregion

        #region ExitEx

        [NodeCompiler(typeof(ExitEx))]
        sealed class ExitExCompiler : ExpressionCompiler<ExitEx>
        {
            public override Evaluation Analyze(ExitEx node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                if (node.ResulExpr != null)
                    node.ResulExpr = node.ResulExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                analyzer.EnterUnreachableCode();
                return new Evaluation(node);
            }

            public override PhpTypeCode Emit(ExitEx node, CodeGenerator codeGenerator)
            {
                Debug.Assert(access == AccessType.None || access == AccessType.Read);
                Statistics.AST.AddNode("ExitEx");

                codeGenerator.EmitLoadScriptContext();

                if (node.ResulExpr == null)
                {
                    codeGenerator.IL.Emit(OpCodes.Ldnull);
                }
                else
                {
                    codeGenerator.EmitBoxing(node.ResulExpr.Emit(codeGenerator));
                }
                codeGenerator.IL.Emit(OpCodes.Call, Methods.ScriptContext.Die);

                if (access == AccessType.Read)
                {
                    codeGenerator.IL.Emit(OpCodes.Ldnull);
                    return PhpTypeCode.Object;
                }
                else return PhpTypeCode.Void;
            }
        }

        #endregion
    }

    /// <summary>
    /// IncludingExCompiler members to be accessed by compiler.
    /// </summary>
    internal interface IIncludingExCompiler : INodeCompiler
    {
        StaticInclusion Inclusion { get; set; }
        Characteristic Characteristic { get; set; }
    }
}
