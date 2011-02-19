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
    /// Declaration of PHP type.
    /// </summary>
    public class ClassDeclaration : DeclarationInfo
    {
        /// <summary>
        /// Project declarations.
        /// </summary>
        private ProjectDeclarations projectdeclarations;

        /// <summary>
        /// Initialization.
        /// </summary>
        /// <param name="astNode">Ast Node.</param>
        public ClassDeclaration(ClassScope/*!*/classScope, string name, TextSpan span,ProjectDeclarations projectdeclarations)
            : base(name, span, 0, null, classScope.ParentScope)
        {
            this.DeclarationScope = classScope;
            this.projectdeclarations = projectdeclarations;

            AddAnalyzer(new TypeAnalyzer(classScope));
        }


        /// <summary>
        /// Find constructor and copy his function parameters.
        /// </summary>
        private void AddConstructorParameters()
        {
            if (DeclarationScope == null)
                return;

            List<DeclarationInfo> members = DeclarationScope.Declarations;

            DeclarationInfo bestctor = null;

            // ctor and his function parameters
            foreach (DeclarationInfo decl in members)
                // public function __construct
                if (decl.Label == ConstructorName &&
                    decl.DeclarationType == DeclarationTypes.Function &&
                    decl.DeclarationVisibility == DeclarationVisibilities.Public)
                {
                    bestctor = decl;

                    if (bestctor.ParentScope == DeclarationScope)
                        break;  // there cannot be better __constructor
                }

            if (bestctor != null)
            {
                List<FunctionParamInfo> ctorparams = bestctor.ObjectParameters;

                if (ctorparams != null)
                    foreach (FunctionParamInfo p in ctorparams)
                        AddParameter(p);
            }
        }

        protected override void InitParameters()
        {
            AddConstructorParameters();
        }
       
        /// <summary>
        /// Type of this scope.
        /// </summary>
        public override DeclarationInfo.DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Class;
            }
        }

        /// <summary>
        /// Class description.
        /// TODO: class description
        /// </summary>
        /// <returns>Description.</returns>
        public override string GetDescription()
        {
            return "class " + Label;
        }

    }
}