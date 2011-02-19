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
    #region .Net types and members implementation

    /// <summary>
    /// Declaration of .Net class from an assembly.
    /// </summary>
    public class AssemblyNetClassDeclaration : DeclarationInfo
    {
        /// <summary>
        /// Scope with class members declaration.
        /// </summary>
        internal class AssemblyNetClassScope : ScopeInfo
        {
            private readonly Type/*!*/assemblyType;

            /// <summary>
            /// Init.
            /// </summary>
            /// <param name="assemblyType"></param>
            /// <param name="parentscope"></param>
            public AssemblyNetClassScope(Type/*!*/assemblyType, ScopeInfo parentscope)
                : base(parentscope, new TextSpan())
            {
                this.assemblyType = assemblyType;
            }

            /// <summary>
            /// Init type members.
            /// </summary>
            protected override void InitScope()
            {
                // methods, constants, properties
                AssemblyDeclScope.InitClassDeclarations(assemblyType, this);
            }

            /// <summary>
            /// Add declaration of constructor
            /// </summary>
            /// <param name="myclassdecl"></param>
            public void AddConstructorsAsMembersWithParameters(DeclarationInfo myclassdecl)
            {
                // init ctors
                ConstructorInfo[] ctors = assemblyType.GetConstructors();
                foreach (ConstructorInfo ctor in ctors)
                {
                    myclassdecl.AddMemberWithParameter(new AssemblyNetMethodDeclaration(ctor, /*ConstructorName*/myclassdecl.Label, this));
                }
            }
        }

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="namespaceScope"></param>
        public AssemblyNetClassDeclaration(Type t, ScopeInfo namespaceScope)
            :base(t.Name, new TextSpan(), (t.IsEnum?18:0), null, namespaceScope)
        {
            // t.IsClass
            // t.IsEnum
            // t.IsArray
            DeclarationScope = new AssemblyNetClassScope(t, namespaceScope);
            AddAnalyzer(new AssemblyClassAnalyzer(this));
        }

        /// <summary>
        /// Class description.
        /// </summary>
        /// <returns>Description text.</returns>
        public override string GetDescription()
        {
            return string.Format("{0} {1}", "class", Label);
        }

        /// <summary>
        /// Init constructor parameters.
        /// </summary>
        protected override void InitParameters()
        {
            AssemblyNetClassScope scope = (AssemblyNetClassScope)DeclarationScope;

            if (scope != null)
            {
                scope.AddConstructorsAsMembersWithParameters(this);
            }
        }

        public override void GetTypeMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
        {
            if (DeclarationScope != null)
                DeclarationScope.SelectDeclarations(result, DeclarationTypes.Class, 0, 0, match, ScopeInfo.SelectStaticDeclaration.Any);
        }

        /// <summary>
        /// Get declaration type.
        /// (Class)
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
    /// Declaration of .Net method.
    /// </summary>
    public class AssemblyNetMethodDeclaration : FunctionDeclaration
    {
        /// <summary>
        /// Method Info (from reflections).
        /// </summary>
        private readonly MethodBase methodinfo;

        private readonly Type returntype;

        /// <summary>
        /// Init method from the assembly.
        /// </summary>
        /// <param name="methodinfo"></param>
        /// <param name="parentscope"></param>
        public AssemblyNetMethodDeclaration(MethodInfo methodinfo, ScopeInfo parentscope)
            : base(methodinfo.Name, null, parentscope)
        {
            // TODO: assembly method description
            this.methodinfo = methodinfo;
            this.returntype = methodinfo.ReturnType;

            AddAnalyzer(new AssemblyTypeInstanceAnalyzer(methodinfo.ReturnType));
        }

        public AssemblyNetMethodDeclaration(ConstructorInfo ctor, string name, ScopeInfo parentscope)
            : base(name, null, parentscope)
        {
            // TODO: assembly method description
            this.methodinfo = ctor;
            this.returntype = null;
        }

        /// <summary>
        /// Init parameters.
        /// </summary>
        protected override void InitParameters()
        {
            // init method parameters.
            ParameterInfo[] prms = methodinfo.GetParameters();

            foreach (ParameterInfo p in prms)
            {
                AddParameter(new FunctionParamInfo(p.Name, p.ParameterType, null, new PHP.Core.Parsers.Position()));
            }
        
        }

        protected override string FunctionHeader
        {
            get
            {
                return string.Format("{0} {1}", (returntype != null) ? returntype.Name : string.Empty, methodinfo.Name);
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
    /// Declaration of .Net field.
    /// </summary>
    public class AssemblyNetFieldDeclaration  : DeclarationInfo
    {
        private readonly FieldInfo field;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="parentscope"></param>
        public AssemblyNetFieldDeclaration(FieldInfo field, ScopeInfo parentscope)
            :base(field.Name, new TextSpan(), 102, null, parentscope)
        {
            this.field = field;

            AddAnalyzer(ValueAnalyzer.Create(field));
        }

        public override DeclarationVisibilities DeclarationVisibility
        {
            get
            {
                if (field.IsPublic)
                    return DeclarationVisibilities.Public;
                else// if (field.IsPrivate)
                    return DeclarationVisibilities.Private;
            }
        }

        public override bool IsStatic
        {
            get
            {
                return field.IsStatic;
            }
        }

        public override DeclarationTypes DeclarationType
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
                "{0} = {1}",
                Label,
                (values != null && values.Count > 0) ? values[0] : "?");
        }
    }

    /// <summary>
    /// Declaration of .Net property.
    /// </summary>
    public class AssemblyNetPropertyDeclaration : DeclarationInfo
    {
        private readonly PropertyInfo property;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="parentscope"></param>
        public AssemblyNetPropertyDeclaration(PropertyInfo property, ScopeInfo parentscope)
            : base(property.Name, new TextSpan(), 102, null, parentscope)
        {
            this.property = property;
            // todo: property type analyzer
        }

        public override DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Variable;
            }
        }

        public override string GetDescription()
        {
            return property.ToString();//.PropertyType.Name + " " + Label;
        }
    }

    /// <summary>
    /// Declaration of .Net event.
    /// </summary>
    public class AssemblyNetEventDeclaration : DeclarationInfo
    {
        /// <summary>
        /// Event object members.
        /// </summary>
        private class EventAnalyzer : ValueAnalyzer
        {
            public override void GetObjectMembers(ProjectDeclarations projectdeclarations, DeclarationList result, DeclarationMatches match)
            {
                result.Add(
                    new SpecialFunctionDecl("Add", "Add method into the .Net event.",
                        new FunctionParamInfo[] { 
                                    new FunctionParamInfo("delegate", "System:::EventHandler", "Method to be called.", new PHP.Core.Parsers.Position())
                                }, true));
                result.Add(
                    new SpecialFunctionDecl("Remove", "Remove method from the .Net event.",
                        new FunctionParamInfo[] { 
                                    new FunctionParamInfo("delegate", "System:::EventHandler", "Method not to be called.", new PHP.Core.Parsers.Position())
                                }, true));
            }
        }

        private static EventAnalyzer _eventAnalyzer = new EventAnalyzer();

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="property"></param>
        /// <param name="parentscope"></param>
        public AssemblyNetEventDeclaration(EventInfo e, ScopeInfo parentscope)
            : base(e.Name, new TextSpan(), 30, null, parentscope)
        {
            // add event methods (Add,Remove) // todo: are there other event methods ?
            AddAnalyzer(_eventAnalyzer);
        }
        
        public override DeclarationTypes DeclarationType
        {
            get
            {
                return DeclarationTypes.Variable;
            }
        }

        public override string GetDescription()
        {
            return "event " + Label;
        }
    }

    #endregion
}