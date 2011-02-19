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
using PHP.VisualStudio.PhalangerLanguageService.Analyzer;

namespace PHP.VisualStudio.PhalangerLanguageService.Declarations
{
    #region Phalanger PHP implementations

    /// <summary>
    /// PHP type declared in an assembly.
    /// </summary>
    public class AssemblyPhpClassDecl : DeclarationInfo
    {
        /// <summary>
        /// Scope with phalanger .net class members.
        /// </summary>
        internal class AssemblyPhpClassScope : ScopeInfo
        {
            /// <summary>
            /// assembly type
            /// </summary>
            private readonly Type assemblyType;

            /// <summary>
            /// scope ctor.
            /// </summary>
            /// <param name="assemblyType"></param>
            /// <param name="parentscope"></param>
            public AssemblyPhpClassScope(Type/*!*/assemblyType, ScopeInfo parentscope)
                : base(parentscope, new TextSpan())
            {
                this.assemblyType = assemblyType;
            }

            /// <summary>
            /// Init class members.
            /// </summary>
            protected override void InitScope()
            {
                // methods, constants, properties
                AssemblyDeclScope.InitClassDeclarations(assemblyType, this);
            }
        }

        /// <summary>
        /// Init phalanger .Net class declaration.
        /// </summary>
        /// <param name="assemblyType"></param>
        /// <param name="parentscope"></param>
        public AssemblyPhpClassDecl(Type assemblyType, ScopeInfo parentscope)
            : base(assemblyType.Name, new TextSpan(), 0, null, parentscope)
        {
            DeclarationScope = new AssemblyPhpClassScope(assemblyType, parentscope);
            AddAnalyzer(new AssemblyClassAnalyzer(this));
        }

        /// <summary>
        /// Description of this class declaration.
        /// </summary>
        /// <returns></returns>
        public override string GetDescription()
        {
            return string.Format("class {0}", Label);
        }

        /// <summary>
        /// Initialize constructor parameters displayed in IntelliSense-methods.
        /// </summary>
        protected override void InitParameters()
        {
            DeclarationList members = new DeclarationList();
            DeclarationScope.SelectDeclarations(members, DeclarationTypes.Function, 0, DeclarationVisibilities.Public, new DeclarationLabelEqual(ConstructorName), ScopeInfo.SelectStaticDeclaration.Any);

            // ctor and his function parameters
            foreach (DeclarationInfo decl in members)
            {
                AddMemberWithParameter(decl);
                break;
            }
        }

        /// <summary>
        /// Class type.
        /// </summary>
        public override DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Class;
            }
        }

    }

    /// <summary>
    /// Function or method declaration from the phalanger class library assembly.
    /// </summary>
    public class AssemblyPhpFunctionDecl : FunctionDeclaration
    {
        /// <summary>
        /// Method Info.
        /// </summary>
        private readonly MethodInfo methodinfo;

        /// <summary>
        /// Init function from the assembly.
        /// </summary>
        /// <param name="methodinfo"></param>
        /// <param name="att"></param>
        public AssemblyPhpFunctionDecl(MethodInfo methodinfo, ImplementsFunctionAttribute att, ScopeInfo parentscope)
            : base(att.Name, null, parentscope)
        {
            // TODO: assembly method description
            this.methodinfo = methodinfo;
        }

        private static ParameterInfo[] GetPublicParameters(MethodInfo/*!*/librarymethod)
        {
            var allParams = librarymethod.GetParameters();
            List<ParameterInfo> realParams = new List<ParameterInfo>(allParams.Length);

            bool internalParametersAllowed = true;

            foreach (var p in allParams)
            {
                if (internalParametersAllowed &&
                    (p.ParameterType == typeof(ScriptContext) ||
                    p.ParameterType == typeof(DTypeDesc) ||
                    p.ParameterType == typeof(NamingContext) ||
                    p.ParameterType == typeof(DObject) ||
                    p.ParameterType == typeof(Dictionary<string, object>))
                )
                {
                    // these parameters are for internal Phalanger use only
                }
                else
                {
                    realParams.Add(p);
                    internalParametersAllowed = false;
                }
            }

            return realParams.ToArray();
        }

        /// <summary>
        /// An alternative to this function was found, try to resolve optional parameters.
        /// </summary>
        /// <param name="methodinfo"></param>
        /// <param name="att"></param>
        public bool ResolveOptionalParameters(MethodInfo methodinfo, ImplementsFunctionAttribute att)
        {
            ParameterInfo[] prms = GetPublicParameters(methodinfo);

            int sameArgsCount = Math.Min(prms.Length, (ObjectParameters != null) ? ObjectParameters.Count : 0);
            for (int i = 0; i < sameArgsCount; ++i)
            {
                if (prms[i].Name != ObjectParameters[i].Name)
                    return false;
            }

            if (ObjectParameters != null && prms.Length < ObjectParameters.Count)
            {
                for (int i = prms.Length; i < ObjectParameters.Count; ++i)
                    ObjectParameters[i].IsOptional = true;
            }
            else if (ObjectParameters == null || prms.Length > ObjectParameters.Count)
            {
                int i = (ObjectParameters == null) ? 0 : ObjectParameters.Count;

                for (; i < prms.Length; ++i)
                {
                    ParameterInfo p = prms[i];
                    AddParameter(new FunctionParamInfo(p.Name, p.ParameterType, null, new PHP.Core.Parsers.Position(), true));
                }
            }

            return true;
        }

        /// <summary>
        /// Init parameters.
        /// </summary>
        protected override void InitParameters()
        {
            // init function PHP parameters.
            ParameterInfo[] prms = GetPublicParameters(methodinfo);

            foreach (ParameterInfo p in prms)
            {
                AddParameter(new FunctionParamInfo(p.Name, p.ParameterType, null, new PHP.Core.Parsers.Position()));
            }
        }

        public override bool IsStatic
        {
            get
            {
                return methodinfo.IsStatic;
            }
        }

        public override DeclarationVisibilities DeclarationVisibility
        {
            get
            {
                if (methodinfo.IsPrivate)
                    return DeclarationVisibilities.Private;
                else
                    return DeclarationVisibilities.Public;
            }
        }
    }

    /// <summary>
    /// Constant declaration from the phalanger class library assembly.
    /// </summary>
    public class AssemblyPhpConstantDecl : DeclarationInfo
    {
        DeclarationVisibilities visibility;
        //bool caseinsensitive = false;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="field">Assembly field.</param>
        /// <param name="c">Constant info.</param>
        /// <param name="parentscope">Parent.</param>
        public AssemblyPhpConstantDecl(FieldInfo field, ImplementsConstantAttribute c, ScopeInfo parentscope)
            : base(c.Name, new TextSpan(), 10, null, parentscope)
        {
            if (field.IsPrivate)
                visibility = DeclarationVisibilities.Private;
            else
                visibility = DeclarationVisibilities.Public;

            // case insensitive constant name
            //caseinsensitive = c.CaseInsensitive;

            AddAnalyzer(ValueAnalyzer.Create(field));
        }

        public override DeclarationVisibilities DeclarationVisibility
        {
            get
            {
                return visibility;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return true;
            }
        }

        public override DeclarationTypes  DeclarationType
        {
            get
            {
                return DeclarationTypes.Constant;
            }
        }

        public override string GetDescription()
        {
            List<string> values = GetResolvedValues();

            return string.Format(
                "const {0} = {1}",
                Label,
                (values != null && values.Count > 0) ? values[0] : "?");
        }
    }

    #endregion

    /// <summary>
    /// Namespace declaration.
    /// </summary>
    public class AssemblyNamespaceDecl : DeclarationInfo
    {
        internal class AssemblyNamespaceScope : ScopeInfo
        {
            private readonly string NamespaceFullName;

            public AssemblyNamespaceScope(List<string> NamespaceName, ScopeInfo parentscope)
                : base(parentscope, new TextSpan())
            {
                NamespaceFullName = null;
                foreach (string name in NamespaceName)
                {
                    if (NamespaceFullName == null)
                        NamespaceFullName = name;
                    else
                        NamespaceFullName += QualifiedName.Separator + name;
                }
            }

            public override string FullName
            {
                get
                {
                    return NamespaceFullName;
                }
            }
            public override ScopeTypes ScopeType
            {
                get
                {
                    return ScopeTypes.Namespace;
                }
            }
        }

        public AssemblyNamespaceDecl(List<string> NamespaceName, ScopeInfo parentscope)
            : base(NamespaceName[NamespaceName.Count - 1], new TextSpan(), 90, null, parentscope)
        {
            this.DeclarationScope = new AssemblyNamespaceScope(NamespaceName, parentscope);
        }

        public override DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Namespace;
            }
        }
        public override string FullName
        {
            get
            {
                return DeclarationScope.FullName;
            }
        }
        public override void GetTypeMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (DeclarationScope != null)
                DeclarationScope.SelectDeclarations(result, 0, 0, 0, match, ScopeInfo.SelectStaticDeclaration.Any);
        }

        public override string GetDescription()
        {
            return "namespace " + FullName;
        }
    }

}