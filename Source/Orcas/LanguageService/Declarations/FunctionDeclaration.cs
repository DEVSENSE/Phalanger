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

using PHP.VisualStudio.PhalangerLanguageService.Scopes;

namespace PHP.VisualStudio.PhalangerLanguageService.Declarations
{
    /// <summary>
    /// Declaration of PHP function.
    /// </summary>
    public class FunctionDeclaration : DeclarationInfo
    {
        /// <summary>
        /// Init function declaration.
        /// </summary>
        /// <param name="scope">Function code scope.</param>
        /// <param name="name">Name of the function.</param>
        /// <param name="span">Declaration TextSpan.</param>
        /// <param name="parentscope">Parent code scope.</param>
        public FunctionDeclaration(FunctionScope scope, string name, TextSpan span, ScopeInfo parentscope)
            : base(name, span, 72, null, parentscope)
        {
            this.DeclarationScope = scope;
        }

        /// <summary>
        /// Protected special init.
        /// </summary>
        /// <param name="name">php function name</param>
        /// <param name="description">function description.</param>
        /// <param name="parentscope">Parent scope.</param>
        protected FunctionDeclaration(string name, string description, ScopeInfo parentscope)
            : this(name, description, 72, parentscope)
        {

        }

        /// <summary>
        /// Protected special init.
        /// </summary>
        /// <param name="name">php function name</param>
        /// <param name="description">function description.</param>
        /// <param name="glyph">image index</param>
        /// <param name="parentscope">Parent scope.</param>
        protected FunctionDeclaration(string name, string description, int glyph, ScopeInfo parentscope)
            : base(name, new TextSpan(), glyph, description, parentscope)
        {

        }

        /// <summary>
        /// Init function call parameters.
        /// </summary>
        protected override void InitParameters()
        {
            FunctionScope scope = DeclarationScope as FunctionScope;
            if (scope != null)
                foreach (FunctionParamInfo p in scope.FormalParameters)
                    AddParameter(p);
        }

        /// <summary>
        /// Get function/method description header (before parameters list, after access modifiers)
        /// </summary>
        /// <returns>"function " + function name</returns>
        protected virtual string FunctionHeader
        {
            get
            {
                return "function " + Label;
            }
        }

        /// <summary>
        /// Parameters list start.
        /// </summary>
        protected virtual string OpenBracket
        {
            get
            {
                return "(";
            }
        }

        /// <summary>
        /// Parameters list end.
        /// </summary>
        protected virtual string CloseBracket
        {
            get
            {
                return ")";
            }
        }

        /// <summary>
        /// Function description.
        /// </summary>
        /// <returns>Declaration + Description</returns>
        public override string GetDescription()
        {
            //
            // create description.
            //
            List<FunctionParamInfo> prms = ObjectParameters;

            FunctionScope scope = DeclarationScope as FunctionScope;

            string resultdesc = (/*(scope == null || scope.DisplayStaticAndVisibility) ? "" :*/ (DeclarationVisibility.ToString().ToLower() + (IsStatic ? " static" : "") + " ")) +
                 FunctionHeader + OpenBracket;

            if (prms != null)
            {
                int i;
                int firstoptional = prms.Count;

                for (i = prms.Count - 1; i >= 0; --i)
                {
                    if (!prms[i].IsOptional)
                        break;

                    firstoptional = i;
                }

                for (i = 0; i < prms.Count; ++i)
                {
                    if (firstoptional <= i)
                        resultdesc += "[";

                    if (i > 0) resultdesc += ", ";

                    resultdesc += prms[i].IsParams ? "..." : "$" + prms[i].Name;
                }

                for (; firstoptional < prms.Count; ++firstoptional)
                    resultdesc += "]";
            }

            resultdesc += CloseBracket;

            // description
            InitSummaryDescription();

            if (description != null && description.Length > 0)
                resultdesc += "\n" + description;

            return resultdesc;
        }

        /// <summary>
        /// Parse summary from doc comment and copy to description.
        /// </summary>
        private void InitSummaryDescription()
        {
            FunctionScope scope = DeclarationScope as FunctionScope;
            if (description == null && scope != null)
            {
                description = scope.GetDescription();
            }
        }

        /// <summary>
        /// Type of this declaration (function).
        /// </summary>
        public override DeclarationTypes  DeclarationType
        {
	        get 
	        { 
		        return DeclarationTypes.Function;
	        }
        }

        /// <summary>
        /// Declaration accessibility.
        /// </summary>
        public override DeclarationVisibilities DeclarationVisibility
        {
            get
            {
                FunctionScope scope = DeclarationScope as FunctionScope;
                return (scope != null) ? scope.Visibility : DeclarationVisibilities.Public;
            }
        }

        /// <summary>
        /// Is function declaration static ?
        /// </summary>
        public override bool IsStatic
        {
            get
            {
                FunctionScope scope = DeclarationScope as FunctionScope;
                return (scope != null) ? scope.IsStatic : false;
            }
        }
        
    }
}