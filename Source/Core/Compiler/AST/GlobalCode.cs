/*

 Copyright (c) 2007- DEVSENSE
 Copyright (c) 2004-2006 Tomas Matousek and Vaclav Novak.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Parsers;
using PHP.Core.Emit;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.AST
{
    partial class NodeCompilers
    {
        #region GlobalCode

        [NodeCompiler(typeof(GlobalCode))]
        sealed class GlobalCodeCompiler : INodeCompiler, IGlobalCodeCompiler
        {
            /// <summary>
            /// Global variables. Not available in pure mode, non-null otherwise.
            /// </summary>
            public VariablesTable/*!*/ VarTable { get { return varTable; } }
            private readonly VariablesTable/*!*/ varTable;

            /// <summary>
            /// Labels (PHP6 feature).
            /// </summary>
            public Dictionary<VariableName, Statement> Labels { get { return labels; } }
            private readonly Dictionary<VariableName, Statement> labels;

            public IncludingEx PrependedInclusion { get; set; }
            public IncludingEx AppendedInclusion { get; set; }

            public GlobalCodeCompiler(GlobalCode/*!*/ast)
            {
                if (!ast.SourceUnit.IsPure)
                {
                    this.varTable = new VariablesTable(20);
                    this.varTable.SetAllRef();
                    this.labels = new Dictionary<VariableName, Statement>();
                }
            }

            #region Analysis

            public void Analyze(GlobalCode/*!*/ast, Analyzer/*!*/ analyzer)
            {
                analyzer.LeaveUnreachableCode();

                ExInfoFromParent info = new ExInfoFromParent(ast);

                // analyze auto-prepended inclusion (no code reachability checks):
                if (PrependedInclusion != null)
                {
                    info.Access = AccessType.None;
                    PrependedInclusion.Analyze(analyzer, info);
                }

                for (int i = 0; i < ast.Statements.Length; i++) // NOTE: ast.Statements may change during analysis, iterate in this way!
                {
                    if (analyzer.IsThisCodeUnreachable() && ast.Statements[i].IsDeclaration)
                    {
                        //unreachable declarations in global code are valid
                        analyzer.LeaveUnreachableCode();
                        ast.Statements[i] = ast.Statements[i].Analyze(analyzer);
                        analyzer.EnterUnreachableCode();
                    }
                    else
                    {
                        ast.Statements[i] = ast.Statements[i].Analyze(analyzer);
                    }
                }

                if (!ast.SourceUnit.IsPure)
                    Analyzer.ValidateLabels(analyzer.ErrorSink, ast.SourceUnit, labels);

                // analyze auto-prepended inclusion (no code reachability checks):
                if (AppendedInclusion != null)
                {
                    info.Access = AccessType.Read;
                    AppendedInclusion.Analyze(analyzer, info);
                }

                analyzer.LeaveUnreachableCode();
            }

            #endregion

            #region Emission

            /// <include file='Doc/Nodes.xml' path='doc/method[@name="Emit"]/*'/>
            /// <param name="ast">Instance.</param>
            public void Emit(GlobalCode/*!*/ast, CodeGenerator/*!*/ codeGenerator)
            {
                // TODO: improve
                codeGenerator.EnterGlobalCodeDeclaration(this.varTable, labels, (CompilationSourceUnit)ast.SourceUnit);

                //
                if (codeGenerator.CompilationUnit.IsTransient)
                {
                    codeGenerator.DefineLabels(labels);

                    codeGenerator.ChainBuilder.Create();

                    foreach (Statement statement in ast.Statements)
                        statement.Emit(codeGenerator);

                    codeGenerator.ChainBuilder.End();

                    // return + appended file emission:
                    codeGenerator.EmitRoutineEpilogue(ast, true);
                }
#if !SILVERLIGHT
                else if (codeGenerator.CompilationUnit.IsPure)
                {
                    codeGenerator.ChainBuilder.Create();

                    foreach (Statement statement in ast.Statements)
                    {
                        // skip empty statements in global code (they emit sequence points, which is undesirable):
                        if (!(statement is EmptyStmt))
                            statement.Emit(codeGenerator);
                    }

                    codeGenerator.ChainBuilder.End();
                }
                else
                {
                    ScriptCompilationUnit unit = (ScriptCompilationUnit)codeGenerator.CompilationUnit;

                    ILEmitter il = codeGenerator.IL;

                    if (codeGenerator.Context.Config.Compiler.Debug)
                    {
                        codeGenerator.MarkSequencePoint(0);
                        il.Emit(OpCodes.Nop);
                    }

                    codeGenerator.DefineLabels(labels);

                    // CALL <self>.<Declare>(context); 
                    codeGenerator.EmitLoadScriptContext();
                    il.Emit(OpCodes.Call, unit.ScriptBuilder.DeclareHelperBuilder);

                    // IF (<is main script>) CALL <prepended script>.Main()
                    if (PrependedInclusion != null)
                        PrependedInclusion.Emit(codeGenerator);

                    codeGenerator.ChainBuilder.Create();

                    foreach (Statement statement in ast.Statements)
                        statement.Emit(codeGenerator);

                    codeGenerator.ChainBuilder.End();

                    // return + appended file emission:
                    codeGenerator.EmitRoutineEpilogue(ast, false);
                }
#endif
                codeGenerator.LeaveGlobalCodeDeclaration();
            }

            #endregion
        }

        #endregion

        #region NamespaceDecl

        [NodeCompiler(typeof(NamespaceDecl), Singleton = true)]
        sealed class NamespaceDeclCompiler : StatementCompiler<NamespaceDecl>
        {
            internal override Statement Analyze(NamespaceDecl node, Analyzer analyzer)
            {
                analyzer.EnterNamespace(node);

                node.Statements.Analyze(analyzer);

                analyzer.LeaveNamespace();

                return node;
            }

            internal override void Emit(NamespaceDecl node, CodeGenerator codeGenerator)
            {
                foreach (Statement statement in node.Statements)
                {
                    if (!(statement is EmptyStmt))
                        statement.Emit(codeGenerator);
                }
            }
        }

        #endregion

        #region GlobalConstDeclList

        [NodeCompiler(typeof(GlobalConstDeclList), Singleton = true)]
        sealed class GlobalConstDeclListCompiler : StatementCompiler<GlobalConstDeclList>
        {
            #region CustomAttributesProvider

            private sealed class CustomAttributesProvider : IPhpCustomAttributeProvider
            {
                private readonly GlobalConstDeclList node;
                public CustomAttributesProvider(GlobalConstDeclList node)
                {
                    this.node = node;
                }

                #region IPhpCustomAttributeProvider Members

                public PhpAttributeTargets AttributeTarget { get { return PhpAttributeTargets.Constant; } }
                public AttributeTargets AcceptsTargets { get { return AttributeTargets.Field; } }

                public int GetAttributeUsageCount(DType/*!*/ type, CustomAttribute.TargetSelectors selector)
                {
                    var attributes = node.Attributes;
                    if (attributes == null || attributes.Attributes == null)
                        return 0;
                    else
                        return attributes.Count(type, selector);
                }

                public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
                {
                    foreach (GlobalConstantDecl cd in node.Constants)
                    {
                        var cdcompiler = cd.NodeCompiler<IGlobalConstantDeclCompiler>();
                        cdcompiler.ApplyCustomAttribute(kind, attribute, selector);
                    }
                }

                public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder, CustomAttribute.TargetSelectors selector)
                {
                    foreach (GlobalConstantDecl cd in node.Constants)
                    {
                        var cdcompiler = cd.NodeCompiler<IGlobalConstantDeclCompiler>();
                        cdcompiler.EmitCustomAttribute(builder);
                    }
                }

                #endregion
            }

            #endregion

            internal override Statement Analyze(GlobalConstDeclList node, Analyzer analyzer)
            {
                var attributes = node.Attributes;
                if (attributes != null)
                {
                    attributes.AnalyzeMembers(analyzer, analyzer.CurrentScope);
                    attributes.Analyze(analyzer, new CustomAttributesProvider(node));
                }

                bool is_unreachable = analyzer.IsThisCodeUnreachable();

                foreach (GlobalConstantDecl cd in node.Constants)
                {
                    var cdcompiler = cd.NodeCompiler<IGlobalConstantDeclCompiler>();
                    cdcompiler.GlobalConstant.Declaration.IsUnreachable = is_unreachable;
                    cdcompiler.Analyze(cd, analyzer);
                }

                if (is_unreachable)
                {
                    analyzer.ReportUnreachableCode(node.Span);
                    return EmptyStmt.Unreachable;
                }
                else
                {
                    return node;
                }
            }

            internal override void Emit(GlobalConstDeclList node, CodeGenerator codeGenerator)
            {
                // TODO: initialization
            }
        }

        #endregion

        #region GlobalConstantDecl

        [NodeCompiler(typeof(GlobalConstantDecl))]
        sealed class GlobalConstantDeclCompiler : ConstantDeclCompiler<GlobalConstantDecl>, IGlobalConstantDeclCompiler
        {
            public override KnownConstant/*!*/Constant { get { return constant; } }
            public GlobalConstant/*!*/GlobalConstant { get { return constant; } }
            private readonly GlobalConstant/*!*/constant;

            public GlobalConstantDeclCompiler(GlobalConstantDecl/*!*/node)
            {
                QualifiedName qn = (node.Namespace != null)
                            ? new QualifiedName(new Name(node.Name.Value), node.Namespace.QualifiedName)
                            : new QualifiedName(new Name(node.Name.Value));
                constant = new GlobalConstant(qn, PhpMemberAttributes.Public, (CompilationSourceUnit)node.SourceUnit, node.IsConditional, node.Scope, node.Span);
                constant.SetNode(node);
            }

            public override void Analyze(GlobalConstantDecl node, Analyzer analyzer)
            {
                if (!this.analyzed)
                {
                    base.Analyze(node, analyzer);

                    // check some special constants (ignoring namespace)
                    if (node.Name.Value == GlobalConstant.Null.FullName ||
                        node.Name.Value == GlobalConstant.False.FullName ||
                        node.Name.Value == GlobalConstant.True.FullName)
                        analyzer.ErrorSink.Add(FatalErrors.ConstantRedeclared, analyzer.SourceUnit, node.Span, node.Name.Value);
                }
            }

            public void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector)
            {
                switch (kind)
                {
                    case SpecialAttributes.Export:
                        constant.ExportInfo = (ExportAttribute)attribute;
                        break;

                    default:
                        Debug.Fail("N/A");
                        throw null;
                }
            }

            public void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder)
            {
                constant.RealFieldBuilder.SetCustomAttribute(builder);
            }
        }

        #endregion
    }

    #region IGlobalCodeCompiler

    internal interface IGlobalCodeCompiler
    {
        /// <summary>
        /// Global variables. Is <c>null</c> in pure mode.
        /// </summary>
        VariablesTable VarTable { get; }

        /// <summary>
        /// Labels (PHP6 feature).
        /// </summary>
        Dictionary<VariableName, Statement> Labels { get; }

        /// <summary>
        /// Prepended inclusion by compiler.
        /// </summary>
        IncludingEx PrependedInclusion { get; set; }

        /// <summary>
        /// Appended inclusion by compiler.
        /// </summary>
        IncludingEx AppendedInclusion { get; set; }

        /// <summary>
        /// Analyzes entire AST.
        /// </summary>
        void Analyze(GlobalCode/*!*/ast, Analyzer/*!*/ analyzer);

        /// <summary>
        /// Emits entire AST.
        /// </summary>
        void Emit(GlobalCode/*!*/ast, CodeGenerator/*!*/ codeGenerator);
    }

    internal static class GlobalCodeCompilerHelper
    {
        public static VariablesTable GetVarTable(this GlobalCode/*!*/ast)
        {
            return ast.NodeCompiler<IGlobalCodeCompiler>().VarTable;
        }

        public static Dictionary<VariableName, Statement> GetLabels(this GlobalCode/*!*/ast)
        {
            return ast.NodeCompiler<IGlobalCodeCompiler>().Labels;
        }

        public static void Analyze(this GlobalCode/*!*/ast, Analyzer/*!*/ analyzer)
        {
            ast.NodeCompiler<IGlobalCodeCompiler>().Analyze(ast, analyzer);
        }
        public static void Emit(this GlobalCode/*!*/ast, CodeGenerator/*!*/ codeGenerator)
        {
            ast.NodeCompiler<IGlobalCodeCompiler>().Emit(ast, codeGenerator);
        }
    }

    #endregion

    #region IGlobalConstantDeclCompiler

    internal static class GlobalConstantDeclCompilerHelper
    {
        public static GlobalConstant GetGlobalConstant(this GlobalConstantDecl/*!*/node)
        {
            return node.NodeCompiler<IGlobalConstantDeclCompiler>().GlobalConstant;
        }
    }

    internal interface IGlobalConstantDeclCompiler : IConstantDeclCompiler
    {
        GlobalConstant/*!*/GlobalConstant { get; }
        void ApplyCustomAttribute(SpecialAttributes kind, Attribute attribute, CustomAttribute.TargetSelectors selector);
        void EmitCustomAttribute(CustomAttributeBuilder/*!*/ builder);
    }

    #endregion
}
