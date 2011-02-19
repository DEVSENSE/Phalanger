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

using PHP.VisualStudio.PhalangerLanguageService.Declarations;

namespace PHP.VisualStudio.PhalangerLanguageService.Scopes
{
    /// <summary>
    /// Specials (native) methods and variables not defined elsewhere.
    /// </summary>
    /// <remarks>
    /// This code scope is included by all the other scopes through the special namespace name. So the declarations here are visible everywhere.
    /// </remarks>
    public class SpecialScope : ScopeInfo
    {
        private readonly ProjectDeclarations projectdeclarations;

        public SpecialScope(ProjectDeclarations projectdeclarations)
            : base(null, new TextSpan())
        {
            this.projectdeclarations = projectdeclarations;
        }

        /// <summary>
        /// Create the special namespaces (Library) scope and
        /// places special (PHP native) keywords and functions into this scope.
        /// </summary>
        protected override void InitScope()
        {
            ScopeInfo scope = InitNamespaceDecl(Namespaces.Library);

            // PHP native functions
            scope.AddDeclaration(new SpecialFunctionDecl(
                "echo", "Output one or more strings.",
                    new FunctionParamInfo[] { 
                        new FunctionParamInfo("arg1", "string", "String to be printed.", new PHP.Core.Parsers.Position()),
                        new FunctionParamInfo("...", "string", "String to be printed.", new PHP.Core.Parsers.Position(), true)
                    },
                false));
            scope.AddDeclaration(new SpecialFunctionDecl("print", "Output a string.", new FunctionParamInfo[] { new FunctionParamInfo("arg", "string", "String to be printed.", new PHP.Core.Parsers.Position()) }, true));
            scope.AddDeclaration(new SpecialFunctionDecl("new", "Create new object instance of the specified type.", new FunctionParamInfo[] { new FunctionParamInfo("object", "string", "Object to be created.", new PHP.Core.Parsers.Position()) }, false));
            scope.AddDeclaration(new SpecialFunctionDecl("clone", null, new FunctionParamInfo[] { new FunctionParamInfo("variable", "string", "Object to be cloned.", new PHP.Core.Parsers.Position()) }, false));
            scope.AddDeclaration(new SpecialFunctionDecl("include", "The include() statement includes and evaluates the specified file.", new FunctionParamInfo[] { new FunctionParamInfo("file_name", "string", "File to be included.", new PHP.Core.Parsers.Position()) }, false));
            scope.AddDeclaration(new SpecialFunctionDecl("include_once", "Includes and evaluates the specified file.\nIf the code from a file has already been included, it will not be included again.", new FunctionParamInfo[] { new FunctionParamInfo("file_name", "string", "File to be included.", new PHP.Core.Parsers.Position()) }, false));
            scope.AddDeclaration(new SpecialFunctionDecl("require", "The require() statement includes and evaluates the specified file.", new FunctionParamInfo[] { new FunctionParamInfo("file_name", "string", "File to be included.", new PHP.Core.Parsers.Position()) }, false));
            scope.AddDeclaration(new SpecialFunctionDecl("require_once", "Includes and evaluates the specified file.\nIf the code from a file has already been included, it will not be included again.", new FunctionParamInfo[] { new FunctionParamInfo("file_name", "string", "File to be included.", new PHP.Core.Parsers.Position()) }, false));

            scope.AddDeclaration(new SpecialFunctionDecl("return", null, new FunctionParamInfo[] { new FunctionParamInfo("value", "mixed", "Return value.", new PHP.Core.Parsers.Position(), true) }, false));

            scope.AddDeclaration(new SpecialFunctionDecl("global", "Use variables from global scope in local scope.",
                new FunctionParamInfo[] { 
                            new FunctionParamInfo("var1", "identifier", "Variable to be used from global scope.", new PHP.Core.Parsers.Position()),
                            new FunctionParamInfo("...", "identifiers", "Variable to be used from global scope.", new PHP.Core.Parsers.Position(), true)
                        }, false));

            scope.AddDeclaration(new SpecialFunctionDecl("isset", "Determine whether a variable is set.",
                new FunctionParamInfo[] { 
                            new FunctionParamInfo("var", "mixed", "The variable to be checked.", new PHP.Core.Parsers.Position()),
                            new FunctionParamInfo("...", "mixed", "Another variable.", new PHP.Core.Parsers.Position(), true)
                        }, true));
            scope.AddDeclaration(new SpecialFunctionDecl("unset", "unset() destroys the specified variables.",
                new FunctionParamInfo[] { 
                            new FunctionParamInfo("var", "mixed", "The variable to be checked.", new PHP.Core.Parsers.Position()),
                            new FunctionParamInfo("...", "mixed", "Another variable.", new PHP.Core.Parsers.Position(), true)
                        }, true));

            scope.AddDeclaration(new SpecialFunctionDecl("empty", "Determine whether a variable is considered to be empty.",
                new FunctionParamInfo[] { 
                            new FunctionParamInfo("var", "mixed", "Variable to be checked.", new PHP.Core.Parsers.Position())
                        }, true));

            // PHP operators
            scope.AddDeclaration(new SpecialKeywordDecl("instanceof", null, DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));
            scope.AddDeclaration(new SpecialKeywordDecl("and", null, DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));
            scope.AddDeclaration(new SpecialKeywordDecl("or", null, DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));
            scope.AddDeclaration(new SpecialKeywordDecl("xor", null, DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));

            // keywords
            scope.AddDeclaration(new SpecialKeywordDecl("true", null, DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));
            scope.AddDeclaration(new SpecialKeywordDecl("false", null, DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));
            scope.AddDeclaration(new SpecialKeywordDecl("null", null, DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));

            scope.AddDeclaration(new SpecialKeywordDecl("import", "Imports specified namespaces.", DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));
            scope.AddDeclaration(new SpecialKeywordDecl("namespace", null, DeclarationInfo.DeclarationUsages.ThisScope | DeclarationInfo.DeclarationUsages.AllChildScopes, null));

            // int, string, array, ...
            scope.AddDeclaration(new ClassAliasDecl("int", string.Format("System{0}Int32", QualifiedName.Separator), projectdeclarations));
            scope.AddDeclaration(new ClassAliasDecl("string", string.Format("System{0}String", QualifiedName.Separator), projectdeclarations));
            scope.AddDeclaration(new SpecialFunctionDecl("array", null,
                new FunctionParamInfo[] { 
                            new FunctionParamInfo("item", "mixed", "Items,", new PHP.Core.Parsers.Position(), true)
                        }, true));
        }

    }
}