/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core.AST;
using PHP.Core.Parsers;
using PHP.Core.Emit;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        [NodeCompiler(typeof(ConditionalEx))]
        sealed class ConditionalExCompiler : ExpressionCompiler<ConditionalEx>
        {
            #region Analysis

            public override Evaluation Analyze(ConditionalEx/*!*/node, Analyzer analyzer, ExInfoFromParent info)
            {
                access = info.Access;

                Evaluation cond_eval = node.CondExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

                if (cond_eval.HasValue)
                {
                    if (Convert.ObjectToBoolean(cond_eval.Value))
                    {
                        if (node.TrueExpr != null)
                            return node.TrueExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
                        else
                            return cond_eval;   // condExpr ?: falseExpr    // ternary shortcut
                    }
                    else
                        return node.FalseExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
                }
                else
                {
                    if (node.TrueExpr != null)
                    {
                        analyzer.EnterConditionalCode();
                        node.TrueExpr = node.TrueExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                        analyzer.LeaveConditionalCode();
                    }

                    analyzer.EnterConditionalCode();
                    node.FalseExpr = node.FalseExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                    analyzer.LeaveConditionalCode();

                    return new Evaluation(node);
                }
            }

            #endregion

            #region Emission

            public override bool IsDeeplyCopied(ConditionalEx node, CopyReason reason, int nestingLevel)
            {
                return
                    (node.TrueExpr ?? node.CondExpr).IsDeeplyCopied(reason, nestingLevel) ||
                    node.FalseExpr.IsDeeplyCopied(reason, nestingLevel);
            }

            public override PhpTypeCode Emit(ConditionalEx node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("TernaryEx");
                Debug.Assert(access == AccessType.Read || access == AccessType.None);

                Label end_label = codeGenerator.IL.DefineLabel();

                if (node.TrueExpr != null)   // standard ternary operator
                {
                    Label else_label = codeGenerator.IL.DefineLabel();

                    // IF (<(bool) condition>) THEN
                    codeGenerator.EmitConversion(node.CondExpr, PhpTypeCode.Boolean);
                    codeGenerator.IL.Emit(OpCodes.Brfalse, else_label);
                    {
                        codeGenerator.EmitBoxing(node.TrueExpr.Emit(codeGenerator));
                        codeGenerator.IL.Emit(OpCodes.Br, end_label);
                    }
                    // ELSE
                    codeGenerator.IL.MarkLabel(else_label, true);
                    {
                        codeGenerator.EmitBoxing(node.FalseExpr.Emit(codeGenerator));
                    }
                }
                else
                {   // ternary shortcut:
                    var il = codeGenerator.IL;

                    // condExpr ?? rightExpr

                    il.EmitBoxing(node.CondExpr.Emit(codeGenerator));

                    // IF (<stack>):
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Call, Methods.Convert.ObjectToBoolean);

                    codeGenerator.IL.Emit(OpCodes.Brtrue, end_label);
                    // ELSE:
                    {
                        il.Emit(OpCodes.Pop);
                        il.EmitBoxing(node.FalseExpr.Emit(codeGenerator));
                    }
                }

                // END IF;
                codeGenerator.IL.MarkLabel(end_label, true);


                if (access == AccessType.None)
                {
                    codeGenerator.IL.Emit(OpCodes.Pop);
                    return PhpTypeCode.Void;
                }

                return PhpTypeCode.Object;
            }

            #endregion
        }
    }
}

