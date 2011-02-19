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

namespace PHP.VisualStudio.PhalangerLanguageService.Analyzer
{
    /// <summary>
    /// Get visible members of TypeDeclScope.
    /// </summary>
    class TypeAnalyzer : ValueAnalyzer
    {
        private readonly ClassScope/*!*/decl;

        public TypeAnalyzer(ClassScope/*!*/decl)
        {
            this.decl = decl;
        }

        /// <summary>
        /// <b>public static</b> Members.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="result">Will be filled with <b>public static</b> Members.</param>
        /// <param name="contains"></param>
        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            decl.SelectDeclarations(
                result,
                DeclarationInfo.DeclarationTypes.Variable| DeclarationInfo.DeclarationTypes.Constant| DeclarationInfo.DeclarationTypes.Function,
                0, DeclarationInfo.DeclarationVisibilities.Public,
                match, ScopeInfo.SelectStaticDeclaration.StaticOnly);
        }

        public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            decl.SelectDeclarations(
                result,
                DeclarationInfo.DeclarationTypes.Variable| DeclarationInfo.DeclarationTypes.Constant| DeclarationInfo.DeclarationTypes.Function,
                0, DeclarationInfo.DeclarationVisibilities.Public,
                match, ScopeInfo.SelectStaticDeclaration.NotStatic);
        }
    }

    /// <summary>
    /// Get visible members of AssemblyTypeDeclScope.
    /// </summary>
    class AssemblyClassAnalyzer : ValueAnalyzer
    {
        private readonly DeclarationInfo/*!*/decl;

        public AssemblyClassAnalyzer(DeclarationInfo/*!*/decl)
        {
            this.decl = decl;
        }

        public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (decl != null && decl.DeclarationScope != null)
            decl.DeclarationScope.SelectDeclarations(
                result,
                DeclarationInfo.DeclarationTypes.Variable | DeclarationInfo.DeclarationTypes.Constant | DeclarationInfo.DeclarationTypes.Function,
                0, DeclarationInfo.DeclarationVisibilities.Public,
                match, ScopeInfo.SelectStaticDeclaration.NotStatic);
        }

        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (decl != null && decl.DeclarationScope != null)
            decl.DeclarationScope.SelectDeclarations(
                result,
                DeclarationInfo.DeclarationTypes.Variable | DeclarationInfo.DeclarationTypes.Constant | DeclarationInfo.DeclarationTypes.Function,
                0, DeclarationInfo.DeclarationVisibilities.Public,
                match, ScopeInfo.SelectStaticDeclaration.StaticOnly);   // public static members only
        }
    }

    /// <summary>
    /// Analyze members of instance of System.Type.
    /// </summary>
    class AssemblyTypeInstanceAnalyzer : ValueAnalyzer
    {
        private readonly Type type;

        public AssemblyTypeInstanceAnalyzer(Type type)
        {
            this.type = type;
        }

        protected DeclarationList ResolveType(ProjectDeclarations projectdeclarations)
        {
            if (type == null)
                return null;

            QualifiedName name = Utils.MakeQualifiedName(type.FullName, ".");

            return ScopeInfo.GetDeclarationsByName(name, projectdeclarations, null);
        }

        public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            DeclarationList decls = ResolveType(projectdeclarations);
            if (decls != null)
                foreach (DeclarationInfo decl in decls)
                    decl.GetObjectMembers(projectdeclarations, result, match);
        }

        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            DeclarationList decls = ResolveType(projectdeclarations);
            if (decls != null)
                foreach (DeclarationInfo decl in decls)
                    decl.GetStaticMembers(projectdeclarations, result, match);
        }
    }

    /// <summary>
    /// $this
    /// </summary>
    class ThisAnalyzer : ValueAnalyzer
    {
        private ScopeInfo/*!*/typescope;
        public ThisAnalyzer(ScopeInfo/*!*/typescope)
        {
            this.typescope = typescope;
        }

        public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            typescope.SelectDeclarations(
                result,
                DeclarationInfo.DeclarationTypes.Constant | DeclarationInfo.DeclarationTypes.Function | DeclarationInfo.DeclarationTypes.Variable,
                0, 0, match, ScopeInfo.SelectStaticDeclaration.NotStatic);
        }
        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            typescope.SelectDeclarations(
                result,
                DeclarationInfo.DeclarationTypes.Constant | DeclarationInfo.DeclarationTypes.Function | DeclarationInfo.DeclarationTypes.Variable,
                0, 0, match, ScopeInfo.SelectStaticDeclaration.StaticOnly);
        }
    }

    /// <summary>
    /// self
    /// </summary>
    class SelfAnalyzer : ValueAnalyzer
    {
        private ScopeInfo/*!*/typescope;
        public SelfAnalyzer(ScopeInfo/*!*/typescope)
        {
            this.typescope = typescope;
        }

        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            typescope.SelectDeclarations(
                result,
                DeclarationInfo.DeclarationTypes.Constant | DeclarationInfo.DeclarationTypes.Function | DeclarationInfo.DeclarationTypes.Variable,
                0, 0, match, ScopeInfo.SelectStaticDeclaration.StaticOnly);
        }
    }

    /// <summary>
    /// parent keyword
    /// </summary>
    class ParentAnalyzer : ValueAnalyzer
    {
        private DeclarationInfo basedecl;

        public ParentAnalyzer(DeclarationInfo basedecl)
        {
            this.basedecl = basedecl;
        }

        /// <summary>
        /// public or protected variables,constants,functions
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="result"></param>
        /// <param name="contains"></param>
        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (basedecl != null && basedecl.DeclarationScope != null)
                basedecl.DeclarationScope.SelectDeclarations(
                    result,
                    DeclarationInfo.DeclarationTypes.Constant | DeclarationInfo.DeclarationTypes.Function | DeclarationInfo.DeclarationTypes.Variable,
                    0,
                    DeclarationInfo.DeclarationVisibilities.Public | DeclarationInfo.DeclarationVisibilities.Protected,
                    match, ScopeInfo.SelectStaticDeclaration.StaticOnly);
        }
    }
}