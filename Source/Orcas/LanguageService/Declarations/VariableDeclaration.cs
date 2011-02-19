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
using PHP.VisualStudio.PhalangerLanguageService.Analyzer;

namespace PHP.VisualStudio.PhalangerLanguageService.Declarations
{
    /// <summary>
    /// Declaration of variable (global, local, property).
    /// </summary>
    public class VariableDeclaration : DeclarationInfo
    {
        /// <summary>
        /// field is static
        /// </summary>
        protected bool _IsStatic = false; // default

        /// <summary>
        /// Display visibility in description.
        /// </summary>
        protected bool DisplayVisibilityBeforeName = false;

        /// <summary>
        /// Variable visibility in local scopes.
        /// </summary>
        protected DeclarationUsages _usage;

        /// <summary>
        /// field visibility
        /// </summary>
        protected DeclarationVisibilities _Visibility;

        /// <summary>
        /// Init of PHP property.
        /// </summary>
        /// <param name="field"></param>
        public VariableDeclaration(FieldDecl/*!*/field, PhpMemberAttributes attributes, ScopeInfo parentscope)
            : base(field.Name.ToString(), Utils.PositionToSpan(field.Position), 102, null, parentscope)
        {
            this._IsStatic = IsMemberStatic(attributes);
            this._Visibility = GetMemberVisibility(attributes);
            this._usage = DeclarationUsages.ThisScope | DeclarationUsages.UntilFunctionOrClass;
            this.DisplayVisibilityBeforeName = true;

            AddAnalyzer(ValueAnalyzer.Create(field.Initializer, parentscope));
        }

        /// <summary>
        /// Init of PHP variable.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="visibility"></param>
        /// <param name="position"></param>
        /// <param name="description"></param>
        public VariableDeclaration(string name, DeclarationVisibilities visibility, PHP.Core.Parsers.Position position, string description, ValueAnalyzer value, ScopeInfo parentscope)
            : base(name, Utils.PositionToSpan(position), 42, description, parentscope)
        {
            this._IsStatic = false;
            this._Visibility = visibility;
            this._usage = DeclarationUsages.ThisScope | DeclarationUsages.UntilFunctionOrClass;

            AddAnalyzer(value);
        }

        /// <summary>
        /// Visibility.
        /// </summary>
        public override DeclarationVisibilities DeclarationVisibility
        {
            get
            {
                return _Visibility;
            }
        }

        /// <summary>
        /// Usage.
        /// </summary>
        public override DeclarationInfo.DeclarationUsages DeclarationUsage
        {
            get
            {
                return _usage;
            }
        }

        /// <summary>
        /// Is static.
        /// </summary>
        public override bool IsStatic
        {
            get
            {
                return _IsStatic;
            }
        }

        /// <summary>
        /// Variable declaration.
        /// </summary>
        public override DeclarationTypes  DeclarationType
        {
            get
            {
                return DeclarationTypes.Variable;
            }
        }

        /// <summary>
        /// Variable description.
        /// </summary>
        /// <returns>Variable and property description.</returns>
        public override string GetDescription()
        {
            List<string> values = GetResolvedValues();

            return
                (DisplayVisibilityBeforeName ? (DeclarationVisibility.ToString().ToLower() + " ") : null) +
                (IsStatic ? "static " : null) +
                ("$" + Label) +
                ((values != null && values.Count > 0) ? (" = " + values[0]) : null) +
                ((description != null) ? ("\n" + description) : null);
        }

    }

    /// <summary>
    /// Declaration of variable from the function parameter.
    /// </summary>
    public class ParameterVariableDeclaration: VariableDeclaration
    {
        public ParameterVariableDeclaration(string name, PHP.Core.Parsers.Position position, string description, ValueAnalyzer value, ScopeInfo parentscope)
            :base(name, DeclarationVisibilities.Private, position, description, value, parentscope)
        {

        }

        /// <summary>
        /// Modified declaration description.
        /// </summary>
        /// <returns></returns>
        public override string GetDescription()
        {
            return "(parameter) " + base.GetDescription();
        }
    }

    /// <summary>
    /// "This" variable declaration.
    /// </summary>
    public class ThisVariableDeclaration : VariableDeclaration
    {
        public ThisVariableDeclaration(ValueAnalyzer value, ScopeInfo parentscope)
            : base("this", DeclarationVisibilities.Private, new PHP.Core.Parsers.Position(), "This.", value, parentscope)
        {
            this._usage = DeclarationUsages.ThisScope | DeclarationUsages.AllChildScopes;
        }
    }

}