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
    /// Phalanger class library declarations scope.
    /// </summary>
    public class AssemblyDeclScope : ScopeInfo
    {
        private Assembly _assembly;
        private readonly string AssemblyFullName;

        /// <summary>
        /// Init assembly declarations.
        /// </summary>
        /// <param name="assembly">Phalanger class library assembly.</param>
        public AssemblyDeclScope(Assembly assembly)
            : base(null, new TextSpan())
        {
            this._assembly = assembly;
            this.AssemblyFullName = assembly.FullName;
        }

        /// <summary>
        /// Init assembly members.
        /// </summary>
        protected override void InitScope()
        {
            AddTypes(_assembly.GetExportedTypes(), this);
        }

        /// <summary>
        /// Add given types declarations.
        /// </summary>
        /// <param name="ts">List of assembly types.</param>
        /// <param name="parentscope"></param>
        internal static void AddTypes(Type[] types, AssemblyDeclScope assemblyScope)
        {
            foreach (Type t in types)
            {
                // add type declarations
                AddTypeDeclaration(t, assemblyScope);
            }
        }

        /// <summary>
        /// Add given Type as new declaration.
        /// If type has not ImplementsTypeAttribute, add his members.
        /// </summary>
        /// <param name="t"></param>
        /// <param name="parentscope"></param>
        internal static void AddTypeDeclaration(Type t, AssemblyDeclScope assemblyScope)
        {
            if (!t.IsVisible || t.IsNotPublic)
                return;
            
            ScopeInfo namespaceScope = assemblyScope.InitNamespaceDecl(t.Namespace);

            object[] t_attr = t.GetCustomAttributes(typeof(ImplementsTypeAttribute), false);

            if (t_attr != null && t_attr.Length > 0)
            {   // t is PHP Type
                namespaceScope.AddDeclaration(new AssemblyPhpClassDecl(t, namespaceScope));
            }
            else
            {   // t is container for PHP functions and constants
                // or .Net class
                DeclarationInfo classdecl = new AssemblyNetClassDeclaration(t, namespaceScope);
                
                if (t.Namespace.StartsWith(Namespaces.Library))
                {   // class contains php global functions, must be initialized here
                    classdecl.DeclarationScope.InitScopeIfNotYet();
                }
                else
                {
                    namespaceScope.AddDeclaration(classdecl);
                }
            }
        }

        /// <summary>
        /// Reads declarations contained in given type.
        /// </summary>
        /// <param name="t">Container with fields and methods.</param>
        /// <param name="parentscope"></param>
        internal static void InitClassDeclarations(Type t, ScopeInfo parentscope)
        {
            // methods
            GetMethodsDeclarations(t, parentscope);

            // fields
            GetFieldsDeclarations(t, parentscope);

            // events
            GetEventsDeclarations(t, parentscope);

            // interfaces
            // ...

            // properties
            GetPropertiesDeclarations(t, parentscope);
        }

        /// <summary>
        /// reads constants from the type
        /// </summary>
        /// <param name="t">Container with fields and methods.</param>
        /// <param name="parentscope"></param>
        internal static void GetFieldsDeclarations(Type t, ScopeInfo parentscope)
        {
            FieldInfo[] fs = t.GetFields();

            foreach (FieldInfo f in fs)
            {
                object[] os = f.GetCustomAttributes(typeof(ImplementsConstantAttribute), false);

                if ( os.Length > 0)
                {
                    // PHP constants
                    foreach (ImplementsConstantAttribute c in os)
                    {
                        // add constant into the namespace scope // TODO: do it nicer
                        parentscope.ParentScope.AddDeclaration(new AssemblyPhpConstantDecl(f, c, parentscope));
                    }
                }
                else
                {
                    if (f.IsSpecialName)
                        continue;

                    parentscope.AddDeclaration(new AssemblyNetFieldDeclaration(f, parentscope));
                }
            }
        }

        /// <summary>
        /// reads functions from the type
        /// </summary>
        /// <param name="t">Container with methods.</param>
        /// <param name="parentscope"></param>
        internal static void GetMethodsDeclarations(Type t, ScopeInfo parentscope)
        {
            MethodInfo[] ms = t.GetMethods();
            AssemblyPhpFunctionDecl lastfunc = null;

            foreach (MethodInfo m in ms)
            {
                object[] os = m.GetCustomAttributes(false);

                ImplementsFunctionAttribute f = null;

                foreach (object o in os)
                {
                    ScopeInfo targetScope;

                    if (o is ImplementsFunctionAttribute)
                    {
                        f = (ImplementsFunctionAttribute)o;

                        if ((f.Options & FunctionImplOptions.Internal) != 0)
                            continue;  // internal Phalanger function, do not add

                        targetScope = parentscope.ParentScope;  // add declaration into the namespace scope
                        
                    }
                    else if (o is ImplementsMethodAttribute)
                    {
                        f = new ImplementsFunctionAttribute(m.Name);
                        targetScope = parentscope;  // add declaration into the class scope
                    }
                    else
                        continue;
                    
                    if (lastfunc != null && lastfunc.Label == f.Name &&
                        lastfunc.ResolveOptionalParameters(m, f))
                    {   // !! in Phalanger Class Library, function with the same name are grouped !!
                        // !! so we should check the last inserted function only !!
                        
                    }
                    else
                    {
                        // add declaration into the target scope
                        targetScope.AddDeclaration(lastfunc = new AssemblyPhpFunctionDecl(m, f, targetScope));
                    }
                }

                // .Net method
                if (f == null)
                {
                    // skip special methods
                    if (m.IsSpecialName)
                    {
                        continue;
                    }

                    // init .Net method
                    parentscope.AddDeclaration(new AssemblyNetMethodDeclaration(m, parentscope));
                }
            }
        }

        /// <summary>
        /// reads properties from the type
        /// </summary>
        /// <param name="t"></param>
        /// <param name="parentscope"></param>
        internal static void GetPropertiesDeclarations(Type t, ScopeInfo parentscope)
        {
            PropertyInfo[] properties = t.GetProperties();

            if (properties == null)
                return;

            foreach (PropertyInfo p in properties)
            {
                if (p.IsSpecialName)
                    continue;

                parentscope.AddDeclaration(new AssemblyNetPropertyDeclaration(p,parentscope));
            }
        }

        /// <summary>
        /// reads events from the type
        /// </summary>
        /// <param name="t"></param>
        /// <param name="parentscope"></param>
        internal static void GetEventsDeclarations(Type t, ScopeInfo parentscope)
        {
            EventInfo[] events = t.GetEvents();
            if (events == null)
                return;

            foreach (EventInfo e in events)
            {
                if (e.IsSpecialName)
                    continue;

                parentscope.AddDeclaration(new AssemblyNetEventDeclaration(e, parentscope));
            }
        }
        
        /// <summary>
        /// Assembly full name.
        /// </summary>
        public override string FileName
        {
            get
            {
                return AssemblyFullName;
            }
        }

        /// <summary>
        /// Full name of the declaration.
        /// </summary>
        public override string FullName
        {
            get
            {
                return AssemblyFullName.Split(new char[] { ' ', ',' })[0];
            }
        }

    }

}