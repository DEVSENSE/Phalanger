/*

 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region WhileStmt

        [NodeCompiler(typeof(WhileStmt), Singleton = true)]
        sealed class WhileStmtCompiler : StatementCompiler<WhileStmt>
        {
            internal override Statement Analyze(WhileStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                Evaluation cond_eval = node.CondExpr.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

                if (cond_eval.HasValue)
                {
                    if (Convert.ObjectToBoolean(cond_eval.Value))
                    {
                        // unbounded loop:
                        node.CondExpr = null;
                    }
                    else
                    {
                        // unreachable body:
                        if (node.LoopType == WhileStmt.Type.While)
                        {
                            node.Body.ReportUnreachable(analyzer);
                            return EmptyStmt.Unreachable;
                        }
                    }
                }

                node.CondExpr = cond_eval.Literalize();

                analyzer.EnterLoopBody();
                node.Body = node.Body.Analyze(analyzer);
                analyzer.LeaveLoopBody();

                return node;
            }

            internal override void Emit(WhileStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("Loop.While");

                ILEmitter il = codeGenerator.IL;
                Label cond_label = il.DefineLabel();
                Label exit_label = il.DefineLabel();
                Label stat_label = il.DefineLabel();

                codeGenerator.BranchingStack.BeginLoop(cond_label, exit_label, codeGenerator.ExceptionBlockNestingLevel);

                if (node.LoopType == WhileStmt.Type.While)
                {
                    il.Emit(OpCodes.Br, cond_label);
                }

                // body:
                il.MarkLabel(stat_label);
                node.Body.Emit(codeGenerator);

                // marks a sequence point containing condition:
                codeGenerator.MarkSequencePoint(node.CondExpr);

                // condition:
                il.MarkLabel(cond_label);

                // bounded loop:
                if (node.CondExpr != null)
                {
                    // IF (<(bool) condition>) GOTO stat;
                    codeGenerator.EmitConversion(node.CondExpr, PhpTypeCode.Boolean);
                    il.Emit(OpCodes.Brtrue, stat_label);
                }

                il.MarkLabel(exit_label);
                codeGenerator.BranchingStack.EndLoop();

                il.ForgetLabel(cond_label);
                il.ForgetLabel(exit_label);
                il.ForgetLabel(stat_label);
            }
        }

        #endregion

        #region ForStmt

        [NodeCompiler(typeof(ForStmt), Singleton = true)]
        sealed class ForStmtCompiler : StatementCompiler<ForStmt>
        {
            internal override Statement Analyze(ForStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                ExInfoFromParent info = new ExInfoFromParent(node);

                info.Access = AccessType.None;

                var initExList = node.InitExList;
                for (int i = 0; i < initExList.Count; i++)
                {
                    initExList[i] = initExList[i].Analyze(analyzer, info).Literalize();
                }

                var condExList = node.CondExList;
                if (condExList.Count > 0)
                {
                    // all but the last expression is evaluated and the result is ignored (AccessType.None), 
                    // the last is read:

                    for (int i = 0; i < condExList.Count - 1; i++)
                    {
                        condExList[i] = condExList[i].Analyze(analyzer, info).Literalize();
                    }

                    condExList[condExList.Count - 1] = condExList[condExList.Count - 1].Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                }

                var actionExList = node.ActionExList;
                for (int i = 0; i < actionExList.Count; i++)
                {
                    actionExList[i] = actionExList[i].Analyze(analyzer, info).Literalize();
                }

                analyzer.EnterLoopBody();
                node.Body = node.Body.Analyze(analyzer);
                analyzer.LeaveLoopBody();

                return node;
            }

            internal override void Emit(ForStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("Loop.For");

                // Template: 
                // we expand the for-statement
                //		for (<expr1>; <expr2>; <expr3>) <loop body>
                // in the while form
                //		{
                //			<expr1>;
                //			while (<expr2>) {
                //				<loop body>;
                //				<expr 3>;
                //			}
                //		}	

                Label cond_label = codeGenerator.IL.DefineLabel();
                Label iterate_label = codeGenerator.IL.DefineLabel();
                Label exit_label = codeGenerator.IL.DefineLabel();
                Label stat_label = codeGenerator.IL.DefineLabel();

                codeGenerator.BranchingStack.BeginLoop(
                    iterate_label, exit_label, codeGenerator.ExceptionBlockNestingLevel);

                // marks a sequence point containing initialization statements (if any):
                codeGenerator.MarkSequencePoint(node.InitExList);

                // Emit <expr1>
                foreach (Expression expr in node.InitExList)
                    expr.Emit(codeGenerator);

                // Branch unconditionally to the begin of condition evaluation
                codeGenerator.IL.Emit(OpCodes.Br, cond_label);

                // Emit loop body
                codeGenerator.IL.MarkLabel(stat_label);
                node.Body.Emit(codeGenerator);
                codeGenerator.IL.MarkLabel(iterate_label);

                // marks a sequence point containing action statements (if any):
                codeGenerator.MarkSequencePoint(node.ActionExList);
                
                // Emit <expr3>
                foreach (Expression expr in node.ActionExList)
                    expr.Emit(codeGenerator);

                // marks a sequence point containing condition (if any):
                codeGenerator.MarkSequencePoint(node.CondExList);

                // Emit <expr2>
                codeGenerator.IL.MarkLabel(cond_label);
                if (node.CondExList.Count > 0)
                {
                    for (int i = 0; i < (node.CondExList.Count - 1); i++)
                        node.CondExList[i].Emit(codeGenerator);

                    // LOAD <(bool) condition>
                    codeGenerator.EmitConversion(node.CondExList[node.CondExList.Count - 1], PhpTypeCode.Boolean);
                }
                else
                    codeGenerator.IL.LdcI4(1);

                codeGenerator.IL.Emit(OpCodes.Brtrue, stat_label);

                codeGenerator.IL.MarkLabel(exit_label);
                codeGenerator.BranchingStack.EndLoop();

                codeGenerator.IL.ForgetLabel(cond_label);
                codeGenerator.IL.ForgetLabel(iterate_label);
                codeGenerator.IL.ForgetLabel(exit_label);
                codeGenerator.IL.ForgetLabel(stat_label);
            }
        }
        #endregion

        #region ForeachStmt

        [NodeCompiler(typeof(ForeachVar), Singleton = true)]
        sealed class ForeachVarCompiler : INodeCompiler, IForeachVarCompiler
        {
            public void Analyze(ForeachVar/*!*/node, Analyzer analyzer)
            {
                ExInfoFromParent info = new ExInfoFromParent(node);
                if (node.Alias) info.Access = AccessType.WriteRef;
                else info.Access = AccessType.Write;

                // retval not needed
                node.Expression.Analyze(analyzer, info);
            }

            public PhpTypeCode Emit(ForeachVar/*!*/node, CodeGenerator codeGenerator)
            {
                var varuse = node.Variable;
                if (varuse != null)
                {
                    return varuse.Emit(codeGenerator);
                }
                else
                {
                    // other epxressions are handled in EmitAssign only
                    return PhpTypeCode.Unknown; // ignored
                }
            }

            public PhpTypeCode EmitAssign(ForeachVar/*!*/node, CodeGenerator codeGenerator)
            {
                // Object (or PhpReference) is on top of evaluation stack

                var varuse = node.Variable;
                if (varuse != null)
                {
                    return VariableUseHelper.EmitAssign(varuse, codeGenerator);
                }
                else
                {
                    var listex = node.List;
                    if (listex != null)
                    {
                        return listex.NodeCompiler<ListExCompiler>().EmitAssign(listex, codeGenerator);
                    }
                    else
                    {
                        throw new InvalidOperationException();
                    }
                }
            }

        }

        [NodeCompiler(typeof(ForeachStmt), Singleton = true)]
        sealed class ForeachStmtCompiler : StatementCompiler<ForeachStmt>
        {
            internal override Statement Analyze(ForeachStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                //next version: array.SetSeqPoint();
                node.Enumeree.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);
                if (node.KeyVariable != null) node.KeyVariable.Analyze(analyzer);
                node.ValueVariable.Analyze(analyzer);

                analyzer.EnterLoopBody();
                node.Body = node.Body.Analyze(analyzer);
                analyzer.LeaveLoopBody();
                return node;
            }

            /// <author>Tomas Matousek</author>
            /// <remarks>
            /// Emits the following code:
            /// <code>
            /// IPhpEnumerable enumerable = ARRAY as IPhpEnumerable;
            /// if (enumerable==null)
            /// {
            ///   PhpException.InvalidForeachArgument();
            /// }
            /// else
            /// FOREACH_BEGIN:
            /// {
            ///   IDictionaryEnumerator enumerator = enumerable.GetForeachEnumerator(KEYED,ALIASED,TYPE_HANDLE);
            ///    
            ///   goto LOOP_TEST;
            ///   LOOP_BEGIN:
            ///   {
            ///     ASSIGN(value,enumerator.Value);
            ///     ASSIGN(key,enumerator.Key);
            ///     
            ///     BODY; 
            ///   }
            ///   LOOP_TEST:
            ///   if (enumerator.MoveNext()) goto LOOP_BEGIN;
            /// } 
            /// FOREACH_END:
            /// </code>
            /// </remarks>
            internal override void Emit(ForeachStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("Loop.Foreach");
                ILEmitter il = codeGenerator.IL;

                Label foreach_end = il.DefineLabel();
                Label foreach_begin = il.DefineLabel();
                Label loop_begin = il.DefineLabel();
                Label loop_test = il.DefineLabel();

                codeGenerator.BranchingStack.BeginLoop(loop_test, foreach_end,
                  codeGenerator.ExceptionBlockNestingLevel);

                LocalBuilder enumerable = il.GetTemporaryLocal(typeof(IPhpEnumerable));

                // marks foreach "header" (the first part of the IL code):
                MarkSequencePointHeader(node, codeGenerator);

                // enumerable = array as IPhpEnumerable;
                node.Enumeree.Emit(codeGenerator);
                il.Emit(OpCodes.Isinst, typeof(IPhpEnumerable));
                il.Stloc(enumerable);

                // if (enumerable==null)
                il.Ldloc(enumerable);
                il.Emit(OpCodes.Brtrue, foreach_begin);
                {
                    // CALL PhpException.InvalidForeachArgument();
                    codeGenerator.EmitPhpException(Methods.PhpException.InvalidForeachArgument);
                    il.Emit(OpCodes.Br, foreach_end);
                }
                // FOREACH_BEGIN:
                il.MarkLabel(foreach_begin);
                {
                    LocalBuilder enumerator = il.GetTemporaryLocal(typeof(System.Collections.IDictionaryEnumerator));

                    // enumerator = enumerable.GetForeachEnumerator(KEYED,ALIASED,TYPE_HANDLE);
                    il.Ldloc(enumerable);
                    il.LoadBool(node.KeyVariable != null);
                    il.LoadBool(node.ValueVariable.Alias);
                    codeGenerator.EmitLoadClassContext();
                    il.Emit(OpCodes.Callvirt, Methods.IPhpEnumerable_GetForeachEnumerator);
                    il.Stloc(enumerator);

                    // goto LOOP_TEST;
                    il.Emit(OpCodes.Br, loop_test);

                    // LOOP_BEGIN:
                    il.MarkLabel(loop_begin);
                    {
                        // enumerator should do dereferencing and deep copying (if applicable):
                        // ASSIGN(value,enumerator.Value);
                        node.ValueVariable.Emit(codeGenerator);
                        il.Ldloc(enumerator);
                        il.Emit(OpCodes.Callvirt, Core.Emit.Properties.IDictionaryEnumerator_Value.GetGetMethod());
                        if (node.ValueVariable.Alias) il.Emit(OpCodes.Castclass, typeof(PhpReference));
                        node.ValueVariable.EmitAssign(codeGenerator);

                        if (node.KeyVariable != null)
                        {
                            // enumerator should do dereferencing and deep copying (if applicable):
                            // ASSIGN(key,enumerator.Key);
                            node.KeyVariable.Emit(codeGenerator);
                            il.Ldloc(enumerator);
                            il.Emit(OpCodes.Callvirt, Core.Emit.Properties.IDictionaryEnumerator_Key.GetGetMethod());
                            node.KeyVariable.EmitAssign(codeGenerator);
                        }

                        // BODY:
                        node.Body.Emit(codeGenerator);
                    }
                    // LOOP_TEST:
                    il.MarkLabel(loop_test);

                    // marks foreach "header" (the second part of the code):
                    MarkSequencePointHeader(node, codeGenerator);

                    // if (enumerator.MoveNext()) goto LOOP_BEGIN;
                    il.Ldloc(enumerator);
                    il.Emit(OpCodes.Callvirt, Methods.IEnumerator_MoveNext);
                    il.Emit(OpCodes.Brtrue, loop_begin);

                    //
                    il.ReturnTemporaryLocal(enumerator);
                }
                // FOREACH_END:
                il.MarkLabel(foreach_end);

                il.ReturnTemporaryLocal(enumerable);

                codeGenerator.BranchingStack.EndLoop();

                il.ForgetLabel(foreach_end);
                il.ForgetLabel(foreach_begin);
                il.ForgetLabel(loop_begin);
                il.ForgetLabel(loop_test);
            }

            /// <summary>
            /// marks foreach "header"
            /// </summary>
            private static void MarkSequencePointHeader(ForeachStmt node, CodeGenerator codeGenerator)
            {
                codeGenerator.MarkSequencePoint(
                    Text.Span.FromBounds(node.Enumeree.Span.Start, node.ValueVariable.Span.End));
            }
        }

        #endregion
    }

    #region IForeachVarCompiler

    internal interface IForeachVarCompiler
    {
        void Analyze(ForeachVar/*!*/node, Analyzer analyzer);
        PhpTypeCode Emit(ForeachVar/*!*/node, CodeGenerator codeGenerator);
        PhpTypeCode EmitAssign(ForeachVar/*!*/node, CodeGenerator codeGenerator);
    }

    internal static class ForeachVarCompilerHelper
    {
        public static void Analyze(this ForeachVar/*!*/node, Analyzer analyzer)
        {
            node.NodeCompiler<IForeachVarCompiler>().Analyze(node, analyzer);
        }
        public static PhpTypeCode Emit(this ForeachVar/*!*/node, CodeGenerator codeGenerator)
        {
            return node.NodeCompiler<IForeachVarCompiler>().Emit(node, codeGenerator);
        }
        public static PhpTypeCode EmitAssign(this ForeachVar/*!*/node, CodeGenerator codeGenerator)
        {
            return node.NodeCompiler<IForeachVarCompiler>().EmitAssign(node, codeGenerator);
        }
    }

    #endregion
}
