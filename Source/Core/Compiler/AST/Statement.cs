/*

 Copyright (c) 2006- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek, Vaclav Novak and Martin Maly.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Diagnostics;

using PHP.Core.AST;
using PHP.Core.Emit;
using PHP.Core.Parsers;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region Statement

        [NodeCompiler(typeof(Statement))]
        abstract class StatementCompiler<T> : IStatementCompiler, INodeCompiler where T : Statement
        {
            /// <summary>
            /// Analyzes an AST node containing a specialization of a statement.
            /// </summary>
            internal abstract Statement/*!*/ Analyze(T/*!*/node, Analyzer/*!*/ analyzer);

            /// <summary>
            /// Emits AST node respective IL code.
            /// </summary>
            internal abstract void Emit(T/*!*/node, CodeGenerator/*!*/ codeGenerator);

            /// <summary>
            /// Reports the statement unreachability. 
            /// The block statement reports the position of its first statement.
            /// </summary>
            protected virtual void ReportUnreachable(T/*!*/node, Analyzer/*!*/ analyzer)
            {
                analyzer.ErrorSink.Add(Warnings.UnreachableCodeDetected, analyzer.SourceUnit, node.Span);
            }

            #region IStatementCompiler Members

            Statement IStatementCompiler.Analyze(Statement node, Analyzer analyzer)
            {
                return this.Analyze((T)node, analyzer);
            }

            void IStatementCompiler.Emit(Statement node, CodeGenerator codeGenerator)
            {
                this.Emit((T)node, codeGenerator);
            }

            void IStatementCompiler.ReportUnreachable(Statement/*!*/node, Analyzer/*!*/ analyzer)
            {
                this.ReportUnreachable((T)node, analyzer);
            }

            #endregion
        }

        #endregion

        #region BlockStmt

        [NodeCompiler(typeof(BlockStmt), Singleton = true)]
        sealed class BlockStmtCompiler : StatementCompiler<BlockStmt>
        {
            internal override Statement Analyze(BlockStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                node.Statements.Analyze(analyzer);
                return node;
            }

            protected override void ReportUnreachable(BlockStmt node, Analyzer analyzer)
            {
                if (node.Statements.Any())
                    node.Statements[0].ReportUnreachable(analyzer);
                else
                    base.ReportUnreachable(node, analyzer);
            }

            internal override void Emit(BlockStmt node, CodeGenerator codeGenerator)
            {
                node.Statements.Emit(codeGenerator);
            }
        }

        #endregion

        #region ExpressionStmt

        [NodeCompiler(typeof(ExpressionStmt), Singleton = true)]
        sealed class ExpressionStmtCompiler : StatementCompiler<ExpressionStmt>
        {
            internal override Statement Analyze(ExpressionStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                ExInfoFromParent info = new ExInfoFromParent(node);
                info.Access = AccessType.None;

                Evaluation expr_eval = node.Expression.Analyze(analyzer, info);

                // skip statement if it is evaluable (has no side-effects):
                if (expr_eval.HasValue)
                    return EmptyStmt.Skipped;

                node.Expression = expr_eval.Expression;
                return node;
            }

            internal override void Emit(ExpressionStmt node, CodeGenerator codeGenerator)
            {
                if (node.Expression.DoMarkSequencePoint)
                    codeGenerator.MarkSequencePoint(node.Span);

                try
                {
                    // emit the expression
                    node.Expression.Emit(codeGenerator);
                }
                catch (CompilerException ex)
                {
                    // put the error into the error sink,
                    // so the user can see, which expression is problematic (work item 20695)
                    codeGenerator.Context.Errors.Add(
                        ex.ErrorInfo,
                        codeGenerator.SourceUnit,
                        node.Span,   // exact position of the statement
                        ex.ErrorParams
                        );

                    // terminate the emit with standard Exception
                    throw new Exception(CoreResources.GetString(ex.ErrorInfo.MessageId, ex.ErrorParams));
                }
            }
        }

        #endregion

        #region EmptyStmt

        [NodeCompiler(typeof(EmptyStmt), Singleton = true)]
        sealed class EmptyStmtCompiler : StatementCompiler<EmptyStmt>
        {
            internal override Statement Analyze(EmptyStmt node, Analyzer analyzer)
            {
                return node;
            }

            internal override void Emit(EmptyStmt node, CodeGenerator codeGenerator)
            {
                codeGenerator.MarkSequencePoint(node.Span);
            }
        }

        #endregion

        #region PHPDocStmt

        [NodeCompiler(typeof(PHPDocStmt), Singleton = true)]
        sealed class PHPDocStmtCompiler : StatementCompiler<PHPDocStmt>
        {
            internal override Statement Analyze(PHPDocStmt node, Analyzer analyzer)
            {
                return node;
            }

            internal override void Emit(PHPDocStmt node, CodeGenerator codeGenerator)
            {   
            }
        }

        #endregion

        #region UnsetStmt

        [NodeCompiler(typeof(UnsetStmt), Singleton = true)]
        sealed class UnsetStmtCompiler : StatementCompiler<UnsetStmt>
        {
            internal override Statement Analyze(UnsetStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                //retval not needed, VariableUse analyzis always returns the same instance
                //Access really shall by Read
                foreach (VariableUse vu in node.VarList)
                    vu.Analyze(analyzer, ExInfoFromParent.DefaultExInfo);

                return node;
            }

            internal override void Emit(UnsetStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("UnsetStmt");

                codeGenerator.MarkSequencePoint(node.Span);

                foreach (VariableUse variable in node.VarList)
                {
                    codeGenerator.ChainBuilder.Create();
                    codeGenerator.ChainBuilder.QuietRead = true;
                    VariableUseHelper.EmitUnset(variable, codeGenerator);
                    codeGenerator.ChainBuilder.End();
                }
            }
        }

        #endregion

        #region GlobalStmt

        [NodeCompiler(typeof(GlobalStmt), Singleton = true)]
        sealed class GlobalStmtCompiler : StatementCompiler<GlobalStmt>
        {
            internal override Statement Analyze(GlobalStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                ExInfoFromParent info = new ExInfoFromParent(node);
                info.Access = AccessType.WriteRef;
                foreach (SimpleVarUse svu in node.VarList)
                    svu.Analyze(analyzer, info);

                return node;
            }

            internal override void Emit(GlobalStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("GlobalStmt");

                foreach (SimpleVarUse variable in node.VarList)
                {
                    variable.Emit(codeGenerator);

                    // CALL Operators.GetItemRef(<string variable name>, ref context.AutoGlobals.GLOBALS);
                    SimpleVarUseHelper.EmitName(variable, codeGenerator);
                    codeGenerator.EmitAutoGlobalLoadAddress(new VariableName(VariableName.GlobalsName));
                    codeGenerator.IL.Emit(OpCodes.Call, Methods.Operators.GetItemRef.String);

                    SimpleVarUseHelper.EmitAssign(variable, codeGenerator);
                }
            }
        }

        #endregion

        #region StaticStmt

        [NodeCompiler(typeof(StaticStmt), Singleton = true)]
        sealed class StaticStmtCompiler : StatementCompiler<StaticStmt>
        {
            internal override Statement Analyze(StaticStmt node, Analyzer analyzer)
            {
                if (analyzer.IsThisCodeUnreachable())
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }

                foreach (StaticVarDecl svd in node.StVarList)
                    StaticVarDeclCompilerHelper.Analyze(svd, analyzer);

                return node;
            }

            internal override void Emit(StaticStmt node, CodeGenerator codeGenerator)
            {
                Statistics.AST.AddNode("StaticStmt");
                foreach (StaticVarDecl svd in node.StVarList)
                    StaticVarDeclCompilerHelper.Emit(svd, codeGenerator);
            }
        }

        #endregion

        #region StaticVarDecl

        [NodeCompiler(typeof(StaticVarDecl), Singleton = true)]
        sealed class StaticVarDeclCompiler : INodeCompiler
        {
            public void Analyze(StaticVarDecl/*!*/node, Analyzer analyzer)
            {
                ExInfoFromParent sinfo = new ExInfoFromParent(node);
                sinfo.Access = AccessType.WriteRef;

                node.Variable.Analyze(analyzer, sinfo);

                if (node.Initializer != null)
                    node.Initializer = node.Initializer.Analyze(analyzer, ExInfoFromParent.DefaultExInfo).Literalize();
            }

            public void Emit(StaticVarDecl/*!*/node, CodeGenerator codeGenerator)
            {
                ILEmitter il = codeGenerator.IL;
                string id = codeGenerator.GetLocationId();

                if (id == null)
                {
                    // we are in global code -> just assign the iniVal to the variable
                    node.Variable.Emit(codeGenerator);

                    if (node.Initializer != null)
                    {
                        codeGenerator.EmitBoxing(node.Initializer.Emit(codeGenerator));
                        il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);
                    }
                    else il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);

                    // continue ...
                }
                else
                {
                    // cache the integer index of static local variable to access its value fast from within the array

                    // unique static local variable string ID
                    id = String.Format("{0}${1}${2}", id, node.Variable.VarName, node.Span.Start);

                    // create static field for static local index: private static int <id>;
                    var type = codeGenerator.IL.TypeBuilder;
                    Debug.Assert(type != null, "The method does not have declaring type! (global code in pure mode?)");
                    var field_id = type.DefineField(id, Types.Int[0], System.Reflection.FieldAttributes.Private | System.Reflection.FieldAttributes.Static);

                    // we are in a function or method -> try to retrieve the local value from ScriptContext
                    node.Variable.Emit(codeGenerator);

                    // <context>.GetStaticLocal( <field> )
                    codeGenerator.EmitLoadScriptContext();  // <context>
                    il.Emit(OpCodes.Ldsfld, field_id);         // <field>
                    il.Emit(OpCodes.Callvirt, Methods.ScriptContext.GetStaticLocal);    // GetStaticLocal
                    il.Emit(OpCodes.Dup);

                    // ?? <context>.AddStaticLocal( <field> != 0 ? <field> : ( <field> = ScriptContext.GetStaticLocalId(<id>) ), <initializer> )
                    if (true)
                    {
                        // if (GetStaticLocal(<field>) == null)
                        Label local_initialized = il.DefineLabel();
                        il.Emit(OpCodes.Brtrue/*not .S, initializer can emit really long code*/, local_initialized);

                        il.Emit(OpCodes.Pop);

                        // <field> != 0 ? <field> : ( <field> = ScriptContext.GetStaticLocalId(<id>) )
                        il.Emit(OpCodes.Ldsfld, field_id);         // <field>

                        if (true)
                        {
                            // if (<field> == 0)
                            Label id_initialized = il.DefineLabel();
                            il.Emit(OpCodes.Brtrue_S, id_initialized);

                            // <field> = GetStaticLocalId( <id> )
                            il.Emit(OpCodes.Ldstr, id);
                            il.Emit(OpCodes.Call, Methods.ScriptContext.GetStaticLocalId);
                            il.Emit(OpCodes.Stsfld, field_id);

                            il.MarkLabel(id_initialized);
                        }

                        // <context>.AddStaticLocal(<field>,<initialize>)
                        codeGenerator.EmitLoadScriptContext();  // <context>
                        il.Emit(OpCodes.Ldsfld, field_id);         // <field>
                        if (node.Initializer != null) codeGenerator.EmitBoxing(node.Initializer.Emit(codeGenerator)); // <initializer>
                        else il.Emit(OpCodes.Ldnull);
                        il.Emit(OpCodes.Callvirt, Methods.ScriptContext.AddStaticLocal);    // AddStaticLocal

                        // 
                        il.MarkLabel(local_initialized);
                    }

                    // continue ...
                }

                // stores value from top of the stack to the variable:
                SimpleVarUseHelper.EmitAssign(node.Variable, codeGenerator);
            }
        }

        static class StaticVarDeclCompilerHelper
        {
            public static void Analyze(StaticVarDecl/*!*/node, Analyzer analyzer)
            {
                node.NodeCompiler<StaticVarDeclCompiler>().Analyze(node, analyzer);
            }

            public static void Emit(StaticVarDecl/*!*/node, CodeGenerator codeGenerator)
            {
                node.NodeCompiler<StaticVarDeclCompiler>().Emit(node, codeGenerator);
            }
        }

        #endregion

        #region DeclareStmt

        [NodeCompiler(typeof(DeclareStmt), Singleton = true)]
        sealed class DeclareStmtCompiler : StatementCompiler<DeclareStmt>
        {
            internal override Statement Analyze(DeclareStmt node, Analyzer analyzer)
            {
                analyzer.ErrorSink.Add(Warnings.NotSupportedFunctionCalled, analyzer.SourceUnit, node.Span, "declare");
                node.Statement.Analyze(analyzer);
                return node;
            }

            protected override void ReportUnreachable(DeclareStmt node, Analyzer analyzer)
            {
                node.Statement.ReportUnreachable(analyzer);
            }

            internal override void Emit(DeclareStmt node, CodeGenerator codeGenerator)
            {
                node.Statement.Emit(codeGenerator);
            }
        }

        #endregion
    }

    #region StatementUtils

    internal static class StatementUtils
    {
        /// <summary>
        /// Analyze all the <see cref="Statement"/> objects in the <paramref name="statements"/> list.
        /// This methods replaces items in the original list if <see cref="IStatementCompiler.Analyze"/> returns a different instance.
        /// </summary>
        /// <param name="statements">List of statements to be analyzed.</param>
        /// <param name="analyzer">Current <see cref="Analyzer"/>.</param>
        public static void Analyze(this IList<Statement>/*!*/statements, Analyzer/*!*/ analyzer)
        {
            Debug.Assert(statements != null);
            Debug.Assert(analyzer != null);

            // analyze statements:
            for (int i = 0; i < statements.Count; i++)
            {
                // analyze the statement
                var statement = statements[i];
                var analyzed = statement.Analyze(analyzer);

                // update the statement in the list
                if (!object.ReferenceEquals(statement, analyzer))
                    statements[i] = analyzed;
            }
        }

        public static Statement Analyze(this Statement/*!*/statement, Analyzer/*!*/ analyzer)
        {
            return statement.NodeCompiler<IStatementCompiler>().Analyze(statement, analyzer);
        }

        /// <summary>
        /// Emits each <see cref="Statement"/> in given <paramref name="statements"/> list.
        /// </summary>
        public static void Emit(this IEnumerable<Statement> statements, CodeGenerator codeGenerator)
        {
            if (statements != null)
            {
                foreach (Statement statement in statements)
                    statement.Emit(codeGenerator);
            }
        }

        public static void Emit(this Statement/*!*/statement, CodeGenerator codeGenerator)
        {
            statement.NodeCompiler<IStatementCompiler>().Emit(statement, codeGenerator);
        }

        public static void ReportUnreachable(this Statement/*!*/statement, Analyzer/*!*/ analyzer)
        {
            statement.NodeCompiler<IStatementCompiler>().ReportUnreachable(statement, analyzer);
        }
    }

    #endregion

    #region IStatementCompiler

    /// <summary>
    /// Base interface for <see cref="Statement"/> compiler implementation.
    /// </summary>
    internal interface IStatementCompiler
    {
        /// <summary>
        /// Analyzes an AST node containing a specialization of a statement.
        /// </summary>
        Statement/*!*/ Analyze(Statement/*!*/node, Analyzer/*!*/ analyzer);

        /// <summary>
        /// Emits AST node respective IL code.
        /// </summary>
        void Emit(Statement/*!*/node, CodeGenerator/*!*/ codeGenerator);

        /// <summary>
        /// Reports the statement unreachability. 
        /// </summary>
        void ReportUnreachable(Statement/*!*/node, Analyzer/*!*/ analyzer);
    }

    #endregion
}
