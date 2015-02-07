/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Reflection;
using System.Xml;
using System.Configuration;

using PHP.Core.Reflection;
using PHP.Core.Emit;
using System.Diagnostics;
using System.Web;

namespace PHP.Core
{
	public sealed partial class ApplicationContext
	{
		#region Properties

		/// <summary>
        /// Singleton instance of <see cref="WebServerCompilerManager"/> manager. Created lazily in HTTP context. 
		/// </summary>
		private volatile WebServerCompilerManager webServerCompilerManager;
		
        /// <summary>
        /// Contains database of scripts, which are contained in loaded script libraries. Used by dynamic inclusions and compiler.
        /// </summary>
        internal ScriptLibraryDatabase ScriptLibraryDatabase
        {
            get
            {
                Debug.Assert(scriptLibraryDatabase != null, "Empty application context doesn't have a script library.");
                return scriptLibraryDatabase;
            }
        }
        private ScriptLibraryDatabase scriptLibraryDatabase;
		

		#endregion

		#region Initialization

        /// <summary>
        /// Gets instance to compiler manager that manages script libraries, WebPages.dll and scripts compiled dynamically in runtime.
        /// </summary>
		internal WebServerCompilerManager/*!*/ RuntimeCompilerManager
		{
            get
            {
                if (webServerCompilerManager == null)
                    lock (this)
                        if (webServerCompilerManager == null)
                            webServerCompilerManager = new WebServerCompilerManager(this);

                return webServerCompilerManager;
            }
		}

		#endregion

 	}

    #region AssemblyLoader

    public sealed partial class AssemblyLoader
    {
        /// <summary>
        /// Loads assemblies whose paths or full names are listed in references.
        /// </summary>
        /// <param name="references">Enumeration of paths to or full names of assemblies to load.</param>
        /// <exception cref="ArgumentNullException"><paramref name="references"/> is a <B>null</B> reference.</exception>
        /// <exception cref="ConfigurationErrorsException">An error occured while loading a library.</exception>
        public void Load(IEnumerable<CompilationParameters.ReferenceItem>/*!*/ references)
        {
            if (references == null)
                throw new ArgumentNullException("references");

            foreach (var reference in references)
            {
                LoadReference(reference.Reference, reference.LibraryRoot);
            }
        }

        /// <summary>
        /// Loads single reference.
        /// </summary>
        /// <param name="reference">Path to or full name of references assembly.</param>
        /// <param name="libraryRoot">If the reference represents a script library, this optional parameter can move scripts in the loaded library to a subdirectory.</param>
        private void LoadReference(string reference, string libraryRoot)
        {
            Assembly realAssembly;

            if (System.IO.File.Exists(reference))
                // TODO: look if this can be simplified
                realAssembly = LoadRealAssemblyFrom(reference);
            else
                realAssembly = LoadRealAssembly(reference);

            if (realAssembly == null)
            {
                throw new ConfigurationErrorsException
                    (CoreResources.GetString("library_assembly_loading_failed", reference), (Exception)null);
            }

            DAssemblyAttribute attr = DAssemblyAttribute.Reflect(realAssembly);

            // TODO: this special case should be removed after WebPages functionality is passed to script libraries
            if (attr is ScriptAssemblyAttribute && ((ScriptAssemblyAttribute)attr).IsMultiScript)
            {
                //load this as script library
                LoadScriptLibrary(realAssembly, libraryRoot);

                return;
            }

            Load(realAssembly, null);
        }

        /// <summary>
        /// Loads assembly as script library, adding all scripts it contains into script library database.
        /// </summary>
        /// <param name="assemblyName">Long assembly name (see <see cref="Assembly.Load"/>) or a <B>null</B> reference.</param>
        /// <param name="assemblyUrl">Assembly file absolute URI or a <B>null</B> reference.</param>
        /// <param name="libraryRoot">Root offset of the script library. All scripts will be loaded with this offset.
        /// Strict behavior forbids conflicts between scriptLibrary and filesystem, all conflicts will be reported as errors.
        /// This is used in the runtime (dynamic include), compiler currently ignores filesystem.
        /// </param>
        public DAssembly LoadScriptLibrary(string assemblyName, Uri assemblyUrl, string libraryRoot)
        {
            if (assemblyName == null && assemblyUrl == null)
                throw new ArgumentNullException("assemblyName");

            if (assemblyUrl != null && !assemblyUrl.IsAbsoluteUri)
                throw new ArgumentException("Absolute URL expected", "assemblyUrl");

            string target = null;

            try
            {
                if (assemblyName != null)
                {
                    // load assembly by full name:
                    target = assemblyName;

                    return LoadScriptLibrary(LoadRealAssembly(target), libraryRoot);
                }
                else
                {
                    // load by URI:
                    target = HttpUtility.UrlDecode(assemblyUrl.AbsoluteUri);

                    return LoadScriptLibrary(LoadRealAssemblyFrom(target), libraryRoot);
                }
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException
                    (CoreResources.GetString("script_library_assembly_loading_failed", target) + " " + e.Message, e);
            }
        }

        /// <summary>
        /// Loads assembly as script library, adding all scripts it contains into script library database.
        /// </summary>
        /// <param name="realAssembly">Script assembly that is to be loaded.</param>
        /// <param name="libraryRoot">Root offset of the script library. All scripts will be loaded with this offset.
        /// Strict behavior forbids conflicts between scriptLibrary and filesystem, all conflicts will be reported as errors.
        /// This is used in the runtime (dynamic include), compiler currently ignores filesystem.
        /// </param>
        public DAssembly LoadScriptLibrary(Assembly/*!*/ realAssembly, string libraryRoot)
        {
            ScriptAssembly scriptAssembly;

            lock (this)
            {
                if (loadedAssemblies.ContainsKey(realAssembly))
                    return loadedAssemblies[realAssembly];

                scriptAssembly = ScriptAssembly.LoadFromAssembly(applicationContext, realAssembly, libraryRoot);
                loadedAssemblies.Add(realAssembly, scriptAssembly);
            }

            applicationContext.ScriptLibraryDatabase.ReflectLibraryNoLock(scriptAssembly);

            return scriptAssembly;
        }
    }

    #endregion AssemblyLoader

    #region ScriptLibraryDatabase
    /// <summary>
    /// Database of library scripts. These scripts are contained in assemblies listed in scriptLibrary configuration section.
    /// Used by DynamicInclude.
    /// </summary>
    internal sealed class ScriptLibraryDatabase
    {
        #region Entry

        /// <summary>
        /// Single entry in the database.
        /// </summary>
        private class Entry
        {
            public FullPath Path;
            public ScriptModule ScriptModule;
            public ScriptAssembly ContainingAssembly;

            public Entry(FullPath path, ScriptModule scriptModule, ScriptAssembly containingAssembly)
            {
                ScriptModule = scriptModule;
                ContainingAssembly = containingAssembly;
                Path = path;
            }
        }

        #endregion

        #region Prepared to be loaded lazily

        /// <summary>
        /// Description of the script library to be loaded later lazily.
        /// </summary>
        internal class ScriptLibraryConfigurationNode
        {
            /// <summary>
            /// The full name of the assembly. If <c>assemblyUri</c> is not provided.
            /// </summary>
            public AssemblyName assemblyName;

            /// <summary>
            /// The full path to the assembly. If <c>assemblyName</c> is not provided.
            /// </summary>
            public Uri assemblyUrl;

            /// <summary>
            /// Relative root path to the scripts in library. Default is ".".
            /// </summary>
            public string libraryRootPath;

            /// <summary>
            /// Value of the <c>url</c> attribute to allow removing the library
            /// by this value within the nested configuration.
            /// </summary>
            public string urlNodeValue;

            /// <summary>
            /// Compares two ScriptLibraryConfigurationNode objects.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                var node = obj as ScriptLibraryConfigurationNode;
                if (node != null)
                {
                    if (string.Equals(node.urlNodeValue, this.urlNodeValue, StringComparison.Ordinal))
                        return true;

                    if (node.assemblyName != null && this.assemblyName != null)
                        return AssemblyName.ReferenceMatchesDefinition(node.assemblyName, this.assemblyName);

                    if (node.assemblyUrl != null && this.assemblyUrl != null)
                        return node.assemblyUrl == this.assemblyUrl;

                    return false;
                }

                return object.ReferenceEquals(this, obj);
            }

            public override int GetHashCode()
            {
                return base.GetHashCode();
            }
        }

        /// <summary>
        /// List of libraries to be loaded lazily.
        /// </summary>
        private List<ScriptLibraryConfigurationNode> libraries = null;

        /// <summary>
        /// Check if the given library was already and returns its index within <see cref="libraries"/>.
        /// </summary>
        private int FindAddedLibrary(ScriptLibraryConfigurationNode/*!*/ desc)
        {
            return libraries.IndexOf(desc);
        }

        /// <summary>
        /// Adds new library to the script library.
        /// </summary>
        /// <returns>True if library was added, false if the library was not added.</returns>
        public bool AddLibrary(string assemblyName, Uri assemblyUrl, string urlNodeValue, string libraryRootPath)
        {
            return AddLibrary(
                new ScriptLibraryDatabase.ScriptLibraryConfigurationNode()
                {
                    assemblyUrl = assemblyUrl,
                    assemblyName = (assemblyName != null) ? new AssemblyName(assemblyName) : null,
                    urlNodeValue = urlNodeValue,
                    libraryRootPath = libraryRootPath
                });
        }

        /// <summary>
        /// Removes specified library from the list of libraries to be loaded lazily.
        /// </summary>
        /// <returns>True if library was removed.</returns>
        public bool RemoveLibrary(string assemblyName, Uri assemblyUrl, string urlNodeValue, string libraryRootPath)
        {
            var existing = FindAddedLibrary(
                new ScriptLibraryDatabase.ScriptLibraryConfigurationNode()
                {
                    assemblyUrl = assemblyUrl,
                    assemblyName = (assemblyName != null) ? new AssemblyName(assemblyName) : null,
                    urlNodeValue = urlNodeValue,
                    libraryRootPath = libraryRootPath
                });

            if (existing >= 0)
            {
                libraries.RemoveAt(existing);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clear the list of libraries to be loaded lazily.
        /// </summary>
        public void ClearLibraries()
        {
            libraries = null;
        }

        /// <summary>
        /// Adds new library to the script library.
        /// </summary>
        /// <param name="desc">Configuration of script library to be added and reflected later.</param>
        /// <returns>True if library was added, false if the library was not added.</returns>
        private bool AddLibrary(ScriptLibraryConfigurationNode/*!*/ desc)
        {
            Debug.Assert(desc != null);

            if (libraries == null)
            {
                libraries = new List<ScriptLibraryConfigurationNode>();
            }
            else
            {
                // check for duplicity
                if (FindAddedLibrary(desc) >= 0)
                    return false;
            }

            // add the library configuration to be loaded lazily
            libraries.Add(desc);
            return true;
        }

        /// <summary>
        /// Ensures that libraries are reflected.
        /// </summary>
        private void EnsureLibrariesReflected()
        {
            if (libraries != null)
                lock (this)
                    if (libraries != null)  // double checked lock
                    {
                        foreach (var lib in libraries)
                        {
                            applicationContext.AssemblyLoader.LoadScriptLibrary(
                                    (lib.assemblyName != null) ? lib.assemblyName.FullName : null,
                                    lib.assemblyUrl,
                                    lib.libraryRootPath);
                        }

                        libraries = null;
                    }
        }

        #endregion

        #region Fields and Properties

        /// <summary>
        /// Database of the library scripts. Cannot be null.
        /// </summary>
        private readonly Dictionary<FullPath, Entry>/*!*/entries = new Dictionary<FullPath, Entry>();

        /// <summary>
        /// Amount of scripts in the database.
        /// </summary>
        public int Count
        {
            get
            {
                EnsureLibrariesReflected();
                return entries.Count;
            }
        }

        /// <summary>
        /// Owning application context.
        /// </summary>
        private readonly ApplicationContext applicationContext;

        #endregion

        #region Construction

        /// <summary>
        /// Creates new ScriptLibraryDatabase object.
        /// </summary>
        /// <param name="context">Owning application context.</param>
        public ScriptLibraryDatabase(ApplicationContext context)
        {
            applicationContext = context;
        }

        #endregion

        #region ScriptLibrary methods

        /// <summary>
        /// Reflect given scriptAssembly and add its modules into <c>entries</c>.
        /// </summary>
        /// <param name="scriptAssembly"><c>ScriptAssembly</c> to be reflected.</param>
        internal void ReflectLibraryNoLock(ScriptAssembly scriptAssembly)
        {
            foreach (ScriptModule module in scriptAssembly.GetModules())
            {
                FullPath fullPath = new FullPath(module.RelativeSourcePath, Configuration.Application.Compiler.SourceRoot);

                Entry entry = new Entry(fullPath, module, scriptAssembly);

                if (!entries.ContainsKey(fullPath))
                {
                    entries.Add(fullPath, entry);
                }
            }
        }

        /// <summary>
        /// Gets a known library script.
        /// </summary>
        /// <param name="path">Application config-dependent path of the script.</param>
        /// <returns>Library script corresponding to the supplied path, or null if there is no such script present in the script library.</returns>
        public ScriptModule GetScriptModule(FullPath path)
        {
            EnsureLibrariesReflected();

            Entry entry;
            if (entries.TryGetValue(path, out entry) && entry != null)
                return entry.ScriptModule;

            return null;
        }

        /// <summary>
        /// Returns a value indicating whether the script library database contains given script.
        /// </summary>
        /// <param name="path">Application config-dependent path of the script.</param>
        /// <returns>True if the script is contained within the script library. Otherwise false.</returns>
        public bool ContainsScript(FullPath path)
        {
            EnsureLibrariesReflected();
            return entries.ContainsKey(path);
        }

        #endregion
    }
    #endregion
}