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
    /// Constant declaration.
    /// Describes PHP constant declarations, its name and value.
    /// </summary>
    public class ConstantDeclaration : DeclarationInfo
    {
        /// <summary>
        /// Constant name is case insensitive.
        /// </summary>
        //protected bool caseinsensitive = false;

        /// <summary>
        /// Constant declaration.
        /// </summary>
        private ConstantDecl constdecl;

        /// <summary>
        /// Visibility.
        /// </summary>
        protected DeclarationVisibilities visibility;

        /// <summary>
        /// Init constant declaration info.
        /// </summary>
        /// <param name="constant">Php constant.</param>
        /// <param name="attributes">Constant declaration attributes.</param>
        /// <param name="parentscope">Parent code scope.</param>
        public ConstantDeclaration(ConstantDecl/*!*/constant, PhpMemberAttributes attributes, ScopeInfo parentscope)
            : base(constant.Name.ToString(), Utils.PositionToSpan(constant.Position), 10, null, parentscope)
        {
            this.constdecl = constant;

            this.visibility = GetMemberVisibility(attributes);

            AddAnalyzer(ValueAnalyzer.Create(constant.Initializer, parentscope));
        }

        /// <summary>
        /// Accessibility modifier of the constant declaration.
        /// </summary>
        public override DeclarationVisibilities DeclarationVisibility
        {
            get
            {
                return visibility;
            }
        }

        /// <summary>
        /// Constant is static, always true.
        /// </summary>
        public override bool IsStatic
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Constant declaration description.
        /// </summary>
        /// <returns>Constant name and its value.</returns>
        public override string GetDescription()
        {
            List<string> values = GetResolvedValues();

            return string.Format("const {0} = {1}", Label, (values != null && values.Count > 0) ? values[0] : "?");
        }

        public override DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Constant;
            }
        }

    }
}