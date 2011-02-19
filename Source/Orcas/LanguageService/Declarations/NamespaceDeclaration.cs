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

namespace PHP.VisualStudio.PhalangerLanguageService.Declarations
{
    /// <summary>
    /// Namespace declaration with statements.
    /// </summary>
    public class NamespaceDeclaration:DeclarationInfo
    {
        /// <summary>
        /// Init namespace declaration with statements.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="scope"></param>
        /// <param name="parentscope"></param>
        public NamespaceDeclaration( string Name, NamespaceDeclScope scope, ScopeInfo parentscope )
            :base(Name, new TextSpan(), 90, null, parentscope)
        {
            this.DeclarationScope = scope;
        }

        /// <summary>
        /// Namespace type.
        /// </summary>
        public override DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Namespace;
            }
        }

        /// <summary>
        /// Namespace full name.
        /// </summary>
        public override string FullName
        {
            get
            {
                return (DeclarationScope != null) ? (DeclarationScope.FullName) : Label;
            }
        }

        public override string GetDescription()
        {
            return "namespace " + FullName;
        }

        /// <summary>
        /// Get namespaces and classes declared in DeclarationScope.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="result"></param>
        /// <param name="match"></param>
        public override void GetTypeMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (DeclarationScope != null)
                DeclarationScope.SelectDeclarations(result, DeclarationTypes.Namespace | DeclarationTypes.Class, 0, 0, match, ScopeInfo.SelectStaticDeclaration.Any);
        }
    }

    /// <summary>
    /// Namespace declaration containing another namespace.
    /// </summary>
    public class NamespacePartDeclaration:DeclarationInfo
    {
        /// <summary>
        /// Member.
        /// </summary>
        private readonly DeclarationInfo MemberNamespace;

        /// <summary>
        /// Namespace FullName.
        /// </summary>
        private readonly string NamespaceFullName;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="FullName"></param>
        /// <param name="MemberNamespace"></param>
        /// <param name="parentscope"></param>
        public NamespacePartDeclaration(string Name, string FullName, DeclarationInfo MemberNamespace, ScopeInfo parentscope)
            :base(Name,new TextSpan(), 90, null, parentscope)
        {
            this.MemberNamespace = MemberNamespace;
            this.NamespaceFullName = FullName;
        }

        /// <summary>
        /// Namespace type.
        /// </summary>
        public override DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Namespace;
            }
        }

        /// <summary>
        /// Namespace full name.
        /// </summary>
        public override string FullName
        {
            get
            {
                return NamespaceFullName;
            }
        }

        public override string GetDescription()
        {
            return "namespace " + FullName;
        }

        /// <summary>
        /// Get namespaces and classes declared in this namespace.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="result"></param>
        /// <param name="match"></param>
        public override void GetTypeMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (MemberNamespace != null && match.Matches(MemberNamespace))
                result.Add(MemberNamespace);
        }
    }
}