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
    /// Custom value analyzer.
    /// </summary>
    public class CustomAnalyzer:ValueAnalyzer
    {
        /// <summary>
        /// ToString() return value.
        /// </summary>
        public string StringValue = null;

        /// <summary>
        /// List of members accessible throw ->
        /// </summary>
        public DeclarationInfo[] ObjectMembers = null;

        /// <summary>
        /// List of members accessible throw ::
        /// </summary>
        public DeclarationInfo[] StaticMembers = null;

        /// <summary>
        /// List of declarations specifying array item usage.
        /// </summary>
        public DeclarationInfo[] ArrayDeclarations = null;

        /// <summary>
        /// List of identifiers accessible throw $
        /// </summary>
        public string[] IndirectIdentifiers = null;

        public override void GetIndirectIdentifiers(ProjectDeclarations projectdeclarations, ScopeInfo localscope, List<string> declarations)
        {
            if (IndirectIdentifiers != null)
                foreach (string identifier in IndirectIdentifiers)
                    declarations.Add(identifier);
        }

        public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (ObjectMembers != null)
                foreach (DeclarationInfo decl in ObjectMembers)
                    if (match.Matches(decl))
                        result.Add(decl);
        }

        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (StaticMembers != null)
                foreach (DeclarationInfo decl in StaticMembers)
                    if (match.Matches(decl))
                        result.Add(decl);
        }

        public override void GetArrayDeclarations(ProjectDeclarations projectdeclarations, DeclarationList result)
        {
            foreach (DeclarationInfo decl in ArrayDeclarations)
                result.Add(decl);
        }

        public override string ToString()
        {
            return StringValue;
        }
    }
}