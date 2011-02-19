/*

 Copyright (c) 2008 Jakub Misek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
 
using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

using System.Collections;
using System.Reflection;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Reflection;

using PHP.VisualStudio.PhalangerLanguageService.Scopes;
using PHP.VisualStudio.PhalangerLanguageService.Declarations;
using PHP.VisualStudio.PhalangerLanguageService.Parsing;

namespace PHP.VisualStudio.PhalangerLanguageService.Analyzer
{
    /// <summary>
    /// Init Members in the given scope.
    /// </summary>
    public partial class ScopeAnalyzer: TreeVisitor
    {
        #region analyze start

        /// <summary>
        /// Current scope.
        /// </summary>
        private readonly ScopeInfo/*!*/this_scope;

        /// <summary>
        /// Project declarations.
        /// </summary>
        private readonly ProjectDeclarations/*!*/projectdeclarations;

        /// <summary>
        /// Init analyzer.
        /// </summary>
        /// <param name="scope_decls">Output list, will contains declarations in given scope.</param>
        /// <param name="this_scope">This scope.</param>
        /// <param name="projectdeclarations">Project declarations.</param>
        public ScopeAnalyzer(ScopeInfo/*!*/this_scope, ProjectDeclarations/*!*/projectdeclarations)
        {
            this.this_scope = this_scope;
            this.projectdeclarations = projectdeclarations;
        }

        /// <summary>
        /// Start analyzing of global code.
        /// </summary>
        /// <param name="codeScope"></param>
        public void AnalyzeGlobalCode(GlobalCode codeNode)
        {
            // add imported namespaces
            if(codeNode.SourceUnit!=null && codeNode.SourceUnit.ImportedNamespaces!=null)
            {
                foreach(QualifiedName namespace_name in codeNode.SourceUnit.ImportedNamespaces)
                {
                    this_scope.AddKnownNamespace(namespace_name);
                }
            }

            //
            // add declarations
            //
            base.VisitGlobalCode(codeNode);
        }

        /// <summary>
        /// Start analyzing class.
        /// </summary>
        /// <param name="typeNode"></param>
        public void AnalyzeClass(TypeDecl typeNode)
        {
            base.VisitTypeDecl(typeNode);
        }

        /// <summary>
        /// Start analyzing function.
        /// </summary>
        /// <param name="funcNode"></param>
        public void AnalyzeFunction(FunctionDecl funcNode)
        {
            base.VisitFunctionDecl(funcNode);
        }

        /// <summary>
        /// Start analyzing method.
        /// </summary>
        /// <param name="methodNode"></param>
        public void AnalyzeMethod(MethodDecl methodNode)
        {
            base.VisitMethodDecl(methodNode);
        }

        /// <summary>
        /// Start analyzing of code scope including given statement.
        /// </summary>
        /// <param name="stmt"></param>
        public void AnalyzeCode(Statement stmt)
        {
            base.VisitElement(stmt);
        }

        /// <summary>
        /// Analyze statements in the given block.
        /// </summary>
        /// <param name="block"></param>
        public void AnalyzeCodeBlock(BlockStmt block)
        {
            base.VisitBlockStmt(block);
        }

        /// <summary>
        /// Analyze statements in the given switch block.
        /// </summary>
        /// <param name="block"></param>
        public void AnalyzeSwitchBlock(SwitchStmt block)
        {
            base.VisitSwitchStmt(block);
        }

        #endregion

        #region catched AST nodes

        /// <summary>
        /// Class found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitTypeDecl(TypeDecl x)
        {
            ClassScope classscope = new ClassScope(x, this_scope, projectdeclarations);
            ClassDeclaration decl = new ClassDeclaration(classscope, x.Type.QualifiedName.Name.Value, Utils.PositionToSpan(x.EntireDeclarationPosition), projectdeclarations);

            this_scope.AddScope(classscope);
            this_scope.AddDeclaration(decl);
        }

        /// <summary>
        /// Namespace declaration found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitNamespaceDecl(NamespaceDecl x)
        {
            NamespaceDeclScope namespace_scope = new NamespaceDeclScope(x, this_scope, projectdeclarations);
            this_scope.AddScope(namespace_scope);

            // add namespace declaration
            if (!x.IsAnonymous)
                AddNamespaceDeclaration(x.QualifiedName, namespace_scope);
        }

        /// <summary>
        /// Add namespace declaration chain.
        /// </summary>
        /// <param name="namespace_name">Namespace name.</param>
        /// <param name="namespace_scope">Namespace scope.</param>
        private void AddNamespaceDeclaration(QualifiedName namespace_name, NamespaceDeclScope namespace_scope)
        {
            Name[] names = namespace_name.Namespaces;
            if (names.Length == 0)
                return;

            DeclarationInfo topdecl =
                new NamespaceDeclaration(
                    names[names.Length - 1].Value,
                    namespace_scope,
                    this_scope);

            for(int i = names.Length - 2; i >= 0; --i)
            {
                string NamespaceFullName = null;
                for(int j = 0; j <= i; ++j)
                {
                    if (NamespaceFullName == null)
                        NamespaceFullName = names[j].Value;
                    else
                        NamespaceFullName += QualifiedName.Separator + names[j].Value;
                }

                topdecl = new NamespacePartDeclaration(names[i].Value, NamespaceFullName, topdecl, this_scope);
            }

            this_scope.AddDeclaration(topdecl);
        }

        /// <summary>
        /// Class method found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitMethodDecl(MethodDecl x)
        {
            FunctionScope funscope = new FunctionScope(x, this_scope, projectdeclarations);
            FunctionDeclaration decl = new FunctionDeclaration(funscope, x.Name.Value, Utils.PositionToSpan(x.EntireDeclarationPosition), this_scope);

            this_scope.AddScope(funscope);
            this_scope.AddDeclaration(decl);
        }
        
        /// <summary>
        /// Class fields found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitFieldDeclList(FieldDeclList x)
        {
            foreach (FieldDecl field in x.Fields)
            {
                this_scope.AddDeclaration(new VariableDeclaration(field, x.Modifiers, this_scope));
            }
        }

        /// <summary>
        /// Class constants found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitConstDeclList(ConstDeclList x)
        {
            foreach (ClassConstantDecl constant in x.Constants)
            {
                this_scope.AddDeclaration(new ConstantDeclaration(constant, x.Modifiers, this_scope));
            }
        }

        /// <summary>
        /// Function found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitFunctionDecl(FunctionDecl x)
        {
            FunctionScope funscope = new FunctionScope(x, this_scope, projectdeclarations);
            FunctionDeclaration decl = new FunctionDeclaration(funscope, x.Name.Value, Utils.PositionToSpan(x.EntireDeclarationPosition), this_scope);

            this_scope.AddScope(funscope);
            this_scope.AddDeclaration(decl);
        }

        /// <summary>
        /// Function/method formal parameter found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitFormalParam(FormalParam x)
        {
            FunctionParamInfo p = new FunctionParamInfo(x);

            // add parameter
            FunctionScope funscope = this_scope as FunctionScope;
            if (funscope != null)
                funscope.FormalParameters.Add(p);
            
            // add variable declaration too
            this_scope.AddDeclaration(
                new ParameterVariableDeclaration(
                    p.Name,
                    p.Position,
                    p.Description,
                    ValueAnalyzer.Create(p, this_scope.ParentScope),
                    this_scope)
                    );
        }

        /// <summary>
        /// include was found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitIncludingEx(IncludingEx x)
        {
            // TODO: analyze expression for ALL possible values
            // use ExpressionAnalyzer

            StringLiteral stringliteral = x.Target as StringLiteral;
            if (stringliteral != null)
            {
                string relativeFileName = (string)stringliteral.Value;
                string this_filename = this_scope.FileName;
                Uri fullname = new Uri(new Uri(this_filename), relativeFileName);

                //
                ScopeInfo baseScope = this_scope;
                while (
                    (baseScope.ScopeType & (ScopeInfo.ScopeTypes.Function | ScopeInfo.ScopeTypes.Class | ScopeInfo.ScopeTypes.Namespace)) == 0 &&
                    baseScope.ParentScope != null) // add inclusion to the parent scope (until parent is null or function/class/namespace)
                        baseScope = baseScope.ParentScope;

                baseScope.AddIncludedFile(new ScopeInfo.FileInclude(fullname.LocalPath, x.Position.FirstLine, x.Position.FirstColumn));

                projectdeclarations.AddIncludeInfo(this_filename, new ProjectDeclarations.IncludedFileInfo(this_scope, fullname.LocalPath));
            }
            else
            {
                // TODO: including expression
            }
        }

        #endregion

        #region catched variables assignment

        public override void VisitValueAssignEx(ValueAssignEx x)
        {
            // list of affected variables
            DeclarationList vardecls = new DeclarationList();

            // a1 = a2 = ... = an = expression;
            // find declarations of all aX
            // find the right value expression
            // assign right value expression to declarations of all aX

            ValueAssignEx assignment = x;
            Expression assignmentRValue = null;

            while (assignment != null)
            {
                // find the previous declaration of this variable and
                DeclarationList curvardecls = GetDeclarations.GetDeclarationsByName(projectdeclarations, this_scope, true, assignment.LValue, null);   // do not cross over includes // create variable declaration if not yet
                
                if (curvardecls != null && curvardecls.Count > 0)
                {   // more declarations of this variable should be found
                    vardecls.Add(curvardecls[curvardecls.Count - 1]);// add analyzer to the lowest found declarations (deepest scope)
                }

                assignmentRValue = assignment.RValue;
                assignment = assignmentRValue as ValueAssignEx;    // next assigned variable (in case of if the right side is assignment again)
            }


            // use this ValueAnalyzer for all declarations
            ValueAnalyzer analyzer = ValueAnalyzer.Create(assignmentRValue, this_scope);

            foreach (DeclarationInfo decl in vardecls)
                decl.AddAnalyzer(analyzer);
            
            // visit RValue expression
            base.VisitElement(assignmentRValue);
        }

        public override void VisitGlobalStmt(GlobalStmt x)
        {
            // add declarations from lower scope
            // find them and add references into local scope

            // TODO: scan scopes which includes this_scope or are included in this_scope.

            foreach (SimpleVarUse varuse in x.VarList)
            {
                DirectVarUse v = varuse as DirectVarUse;

                if ( v != null )
                {
                    string varname = v.VarName.Value;
                    ScopeInfo scope = this_scope;

                    // find the variable
                    while (scope != null)
                    {
                        if ( scope.Declarations != null )
                            foreach (DeclarationInfo decl in scope.Declarations)
                                if (decl.DeclarationType == DeclarationInfo.DeclarationTypes.Variable &&
                                    decl.Label == varname)
                                {
                                    if ( scope != this_scope )
                                    {
                                        this_scope.AddDeclaration(decl);
                                        break;
                                    }
                                }

                        scope = scope.ParentScope;
                    }
                }                
            }
        }

        #endregion

        #region catched block statements

        private static void AddLoopMembers(ScopeInfo scope)
        {
            // break [count];
            scope.AddDeclaration(
                new SpecialFunctionDecl(
                    "break",
                    "break ends execution of the current for, foreach, while, do-while or switch structure.",
                    new FunctionParamInfo[] { new FunctionParamInfo("count", "int", "optional numeric argument which tells it how many nested enclosing structures are to be broken out of.", new PHP.Core.Parsers.Position(), true) },
                    false
                    ));

            // continue [count];
            scope.AddDeclaration(
                new SpecialFunctionDecl(
                    "continue",
                    "continue is used within looping structures to skip the rest of the current loop iteration and continue execution at the condition evaluation and then the beginning of the next iteration.",
                    new FunctionParamInfo[] { new FunctionParamInfo("count", "int", "an optional numeric argument which tells it how many levels of enclosing loops it should skip to the end of.", new PHP.Core.Parsers.Position(), true) },
                    false
                    ));
        }

        /// <summary>
        /// Block found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitBlockStmt(BlockStmt x)
        {
            this_scope.AddScope(new CodeBlockScope(x, this_scope, projectdeclarations));
        }

        /// <summary>
        /// If conditional statement found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitConditionalStmt(ConditionalStmt x)
        {
            // TODO: analyze variables in condition
            this_scope.AddScope(new CodeDeclScope(x.Statement, this_scope, projectdeclarations));
        }

        /// <summary>
        /// While or Do found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitWhileStmt(WhileStmt x)
        {
            CodeDeclScope newscope = new CodeDeclScope(x.Body, this_scope, projectdeclarations);
            this_scope.AddScope(newscope);

            // break, continue
            AddLoopMembers(newscope);
        }

        /// <summary>
        /// for(;;) found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitForStmt(ForStmt x)
        {
            CodeDeclScope newscope = new CodeDeclScope(x.Body, this_scope, projectdeclarations);

            this_scope.AddScope(newscope);

            // initializer list
            ScopeAnalyzer analyzer = new ScopeAnalyzer(newscope, projectdeclarations);
            foreach(Expression initexpr in x.InitExList)
                analyzer.VisitElement(initexpr);

            // break, continue
            AddLoopMembers(newscope);
        }

        /// <summary>
        /// foreach() found.
        /// </summary>
        /// <param name="x"></param>
        public override void VisitForeachStmt(ForeachStmt x)
        {
            CodeDeclScope newscope = new CodeDeclScope(x.Body, this_scope, projectdeclarations);
            // TODO: analyze variables in enumeree list
            this_scope.AddScope(newscope);

            // break, continue
            AddLoopMembers(newscope);
        }

        /// <summary>
        /// switch (){...} found.
        /// </summary>
        /// <param name="x">Switch statement block.</param>
        public override void VisitSwitchStmt(SwitchStmt x)
        {
            SwitchBlockDeclScope newscope = new SwitchBlockDeclScope(x, this_scope, projectdeclarations);

            this_scope.AddScope(newscope);

            // break, continue // where continue has the same effect as break.
            AddLoopMembers(newscope);
        }

        #endregion
    }
}