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
using PHP.Core.EmbeddedDoc;

using PHP.VisualStudio.PhalangerLanguageService.Analyzer;
using PHP.VisualStudio.PhalangerLanguageService.Declarations;

namespace PHP.VisualStudio.PhalangerLanguageService.Scopes
{
    /// <summary>
    /// Class code scope.
    /// </summary>
    public class ClassScope:ScopeInfo
    {
        readonly TypeDecl/*!*/decl;
        readonly ProjectDeclarations projectdeclarations;

        /// <summary>
        /// Init class scope.
        /// </summary>
        /// <param name="astNode"></param>
        /// <param name="parentscope"></param>
        public ClassScope(TypeDecl/*!*/ astNode, ScopeInfo parentscope, ProjectDeclarations projectdeclarations)
            :base(parentscope,Utils.PositionToSpan(astNode.EntireDeclarationPosition))
        {
            this.decl = astNode;
            this.projectdeclarations = projectdeclarations;

            // body span
            BodySpan.iStartLine = astNode.HeadingEndPosition.Line - 1;
            BodySpan.iStartIndex = astNode.HeadingEndPosition.Column - 1;
        }

        /// <summary>
        /// Base class decl scope.
        /// </summary>
        private DeclarationInfo _basedecl = null;

        /// <summary>
        /// Add public or protected members to the local declarations list.
        /// </summary>
        /// <param name="decls"></param>
        private void AddParentMembers(List<DeclarationInfo> decls)
        {
            if (decls != null)
                foreach (DeclarationInfo decl in decls)
                {
                    if ((decl.DeclarationVisibility & (DeclarationInfo.DeclarationVisibilities.Public | DeclarationInfo.DeclarationVisibilities.Protected)) != 0 &&
                        ((decl.DeclarationType & (DeclarationInfo.DeclarationTypes.Variable | DeclarationInfo.DeclarationTypes.Constant | DeclarationInfo.DeclarationTypes.Function)) != 0))
                    {
                        AddDeclaration(decl);
                    }
                }
        }

        /// <summary>
        /// Base class.
        /// </summary>
        public DeclarationInfo BaseClass
        {
            get
            {
                if (_basedecl == null)
                {
                    if (decl.BaseClassName != null)
                    {
                        string basename = decl.BaseClassName.Value.QualifiedName.Name.Value;
                        DeclarationList decls = new DeclarationList();
                        GetLocalDeclarations(decls, DeclarationInfo.DeclarationTypes.Class, new DeclarationLabelEqual(basename), projectdeclarations);

                        if(decls.Count>0)
                            _basedecl = decls[0];
                    }
                    else
                    {
                        return null;
                    }
                }

                return _basedecl;
            }
        }

        /// <summary>
        /// Init scope declarations.
        /// </summary>
        protected override void  InitScope()
        {
            // class members
            ScopeAnalyzer analyzer = new ScopeAnalyzer(this, projectdeclarations);
            analyzer.AnalyzeClass(decl);

            // $this, self
            AddDeclaration(new ThisVariableDeclaration(
                new ThisAnalyzer(this), this));

            AddDeclaration(new SpecialKeywordDecl(
                "self", "This class.", DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, new SelfAnalyzer(this)));

            // other special keywords
            AddDeclaration(new SpecialKeywordDecl("private", "Private modifier.", DeclarationInfo.DeclarationUsages.ThisScope, null));
            AddDeclaration(new SpecialKeywordDecl("protected", "Protected modifier.", DeclarationInfo.DeclarationUsages.ThisScope, null));
            AddDeclaration(new SpecialKeywordDecl("public", "Public modifier.", DeclarationInfo.DeclarationUsages.ThisScope, null));
            AddDeclaration(new SpecialKeywordDecl("abstract", "Abstract modifier.", DeclarationInfo.DeclarationUsages.ThisScope, null));
            AddDeclaration(new SpecialKeywordDecl("final", "Abstract modifier.", DeclarationInfo.DeclarationUsages.ThisScope, null));
            AddDeclaration(new SpecialKeywordDecl("const", "Define constant.", DeclarationInfo.DeclarationUsages.ThisScope, null));
            AddDeclaration(new SpecialKeywordDecl("static", "Static modifier.", DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));
            AddDeclaration(new SpecialKeywordDecl("var", "Variable declaration.", DeclarationInfo.DeclarationUsages.ThisScope, null));

            // visible parent members
            if (BaseClass != null)
            {
                ScopeInfo BaseClassScope = BaseClass.DeclarationScope;

                if (BaseClassScope != null)
                    AddParentMembers(BaseClassScope.Declarations);

                // parent keyword
                ParentAnalyzer parentanalyzer = new ParentAnalyzer(BaseClass);

                DeclarationInfo parentDecl = new SpecialKeywordDecl(
                    "parent", "parent class", DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, parentanalyzer);
                //DeclarationInfo parentclassDecl = new SpecialKeywordDecl(
                //    BaseClass.Label, "extends class " + BaseClass.Label, DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, parentanalyzer);

                // copy ctor parameters
                List<FunctionParamInfo> ctorparams = BaseClass.ObjectParameters;
                if (ctorparams != null)
                {
                    foreach(FunctionParamInfo p in ctorparams)
                    {
                        parentDecl.AddParameter(p);
                        //parentclassDecl.AddParameter(p);
                    }
                }

                // add declarations
                // - parent
                // - ParentClassName
                AddDeclaration(parentDecl);
                //AddDeclaration(parentclassDecl);
                
            }
        }

        public override ScopeTypes ScopeType
        {
            get
            {
                return ScopeTypes.Class;
            }
        }

        public override string FullName
        {
            get
            {
                return base.FullName + "::" + decl.Name.Value;
            }
        }
    }

    /// <summary>
    /// Namespace scope.
    /// Contains statements accessible throw this scope only.
    /// </summary>
    public class NamespaceDeclScope : ScopeInfo
    {
        /// <summary>
        /// Namespace name.
        /// Used for displaying only.
        /// </summary>
        private string NamespaceName = string.Empty;

        /// <summary>
        /// Namespace scope init.
        /// </summary>
        /// <param name="namespacedecl"></param>
        /// <param name="parentscope"></param>
        /// <param name="projectdeclarations"></param>
        public NamespaceDeclScope(NamespaceDecl namespacedecl, ScopeInfo parentscope, ProjectDeclarations projectdeclarations)
            : base(parentscope, new TextSpan())
        {
            if (!namespacedecl.IsAnonymous)
            {
                InitNamespaceName(namespacedecl.QualifiedName);

                // add imported namespace (this)
                AddKnownNamespace(namespacedecl.QualifiedName);
            }

            // analyze statements in this namespace
            if(namespacedecl.Statements != null && namespacedecl.Statements.Count > 0)
            {
                ScopeAnalyzer analyzer = new ScopeAnalyzer(this, projectdeclarations);

                foreach (Statement statement in namespacedecl.Statements)
                {
                    analyzer.AnalyzeCode(statement);
                }
            }

            // update body span
            UpdateBodySpan();
        }

        /// <summary>
        /// Get namespace name.
        /// </summary>
        /// <param name="namespacename"></param>
        private void InitNamespaceName(QualifiedName namespacename)
        {
            NamespaceName = string.Empty;

            foreach(Name name in namespacename.Namespaces)
            {
                if (NamespaceName.Length > 0) NamespaceName += QualifiedName.Separator;

                NamespaceName += name.Value;
            }
        }

        /// <summary>
        /// update body span (from members)
        /// </summary>
        private void UpdateBodySpan()
        {
            bool bFirst = true;

            // decls
            if (Declarations != null)
                foreach(DeclarationInfo decl in Declarations)
                {
                    if (bFirst)
                    {
                        BodySpan = decl.Span;
                        bFirst = false;
                    }
                    else
                    {
                        BodySpan = Utils.UniteSpans(BodySpan, decl.Span);
                    }
                }

            // scopes
            if (Scopes != null)
                foreach (ScopeInfo scope in Scopes)
                {
                    if (bFirst)
                    {
                        BodySpan = scope.BodySpan;
                        bFirst = false;
                    }
                    else
                    {
                        BodySpan = Utils.UniteSpans(BodySpan, scope.BodySpan);
                    }
                }
        }

        public override ScopeTypes ScopeType
        {
            get
            {
                return ScopeTypes.Namespace;
            }
        }

        protected override void InitScope()
        {
            // analyzed in .ctor
        }

        /// <summary>
        /// namespace FullName = Namespace name
        /// </summary>
        /// <remarks>The FullName consists of the full namespace name.
        /// The scope file name is ignored. This string can be used as a regular PHP namespace path in the source code.</remarks>
        public override string FullName
        {
            get
            {
                return NamespaceName;    // ignore filename
            }
        }
    }

    /// <summary>
    /// Function/method scope.
    /// </summary>
    public class FunctionScope:ScopeInfo
    {
        private readonly ProjectDeclarations projectdeclarations;

        /// <summary>
        /// Function/method parameters;
        /// </summary>
        private List<FunctionParamInfo> _FormalParameters = null;

        /// <summary>
        /// Function/method parameters;
        /// </summary>
        public List<FunctionParamInfo> FormalParameters
        {
            get
            {
                if (_FormalParameters == null)
                    _FormalParameters = new List<FunctionParamInfo>();

                InitScopeIfNotYet();
                return _FormalParameters;
            }
        }

        /// <summary>
        /// Function/method return type.
        /// TODO: resolve return type (from doc comment or from return statement analysis).
        /// </summary>
        public QualifiedName ReturnType
        {
            get
            {
                return new QualifiedName();
            }
        }

        /// <summary>
        /// Parse function description.
        /// TODO: Xml comments
        /// </summary>
        /// <returns>Description.</returns>
        public string GetDescription()
        {
            string description = string.Empty;

            // parse doc comment
            DocFunctionBlock block = null;
            DocResolver d = new DocResolver();
            if (astFunctionNode != null)
            {
                d.VisitFunctionDecl(astFunctionNode);
                block = (DocFunctionBlock)astFunctionNode.Annotations.Get<DocBlock>();
            }
            else if (astMethodNode != null)
            {
                d.VisitMethodDecl(astMethodNode);
                block = (DocFunctionBlock)astMethodNode.Annotations.Get<DocBlock>();
            }

            if (block != null && block.Summary != null)
            {
                DocExpression[] docexpressions = ((DocSummaryElement)block.Summary).Description.Expressions;
                foreach (DocExpression docexpr in docexpressions)
                    description += ((DocTextExpr)docexpr).Text.Trim();
            }

            return description;
        }

        /// <summary>
        /// method is static
        /// </summary>
        public readonly bool IsStatic = false; // default

        /// <summary>
        /// method visibility
        /// </summary>
        public readonly DeclarationInfo.DeclarationVisibilities Visibility = DeclarationInfo.DeclarationVisibilities.Public;   // default PHP visibility is PUBLIC

        /// <summary>
        /// for functions; FunctionDecl AstNode.
        /// Should be null.
        /// </summary>
        private readonly FunctionDecl astFunctionNode;

        /// <summary>
        /// for methods; MethodDecl AstNode.
        /// Should be null.
        /// </summary>
        private readonly MethodDecl astMethodNode;

        /// <summary>
        /// display static and/or public|private|protected before name.
        /// </summary>
        public bool DisplayStaticAndVisibility
        {
            get
            {
                return astMethodNode != null;
            }
        }

        /// <summary>
        /// Init function.
        /// </summary>
        /// <param name="astNode"></param>
        public FunctionScope(FunctionDecl astNode, ScopeInfo parentscope, ProjectDeclarations projectdeclarations)
            : base(parentscope, Utils.PositionToSpan(astNode.EntireDeclarationPosition))
        {
            this.astFunctionNode = astNode;
            this.projectdeclarations = projectdeclarations;

            // body span
            this.BodySpan.iStartLine = astNode.HeadingEndPosition.Line - 1;
            this.BodySpan.iStartIndex = astNode.HeadingEndPosition.Column - 1;
        }

        /// <summary>
        /// Init method.
        /// </summary>
        /// <param name="astNode"></param>
        public FunctionScope(MethodDecl astNode, ScopeInfo parentscope, ProjectDeclarations projectdeclarations)
            : base(parentscope, Utils.PositionToSpan(astNode.EntireDeclarationPosition))
        {
            this.astMethodNode = astNode;
            this.projectdeclarations = projectdeclarations;

            this.IsStatic = DeclarationInfo.IsMemberStatic(astNode.Modifiers);
            this.Visibility = DeclarationInfo.GetMemberVisibility(astNode.Modifiers);

            // body span
            this.BodySpan.iStartLine = astNode.HeadingEndPosition.Line - 1;
            this.BodySpan.iStartIndex = astNode.DeclarationBodyPosition.Column - 1;
        }

        /// <summary>
        /// Init declarations in the function.
        /// </summary>
        protected override void InitScope()
        {
            ScopeAnalyzer analyzer = new ScopeAnalyzer(this, projectdeclarations);
            if ( astFunctionNode != null ) analyzer.AnalyzeFunction(astFunctionNode);
            else if (astMethodNode != null) analyzer.AnalyzeMethod(astMethodNode);

            //AddDeclaration(new SpecialKeywordDecl("static", "Static modifier.", DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));//added in class scope
        }

        public override ScopeTypes ScopeType
        {
            get
            {
                return ScopeTypes.Function;
            }
        }
    }

    /// <summary>
    /// Code scope.
    /// Inner statements of If, while, for, foreach, ...
    /// </summary>
    public class CodeDeclScope : ScopeInfo
    {
        //private readonly Statement statement;
        //private readonly ProjectDeclarations projectdeclarations;

        public CodeDeclScope(Statement statement, ScopeInfo parentscope, ProjectDeclarations projectdeclarations)
            :base(parentscope,Utils.PositionToSpan(statement.Position))
        {
            //this.statement = statement;
            //this.projectdeclarations = projectdeclarations;

            ScopeAnalyzer analyzer = new ScopeAnalyzer(this, projectdeclarations);
            analyzer.AnalyzeCode(statement);
        }

        public override ScopeTypes ScopeType
        {
            get
            {
                return ScopeTypes.Block;
            }
        }

        protected override void InitScope()
        {
            // analyzed in .ctor
        }
    }

    /// <summary>
    /// Code Block.
    /// </summary>
    public class CodeBlockScope : ScopeInfo
    {
        //private readonly BlockStmt block;
        //private readonly ProjectDeclarations projectdeclarations;

        public CodeBlockScope(BlockStmt block, ScopeInfo parentscope, ProjectDeclarations projectdeclarations)
            : base(parentscope,Utils.PositionToSpan(block.Position))
        {
            //this.block = block;
            //this.projectdeclarations = projectdeclarations;

            ScopeAnalyzer analyzer = new ScopeAnalyzer(this, projectdeclarations);
            analyzer.AnalyzeCodeBlock(block);
        }

        public override ScopeTypes ScopeType
        {
            get
            {
                return ScopeTypes.Block;
            }
        }

        protected override void InitScope()
        {
            // analyzed in .ctor
        }
    }

    /// <summary>
    /// SwitchStmt block.
    /// </summary>
    public class SwitchBlockDeclScope: ScopeInfo
    {
        public SwitchBlockDeclScope(SwitchStmt block, ScopeInfo parentscope, ProjectDeclarations projectdeclarations)
            :base(parentscope, Utils.PositionToSpan(block.Position))
        {
            ScopeAnalyzer analyzer = new ScopeAnalyzer(this, projectdeclarations);
            analyzer.AnalyzeSwitchBlock(block);
        }

        public override ScopeTypes ScopeType
        {
            get
            {
                return ScopeTypes.Block;
            }
        }

        protected override void InitScope()
        {
            // analyzed in .ctor

            // add special keywords
            // case
            AddDeclaration(new SpecialKeywordDecl("case", null, DeclarationInfo.DeclarationUsages.ThisScope, null));
            // default
            AddDeclaration(new SpecialKeywordDecl("default", null, DeclarationInfo.DeclarationUsages.ThisScope, null));
        }
    }
}