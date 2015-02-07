/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak, and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Parsers;
using PHP.Core.Emit;
using System.Diagnostics;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region IfStmt

        [NodeCompiler(typeof(IfStmt), Singleton = true)]
        sealed class IfStmtCompiler : StatementCompiler<IfStmt>
        {
            internal override Statement Analyze(IfStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Position);
                    return EmptyStmt.Unreachable;
                }

                ExInfoFromParent info = ExInfoFromParent.DefaultExInfo;
                var/*!*/conditions = node.Conditions;

                bool is_first = true;
                int remaining = conditions.Count;
                int last_non_null = -1;

                for (int i = 0; i < conditions.Count; i++)
                {
                    // "else":
                    if (conditions[i].Condition == null)
                    {
                        Debug.Assert(i > 0);

                        if (!is_first) analyzer.EnterConditionalCode();
                        conditions[i].Statement = conditions[i].Statement.Analyze(analyzer);
                        if (!is_first) analyzer.LeaveConditionalCode();
                        last_non_null = i;

                        break;
                    }

                    // all but the condition before the first non-evaluable including are conditional:
                    if (!is_first) analyzer.EnterConditionalCode();
                    Evaluation cond_eval = conditions[i].Condition.Analyze(analyzer, info);
                    if (!is_first) analyzer.LeaveConditionalCode();

                    if (cond_eval.HasValue)
                    {
                        if (Convert.ObjectToBoolean(cond_eval.Value))
                        {
                            // condition is evaluated to be true //

                            // analyze the first statement unconditionally, the the others conditionally:
                            if (!is_first) analyzer.EnterConditionalCode();
                            conditions[i].Statement = conditions[i].Statement.Analyze(analyzer);
                            if (!is_first) analyzer.LeaveConditionalCode();

                            // the remaining conditions are unreachable:
                            for (int j = i + 1; j < conditions.Count; j++)
                            {
                                conditions[j].Statement.ReportUnreachable(analyzer);
                                conditions[j] = null;
                                remaining--;
                            }

                            conditions[i].Condition = null;
                            last_non_null = i;

                            break;
                        }
                        else
                        {
                            // condition is evaluated to be false //

                            // remove the condition, report unreachable code:
                            conditions[i].Statement.ReportUnreachable(analyzer);
                            conditions[i] = null;
                            remaining--;
                        }
                    }
                    else
                    {
                        // condition is not evaluable:
                        conditions[i].Condition = cond_eval.Expression;

                        // analyze statement conditinally:
                        analyzer.EnterConditionalCode();
                        conditions[i].Statement = conditions[i].Statement.Analyze(analyzer);
                        analyzer.LeaveConditionalCode();

                        is_first = false;
                        last_non_null = i;
                    }
                }

                if (remaining == 0)
                    return EmptyStmt.Skipped;

                Debug.Assert(last_non_null != -1 && conditions[last_non_null] != null);

                // only "else" remained:
                if (remaining == 1 && conditions[last_non_null].Condition == null)
                    return conditions[last_non_null].Statement;

                // compact the list (remove nulls):
                if (remaining < conditions.Count)
                {
                    List<ConditionalStmt> compacted = new List<ConditionalStmt>(remaining);
                    foreach (ConditionalStmt condition in conditions)
                    {
                        if (condition != null)
                            compacted.Add(condition);
                    }
                    conditions = compacted;
                }

                return node;
            }

            internal override void Emit(IfStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("IfStmt");
                var/*!*/conditions = node.Conditions;

                Debug.Assert(conditions.Count > 0);

                // marks a sequence point containing whole condition:
                codeGenerator.MarkSequencePoint(conditions[0].Condition);   // NOTE: (J) when emitting a statement, sequence point has to be marked. Normally it is done in Statement.Emit()

                ILEmitter il = codeGenerator.IL;

                Label exit_label = il.DefineLabel();
                Label false_label = il.DefineLabel();

                // IF
                codeGenerator.EmitConversion(conditions[0].Condition, PhpTypeCode.Boolean);

                il.Emit(OpCodes.Brfalse, false_label);
                conditions[0].Statement.Emit(codeGenerator);

                codeGenerator.MarkSequencePoint(    // (J) Mark the end of condition body so debugger will jump off the block properly
                    conditions[0].Statement.Position.LastLine, conditions[0].Statement.Position.LastColumn,
                    conditions[0].Statement.Position.LastLine, conditions[0].Statement.Position.LastColumn + 1);

                il.Emit(OpCodes.Br, exit_label);

                // ELSEIF:
                for (int i = 1; i < conditions.Count && conditions[i].Condition != null; i++)
                {
                    il.MarkLabel(false_label, true);
                    false_label = il.DefineLabel();

                    // IF (!<(bool) condition>)
                    codeGenerator.MarkSequencePoint(conditions[i].Condition);   // marks a sequence point of the condition "statement"
                    codeGenerator.EmitConversion(conditions[i].Condition, PhpTypeCode.Boolean);
                    il.Emit(OpCodes.Brfalse, false_label);

                    conditions[i].Statement.Emit(codeGenerator);
                    il.Emit(OpCodes.Br, exit_label);
                }

                il.MarkLabel(false_label, true);

                // ELSE
                if (conditions[conditions.Count - 1].Condition == null)
                    conditions[conditions.Count - 1].Statement.Emit(codeGenerator);

                il.MarkLabel(exit_label, true);
            }
        }

        #endregion

        #region ConditionalStmt

        [NodeCompiler(typeof(ConditionalStmt), Singleton = true)]
        sealed class ConditionalStmtCompiler : INodeCompiler
        {

        }

        #endregion
    }
}
