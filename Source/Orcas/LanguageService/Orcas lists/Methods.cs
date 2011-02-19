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
using Microsoft.VisualStudio.TextManager.Interop;

using PHP.Core;
using PHP.Core.AST;
using PHP.Core.Reflection;

using Microsoft.VisualStudio.Package;
using System.Diagnostics.CodeAnalysis;

using PHP.VisualStudio.PhalangerLanguageService.Declarations;

namespace PHP.VisualStudio.PhalangerLanguageService.OrcasLists
{
    /// <summary>
    /// Implementation of the list of methods with their parameters.
    /// </summary>
    class PhpMethods:Methods
    {
        /// <summary>
        /// Temporary class for special alternatives of function.
        /// </summary>
        private class DeclInfoAndScope
        {
            public readonly DeclarationInfo declaration;
            public readonly int ParamsCount;
            
            public DeclInfoAndScope(DeclarationInfo declaration, int ParamsCount)
            {
                this.declaration = declaration;
                this.ParamsCount = ParamsCount;
            }
        }

        private List<DeclInfoAndScope> methods = new List<DeclInfoAndScope>();

        /// <summary>
        /// Compare object parameters of two declarations.
        /// </summary>
        /// <param name="decl1"></param>
        /// <param name="decl2"></param>
        /// <returns>True if object parameters looks same.</returns>
        private bool HasSameParameters(DeclarationInfo/*!*/decl1, DeclarationInfo/*!*/decl2)
        {
            if (decl1.ObjectParameters.Count != decl2.ObjectParameters.Count)
                return false;

            for(int i = 0; i < decl1.ObjectParameters.Count; ++i)
            {
                if (decl1.ObjectParameters[i].TypeName != decl2.ObjectParameters[i].TypeName ||
                    decl1.ObjectParameters[i].Name != decl2.ObjectParameters[i].Name)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Add method to the list of methods.
        /// </summary>
        /// <param name="decl">Method to be added.</param>
        /// <remarks>Also methods from MembersWithParameters are included. Duplicities are ignored.</remarks>
        public void AddMethod( DeclarationInfo decl )
        {
            // add all alternatives of this function
            List<FunctionParamInfo> parameters = decl.ObjectParameters;

            if (parameters != null)
            {
                // skip possible duplicities
                foreach (DeclInfoAndScope m in methods)
                {
                    if (m.declaration.IsSameAs(decl) &&
                        m.declaration.FullName == decl.FullName &&
                        HasSameParameters(m.declaration,decl))
                        return;
                }

                // add parameters list
                int ParamsCount = parameters.Count;

                while (ParamsCount >= 0)
                {
                    methods.Add(new DeclInfoAndScope(decl, ParamsCount));

                    if ( ParamsCount > 0 )
                    {
                        if (!parameters[ParamsCount - 1].IsOptional)
                            break;
                    }

                    --ParamsCount;
                }                
            }

            // add alternative parameter lists
            DeclarationList altdecls = decl.MembersWithParameters;
            if (altdecls != null)
                foreach (DeclarationInfo altdecl in altdecls)
                    AddMethod(altdecl);
        }

        /// <summary>
        /// Parameters list open bracket.
        /// </summary>
        public override string OpenBracket
        {
            get
            {
                return base.OpenBracket;
            }
        }

        /// <summary>
        /// Parameters list close bracket.
        /// </summary>
        public override string CloseBracket
        {
            get
            {
                return base.CloseBracket;
            }
        }

        /// <summary>
        /// Default method index.
        /// </summary>
        public override int DefaultMethod
        {
            get
            {
                return base.DefaultMethod;
            }
        }

        /// <summary>
        /// Parameters delimiter.
        /// </summary>
        public override string Delimiter
        {
            get
            {
                return base.Delimiter;
            }
        }

        /// <summary>
        /// Methods count.
        /// </summary>
        /// <returns>Methods count.</returns>
        public override int GetCount()
        {
            return methods.Count;
        }

        /// <summary>
        /// Description of the specified method.
        /// </summary>
        /// <param name="index">Method index.</param>
        /// <returns>Description of the specified method.</returns>
        public override string GetDescription(int index)
        {
            return methods[index].declaration.GetDescription();
        }

        /// <summary>
        /// Name of the specified method.
        /// </summary>
        /// <param name="index">Method index.</param>
        /// <returns>Name of the specified method.</returns>
        public override string GetName(int index)
        {
            return methods[index].declaration.Label;
        }

        /// <summary>
        /// Parameters count of the specified method.
        /// </summary>
        /// <param name="index">Method index.</param>
        /// <returns>Parameters count of the specified method.</returns>
        public override int GetParameterCount(int index)
        {
            return methods[index].ParamsCount;
        }

        /// <summary>
        /// Get the info about the specified parameter.
        /// </summary>
        /// <param name="index">Method index.</param>
        /// <param name="parameter">Method parameter index.</param>
        /// <param name="name">Parameter name.</param>
        /// <param name="display">Parameter displayed name.</param>
        /// <param name="description">Parameter description.</param>
        public override void GetParameterInfo(int index, int parameter, out string name, out string display, out string description)
        {
            List<FunctionParamInfo> parameters;

            if ((parameters = methods[index].declaration.ObjectParameters) != null)
            {
                FunctionParamInfo p = parameters[parameter];

                name = p.Name;
                display = (name != null) ? "$" + name : null;
                description = p.Description;

                string typename = p.TypeName;
                if (typename != null)
                    display = typename + ((display != null) ? (" " + display) : null);
            }
            else
            {
                name = display = description = null;
            }            
        }

        /// <summary>
        /// Get the type name of the specified method.
        /// </summary>
        /// <param name="index">Method index.</param>
        /// <returns>Type name or null.</returns>
        public override string GetType(int index)
        {
            DeclarationInfo info = methods[index].declaration;
            return null;// throw new NotImplementedException();
        }

        public override string TypePostfix
        {
            get
            {
                return null;
            }
        }

        public override string TypePrefix
        {
            get
            {
                return null;
            }
        }

        public override bool TypePrefixed
        {
            get
            {
                return true;// base.TypePrefixed;
            }
        }

    }
}
