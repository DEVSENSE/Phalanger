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
using PHP.VisualStudio.PhalangerLanguageService.Parsing;
using PHP.VisualStudio.PhalangerLanguageService.Declarations;

namespace PHP.VisualStudio.PhalangerLanguageService.Analyzer
{
    #region Analyzers for specified expressions.
    /// <summary>
    /// Specified Expression analyzer for "new xxx()".
    /// Analyzes immediately in .ctor.
    /// </summary>
    class NewExpressionAnalyzer:ValueAnalyzer
    {
        /// <summary>
        /// Type followed by new.
        /// </summary>
        private DeclarationInfo resolvedDecl = null;
        private bool resolvingRightNow = false;

        /// <summary>
        /// the new expression
        /// </summary>
        private NewEx/*!*/newex;

        /// <summary>
        /// local scope
        /// </summary>
        private ScopeInfo localscope;

        /// <summary>
        /// Resolve type followed by new.
        /// </summary>
        /// <param name="newex"></param>
        public NewExpressionAnalyzer(NewEx newex, ScopeInfo localscope)
        {
            if (newex == null)
                throw new ArgumentNullException("newex");

            this.newex = newex;
            this.localscope = localscope;            
        }

        /// <summary>
        /// Try to resolve the variable type.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <returns></returns>
        private DeclarationInfo ResolveType(ProjectDeclarations projectdeclarations)
        {
        	// new argument
            DirectTypeRef directtype = newex.ClassNameRef as DirectTypeRef;
            if (directtype != null)
            {
                // new {ClassName}
                DeclarationList decls = localscope.GetDeclarationsByName(directtype.ClassName, projectdeclarations);

                if (decls.Count > 0)
                    return decls[0];
            }

            PrimitiveTypeRef primitivetype = newex.ClassNameRef as PrimitiveTypeRef;
            if (primitivetype != null)
            {
                // new {ClassName}
                DeclarationList decls = localscope.GetDeclarationsByName(Utils.MakeQualifiedName(primitivetype.Type.FullName), projectdeclarations);

                if (decls.Count > 0)
                    return decls[0];
            }

		    // nothing found
		    return null;
        }

        /// <summary>
        /// Resolve the variable type if not yet.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        protected void InitIfNotYet(ProjectDeclarations projectdeclarations)
        {
            if (resolvedDecl == null && !resolvingRightNow)
            {
                resolvingRightNow = true;

                resolvedDecl = ResolveType(projectdeclarations);

                resolvingRightNow = false;
            }
        }

        /// <summary>
        /// Get visible members after -> of variable of type resolvedType.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="result"></param>
        /// <param name="contains"></param>
        public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            InitIfNotYet(projectdeclarations);

            // select required members
            if (resolvedDecl != null)
            {
                resolvedDecl.GetObjectMembers(projectdeclarations, result, match);
            }
        }

        /// <summary>
        /// Get members after ::
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="result"></param>
        /// <param name="contains"></param>
        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            InitIfNotYet(projectdeclarations);

            // select required members
            if (resolvedDecl != null)
            {
                resolvedDecl.GetStaticMembers(projectdeclarations, result, match);
            }
        }

        /// <summary>
        /// new {typename}
        /// </summary>
        /// <returns></returns>
        public override string GetValueText()
        {
            //InitIfNotYet(projectdeclarations);//projectdeclarations not known

            if (resolvedDecl != null)
                return "new " + resolvedDecl.Label;
            else
                return base.GetValueText();
        }
    }

    /// <summary>
    /// Specified expression analyzer for "$varname";
    /// Analyzes immediately in .ctor.
    /// </summary>
    class VariableExpressionAnalyzer:ValueAnalyzer
    {
        /// <summary>
        /// Resolved variable. Cannot be null.
        /// </summary>
        DeclarationList resolvedVariables = null;

        VarLikeConstructUse varuse;
        ScopeInfo localscope;

        /// <summary>
        /// Find variable "varuse".
        /// </summary>
        /// <param name="varuse"></param>
        /// <param name="localscope"></param>
        /// <param name="projectdeclarations"></param>
        public VariableExpressionAnalyzer(VarLikeConstructUse varuse, ScopeInfo localscope)
        {
            if (varuse == null)
                throw new ArgumentNullException("varuse");

            this.varuse = varuse;
            this.localscope = localscope;
        }

        /// <summary>
        /// Resolve variable by its name if not yet.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        protected void InitIfNotYet(ProjectDeclarations projectdeclarations)
        {
            if (resolvedVariables == null)
            {
                resolvedVariables = new DeclarationList();  // avoid cycling
                resolvedVariables = GetDeclarations.GetDeclarationsByName(projectdeclarations, localscope, false, varuse, null);
            }
        }

        /// <summary>
        /// Get the resolved variable object members.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="result"></param>
        /// <param name="match"></param>
        public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            InitIfNotYet(projectdeclarations);
            foreach (DeclarationInfo resolvedVariable in resolvedVariables)
                resolvedVariable.GetObjectMembers(projectdeclarations, result, match);
        }

        /// <summary>
        /// Get the resolved variable static members.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="result"></param>
        /// <param name="match"></param>
        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            InitIfNotYet(projectdeclarations);
            foreach (DeclarationInfo resolvedVariable in resolvedVariables)
                resolvedVariable.GetStaticMembers(projectdeclarations, result, match);
        }

        /// <summary>
        /// Get some resolved variable label.
        /// </summary>
        /// <returns></returns>
        public override string GetValueText()
        {
            //InitIfNotYet(projectdeclarations);//projectdeclarations not known
            if(resolvedVariables!=null)
            foreach (DeclarationInfo resolvedVariable in resolvedVariables)
                return resolvedVariable.Label;

            return null;
        }
    }

    /// <summary>
    /// Specified expression analyzer for string value.
    /// </summary>
    class StringExpressionAnalyzer:ValueAnalyzer
    {
        private readonly string resolvedvalue;

        private DeclarationInfo resolvedtype = null;
        private readonly ScopeInfo   localscope;

        public StringExpressionAnalyzer(StringLiteral str, ScopeInfo localscope)
        {
            resolvedvalue = (string)str.Value;
            this.localscope = localscope;
        }

        /// <summary>
        /// Try to resolve declaration info from type name in string
        /// </summary>
        /// <param name="projectdeclarations"></param>
        private void TryToResolveType(ProjectDeclarations projectdeclarations)
        {
            if (resolvedtype != null || resolvedvalue == null || resolvedvalue == string.Empty)
                return;

            // "ClassName"
            DeclarationList decls = localscope.GetDeclarationsByName(Utils.MakeQualifiedName(resolvedvalue), projectdeclarations);
            
            if(decls.Count>0)
                resolvedtype = decls[0];
        }

        /// <summary>
        /// Declaration names equals to $var.
        /// </summary>
        /// <param name="projectdeclarations">Project declarations.</param>
        /// <param name="localscope">Current scope.</param>
        /// <param name="declarations">Results.</param>
        public override void GetIndirectIdentifiers(ProjectDeclarations projectdeclarations, ScopeInfo localscope, List<string> declarations)
        {
            if (resolvedvalue != null)
                declarations.Add(resolvedvalue);
        }

        /// <summary>
        /// Members after ::
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="result"></param>
        /// <param name="contains"></param>
        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            // resolvedvalue should be variable name or class name
            TryToResolveType(projectdeclarations);

            if (resolvedtype != null)
            {
                resolvedtype.GetStaticMembers(projectdeclarations, result, match);
            }
        }

        public override string GetValueText()
        {
            return "'" + resolvedvalue + "'";
        }
    }

    #endregion

    /// <summary>
    /// TODO:Analyze possible function parameter values. Offer members.
    /// </summary>
    class ParameterAnalyzer:ValueAnalyzer
    {
        /// <summary>
        /// Expression.
        /// </summary>
        protected readonly FunctionParamInfo param;

        /// <summary>
        /// Local scope.
        /// </summary>
        protected readonly ScopeInfo localscope;

        /// <summary>
        /// List of resolved type declarations of this parameter.
        /// </summary>
        protected DeclarationList resolvedDeclarations = null;

        /// <summary>
        /// Init analyzer.
        /// </summary>
        /// <param name="param">Function parameter.</param>
        /// <param name="localscope">Local scope.</param>
        public ParameterAnalyzer(FunctionParamInfo param, ScopeInfo localscope)
        {
            if (param == null)
                throw new ArgumentNullException("param");

            this.param = param;
            this.localscope = localscope;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="projectdeclarations"></param>
        protected void InitResolvedDeclarations(ProjectDeclarations projectdeclarations)
        {
            if (resolvedDeclarations != null)
                return;

            // type is known
            QualifiedName typename = param.TypeQualifiedName;
            if (typename == null || typename.Name.Value == null)
                return;

            resolvedDeclarations = localscope.GetDeclarationsByName(typename, projectdeclarations);
        }

        // members after ->
        public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            InitResolvedDeclarations(projectdeclarations);

            if (resolvedDeclarations != null)
                foreach (DeclarationInfo decl in resolvedDeclarations)
                {
                    decl.GetObjectMembers(projectdeclarations, result, match);
                }
        }

        // members after ::
        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            InitResolvedDeclarations(projectdeclarations);

            if (resolvedDeclarations != null)
                foreach (DeclarationInfo decl in resolvedDeclarations)
                {
                    decl.GetStaticMembers(projectdeclarations, result, match);
                }
        }

        // expression or parameter type text
        public override string GetValueText()
        {
            return null;
        }
    }
}