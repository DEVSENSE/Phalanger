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

#if SILVERLIGHT
using PHP.CoreCLR;
using System.Windows.Browser;
#else
using System.Web;
#endif

namespace PHP.Core
{
	[DebuggerNonUserCode]
	public sealed partial class ApplicationContext
	{
		#region Properties

		/// <summary>
		/// Whether full reflection of loaded libraries should be postponed until really needed.
		/// Set to <B>false</B> by command line compiler (phpc) and web server manager as they both need
		/// to compile source files. 
		/// </summary>
		public bool LazyFullReflection { get { return lazyFullReflection; } }
		private bool lazyFullReflection;

		internal Dictionary<string, DTypeDesc>/*!*/ Types { get { Debug.Assert(types != null); return types; } }
		private readonly Dictionary<string, DTypeDesc> types;

		internal Dictionary<string, DRoutineDesc>/*!*/ Functions { get { Debug.Assert(functions != null); return functions; } }
		private readonly Dictionary<string, DRoutineDesc> functions;

		internal DualDictionary<string, DConstantDesc>/*!*/ Constants { get { Debug.Assert(constants != null); return constants; } }
		private readonly DualDictionary<string, DConstantDesc> constants;

		/// <summary>
		/// Associated assembly loader.
		/// </summary>
		/// <exception cref="InvalidOperationException">Context is readonly.</exception>
		public AssemblyLoader/*!*/ AssemblyLoader
		{
			get
			{
				Debug.Assert(assemblyLoader != null, "Empty application context doesn't have a loader.");
				return assemblyLoader;
			}
		}
		private readonly AssemblyLoader assemblyLoader;

		/// <summary>
		/// Assembly builder where compiled pieces of eval'd code are stored.
		/// </summary>
		internal TransientAssemblyBuilder/*!*/ TransientAssemblyBuilder
		{
			get
			{
				if (transientAssemblyBuilder == null)
					throw new InvalidOperationException();

				return transientAssemblyBuilder;
			}
		}
		private readonly TransientAssemblyBuilder transientAssemblyBuilder;

		public bool HasTransientAssemblyBuilder { get { return transientAssemblyBuilder != null; } }

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

		#region Default Contexts

		private static object/*!*/ mutex = new object();

		/// <summary>
		/// Default context.
		/// </summary>
		public static ApplicationContext/*!*/ Default
		{
			get
			{
				if (_defaultContext == null)
					DefineDefaultContext(true, false, true);
				return _defaultContext;
			}
		}
		private static ApplicationContext _defaultContext; // lazy

		public static bool DefineDefaultContext(bool lazyFullReflection, bool reflectionOnly, bool createTransientBuilder)
		{
			bool created = false;

			if (_defaultContext == null)
			{
				lock (mutex)
				{
					if (_defaultContext == null)
					{
						_defaultContext = new ApplicationContext(lazyFullReflection, reflectionOnly, createTransientBuilder);
						created = true;
					}
				}
			}

			return created;
		}

		internal static readonly ApplicationContext/*!*/ Empty = new ApplicationContext();

		#endregion

		#region Construction

		private ApplicationContext()
		{
		}

		public ApplicationContext(bool lazyFullReflection, bool reflectionOnly, bool createTransientBuilder)
		{
			this.lazyFullReflection = lazyFullReflection;

			this.assemblyLoader = new AssemblyLoader(this, reflectionOnly);
			this.transientAssemblyBuilder = createTransientBuilder ? new TransientAssemblyBuilder(this) : null;

			this.types = new Dictionary<string, DTypeDesc>(StringComparer.OrdinalIgnoreCase);
			this.functions = new Dictionary<string, DRoutineDesc>(StringComparer.OrdinalIgnoreCase);
			this.constants = new DualDictionary<string, DConstantDesc>(null, StringComparer.OrdinalIgnoreCase);
            this.scriptLibraryDatabase = new ScriptLibraryDatabase(this);

			PopulateTables();
		}

		#endregion

		#region Initialization

        private void PopulateTables()
        {
            // primitive types (prefixed by '@' to prevent ambiguities with identifiers, e.g. i'Array'):
            types.Add("@" + QualifiedName.Integer.Name.Value, DTypeDesc.IntegerTypeDesc);
            types.Add("@" + QualifiedName.Boolean.Name.Value, DTypeDesc.BooleanTypeDesc);
            types.Add("@" + QualifiedName.LongInteger.Name.Value, DTypeDesc.LongIntegerTypeDesc);
            types.Add("@" + QualifiedName.Double.Name.Value, DTypeDesc.DoubleTypeDesc);
            types.Add("@" + QualifiedName.String.Name.Value, DTypeDesc.StringTypeDesc);
            types.Add("@" + QualifiedName.Resource.Name.Value, DTypeDesc.ResourceTypeDesc);
            types.Add("@" + QualifiedName.Array.Name.Value, DTypeDesc.ArrayTypeDesc);
            types.Add("@" + QualifiedName.Object.Name.Value, DTypeDesc.ObjectTypeDesc);

            // types implemented in Core
            Action<Type> addType = (x) => { types.Add(x.Name, DTypeDesc.Create(x)); };

            addType(typeof(Library.stdClass));
            addType(typeof(Library.__PHP_Incomplete_Class));
            addType(typeof(Library.EventClass<>));
            addType(typeof(Library.SPL.ArrayAccess));
            addType(typeof(Library.SPL.Exception));
            addType(typeof(Library.SPL.Traversable));
            addType(typeof(Library.SPL.Iterator));
            addType(typeof(Library.SPL.IteratorAggregate));
            addType(typeof(Library.SPL.Serializable));
            addType(typeof(Library.SPL.Countable));
            addType(typeof(Library.SPL.Reflector));

            // primitive constants
            constants.Add("TRUE", GlobalConstant.True.ConstantDesc, true);
            constants.Add("FALSE", GlobalConstant.False.ConstantDesc, true);
            constants.Add("NULL", GlobalConstant.Null.ConstantDesc, true);

            // the constants are same for all platforms (Phalanger use Int32 for integers in PHP):
            constants.Add("PHP_INT_SIZE", GlobalConstant.PhpIntSize.ConstantDesc, false);
            constants.Add("PHP_INT_MAX", GlobalConstant.PhpIntMax.ConstantDesc, false);
        }

		internal void LoadModuleEntries(DModule/*!*/ module)
		{
			module.Reflect(!lazyFullReflection, types, functions, constants);
		}

		#endregion

		#region Libraries

		public List<DAssembly>/*!*/ GetLoadedAssemblies()
		{
			return assemblyLoader.GetLoadedAssemblies<DAssembly>();
		}

		public IEnumerable<PhpLibraryAssembly>/*!*/ GetLoadedLibraries()
		{
			foreach (PhpLibraryAssembly library in assemblyLoader.GetLoadedAssemblies<PhpLibraryAssembly>())
				yield return library;
		}

		public IEnumerable<string>/*!*/ GetLoadedExtensions()
		{
            //if (assemblyLoader.ReflectionOnly)
            //    throw new InvalidOperationException("Cannot retrieve list of extensions loaded for reflection only");
			
			foreach (PhpLibraryAssembly library in assemblyLoader.GetLoadedAssemblies<PhpLibraryAssembly>())
			{
				foreach (string name in library.ImplementedExtensions)
					yield return name;
			}
		}

		/// <summary>
		/// Finds a library among currently loaded ones that implements an extension with a specified name.
		/// </summary>
		/// <param name="name">The name of the extension to look for.</param>
		/// <returns>The library descriptor.</returns>
		/// <remarks>Not thread-safe. Not available at compilation domain.</remarks>
		public PhpLibraryDescriptor/*!*/ GetExtensionImplementor(string name)
		{
			if (assemblyLoader.ReflectionOnly)
				throw new InvalidOperationException("Cannot retrieve list of extensions loaded for reflection only");
			
			foreach (PhpLibraryAssembly library in assemblyLoader.GetLoadedAssemblies<PhpLibraryAssembly>())
			{
				if (CollectionUtils.ContainsString(library.ImplementedExtensions, name, true))
					return library.Descriptor;
			}

			return null;
		}

		#endregion

		#region Helpers

		public IEnumerable<KeyValuePair<string, DRoutineDesc>> GetFunctions()
		{
			return functions;
		}

		public DRoutine GetFunction(QualifiedName qualifiedName, ref string/*!*/ fullName)
		{
			if (fullName == null)
				fullName = qualifiedName.ToString();

			DRoutineDesc desc;
			return (functions.TryGetValue(fullName, out desc)) ? desc.Routine : null;
		}

		public DType GetType(QualifiedName qualifiedName, ref string/*!*/ fullName)
		{
			if (fullName == null)
				fullName = qualifiedName.ToString();

			DTypeDesc desc;
			return (types.TryGetValue(fullName, out desc)) ? desc.Type : null;
		}

		public GlobalConstant GetConstant(QualifiedName qualifiedName, ref string/*!*/ fullName)
		{
			if (fullName == null)
				fullName = qualifiedName.ToString();

			DConstantDesc desc;
			return (constants.TryGetValue(fullName, out desc)) ? desc.GlobalConstant : null;
		}

		/// <summary>
		/// Declares a PHP type globally. Replaces any previous declaration.
		/// To be called from the compiled scripts before library loading; libraries should check for conflicts.
		/// </summary>
		[Emitted]
		public void DeclareType(DTypeDesc/*!*/ typeDesc, string/*!*/ fullName)
		{
			types[fullName] = typeDesc;
		}

		/// <summary>
		/// Declares a PHP type globally. Replaces any previous declaration.
		/// To be called from the compiled scripts before library loading; libraries should check for conflicts.
		/// </summary>
		[Emitted]
		public void DeclareType(RuntimeTypeHandle/*!*/ typeHandle, string/*!*/ fullName)
		{
			types[fullName] = DTypeDesc.Create(typeHandle);
		}

		/// <summary>
		/// Declares a PHP function globally. Replaces any previous declaration.
		/// To be called from the compiled scripts before library loading; libraries should check for conflicts.
		/// </summary>
		[Emitted]
		public void DeclareFunction(RoutineDelegate/*!*/ arglessStub, string/*!*/ fullName, PhpMemberAttributes memberAttributes)
		{
			functions[fullName] = new PhpRoutineDesc(memberAttributes, arglessStub);
		}

		/// <summary>
		/// Declares a PHP constant globally. Replaces any previous declaration.
		/// To be called from the compiled scripts before library loading; libraries should check for conflicts.
		/// </summary>
		[Emitted]
		public void DeclareConstant(string/*!*/ fullName, object value)
		{
			constants[fullName, false] = new DConstantDesc(UnknownModule.RuntimeModule, PhpMemberAttributes.None, value);
		}

		/// <summary>
		/// Checkes whether a type is transient.
		/// </summary>
		public bool IsTransientRealType(Type/*!*/ realType)
		{
			return transientAssemblyBuilder.IsTransientRealType(realType);
		}

		#endregion
	}

	#region AssemblyLoader

	public sealed class AssemblyLoader
	{
		/// <summary>
		/// The owning AC.
		/// </summary>
		private readonly ApplicationContext/*!*/ applicationContext;

		public bool ReflectionOnly { get { return reflectionOnly; } }
		private readonly bool reflectionOnly;

		public bool ClrReflectionOnly { get { return clrReflectionOnly; } }
		private readonly bool clrReflectionOnly;
		
		/// <summary>
		/// Loaded assemblies. Contains all instances loaded by the loader. Synchronized.
		/// </summary>
		private readonly Dictionary<Assembly, DAssembly>/*!!*/ loadedAssemblies = new Dictionary<Assembly, DAssembly>();


		internal AssemblyLoader(ApplicationContext/*!*/ applicationContext, bool reflectionOnly)
		{
			this.applicationContext = applicationContext;
			this.reflectionOnly = reflectionOnly;
			
			// not supported yet:
			this.clrReflectionOnly = false;
		}

		internal Assembly LoadRealAssembly(string/*!*/ target)
		{
#if SILVERLIGHT
			return Assembly.Load(target);
#else
			return (clrReflectionOnly) ? Assembly.ReflectionOnlyLoad(target) : Assembly.Load(target);
#endif
		}

		internal Assembly LoadRealAssemblyFrom(string/*!*/ target)
		{
#if SILVERLIGHT
			return Assembly.LoadFrom(target);
#else
			return (clrReflectionOnly) ? Assembly.ReflectionOnlyLoadFrom(target) : Assembly.LoadFrom(target);
#endif
		}

		public List<T> GetLoadedAssemblies<T>()
			where T : DAssembly
		{
			lock (this)
			{
				List<T> result = new List<T>(loadedAssemblies.Count);

				foreach (DAssembly loaded_assembly in loadedAssemblies.Values)
				{
					T assembly = loaded_assembly as T;
					if (assembly != null)
						result.Add(assembly);
				}

				return result;
			}
		}

		/// <summary>
		/// Loads a library assembly given its name and configuration node.
		/// </summary>
		/// <param name="assemblyName">Long assembly name (see <see cref="Assembly.Load"/>) or a <B>null</B> reference.</param>
		/// <param name="assemblyUrl">Assembly file absolute URI or a <B>null</B> reference.</param>
		/// <param name="config">Configuration node describing the assembly to load (or a <B>null</B> reference).</param>
		/// <exception cref="ConfigurationErrorsException">An error occured while loading the library.</exception>
		public DAssembly/*!*/ Load(string assemblyName, Uri assemblyUrl, LibraryConfigStore config)
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

					return Load(LoadRealAssembly(target), config);
				}
				else
				{
					// load by URI:
					target = HttpUtility.UrlDecode(assemblyUrl.AbsoluteUri);

					return Load(LoadRealAssemblyFrom(target), config);
				}
			}
			catch (Exception e)
			{
				throw new ConfigurationErrorsException
					(CoreResources.GetString("library_assembly_loading_failed", target) + " " + e.Message, e);
			}
		}

		public DAssembly/*!*/ Load(Assembly/*!*/ realAssembly, LibraryConfigStore config)
		{
			Debug.Assert(realAssembly != null);

			DAssembly assembly;

			lock (this)
			{
				if (loadedAssemblies.TryGetValue(realAssembly, out assembly))
					return assembly;

				assembly = DAssembly.CreateNoLock(applicationContext, realAssembly, config);

				loadedAssemblies.Add(realAssembly, assembly);

				// load the members contained in the assembly to the global tables:
				applicationContext.LoadModuleEntries(assembly.ExportModule);
			}

			if (!reflectionOnly)
				assembly.LoadCompileTimeReferencedAssemblies(this);

			return assembly;
		}

		/// <summary>
		/// Loads assemblies whose paths or full names are listed in references.
		/// </summary>
		/// <param name="references">Enumeration of paths to or full names of assemblies to load.</param>
		/// <exception cref="ArgumentNullException"><paramref name="references"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ConfigurationErrorsException">An error occured while loading a library.</exception>
		public void Load(IEnumerable<string>/*!*/ references)
		{
			if (references == null)
				throw new ArgumentNullException("references");

			foreach (string reference in references)
			{
                LoadReference(reference);
			}
		}

        /// <summary>
        /// Loads single reference.
        /// </summary>
        /// <param name="reference">Path to or full name of references assembly.</param>
        private void LoadReference(string reference)
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
                LoadScriptLibrary(realAssembly, ".");

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

                scriptAssembly = ScriptAssembly.LoadFromAssembly(applicationContext, realAssembly);
                loadedAssemblies.Add(realAssembly, scriptAssembly);
            }

            applicationContext.ScriptLibraryDatabase.ReflectLibraryNoLock(scriptAssembly);

            return scriptAssembly;
        }
	}

	#endregion

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
            /// Compares two ScriptLibraryConfigurationNode objects.
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public override bool Equals(object obj)
            {
                var node = obj as ScriptLibraryConfigurationNode;
                if (node != null)
                {
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
        /// Check if the given library was already and returns its config.
        /// </summary>
        /// <param name="desc"></param>
        /// <returns></returns>
        private ScriptLibraryConfigurationNode FindAddedLibrary(ScriptLibraryConfigurationNode/*!*/ desc)
        {
            if (libraries != null)
                foreach (var lib in libraries)
                {
                    if (lib.Equals(desc))
                        return lib;
                }

            return null;
        }

        /// <summary>
        /// Adds new library to the script library.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="assemblyUrl"></param>
        /// <param name="libraryRootPath"></param>
        /// <returns>True if library was added, false if the library was not added.</returns>
        public bool AddLibrary(string assemblyName, Uri assemblyUrl, string libraryRootPath)
        {
            return AddLibrary(
                new ScriptLibraryDatabase.ScriptLibraryConfigurationNode()
                  {
                      assemblyUrl = assemblyUrl,
                      assemblyName = (assemblyName != null) ? new AssemblyName(assemblyName) : null,
                      libraryRootPath = libraryRootPath
                  });
        }

        /// <summary>
        /// Removes specified library from the list of libraries to be loaded lazily.
        /// </summary>
        /// <param name="assemblyName"></param>
        /// <param name="assemblyUrl"></param>
        /// <param name="libraryRootPath"></param>
        /// <returns>True if library was removed.</returns>
        public bool RemoveLibrary(string assemblyName, Uri assemblyUrl, string libraryRootPath)
        {
            var existing = FindAddedLibrary(
                new ScriptLibraryDatabase.ScriptLibraryConfigurationNode()
                {
                    assemblyUrl = assemblyUrl,
                    assemblyName = (assemblyName != null) ? new AssemblyName(assemblyName) : null,
                    libraryRootPath = libraryRootPath
                });

            if (existing != null)
            {
                return libraries.Remove(existing);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Clear the list of libraries to be loaded lazily.
        /// </summary>
        /// <param name="obj">Not used.</param>
        public void ClearLibraries(object obj)
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
                libraries = new List<ScriptLibraryConfigurationNode>();
            else
            {
                // check for duplicity
                if (FindAddedLibrary(desc) != null)
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
            if (libraries  != null)
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
        /// Database of the library scripts.
        /// </summary>
        private readonly Dictionary<FullPath, Entry> entries;

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
            entries = new Dictionary<FullPath, Entry>();
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

            if (entries.ContainsKey(path))
            {
                return entries[path].ScriptModule;
            }

            return null;
        }

        /// <summary>
        /// Returns a value indicating whether the script library database contains given script.
        /// </summary>
        /// <param name="path">Application config-dependent path of the script.</param>
        /// <returns>True if the script is contained within the script library. Otherwise false.</returns>
        public Boolean ContainsScript(FullPath path)
        {
            EnsureLibrariesReflected();

            return entries.ContainsKey(path);
        }

        #endregion
    }
    #endregion
}
