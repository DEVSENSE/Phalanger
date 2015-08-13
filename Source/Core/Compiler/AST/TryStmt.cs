/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
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
        [NodeCompiler(typeof(TryStmt), Singleton = true)]
        sealed class TryStmtCompiler : StatementCompiler<TryStmt>
        {
            internal override Statement Analyze(TryStmt node, Analyzer analyzer)
            {
                // try {}
                analyzer.EnterConditionalCode();
                node.Statements.Analyze(analyzer);
                analyzer.LeaveConditionalCode();

                // catch {}
                if (node.HasCatches)
                {
                    foreach (var c in node.Catches)
                        c.Analyze(analyzer);
                }

                // finally {}
                if (node.HasFinallyStatements)
                {
                    node.FinallyItem.Analyze(analyzer);
                }

                return node;
            }

            /// <summary>
            /// Emits the try block and the catch blocks.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">A code generator.</param>
            /// <remarks>
            /// <code>
            ///	try
            /// {
            ///   // guarded code //
            /// }
            /// catch(E1 $e1)
            /// {
            ///   // E1 //
            /// }
            /// catch(E2 $e2)
            /// {
            ///   // E2 //
            /// } 
            /// </code>
            /// is translated as follows:
            /// <code>
            /// try
            /// {
            ///   // guarded code //
            /// }
            /// catch(PhpUserException _e)
            /// {
            ///   PhpObject _o = _e.UserException;
            ///   if (_o instanceOf E1)
            ///   {
            ///     $e1 = _o;
            ///     // E1 //
            ///   }
            ///   else if (_o instanceOf E2)
            ///   {
            ///     $e2 = _o;
            ///     // E2 //
            ///   }
            ///   else
            ///   {
            ///     throw;
            ///   }
            /// }
            /// </code> 
            /// </remarks>
            internal override void Emit(TryStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("TryStmt");

                // emit try block without CLR exception block if possible

                if (!node.HasCatches && !node.HasFinallyStatements)
                {
                    node.Statements.Emit(codeGenerator);
                    return;
                }

                // emit CLR exception block

                ILEmitter il = codeGenerator.IL;
                codeGenerator.ExceptionBlockNestingLevel++;

                // TRY
                Label end_label = il.BeginExceptionBlock();

                node.Statements.Emit(codeGenerator);

                // catches

                if (node.HasCatches)
                {
                    // catch (PHP.Core.ScriptDiedException)
                    // { throw; }

                    il.BeginCatchBlock(typeof(PHP.Core.ScriptDiedException));
                    il.Emit(OpCodes.Rethrow);

                    // catch (System.Exception ex)

                    il.BeginCatchBlock(typeof(System.Exception));

                    // <exception_local> = (DObject) (STACK is PhpUserException) ? ((PhpUserException)STACK).UserException : ClrObject.WrapRealObject(STACK)

                    Label clrExceptionLabel = il.DefineLabel();
                    Label wrapEndLabel = il.DefineLabel();
                    LocalBuilder exception_local = il.GetTemporaryLocal(typeof(DObject));

                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Isinst, typeof(PHP.Core.PhpUserException)); // <STACK> as PhpUserException
                    il.Emit(OpCodes.Brfalse, clrExceptionLabel);

                    // if (<STACK> as PhpUserException != null)
                    {
                        il.Emit(OpCodes.Ldfld, Fields.PhpUserException_UserException);
                        il.Emit(OpCodes.Br, wrapEndLabel);
                    }

                    // else
                    il.MarkLabel(clrExceptionLabel);
                    {
                        il.Emit(OpCodes.Call, Methods.ClrObject_WrapRealObject);
                    }
                    il.MarkLabel(wrapEndLabel);
                    il.Stloc(exception_local);

                    // emits all PHP catch-blocks processing into a single CLI catch-block:
                    foreach (CatchItem c in node.Catches)
                    {
                        Label next_catch_label = il.DefineLabel();

                        // IF (exception <instanceOf> <type>);
                        c.Emit(codeGenerator, exception_local, end_label, next_catch_label);

                        // ELSE
                        il.MarkLabel(next_catch_label);
                    }

                    il.ReturnTemporaryLocal(exception_local);

                    // emits the "else" branch invoked if the exceptions is not catched:
                    il.Emit(OpCodes.Rethrow);
                }

                // finally

                if (node.HasFinallyStatements)
                {
                    node.FinallyItem.Emit(codeGenerator);
                }

                //
                il.EndExceptionBlock();

                codeGenerator.ExceptionBlockNestingLevel--;
            }
        }

        [NodeCompiler(typeof(CatchItem))]
        sealed class CatchItemCompiler : INodeCompiler, ICatchItemCompiler
        {
            private DType resolvedType;

            public void Analyze(CatchItem/*!*/node, Analyzer/*!*/ analyzer)
            {
                ExInfoFromParent info = new ExInfoFromParent(node);
                info.Access = AccessType.Write;

                TypeRefHelper.Analyze(node.TypeRef, analyzer);
                resolvedType = TypeRefHelper.ResolvedTypeOrUnknown(node.TypeRef);
                //resolvedType = analyzer.ResolveTypeName(node.ClassName, analyzer.CurrentType, analyzer.CurrentRoutine, node.Span, false);

                node.Variable.Analyze(analyzer, info);

                analyzer.EnterConditionalCode();
                node.Statements.Analyze(analyzer);
                analyzer.LeaveConditionalCode();
            }

            /// <summary>
            /// Emits the catch-block.
            /// </summary>
            /// <param name="node">Instance.</param>
            /// <param name="codeGenerator">A code generator.</param>
            /// <param name="exceptionLocal">A local variable containing an instance of <see cref="Library.SPL.Exception"/>.</param>
            /// <param name="endLabel">A label in IL stream where the processing of the try-catch blocks ends.</param>
            /// <param name="nextCatchLabel">A label in IL stream where the next catch block processing begins.</param>
            public void Emit(CatchItem/*!*/node, CodeGenerator/*!*/ codeGenerator,
                LocalBuilder/*!*/ exceptionLocal,
                Label endLabel, Label nextCatchLabel)
            {
                ILEmitter il = codeGenerator.IL;

                codeGenerator.MarkSequencePoint(node.Variable);

                // IF !InstanceOf(<class name>) GOTO next_catch;
                il.Ldloc(exceptionLocal);
                resolvedType.EmitInstanceOf(codeGenerator, null);
                il.Emit(OpCodes.Brfalse, nextCatchLabel);

                // variable = exception;
                node.Variable.Emit(codeGenerator);
                il.Ldloc(exceptionLocal);
                SimpleVarUseHelper.EmitAssign(node.Variable, codeGenerator);

                node.Statements.Emit(codeGenerator);

                // LEAVE end;
                il.Emit(OpCodes.Leave, endLabel);
            }
        }

        [NodeCompiler(typeof(FinallyItem), Singleton = true)]
        sealed class FinallyItemCompiler : INodeCompiler, IFinallyItemCompiler
        {
            private void Analyze(FinallyItem/*!*/node, Analyzer/*!*/ analyzer)
            {
                analyzer.EnterConditionalCode();
                node.Statements.Analyze(analyzer);
                analyzer.LeaveConditionalCode();
            }

            private void Emit(FinallyItem/*!*/node, CodeGenerator codeGenerator)
            {
                codeGenerator.IL.BeginFinallyBlock();
                node.Statements.Emit(codeGenerator);
            }

            #region IFinallyItemCompiler Members

            void IFinallyItemCompiler.Analyze(FinallyItem node, Analyzer analyzer)
            {
                Analyze(node, analyzer);
            }

            void IFinallyItemCompiler.Emit(FinallyItem node, CodeGenerator codeGenerator)
            {
                Emit(node, codeGenerator);
            }

            #endregion
        }

        [NodeCompiler(typeof(ThrowStmt), Singleton = true)]
        sealed class ThrowStmtCompiler : StatementCompiler<ThrowStmt>
        {
            internal override Statement Analyze(ThrowStmt node, Analyzer analyzer)
            {
                node.Expression = node.Expression.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
                return node;
            }

            internal override void Emit(ThrowStmt node, CodeGenerator codeGenerator)
            {
                codeGenerator.MarkSequencePoint(node.Span);

                // CALL Operators.Throw(<context>, <expression>);
                codeGenerator.EmitLoadScriptContext();
                node.Expression.Emit(codeGenerator);
                codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.Throw);
            }
        }
    }

    #region ICatchItemCompiler

    internal interface ICatchItemCompiler
    {
        void Analyze(CatchItem/*!*/node, Analyzer/*!*/ analyzer);
        void Emit(CatchItem/*!*/node, CodeGenerator/*!*/ codeGenerator, LocalBuilder/*!*/ exceptionLocal, Label endLabel, Label nextCatchLabel);
    }

    internal static class CatchItemCompilerHelper
    {
        public static void Analyze(this CatchItem/*!*/node, Analyzer/*!*/ analyzer)
        {
            node.NodeCompiler<ICatchItemCompiler>().Analyze(node, analyzer);
        }
        public static void Emit(this CatchItem/*!*/node, CodeGenerator/*!*/ codeGenerator, LocalBuilder/*!*/ exceptionLocal, Label endLabel, Label nextCatchLabel)
        {
            node.NodeCompiler<ICatchItemCompiler>().Emit(node, codeGenerator, exceptionLocal, endLabel, nextCatchLabel);
        }
    }

    #endregion

    #region IFinallyItemCompiler

    internal interface IFinallyItemCompiler
    {
        void Analyze(FinallyItem/*!*/node, Analyzer/*!*/ analyzer);
        void Emit(FinallyItem/*!*/node, CodeGenerator codeGenerator);
    }

    internal static class FinallyItemCompilerHelper
    {
        public static void Analyze(this FinallyItem/*!*/node, Analyzer/*!*/ analyzer)
        {
            node.NodeCompiler<IFinallyItemCompiler>().Analyze(node, analyzer);
        }
        public static void Emit(this FinallyItem/*!*/node, CodeGenerator codeGenerator)
        {
            node.NodeCompiler<IFinallyItemCompiler>().Emit(node, codeGenerator);
        }
    }

    #endregion

}
