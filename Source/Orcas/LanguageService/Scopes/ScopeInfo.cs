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
    /// Base class for all code scopes.
    /// </summary>
    /// <remarks>
    /// The ScopeInfo is a tree structure of code scopes within one source code file.
    /// It contains other scopes and declarations.
    /// It defines included files and imported namespaces.
    /// </remarks>
    public class ScopeInfo
    {
        #region Properties

        /// <summary>
        /// Parent scope. Can be null.
        /// </summary>
        public readonly ScopeInfo ParentScope;

        /// <summary>
        /// Scope type.
        /// </summary>
        public virtual ScopeTypes ScopeType
        {
            get
            {
                return ScopeTypes.Block;
            }
        }

        /// <summary>
        /// File containing this declaration.
        /// </summary>
        public virtual string FileName
        {
            get
            {
                if (ParentScope == null)
                    return null;
                else
                    return ParentScope.FileName;
            }
        }

        /// <summary>
        /// Full name of the scope.
        /// Typically it's equal to the full name of the parent scope.
        /// </summary>
        public virtual string FullName
        {
            get
            {
                if (ParentScope != null)
                    return ParentScope.FullName;
                else
                    return string.Empty;
            }
        }        

        #endregion

        /// <summary>
        /// Types of scopes.
        /// </summary>
        public enum ScopeTypes
        {
            Block,  // global code or code scope in the method or function
            Function,  // function or method
            Class, // class
            Namespace,  // namespace
        }

        /// <summary>
        /// Init scope.
        /// </summary>
        /// <param name="ParentScope"></param>
        public ScopeInfo( ScopeInfo ParentScope, TextSpan BodySpan )
        {
            this.ParentScope = ParentScope;
            this.BodySpan = BodySpan;
        }

        #region Childs list

        private bool ScopeInitialized = false;

        private List<ScopeInfo> _Scopes = null;
        private List<DeclarationInfo> _Declarations = null;
        private List<string[]> _KnownNamespaces = null;

        /// <summary>
        /// Scopes in this scope.
        /// </summary>
        public List<ScopeInfo> Scopes { get { InitScopeIfNotYet(); return _Scopes; } }

        /// <summary>
        /// Declarations in this scope.
        /// </summary>
        public List<DeclarationInfo> Declarations { get { InitScopeIfNotYet(); return _Declarations; } }

        /// <summary>
        /// Namespaces used in this scope.
        /// </summary>
        public List<string[]> KnownNamespaces { get { InitScopeIfNotYet(); return _KnownNamespaces; } }

        /// <summary>
        /// Init scope if not yet.
        /// </summary>
        internal void InitScopeIfNotYet()
        {
            if (!ScopeInitialized)
            {
                ScopeInitialized = true;

                InitScope();
            }
        }

        /// <summary>
        /// Called when child scopes or declarations are required.
        /// </summary>
        protected virtual void InitScope() { /* nothing */ }

        /// <summary>
        /// Add scope into scopes list.
        /// If scopes list is null, create the list first.
        /// </summary>
        /// <param name="scope">New scope to be added.</param>
        public void AddScope(ScopeInfo scope)
        {
            if (scope != null)
            {
                if (_Scopes == null)
                    _Scopes = new List<ScopeInfo>();

                InitScopeIfNotYet();

                _Scopes.Add(scope);
            }
        }

        /// <summary>
        /// Add declaration into declarations list.
        /// If the declarations list is null, create the list first.
        /// </summary>
        /// <param name="decl">New declaration to be added.</param>
        public void AddDeclaration(DeclarationInfo decl)
        {
            if(decl != null)
            {
                if (_Declarations == null)
                    _Declarations = new List<DeclarationInfo>();

                InitScopeIfNotYet();

                _Declarations.Add(decl);
            }
        }

        /// <summary>
        /// Add known namespace. (Imported namespace in this scope)
        /// </summary>
        /// <param name="namespace_name">Namespace name split into chain.</param>
        public void AddKnownNamespace(QualifiedName namespace_qualifiedname)
        {
            if (namespace_qualifiedname != null && namespace_qualifiedname.Namespaces != null && namespace_qualifiedname.Namespaces.Length > 0)
            {
                Name[] namespace_name = namespace_qualifiedname.Namespaces;
                
                // check duplicity
                if (_KnownNamespaces != null)
                    foreach (string[] name in _KnownNamespaces)
                        if (name.Length == namespace_name.Length)
                        {
                            int i = name.Length - 1;

                            for (; i >= 0; --i)
                                if (name[i] != namespace_name[i].Value)
                                    break;

                            if (i >= 0)
                                return; // already inserted
                        }

                if (_KnownNamespaces == null)
                    _KnownNamespaces = new List<string[]>();

                InitScopeIfNotYet();

                // make string[]
                string[] namespace_stringname = new string[namespace_name.Length];
                for (int i = 0; i < namespace_name.Length; ++i )
                {
                    namespace_stringname[i] = namespace_name[i].Value;
                }

                //
                _KnownNamespaces.Add(namespace_stringname);
            }
        }

        #endregion

        #region Includes list

        #region included scope information

        /// <summary>
        /// Included file base class.
        /// </summary>
        public abstract class DeclScopeInclude
        {
            /// <summary>
            /// Init include statement.
            /// </summary>
            /// <param name="line">Line position.</param>
            /// <param name="col">Column position.</param>
            public DeclScopeInclude(int line, int col)
            {
                this.line = line;
                this.col = col;
            }

            /// <summary>
            /// Include position.
            /// </summary>
            private int line, col;

            /// <summary>
            /// Statement line position.
            /// </summary>
            public int Line
            {
                get
                {
                    return line;
                }
            }

            /// <summary>
            /// Statement column position.
            /// </summary>
            public int Col
            {
                get
                {
                    return col;
                }
            }

            /// <summary>
            /// Update position.
            /// </summary>
            /// <param name="change">Change information.</param>
            public void UpdatePosition(TextLineChange change)
            {
                Utils.ChangeCoord(line, col, out line, out col, change);
            }

            /// <summary>
            /// Get base scope of the included file. Can be null.
            /// </summary>
            /// <param name="projectdeclarations">Project declarations.</param>
            /// <returns></returns>
            abstract public ScopeInfo GetScope(ProjectDeclarations/*!*/projectdeclarations);
        }

        /// <summary>
        /// Included scope which won't be replaced (assembly).
        /// So we can use reference to the scope directly.
        /// </summary>
        public sealed class StaticScopeInclude : DeclScopeInclude
        {
            private readonly ScopeInfo scope;
            public StaticScopeInclude(ScopeInfo/*!*/scope, int line, int col)
                :base(line,col)
            {
                this.scope = scope;
            }

            public override ScopeInfo GetScope(ProjectDeclarations projectdeclarations)
            {
                return scope;
            }
        }

        /// <summary>
        /// Included file. Scope of this file can be replaced,
        /// so we use the filename to find current scope reference each time.
        /// </summary>
        public sealed class FileInclude : DeclScopeInclude
        {
            private readonly string filename;
            public FileInclude(string filename, int line, int col)
                :base(line,col)
            {
                this.filename = filename;
            }

            public override ScopeInfo GetScope(ProjectDeclarations projectdeclarations)
            {
                if(projectdeclarations != null)
                {
                    ScopeInfo scope;
                    if (projectdeclarations.Declarations.TryGetValue(filename, out scope))
                    {
                        return scope;
                    }
                    else
                    {
                        // file should be out of the project root directory, try to find it and parse it
                        scope = projectdeclarations.AddPhpFileDeclarations(filename);
                    }
                }

                return null;
            }
        }

        #endregion

        private List<DeclScopeInclude> _IncludedFiles = null;

        /// <summary>
        /// List of included scopes (files).
        /// </summary>
        public List<DeclScopeInclude> IncludedFiles { get { return _IncludedFiles; } }

        /// <summary>
        /// Add new included file information.
        /// </summary>
        /// <param name="includedscope">Included file information.</param>
        public void AddIncludedFile(DeclScopeInclude includedscope)
        {
            if (includedscope != null)
            {
                if (_IncludedFiles == null)
                    _IncludedFiles = new List<DeclScopeInclude>();

                _IncludedFiles.Add(includedscope);
            }
        }

        #endregion

        /// <summary>
        /// Get scope at the given position.
        /// </summary>
        /// <param name="line">Line.</param>
        /// <param name="col">Column.</param>
        /// <returns>DeclarationsScope on the given position. Cannot be null.</returns>
        internal ScopeInfo/*!*/GetScopeAt(int line, int col)
        {
            // find member scope at the position recursively
            if (Scopes != null)
                foreach (ScopeInfo scope in Scopes)
                {
                    if (Utils.IsInSpan(line, col, scope.BodySpan))
                        return scope.GetScopeAt(line, col);
                }

            // no member scope at given position
            return this;
        }

        public enum SelectStaticDeclaration
        {
            StaticOnly, NotStatic, Any
        }

        /// <summary>
        /// Select specified declarations.
        /// </summary>
        /// <param name="result">Output list.</param>
        /// <param name="typesmask">Scope type mask. 0 ignored.</param>
        /// <param name="usagemask">Usage mask. 0 ignored.</param>
        /// <param name="visibilitymask">Visibility mask. 0 ignored.</param>
        /// <param name="contains">Declaration must contains this. null ignored.</param>
        /// <param name="isStatic">Select static members.</param>
        /// <remarks>
        /// Declarations in this scope are used. The ones matching the criteria are added into the result list.
        /// </remarks>
        public void SelectDeclarations(
            DeclarationList/*!*/result,
            DeclarationInfo.DeclarationTypes typesmask, DeclarationInfo.DeclarationUsages usagemask, DeclarationInfo.DeclarationVisibilities visibilitymask,
            DeclarationMatches match, SelectStaticDeclaration isStatic)
        {
            List<DeclarationInfo> decls = Declarations;
            if (decls == null)
                return;

            foreach (DeclarationInfo decl in decls)
            {
                if (((decl.DeclarationType & typesmask) != 0 || typesmask == 0) &&
                    ((decl.DeclarationVisibility & visibilitymask) != 0 || visibilitymask == 0) &&
                    ((decl.DeclarationUsage & usagemask) != 0 || usagemask == 0) &&
                    (
                        isStatic == SelectStaticDeclaration.Any ||
                        (decl.IsStatic && isStatic == SelectStaticDeclaration.StaticOnly) ||
                        (!decl.IsStatic && isStatic == SelectStaticDeclaration.NotStatic)) &&
                    match.Matches(decl))
                {
                    result.Add(decl);
                }
            }
        }

        #region Namespaces

        /// <summary>
        /// Find/create the namespace scope by the full namespace name. If the namespace does not exists yet, namespace declaration will be created and added into the scope.
        /// </summary>
        /// <param name="NamespaceName">.Net namespace name. (names are separated with dot)</param>
        /// <returns>Requested namespace scope.</returns>
        /// <remarks>
        /// The namespace consists of names separated by dot. It corresponds to the tree structure of the namespace declarations.
        /// Every namespace declaration contains a scope, where the members of this namespace are placed.
        /// </remarks>
        protected ScopeInfo InitNamespaceDecl(string NamespaceName)
        {
            if (NamespaceName == null)
                return this;

            string[] names = NamespaceName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            List<string> CurNamespaceName = new List<string>();

            ScopeInfo scope = this;

            foreach (string name in names)
            {
                CurNamespaceName.Add(name);

                List<DeclarationInfo> members = scope.Declarations;
                DeclarationInfo namespacedecl = null;

                if (members != null)
                    foreach (DeclarationInfo decl in members)
                        if (decl.DeclarationType == DeclarationInfo.DeclarationTypes.Namespace && decl.Label == name)
                        {
                            namespacedecl = decl;
                            break;
                        }

                if (namespacedecl == null)
                {   // not found, create it
                    namespacedecl = new AssemblyNamespaceDecl(CurNamespaceName, scope);
                    scope.AddDeclaration(namespacedecl);
                }

                scope = namespacedecl.DeclarationScope;
            }

            return scope;
        }

        /// <summary>
        /// Get list of imported namespaces.
        /// Includes are ignored.
        /// </summary>
        /// <param name="result">Output list of namespaces.</param>
        protected void SelectImportedNamespaces(List<string[]> result)
        {
            ScopeInfo scope = this;
            while (scope != null)
            {
                if (scope.KnownNamespaces != null)
                    result.AddRange(scope.KnownNamespaces);

                scope = scope.ParentScope;
            }

        }

        /// <summary>
        /// Select all matching types in specified namespaces.
        /// </summary>
        /// <param name="result">Output list.</param>
        /// <param name="match">Declaration match.</param>
        /// <param name="projectdeclarations">Project declarations.</param>
        /// <param name="namespaces">Namespaces to make selection from.</param>
        protected static void SelectTypesInNamespaces(DeclarationList result, DeclarationMatches match, ProjectDeclarations projectdeclarations, List<string[]> namespaces)
        {
            if (projectdeclarations == null)
                return;

            foreach (string[] names in namespaces)
            {
                if (names == null || names.Length == 0)
                    continue;

                // find namespace
                DeclarationList tmp = new DeclarationList();
                projectdeclarations.SelectRootNamespaces(new DeclarationLabelEqual(names[0]), tmp);

                for (int i = 1; i < names.Length; ++i)
                {
                    DeclarationList tmp2 = new DeclarationList();
                    DeclarationMatches tmp2match = new DeclarationLabelEqual(names[i]);
                    foreach (DeclarationInfo decl in tmp)
                        decl.GetTypeMembers(projectdeclarations, tmp2, tmp2match);

                    tmp = tmp2;
                }

                // select types in namespace
                foreach (DeclarationInfo decl in tmp)
                    decl.GetTypeMembers(projectdeclarations, result, match);
            }
        }

        #endregion

        #region Getting declarations

        /// <summary>
        /// List of declarations visible in local scope.
        /// </summary>
        /// <param name="result">Output list of local declarations.</param>
        /// <param name="typesmask">Declaration type mask. 0 ignored.</param>
        /// <param name="contains">String contained in each of added declaration.</param>
        /// <param name="projectdeclarations">List of project declarations.</param>
        /// <param name="skipthis">Scopes in this list must be skipped.</param>
        /// <param name="FunctionOrClassPassed"></param>
        /// <param name="current_scope">Scope where the local declarations are required</param>
        protected void GetLocalDeclarations(DeclarationList result, DeclarationInfo.DeclarationTypes typesmask, DeclarationMatches match, ProjectDeclarations projectdeclarations, Dictionary<ScopeInfo, int> skipthis, bool FunctionOrClassPassed, ScopeInfo current_scope)
        {
            ScopeInfo basescope = null;

            for (ScopeInfo s = this; s != null; s = s.ParentScope)
            {
                basescope = s;

                // avoid cycling
                if (skipthis.ContainsKey(s)) break;
                skipthis.Add(s, 0);

                // select requested members
                s.SelectDeclarations(
                    result,
                    typesmask,
                    ((s == current_scope) ? (DeclarationInfo.DeclarationUsages.ThisScope) : (DeclarationInfo.DeclarationUsages.AllChildScopes)) | (FunctionOrClassPassed ? 0 : DeclarationInfo.DeclarationUsages.UntilFunctionOrClass),
                    0, match, ScopeInfo.SelectStaticDeclaration.Any);

                // go throw included scopes
                // problems: cycling OK, assemblies OK, duplicities OK
                // using s.IncludedFiles
                if (s.IncludedFiles != null && projectdeclarations != null)
                    foreach (DeclScopeInclude include in s.IncludedFiles)
                    {
                        ScopeInfo includedscope = include.GetScope(projectdeclarations);
                        if (includedscope != null)
                        {
                            includedscope.GetLocalDeclarations(result, typesmask, match, projectdeclarations, skipthis, FunctionOrClassPassed, current_scope);
                        }
                    }

                // Determine if function of class was passed,
                // variables after this border should not be visible.
                if (s.ScopeType == ScopeTypes.Class || s.ScopeType == ScopeTypes.Function)
                    FunctionOrClassPassed = true;
            }

            // go throw scopes which includes basescope
            // using projectdeclarations.WhereIsFileIncluded(basescope.FileName);
            if(projectdeclarations != null)
            {
                List<ScopeInfo> parentfiles = projectdeclarations.WhereIsFileIncluded(basescope.FileName);
                if (parentfiles != null)
                {
                    foreach (ScopeInfo parentfile in parentfiles)
                    {
                        // avoid cycling
                        if (skipthis.ContainsKey(parentfile)) continue;
                        
                        parentfile.GetLocalDeclarations(result, typesmask, match, projectdeclarations, skipthis, FunctionOrClassPassed, current_scope);
                    }
                }
            }
        }

        /// <summary>
        /// List of members visible in local scope.
        /// </summary>
        /// <param name="result">Output list of local declarations.</param>
        /// <param name="typesmask">Scope type mask. 0 ignored.</param>
        /// <param name="contains">String contained in each of added declaration.</param>
        /// <param name="projectdeclarations">List of project declarations.</param>
        public void GetLocalDeclarations(DeclarationList result, DeclarationInfo.DeclarationTypes typesmask, DeclarationMatches match, ProjectDeclarations projectdeclarations)
        {
            GetLocalDeclarations(result, typesmask, match, projectdeclarations, this);
        }

        /// <summary>
        /// Get declarations visible in the specified code scope.
        /// </summary>
        /// <param name="result">The output list.</param>
        /// <param name="typesmask">Declaration type mask, 0 to ignore mask.</param>
        /// <param name="match">Match object to filter declarations.</param>
        /// <param name="projectdeclarations">All project sources.</param>
        /// <param name="localscope">Code scope where the declarations are locally visible.</param>
        /// <remarks>
        /// Declarations visible in the current and parent scopes are included.
        /// All root namespace names are included too.
        /// Also declarations in active namespaces are listed.
        /// </remarks>
        public static void GetLocalDeclarations(DeclarationList result, DeclarationInfo.DeclarationTypes typesmask, DeclarationMatches match, ProjectDeclarations projectdeclarations, ScopeInfo localscope)
        {
            if (typesmask != DeclarationInfo.DeclarationTypes.Namespace && localscope != null)    // not namespaces only
            {
                // local declarations
                localscope.GetLocalDeclarations(result, typesmask, match, projectdeclarations, new Dictionary<ScopeInfo, int>(), false, localscope);
            }

            // root namespaces
            if (projectdeclarations != null &&
                ((typesmask & DeclarationInfo.DeclarationTypes.Namespace) != 0 || typesmask == 0))
            {
                projectdeclarations.SelectRootNamespaces(match, result);
            }

            // get list of imported namespaces
            List<string[]> importedNamespaces = new List<string[]>();
            if (localscope != null)
            {
                localscope.SelectImportedNamespaces(importedNamespaces);
            }
            // imported namespaces by default (by phalanger)
            if (typesmask != DeclarationInfo.DeclarationTypes.Namespace)
            {
                importedNamespaces.Add(Namespaces.Library.Split(new char[] { '.' }));   // does not contains other namespaces, so we can skip this in some cases
                importedNamespaces.Add("PHP.Library.Xml".Split(new char[] { '.' }));   // does not contains other namespaces, so we can skip this in some cases
            }
            //importedNamespaces.Add(Namespaces.LibraryStubs.Split(new char[] { '.' }));

            // select types in imported namespaces
            SelectTypesInNamespaces(result, match, projectdeclarations, importedNamespaces);
        }

        /// <summary>
        /// Find declarations of requested qualified name.
        /// (A:::B:::C)
        /// </summary>
        /// <param name="name"></param>
        /// <param name="projectdeclarations"></param>
        public DeclarationList GetDeclarationsByName(QualifiedName name, ProjectDeclarations projectdeclarations)
        {
            return GetDeclarationsByName(name, projectdeclarations, this);
        }

        /// <summary>
        /// Find declarations satisfying given qualified name.
        /// A:::B:::C
        /// </summary>
        /// <param name="name">Qualified name.</param>
        /// <param name="projectdeclarations">Project declarations.</param>
        /// <param name="localscope">Current scope.</param>
        /// <returns>Declaration list.</returns>
        /// <remarks>
        /// The qualified name is parsed.
        /// The first element represents some locally visible declaration (or more declarations).
        /// The other elements represent the member declarations of the previously selected ones.
        /// </remarks>
        public static DeclarationList GetDeclarationsByName(QualifiedName name, ProjectDeclarations projectdeclarations, ScopeInfo localscope)
        {
            // build list of name
            List<string> names = new List<string>();

            if (name.Namespaces != null)
                for (int i = 0; i < name.Namespaces.Length; ++i)
                {
                    names.Add( name.Namespaces[i].Value );
                }
            if (name.Name.Value != null && name.Name.Value.Length > 0)
                names.Add( name.Name.Value );

            // select requested declarations
            DeclarationList result = new DeclarationList();

            if (names.Count > 0)
            {
                // root name
                GetLocalDeclarations(
                                result,
                                (names.Count > 1) ?
                                    (DeclarationInfo.DeclarationTypes.Namespace) :
                                    0,//(DeclarationInfo.DeclarationTypes.Class | DeclarationInfo.DeclarationTypes.Constant | DeclarationInfo.DeclarationTypes.Variable | DeclarationInfo.DeclarationTypes.Namespace),
                                new DeclarationLabelEqual(names[0]),
                                projectdeclarations, localscope);

                // member names
                for (int i = 1; i < names.Count; ++i)
                {
                    DeclarationList tmp = new DeclarationList();
                    DeclarationMatches match = new DeclarationLabelEqual(names[i]);

                    if (result != null)
                        foreach (DeclarationInfo decl in result)
                        {
                            decl.GetTypeMembers(projectdeclarations, tmp, match);
                        }

                    result = tmp;

                    // everything before the last one has to be namespace
                    if (i < names.Count - 1)
                        result.FilterType(DeclarationInfo.DeclarationTypes.Namespace);
                }
            }

            return result;
        }

        #endregion

        #region Position

        /// <summary>
        /// Scope position.
        /// </summary>
        public TextSpan BodySpan;

        /// <summary>
        /// Update member declarations and recursively child scopes position.
        /// Span property is changed if it lays after the specified change.
        /// </summary>
        /// <param name="change">Change information.</param>
        /// <returns>True if some change was made.</returns>
        public virtual bool UpdatePositions(TextLineChange change)
        {
            if ( Utils.UpdateSpan(change, BodySpan, out BodySpan) )
            {
                // decls
                if (Declarations != null)
                    foreach (DeclarationInfo decl in Declarations)
                    {
                        TextSpan newSpan;
                        if (Utils.UpdateSpan(change, decl.Span, out newSpan))
                            decl.Span = newSpan;
                    }

                //scopes
                if (Scopes != null)
                    foreach (ScopeInfo scope in Scopes)
                        scope.UpdatePositions(change);

                // includes
                if (IncludedFiles != null)
                    foreach (DeclScopeInclude inc in IncludedFiles)
                        inc.UpdatePosition(change);

                return true;
            }

            return false;
        }

        #endregion
    }
}