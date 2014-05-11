/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using PHP.Core.Emit;
using System.Diagnostics;

using PHP.Core.AST;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region SwitchStmt

        [NodeCompiler(typeof(SwitchStmt), Singleton = true)]
        sealed class SwitchStmtCompiler : StatementCompiler<SwitchStmt>
        {
            internal override Statement Analyze(SwitchStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                node.SwitchValue = node.SwitchValue.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                analyzer.EnterSwitchBody();

                foreach (SwitchItem item in node.SwitchItems)
                    item.Analyze(analyzer);

                analyzer.LeaveSwitchBody();
                return node;
            }

            internal override void Emit(SwitchStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("SwitchStmt");
                ILEmitter il = codeGenerator.IL;

                // Note: 
                //  SwitchStmt is now implemented in the most general (and unefficient) way. The whole switch
                //  is understood as a series of if-elseif-else statements.

                Label exit_label = il.DefineLabel();
                bool fall_through = false;
                Label fall_through_label = il.DefineLabel();
                Label last_default_label = il.DefineLabel();
                DefaultItem last_default = GetLastDefaultItem(node);
                LocalBuilder branch_to_lastdefault = null;

                if (last_default != null)
                {
                    branch_to_lastdefault = il.DeclareLocal(Types.Bool[0]);
                    il.LdcI4(0);
                    il.Stloc(branch_to_lastdefault);
                }

                codeGenerator.BranchingStack.BeginLoop(exit_label, exit_label,
                  codeGenerator.ExceptionBlockNestingLevel);

                // marks a sequence point containing the discriminator evaluation:
                codeGenerator.MarkSequencePoint(node.SwitchValue.Span);

                // Evaluate condition value and store the result into local variable
                codeGenerator.EmitBoxing(node.SwitchValue.Emit(codeGenerator));
                LocalBuilder condition_value = il.DeclareLocal(Types.Object[0]);
                il.Stloc(condition_value);

                foreach (SwitchItem item in node.SwitchItems)
                {
                    item.MarkSequencePoint(codeGenerator);

                    // switch item is either CaseItem ("case xxx:") or DefaultItem ("default") item:
                    CaseItem case_item = item as CaseItem;
                    if (case_item != null)
                    {
                        Label false_label = il.DefineLabel();

                        // PhpComparer.Default.CompareEq(<switch expr. value>,<case value>);
                        /*changed to static method*/
                        //il.Emit(OpCodes.Ldsfld, Fields.PhpComparer_Default);
                        codeGenerator.EmitCompareEq(
                            cg => { cg.IL.Ldloc(condition_value); return PhpTypeCode.Object; },
                            cg => case_item.CaseVal.Emit(cg));

                        // IF (!STACK) GOTO false_label;
                        il.Emit(OpCodes.Brfalse, false_label);
                        if (fall_through == true)
                        {
                            il.MarkLabel(fall_through_label, true);
                            fall_through = false;
                        }

                        case_item.EmitStatements(codeGenerator);

                        if (fall_through == false)
                        {
                            fall_through_label = il.DefineLabel();
                            fall_through = true;
                        }

                        il.Emit(OpCodes.Br, fall_through_label);

                        il.MarkLabel(false_label, true);
                    }
                    else
                    {
                        DefaultItem default_item = (DefaultItem)item;

                        // Only the last default branch defined in source code is used.
                        // So skip default while testing "case" items at runtime.
                        Label false_label = il.DefineLabel();
                        il.Emit(OpCodes.Br, false_label);

                        if (default_item == last_default)
                        {
                            il.MarkLabel(last_default_label, false);
                        }

                        if (fall_through == true)
                        {
                            il.MarkLabel(fall_through_label, true);
                            fall_through = false;
                        }

                        default_item.EmitStatements(codeGenerator);

                        if (fall_through == false)
                        {
                            fall_through_label = il.DefineLabel();
                            fall_through = true;
                        }

                        il.Emit(OpCodes.Br, fall_through_label);
                        il.MarkLabel(false_label, true);
                    }
                }

                // If no case branch matched, branch to last default case if any is defined
                if (last_default != null)
                {
                    // marks a sequence point containing the condition evaluation or skip of the default case:
                    codeGenerator.MarkSequencePoint(last_default.Span);

                    Debug.Assert(branch_to_lastdefault != null);
                    Label temp = il.DefineLabel();

                    // IF (!branch_to_lastdefault) THEN 
                    il.Ldloc(branch_to_lastdefault);
                    il.LdcI4(0);
                    il.Emit(OpCodes.Bne_Un, temp);
                    if (true)
                    {
                        // branch_to_lastdefault = TRUE; 
                        il.LdcI4(1);
                        il.Stloc(branch_to_lastdefault);

                        // GOTO last_default_label;
                        il.Emit(OpCodes.Br, last_default_label);
                    }
                    il.MarkLabel(temp, true);
                    // END IF;

                    il.ForgetLabel(last_default_label);
                }

                if (fall_through == true)
                {
                    il.MarkLabel(fall_through_label, true);
                }

                il.MarkLabel(exit_label);
                codeGenerator.BranchingStack.EndLoop();
                il.ForgetLabel(exit_label);
            }

            /// <summary>
            /// Determines the last default item in the list of switch items.
            /// </summary>
            /// <returns>The last default item or a <b>null</b> reference if there is no default item.</returns>
            private static DefaultItem GetLastDefaultItem(SwitchStmt/*!*/node)
            {
                DefaultItem result = null;
                foreach (SwitchItem item in node.SwitchItems)
                {
                    DefaultItem di = item as DefaultItem;
                    if (di != null) result = di;
                }
                return result;
            }
        }

        #endregion

        #region SwitchItem

        abstract class SwitchItemCompiler<T> : INodeCompiler, ISwitchItemCompiler where T : SwitchItem
        {
            protected virtual void Analyze(T/*!*/node, Analyzer/*!*/ analyzer)
            {
                analyzer.EnterConditionalCode();

                node.Statements.Analyze(analyzer);

                analyzer.LeaveConditionalCode();
            }

            protected abstract void MarkSequencePoint(T/*!*/node, CodeGenerator/*!*/codeGenerator);

            protected virtual void EmitStatements(T/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                node.Statements.Emit(codeGenerator);
            }

            #region ISwitchItemCompiler Members

            void ISwitchItemCompiler.Analyze(SwitchItem node, Analyzer analyzer)
            {
                Analyze((T)node, analyzer);
            }

            void ISwitchItemCompiler.MarkSequencePoint(SwitchItem node, CodeGenerator codeGenerator)
            {
                MarkSequencePoint((T)node, codeGenerator);
            }

            void ISwitchItemCompiler.EmitStatements(SwitchItem node, CodeGenerator codeGenerator)
            {
                EmitStatements((T)node, codeGenerator);
            }

            #endregion
        }

        [NodeCompiler(typeof(CaseItem), Singleton = true)]
        sealed class CaseItemCompiler : SwitchItemCompiler<CaseItem>
        {
            protected override void Analyze(CaseItem/*!*/node, Analyzer analyzer)
            {
                node.CaseVal = node.CaseVal.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();

                if (node.CaseVal.HasValue())
                    analyzer.AddConstCaseToCurrentSwitch(node.CaseVal.GetValue(), node.Span);

                base.Analyze(node, analyzer);
            }

            /// <summary>
            /// Marks a sequence point "case {caseVal}".
            /// </summary>
            protected override void MarkSequencePoint(CaseItem/*!*/node, CodeGenerator codeGenerator)
            {
                codeGenerator.MarkSequencePoint(node.Span);
            }

            protected override void EmitStatements(CaseItem/*!*/node, CodeGenerator codeGenerator)
            {
                base.EmitStatements(node, codeGenerator);
            }
        }

        [NodeCompiler(typeof(DefaultItem), Singleton = true)]
        sealed class DefaultItemCompiler : SwitchItemCompiler<DefaultItem>
        {
            protected override void Analyze(DefaultItem/*!*/node, Analyzer analyzer)
            {
                analyzer.AddDefaultToCurrentSwitch(node.Span);
                base.Analyze(node, analyzer);
            }

            protected override void MarkSequencePoint(DefaultItem/*!*/node, CodeGenerator codeGenerator)
            {
                codeGenerator.MarkSequencePoint(node.Span);
            }

            protected override void EmitStatements(DefaultItem/*!*/node, CodeGenerator/*!*/ codeGenerator)
            {
                base.EmitStatements(node, codeGenerator);
            }
        }

        #endregion
    }

    #region ISwitchItemCompiler

    internal interface ISwitchItemCompiler
    {
        void Analyze(SwitchItem/*!*/node, Analyzer/*!*/ analyzer);

        void MarkSequencePoint(SwitchItem/*!*/node, CodeGenerator/*!*/codeGenerator);

        void EmitStatements(SwitchItem/*!*/node, CodeGenerator/*!*/ codeGenerator);
    }

    internal static class SwitchItemCompilerHelper
    {
        public static void Analyze(this SwitchItem/*!*/node, Analyzer/*!*/ analyzer)
        {
            node.NodeCompiler<ISwitchItemCompiler>().Analyze(node, analyzer);
        }

        public static void MarkSequencePoint(this SwitchItem/*!*/node, CodeGenerator/*!*/codeGenerator)
        {
            node.NodeCompiler<ISwitchItemCompiler>().MarkSequencePoint(node, codeGenerator);
        }

        public static void EmitStatements(this SwitchItem/*!*/node, CodeGenerator/*!*/ codeGenerator)
        {
            node.NodeCompiler<ISwitchItemCompiler>().EmitStatements(node, codeGenerator);
        }
    }

    #endregion
}
