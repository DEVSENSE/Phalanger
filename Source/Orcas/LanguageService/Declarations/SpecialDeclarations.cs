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

using OrcasLists = PHP.VisualStudio.PhalangerLanguageService.OrcasLists;
using PHP.VisualStudio.PhalangerLanguageService.Analyzer;

namespace PHP.VisualStudio.PhalangerLanguageService.Declarations
{
    /// <summary>
    /// Declaration of snippet to be compatible with other declarations displayed in Intellisense drop-down list.
    /// </summary>
    public class SnippedDecl : DeclarationInfo
    {
        private readonly string shortcut;

        public SnippedDecl(VsExpansion expansionInfo)
            : base(expansionInfo.title, new TextSpan(), 205, expansionInfo.description, null)
        {
            shortcut = expansionInfo.shortcut;
        }

        public override string GetDescription()
        {
            return description;
        }

        public override string FullName
        {
            get
            {
                // FullName in special format, so we can recognize it as snippet shortcut.
                return OrcasLists.PhpDeclarations.SnippetsFullNameStartsWith + shortcut;
            }
        }
    }

    /// <summary>
    /// Special functions, not defined elsewhere.
    /// echo, include, ...
    /// </summary>
    public class SpecialFunctionDecl : FunctionDeclaration
    {
        /// <summary>
        /// Parameters are enclosed in bracket.
        /// </summary>
        private readonly bool ParametersInBracket;

        /// <summary>
        /// Function info initialization.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="description"></param>
        /// <param name="parameters"></param>
        public SpecialFunctionDecl(string name, string description, IEnumerable<FunctionParamInfo> parameters, bool ParametersInBracket)
            : base(name, description, 78, null)
        {
            this.ParametersInBracket = ParametersInBracket;

            if (parameters != null)
                foreach (FunctionParamInfo p in parameters)
                    AddParameter(p);
        }

        protected override string OpenBracket
        {
            get
            {
                return ParametersInBracket ? "(" : " ";
            }
        }
        protected override string CloseBracket
        {
            get
            {
                return ParametersInBracket ? ")" : " ";
            }
        }
    }

    /// <summary>
    /// A keyword declaration, described by its name and "keyword" type. Custom analyzer is used.
    /// </summary>
    public class SpecialKeywordDecl : DeclarationInfo
    {
        readonly DeclarationUsages usage;

        public SpecialKeywordDecl(string name, string description, DeclarationUsages usage, ValueAnalyzer value)
            : base(name, new TextSpan(), 206, description, null)
        {
            this.usage = usage;

            AddAnalyzer(value);
        }

        public override DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Keyword;
            }
        }

        public override string GetDescription()
        {
            return description;
        }

        public override DeclarationUsages DeclarationUsage
        {
            get
            {
                return usage;
            }
        }
    }

}