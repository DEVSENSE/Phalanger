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
    /// Analyze an expression.
    /// Find possible members after -> and :: etc.
    /// Find possible value.
    /// </summary>
    public class ValueAnalyzer
    {
        #region Create new specified analyzer

        /// <summary>
        /// Empty (default) analyzer.
        /// </summary>
        public static readonly ValueAnalyzer EmptyAnalyzer = new ValueAnalyzer();

        /// <summary>
        /// Create analyzer from the Expression.
        /// </summary>
        /// <param name="value">Expression.</param>
        /// <returns>New analyzer or null.</returns>
        public static ValueAnalyzer Create(Expression value, ScopeInfo localscope)
        {
            try
            {
                // Try to resolve value expression pattern.
                if (value != null)
                {
                    // NEW {type} == NewEx
                    NewEx newex = value as NewEx;
                    if (newex != null)
                    {
                        return new NewExpressionAnalyzer(newex, localscope);
                    }

                    // $VARIABLE == DirectVarUse
                    VarLikeConstructUse varuse = value as VarLikeConstructUse;
                    if (varuse != null)
                    {
                        return new VariableExpressionAnalyzer(varuse, localscope);
                    }
                    
                    // "..." == StringLiteral
                    StringLiteral stringliteral = value as StringLiteral;
                    if (stringliteral != null)
                    {
                        return new StringExpressionAnalyzer(stringliteral, localscope);
                    }

                    // TODO: BinaryEx
                }
            }
            catch (Exception)
            { }

            return null;
        }

        /// <summary>
        /// Create analyzer of function parameter.
        /// </summary>
        /// <param name="param">Function parameter.</param>
        /// <param name="localscope">Local scope.</param>
        /// <returns>New analyzer or null.</returns>
        public static ValueAnalyzer Create(FunctionParamInfo param, ScopeInfo localscope)
        {
            if (param != null)
                return new ParameterAnalyzer(param, localscope);

            return null;
        }

        /// <summary>
        /// Create analyzer from assembly FieldInfo (constant declaration).
        /// </summary>
        /// <param name="field">FieldInfo, with GetValue method</param>
        /// <returns>new Value analyzer or null.</returns>
        public static ValueAnalyzer Create( FieldInfo field )
        {
            if (field != null)
                return new FieldInfoAnalyzer(field);

            return null;
        }

        #endregion

        #region Analyzer methods

        /// <summary>
        /// List of public members visible after -> in IntelliSence.
        /// </summary>
        /// <param name="projectdeclarations">All project declarations.</param>
        /// <param name="result">Output list of visible members.</param>
        /// <param name="contains">String contained in returned declarations.</param>
        public virtual void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {

        }

        /// <summary>
        /// List of public members visible after :: in IntelliSence.
        /// </summary>
        /// <param name="projectdeclarations">All project declarations.</param>
        /// <param name="result">Output list of constant visible members.</param>
        /// <param name="contains">String contained in returned declarations.</param>
        public virtual void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {

        }

        /// <summary>
        /// Declaration names equals to $var.
        /// </summary>
        /// <param name="projectdeclarations">Project declarations.</param>
        /// <param name="localscope">Current scope.</param>
        /// <param name="declarations">Results.</param>
        public virtual void GetIndirectIdentifiers(ProjectDeclarations projectdeclarations, ScopeInfo localscope, List<string> declarations)
        {
            
        }

        /// <summary>
        /// Get list of declarations describing the array item usage.
        /// </summary>
        /// <param name="projectdeclarations"></param>
        /// <param name="localscope"></param>
        /// <param name="result"></param>
        public virtual void GetArrayDeclarations(ProjectDeclarations projectdeclarations, DeclarationList result)
        {

        }

        /// <summary>
        /// Value "string".
        /// Default is null (not known value).
        /// </summary>
        /// <returns>Value "string".</returns>
        public virtual string GetValueText()
        {
            return null;
        }

        #endregion
    }
}