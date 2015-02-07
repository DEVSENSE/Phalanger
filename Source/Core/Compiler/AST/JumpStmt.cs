/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Ladislav Prosek, Vaclav Novak, and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections.Generic;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region JumpStmt

        [NodeCompiler(typeof(JumpStmt), Singleton = true)]
        sealed class JumpStmtCompiler : StatementCompiler<JumpStmt>
        {
            internal override Statement Analyze(JumpStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Position);
                    return EmptyStmt.Unreachable;
                }

                if (node.Expression != null)
                {
                    ExInfoFromParent sinfo = ExInfoFromParent.DefaultExInfo;

                    if (node.Type == JumpStmt.Types.Return
                        && analyzer.CurrentRoutine != null && analyzer.CurrentRoutine.Signature.AliasReturn
                        && node.Expression is VarLikeConstructUse)
                    {
                        sinfo.Access = AccessType.ReadRef;
                    }

                    node.Expression = node.Expression.Analyze(analyzer, sinfo).Literalize();

                    if (node.Type != JumpStmt.Types.Return && node.Expression.HasValue())
                    {
                        int level = Convert.ObjectToInteger(node.Expression.GetValue());
                        if (level > analyzer.LoopNestingLevel || level < 0)
                        {
                            analyzer.ErrorSink.Add(Errors.InvalidBreakLevelCount, analyzer.SourceUnit, node.Position, level);
                        }
                    }
                }
                else if (node.Type != JumpStmt.Types.Return && analyzer.LoopNestingLevel == 0)
                {
                    analyzer.ErrorSink.Add(Errors.InvalidBreakLevelCount, analyzer.SourceUnit, node.Position, 1);
                }

                // code in the same block after return, break, continue is unreachable
                analyzer.EnterUnreachableCode();
                return node;
            }

            internal override void Emit(JumpStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("JumpStmt");

                // marks a sequence point:
                codeGenerator.MarkSequencePoint(node.Position);

                switch (node.Type)
                {
                    case JumpStmt.Types.Break:
                        // Emit simple break; - break the most inner loop
                        if (node.Expression == null)
                        {
                            codeGenerator.BranchingStack.EmitBreak();
                        }
                        else if (node.Expression.HasValue())
                        {
                            // We can get the number at compile time and generate the right branch 
                            // instruction for break x; where x is Literal
                            codeGenerator.BranchingStack.EmitBreak(Convert.ObjectToInteger(node.Expression.GetValue()));
                        }
                        else
                        {
                            // In this case we emit the switch that decides where to branch at runtime.
                            codeGenerator.EmitConversion(node.Expression, PhpTypeCode.Integer);
                            codeGenerator.BranchingStack.EmitBreakRuntime();
                        }
                        break;

                    case JumpStmt.Types.Continue:
                        // Emit simple continue; - banch back to the condition of the most inner loop
                        if (node.Expression == null)
                        {
                            codeGenerator.BranchingStack.EmitContinue();
                        }
                        else if (node.Expression.HasValue())
                        {
                            // We can get the number at compile time and generate the right branch 
                            // instruction for continue x; where x is Literal
                            codeGenerator.BranchingStack.EmitContinue(Convert.ObjectToInteger(node.Expression.GetValue()));
                        }
                        else
                        {
                            // In this case we emit the switch that decides where to branch at runtime.
                            codeGenerator.EmitConversion(node.Expression, PhpTypeCode.Integer);
                            codeGenerator.BranchingStack.EmitContinueRuntime();
                        }
                        break;

                    case JumpStmt.Types.Return:
                        if (codeGenerator.ReturnsPhpReference)
                            EmitReturnPhpReference(node.Expression, codeGenerator);
                        else
                            EmitReturnObject(node.Expression, codeGenerator);
                        break;

                    default:
                        throw null;
                }
            }

            /// <summary>
            /// Return value is not deeply copied since the deep copy takes place when the caller accesses the value.
            /// </summary>
            private void EmitReturnObject(Expression expr, CodeGenerator/*!*/ codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                PhpTypeCode result;

                if (expr != null)
                {
                    result = expr.Emit(codeGenerator);

                    // dereference return value:
                    if (result == PhpTypeCode.PhpReference)
                    {
                        il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
                    }
                    else if (result == PhpTypeCode.PhpArray)
                    {
                        // <array>.InplaceCopyOnReturn = true;
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldc_I4_1);
                        il.Emit(OpCodes.Call, Core.Emit.Properties.PhpArray_InplaceCopyOnReturn.GetSetMethod());
                    }
                    else
                    {
                        codeGenerator.EmitBoxing(result);
                    }
                }
                else
                {
                    il.Emit(OpCodes.Ldnull);
                }

                codeGenerator.EmitReturnBranch();
            }

            private void EmitReturnPhpReference(Expression expr, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                PhpTypeCode result;

                if (expr != null)
                {
                    result = expr.Emit(codeGenerator);

                    if (result != PhpTypeCode.PhpReference)
                    {
                        // return value is "boxed" to PhpReference:
                        if (result != PhpTypeCode.Void)
                        {
                            codeGenerator.EmitBoxing(result);

                            // We can box the value without making a copy since the result of the return expression
                            // is not accessible after returnign from the routine as it is a value (not a reference).
                            il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
                        }
                        else
                        {
                            il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
                        }
                    }
                }
                else
                {
                    il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
                }

                codeGenerator.EmitReturnBranch();
            }
        }

        #endregion

        #region GotoStmt

        [NodeCompiler(typeof(GotoStmt), Singleton = true)]
        sealed class GotoStmtCompiler : StatementCompiler<GotoStmt>
        {
            internal override Statement Analyze(GotoStmt node, Analyzer analyzer)
            {
                //
                // TODO: analyze reachability, restrict jumps inside blocks, ...
                //
                // goto x;
                // // unreachable
                // x:
                //

                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Position);
                    return EmptyStmt.Unreachable;
                }

                Dictionary<VariableName, Statement> labels = analyzer.CurrentLabels;

                Statement stmt;
                if (labels.TryGetValue(node.LabelName, out stmt))
                {
                    LabelStmt label = stmt as LabelStmt;
                    if (label != null)
                        label.IsReferred = true;
                }
                else
                {
                    // add a stub (this node):
                    labels.Add(node.LabelName, node);
                }

                return node;
            }

            internal override void Emit(GotoStmt node, CodeGenerator codeGenerator)
            {
                Debug.Assert(codeGenerator.CurrentLabels.ContainsKey(node.LabelName));
                Debug.Assert(codeGenerator.CurrentLabels[node.LabelName] is LabelStmt);

                // marks a sequence point:
                codeGenerator.MarkSequencePoint(node.Position);

                codeGenerator.IL.Emit(OpCodes.Br, ((LabelStmt)codeGenerator.CurrentLabels[node.LabelName]).Label);
            }
        }

        #endregion

        #region LabelStmt

        [NodeCompiler(typeof(LabelStmt), Singleton = true)]
        sealed class LabelStmtCompiler : StatementCompiler<LabelStmt>
        {
            internal override Statement Analyze(LabelStmt node, Analyzer analyzer)
            {
                Dictionary<VariableName, Statement> labels = analyzer.CurrentLabels;

                Statement stmt;
                if (labels.TryGetValue(node.Name, out stmt))
                {
                    if (stmt is LabelStmt)
                    {
                        analyzer.ErrorSink.Add(Errors.LabelRedeclared, analyzer.SourceUnit, node.Position, node.Name);
                        analyzer.ErrorSink.Add(Errors.RelatedLocation, analyzer.SourceUnit, stmt.Position);
                    }
                    else
                    {
                        labels[node.Name] = node;
                        node.IsReferred = true;
                    }
                }
                else
                {
                    labels.Add(node.Name, node);
                }

                return node;
            }

            internal override void Emit(LabelStmt node, CodeGenerator codeGenerator)
            {
                codeGenerator.IL.MarkLabel(node.Label);
            }
        }

        #endregion
    }
}
