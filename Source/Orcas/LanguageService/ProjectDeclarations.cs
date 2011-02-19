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
using System.IO;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

using System.Collections;
using System.Reflection;
using PHP.Core.AST;
using PHP.Core.Reflection;
using PHP.Core;

using PHP.VisualStudio.PhalangerLanguageService.Scopes;
using PHP.VisualStudio.PhalangerLanguageService.Declarations;

namespace PHP.VisualStudio.PhalangerLanguageService
{
    /// <summary>
    /// Project declarations.
    /// List of all source code scopes for the current project
    /// (inside the same directory or referenced from some file in the project directory.
    /// </summary>
    public class ProjectDeclarations
    {
        /// <summary>
        /// Last app config update time.
        /// If new app config file is found, referenced assemblies must be reloaded.
        /// </summary>
        public DateTime AppConfigLastWriteTime = new DateTime();

        /// <summary>
        /// Root project directory.
        /// </summary>
        public readonly DirectoryInfo ProjectRootDirectory;

        /// <summary>
        /// Init.
        /// </summary>
        /// <param name="ProjectRootDirectory">Root project directory.</param>
        public ProjectDeclarations(DirectoryInfo ProjectRootDirectory)
        {
            this.ProjectRootDirectory = ProjectRootDirectory;
        }

        #region All parsed files in this project

        /// <summary>
        /// All project declarations [FullFileName, Scope]
        /// Cannot be null.
        /// </summary>
        public readonly Dictionary<string, ScopeInfo> Declarations = new Dictionary<string, ScopeInfo>();

        /// <summary>
        /// Add new scope into project declarations.
        /// Parses given file, ignores errors and add the result code scope into the list.
        /// </summary>
        /// <param name="fullfilename">File name of the source code.</param>
        /// <returns>Code scope of the parsed file.</returns>
        public ScopeInfo AddPhpFileDeclarations(string/*!*/fullfilename)
        {
            if (fullfilename == null || fullfilename.Length == 0)
                return null;

            ScopeInfo ret = null;

            PureCompilationUnit compilation_unit = new PureCompilationUnit(true, true);

            /*parse file, add to declarations*/
            try
            {
                TextReader tr = new StreamReader(fullfilename);
                string code = tr.ReadToEnd();

                PhpSourceFile source_file = new PhpSourceFile(new FullPath(ProjectRootDirectory.FullName), new FullPath(fullfilename));
                VirtualSourceFileUnit source_unit = new VirtualSourceFileUnit(compilation_unit, code, source_file, Encoding.Default);

                VirtualSourceFileUnit[] parsed_source_units =
                    compilation_unit.ParseSourceFiles(
                        new VirtualSourceFileUnit[] { source_unit },
                        new EmptyErrorSink(),
                        LanguageFeatures.PhpClr);

                if (parsed_source_units != null)
                {
                    // add into the cache
                    Declarations[fullfilename] = ret = new GlobalCodeDeclScope(parsed_source_units[0], this);
                }

            }
            catch (Exception)
            {
                // error, do not add
            }

            // file scope
            return ret;
        }

        /// <summary>
        /// Remove all assembly declarations scopes.
        /// </summary>
        public void RemoveAssemblyDeclarations()
        {
            List<string> toremove = new List<string>();

            foreach (KeyValuePair<string, ScopeInfo> decl in Declarations)
            {
                if (decl.Value is AssemblyDeclScope)
                {
                    toremove.Add(decl.Key);
                }
            }

            foreach (string assemblyfullname in toremove)
            {
                Declarations.Remove(assemblyfullname);
            }
        }

        #endregion

        #region Including

        /// <summary>
        /// Include information.
        /// </summary>
        public sealed class IncludedFileInfo
        {
            /// <summary>
            /// Where is the include used.
            /// </summary>
            public readonly ScopeInfo IncludeWhere;

            /// <summary>
            /// What file is included.
            /// </summary>
            public readonly string IncludedFile;

            /// <summary>
            /// Init.
            /// </summary>
            /// <param name="where">Where is the include used.</param>
            /// <param name="filename">What file is included.</param>
            public IncludedFileInfo(ScopeInfo where, string filename)
            {
                this.IncludeWhere = where;
                this.IncludedFile = filename;
            }
        }

        public sealed class FileInclusions
        {
            public readonly List<IncludedFileInfo> Inclusions = new List<IncludedFileInfo>();
        }

        /// <summary>
        /// What every file includes [FullFileName, Includes list].
        /// </summary>
        public readonly Dictionary<string, FileInclusions> Includes = new Dictionary<string, FileInclusions>();

        /// <summary>
        /// Removes information of includes by the specified file.
        /// </summary>
        /// <param name="filename">File which info will be removed.</param>
        public void RemoveIncludesInfo(string filename)
        {
            Includes.Remove(filename);
        }

        /// <summary>
        /// Add include info.
        /// </summary>
        /// <param name="fileFrom">Full file name of the file which includes.</param>
        /// <param name="?">Info of inclusion.</param>
        public void AddIncludeInfo(string fileFrom, IncludedFileInfo info)
        {
            FileInclusions inclusions;
            if (!Includes.TryGetValue(fileFrom, out inclusions))
                Includes.Add(fileFrom, inclusions = new FileInclusions());

            inclusions.Inclusions.Add(info);
        }

        /// <summary>
        /// Get scopes, where the specified file is included. Can be null.
        /// </summary>
        /// <param name="filename">The file name.</param>
        /// <returns>List of code scopes where the given file name is included.</returns>
        public List<ScopeInfo>   WhereIsFileIncluded(string filename)
        {
            List<ScopeInfo> result = null;

            foreach (FileInclusions inclusions in Includes.Values)
            {
                if (inclusions.Inclusions != null)
                    foreach (IncludedFileInfo info in inclusions.Inclusions)
                        if ( info.IncludedFile == filename )
                        {
                            if (result == null)
                                result = new List<ScopeInfo>();

                            result.Add(info.IncludeWhere);
                        }
            }

            return result;
        }

        #endregion

        #region Namespaces support

        /// <summary>
        /// Get list of root namespace declarations in all project files.
        /// </summary>
        /// <param name="match">Namespace name match.</param>
        /// <param name="result">Output list.</param>
        public void SelectRootNamespaces(DeclarationMatches match, DeclarationList result)
        {
            foreach (ScopeInfo scope in Declarations.Values)
            {
                scope.SelectDeclarations(result, DeclarationInfo.DeclarationTypes.Namespace, 0, 0, match, ScopeInfo.SelectStaticDeclaration.Any);
            }
        }

        #endregion
    }    
    
}