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
using PHP.VisualStudio.PhalangerLanguageService.Analyzer;

namespace PHP.VisualStudio.PhalangerLanguageService.Declarations
{
    /// <summary>
    /// Declaration of class equals to another class declaration.
    /// Properties of this alias are same as referred class properties.
    /// </summary>
    public class ClassAliasDecl : DeclarationInfo
    {
        /// <summary>
        /// Resolve the declaration by full php name.
        /// </summary>
        class AliasResolver
        {
            private readonly ProjectDeclarations projectsdeclarations;
            private readonly string ClassFullName;

            /// <summary>
            /// Resolved Declaration from the ClassName.
            /// </summary>
            private DeclarationInfo resolveddecl = null;

            public AliasResolver(string ClassFullName, ProjectDeclarations projectsdeclarations)
            {
                this.ClassFullName = ClassFullName;
                this.projectsdeclarations = projectsdeclarations;
            }

            /// <summary>
            /// Get resolved declaration. Can be null.
            /// </summary>
            /// <returns>Resolved declaration.</returns>
            public DeclarationInfo ResolveDeclaration()
            {
                if (resolveddecl == null)
                {
                    DeclarationList decls = ScopeInfo.GetDeclarationsByName(Utils.MakeQualifiedName(ClassFullName), projectsdeclarations, null);

                    if (decls != null && decls.Count > 0)
                        resolveddecl = decls[0];
                }

                return resolveddecl;
            }
        }

        /// <summary>
        /// Analyzer of the alias.
        /// Members are copy of the origin class members.
        /// </summary>
        class AliasAnalyzer : ValueAnalyzer
        {
            private readonly AliasResolver alias;

            public AliasAnalyzer(AliasResolver alias)
            {
                this.alias = alias;
            }

            public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
            {
                DeclarationInfo decl = alias.ResolveDeclaration();
                if (decl != null)
                    decl.GetObjectMembers(projectdeclarations, result, match);
            }
            public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
            {
                DeclarationInfo decl = alias.ResolveDeclaration();
                if (decl != null)
                    decl.GetStaticMembers(projectdeclarations, result, match);
            }
        }

        private readonly AliasResolver alias;

        /// <summary>
        /// Init the class alias declaration.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="ClassFullName"></param>
        /// <param name="projectsdeclarations"></param>
        public ClassAliasDecl(string name, string ClassFullName, ProjectDeclarations projectsdeclarations)
            : base(name, new TextSpan(), 0, null, null)
        {
            alias = new AliasResolver(ClassFullName, projectsdeclarations);
            AddAnalyzer(new AliasAnalyzer(alias));
        }

        public override DeclarationUsages DeclarationUsage
        {
            get
            {
                DeclarationInfo decl = alias.ResolveDeclaration();
                if (decl != null)
                    return decl.DeclarationUsage;
                return base.DeclarationUsage;
            }
        }
        public override DeclarationInfo.DeclarationVisibilities DeclarationVisibility
        {
            get
            {
                DeclarationInfo decl = alias.ResolveDeclaration();
                if (decl != null)
                    return decl.DeclarationVisibility;
                return base.DeclarationVisibility;
            }
        }
        public override string FileName
        {
            get
            {
                DeclarationInfo decl = alias.ResolveDeclaration();
                if (decl != null)
                    return decl.FileName;
                return base.FileName;
            }
        }
        public override string FullName
        {
            get
            {
                DeclarationInfo decl = alias.ResolveDeclaration();
                if (decl != null)
                    return decl.FullName;
                return base.FullName;
            }
        }
        public override string GetDescription()
        {
            DeclarationInfo decl = alias.ResolveDeclaration();
            if (decl != null)
                return decl.GetDescription();
            return base.GetDescription();
        }
        public override void GetIndirectIdentifiers(ProjectDeclarations projectdeclarations, ScopeInfo localscope, List<string> declarations)
        {
            DeclarationInfo decl = alias.ResolveDeclaration();
            if (decl != null)
                decl.GetIndirectIdentifiers(projectdeclarations, localscope, declarations);
        }
        public override void GetTypeMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            DeclarationInfo decl = alias.ResolveDeclaration();
            if (decl != null)
                decl.GetTypeMembers(projectdeclarations, result, match);
        }
        public override bool IsStatic
        {
            get
            {
                DeclarationInfo decl = alias.ResolveDeclaration();
                if (decl != null)
                    return decl.IsStatic;
                return base.IsStatic;
            }
        }
        protected override void InitParameters()
        {
            DeclarationInfo decl = alias.ResolveDeclaration();
            if (decl != null)
            {
                DeclarationList prms = decl.MembersWithParameters;
                foreach (DeclarationInfo prm in prms)
                    AddMemberWithParameter(prm);

            }
        }
        public override DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Class;
            }
        }

    }
}