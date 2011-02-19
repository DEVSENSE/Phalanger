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
using PHP.VisualStudio.PhalangerLanguageService.Analyzer;
using PHP.VisualStudio.PhalangerLanguageService.Declarations;

namespace PHP.VisualStudio.PhalangerLanguageService.Scopes
{
    /// <summary>
    /// Global code scope.
    /// </summary>
    /// <remarks>
    /// Represents PHP source code, root scope.
    /// </remarks>
    public class GlobalCodeDeclScope : ScopeInfo
    {
        /// <summary>
        /// File name.
        /// </summary>
        private readonly string _filename;

        /// <summary>
        /// source unit waiting for the first declarations request.
        /// </summary>
        private VirtualSourceFileUnit sourceUnit;

        /// <summary>
        /// Project declarations.
        /// </summary>
        private ProjectDeclarations projectdeclarations;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="sourceUnit"></param>
        /// <param name="projectdeclarations"></param>
        public GlobalCodeDeclScope(VirtualSourceFileUnit sourceUnit, ProjectDeclarations projectdeclarations)
            : base(null,new TextSpan())
        {
            this.sourceUnit = sourceUnit;
            this.projectdeclarations = projectdeclarations;
            _filename = sourceUnit.SourceFile.FullPath;

            // code span
            if (sourceUnit.Ast != null && sourceUnit.Ast.Statements != null)
            {
                // try to get code span
                TextSpan newSpan = new TextSpan();

                foreach (Statement stmnt in sourceUnit.Ast.Statements)
                {
                    newSpan.iEndLine = Math.Max(newSpan.iEndLine, stmnt.Position.LastLine);
                    if (newSpan.iEndLine == stmnt.Position.LastLine)
                        newSpan.iEndIndex = Math.Max(newSpan.iEndIndex, stmnt.Position.LastColumn);
                }

                BodySpan = newSpan;
            }
        }

        /// <summary>
        /// Use previously parsed declarations and 
        /// add missing declarations, caused by invalid part of code due editing.
        /// </summary>
        /// <param name="cacheddeclarations"></param>
        public void MergeDeclarations(ScopeInfo cacheddeclarations)
        {
            if (cacheddeclarations == null)
                return;

            List<DeclarationInfo> decls = Declarations;
            List<ScopeInfo> scopes = Scopes;

            // find last parsed line
            int lastline = -1;
            if(decls!=null)
                foreach (DeclarationInfo decl in decls)
                    lastline = Math.Max(decl.Span.iEndLine, lastline);
            if(scopes!=null)
                foreach (ScopeInfo scope in scopes)
                    lastline = Math.Max(scope.BodySpan.iEndLine, lastline);
            
            // add declarations from the cache after last parsed line
            if(cacheddeclarations.Declarations != null)
                foreach (DeclarationInfo decl in cacheddeclarations.Declarations)
                    if (decl.Span.iStartLine > lastline)
                        AddDeclaration(decl);
            if(cacheddeclarations.Scopes != null)
                foreach (ScopeInfo scope in cacheddeclarations.Scopes)
                    if (scope.BodySpan.iStartLine > lastline)
                        AddScope(scope);

            // add included files from the cache
            if (cacheddeclarations.IncludedFiles != null)
                foreach (DeclScopeInclude inc in cacheddeclarations.IncludedFiles)
                    if(inc.Line > lastline)
                        AddIncludedFile(inc);
                    
            // update code span
            BodySpan = cacheddeclarations.BodySpan;
        }

        /// <summary>
        /// Initialize declarations and scopes here.
        /// </summary>
        protected override void InitScope()
        {
            // analyze tree
            if(sourceUnit != null && sourceUnit.Ast != null)
            {
                ScopeAnalyzer analyzer = new ScopeAnalyzer(this, projectdeclarations);
                analyzer.AnalyzeGlobalCode(sourceUnit.Ast);
            }
        }

        /// <summary>
        /// File name.
        /// </summary>
        public override string FileName
        {
            get
            {
                return _filename;
            }
        }

        /// <summary>
        /// Full name of the scope.
        /// It's equal to the file name.
        /// </summary>
        public override string FullName
        {
            get
            {
                return FileName.Substring(FileName.LastIndexOf('\\') + 1);
            }
        }
    }
}