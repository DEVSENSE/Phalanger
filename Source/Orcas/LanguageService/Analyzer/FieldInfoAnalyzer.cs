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

namespace PHP.VisualStudio.PhalangerLanguageService.Analyzer
{
    /// <summary>
    /// Analyze value from assembly FieldInfo.GetValue
    /// </summary>
    class FieldInfoAnalyzer: ValueAnalyzer
    {
        protected object value;

        //protected readonly FieldInfo field;

        /// <summary>
        /// Init the analyzer.
        /// </summary>
        /// <param name="field"></param>
        public FieldInfoAnalyzer( FieldInfo field )
        {
            //this.field = field;

            try
            {
                // constant static value
                value = field.GetValue(null);

                // try to convert to int
                if(value is int)
                    value = (int) value;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Value text.
        /// </summary>
        /// <returns></returns>
        public override string GetValueText()
        {
            // string
            try { return "'" + (string)value + "'"; }
            catch (Exception) { }

            // int or enum: show the value, not the .NET field
            try{return ((int)value).ToString();}
            catch(Exception){}

            // anything else
            return value.ToString();
        }

        public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            
        }
        public override void GetStaticMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            
        }
        
    }
}