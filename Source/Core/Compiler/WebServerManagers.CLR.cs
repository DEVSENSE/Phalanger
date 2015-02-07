/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Web;
using System.Web.Caching;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Globalization;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Configuration.Assemblies;
using System.Runtime.Remoting;

using PHP.Core.Emit;
using PHP.Core.Reflection;
using System.Text.RegularExpressions;

namespace PHP.Core
{
	#region WebCompilationContext

	/// <summary>
	/// Compilation context used by the manager.
	/// </summary>
	internal sealed class WebCompilationContext : CompilationContext
	{
		/// <summary>
		/// A timestamp of the current request.
		/// </summary>
		public DateTime RequestTimestamp { get { return requestTimestamp; } }
		private readonly DateTime requestTimestamp;

        public override bool SaveOnlyAssembly
        {
            get
            {
                return ((WebServerCompilerManager)Manager).SaveOnlyAssembly;   // in debug mode, do not load built assemblies into memory
            }
        }

        public WebCompilationContext(ApplicationContext applicationContext, ICompilerManager/*!*/ manager, CompilerConfiguration/*!*/ config, string/*!*/ workingDirectory,
		  DateTime requestTimestamp)
			: base(applicationContext, manager, config, new WebErrorSink(config.Compiler.DisabledWarnings, config.Compiler.DisabledWarningNumbers), workingDirectory)
		{
			this.requestTimestamp = requestTimestamp;
		}

		#region Assembly Naming

		/// <summary>
		/// Translates a source path to an assembly coded name.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <param name="config">The compiler configuration.</param>
		/// <returns>
		/// The code name consisting of significant configuration hashcode and source 
		/// path relative to the application source root.
		/// Format of the name: <code>{relativized path}(~{level_count})?#{config hash}#</code>
		/// Backslashes and colons are replaced with underscores, underscores are doubled.
		/// </returns>
		public static string GetAssemblyCodedName(PhpSourceFile/*!*/ sourceFile, CompilerConfiguration/*!*/ config)
		{
			RelativePath rp = sourceFile.RelativePath;
			StringBuilder sb = new StringBuilder(rp.Path);

			if (rp.Level >= 0)
			{
				sb.Append('~');
				sb.Append(rp.Level);
			}

			sb.Append('#');
			sb.Append(config.Compiler.HashCode.ToString("x"));
			sb.Append('#');

            return sb.Replace("_", "__").Replace('/', '_').Replace('\\', '_').Replace(':', '_').ToString();
		}

		/// <summary>
		/// Translates a source path to an assembly coded name.
		/// </summary>
		public string GetAssemblyCodedName(PhpSourceFile/*!*/ sourceFile)
		{
			return GetAssemblyCodedName(sourceFile, config);
		}

		/// <summary>
		/// Gets a full name of an assembly in which a specified source script is compiled.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <returns>The assembly full name.</returns>
		/// <remarks>A name of the assembly consists of a hexa-timestamp and a assembly coded name.</remarks>
		public AssemblyName GetAssemblyFullName(PhpSourceFile/*!*/ sourceFile)
		{
			// timestamp ensures there won't be two loaded assemblies of the same name:
			// (consider editing of a source file)
			string stamp = requestTimestamp.Ticks.ToString("x16");

			AssemblyName result = new AssemblyName();
			result.Name = GetAssemblyCodedName(sourceFile, config) + stamp;
			result.Version = new Version(1, 0, 0, 0);

			return result;
		}

		/// <summary>
		/// Extracts assembly coded name from its full name.
		/// </summary>
		/// <param name="name">The full name of the assembly.</param>
		/// <returns>The coded name.</returns>
		public string ParseAssemblyFullName(AssemblyName/*!*/ name)
		{
			// the last 16 characters contains hexa-timestamp:
			return name.Name.Substring(0, name.Name.Length - 16);
		}

		public string ParseAssemblyFullName(string/*!*/ name)
		{
			// the last 16 + 4 characters contains hexa-timestamp and extension:
			return name.Substring(0, name.Length - 16 - 4);
		}

		#endregion
	}

	#endregion

	#region Compiler Manager

	/// <summary>
	/// Manager for a compiling a web aplication by multiple compilers in parallel.
	/// Designed to be able to work in remote appdomain, yet this feature is currently not utilized.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Cache maps <I>coded assembly names</I> to <I>assembly file paths</I>.
	/// In addition, a set of dependencies is added to each cache item. These dependencies comprises of
	/// <list type="bullet">
	///   <item>source script file</item>
	///   <item>cache items representing included script</item>
	/// </list>
	/// Cache item key should be determined by script source path which is the only information
	/// the compiler provides to the manager. Thus, it equals to the coded name of the assembly got by
	/// <see cref="WebCompilationContext.GetAssemblyCodedName"/>.
	/// </para>
	/// <para>
	/// Assembly name is composed from request timestamp and coded assembly name.
	/// Assembly file name equals to the assembly name suffixed with .dll extension.
	/// <see cref="SingleScriptAssemblyBuilder"/> is used to build assemblies up.
	/// </para>
	/// </remarks>
	/// <threadsafety static="true" instance="true"/>
	internal class WebServerCompilerManager : ICompilerManager
	{
		#region Constants, Fields

		/// <summary>
		/// The number of attepts which are made to get an assembly which is being compiled by another thread
		/// before this thread starts its own compilaation. Each attempt is limited in time by <see cref="CompilationTimeout"/>.
		/// </summary>
		private const int AttemptsToGetCompiledAssembly = 3;
		private const int CompilationTimeout = 2 * 60 * 1000; // = 2 minutes

		private const int AttemptsToCompileScript = 5;
		private const string AssemblyExt = ".dll";

		private const string TemporaryFilesSearchPattern = ".*\\#(?<Stamp>[0-9a-f]*)\\.dll";

        /// <summary>
        /// Decide whether allow loading built assemblies into memory. This would disallows proper debugging.
        /// </summary>
        public bool SaveOnlyAssembly
        {
            get
            {
                return Configuration.Application.Compiler.Debug;   // in debug mode, do not load built assemblies into memory
                // only when Debugger.IsAttached ?
            }
        }

		/// <summary> Output directory. </summary>
		private readonly string/*!*/ outDir;

		public ApplicationContext/*!*/ ApplicationContext { get { return applicationContext; } }
		private readonly ApplicationContext/*!*/ applicationContext;

		/// <summary>
		/// A table of <see cref="ManualResetEvent"/>s on which threads are waiting when more than one thread 
		/// requires to compile one script. 
		/// </summary>
		private readonly Dictionary<PhpSourceFile, ManualResetEvent>/*!*/ events;
		private object/*!*/ eventsMutex = new object();
		
		private MultiScriptAssembly precompiledAssembly;
		private volatile bool precompiledAssemblyProbed = false;
		private object/*!*/ precompiledLoadMutex = new object();
		
		/// <summary>
		/// Maps subnamespaces to the cache entries describing source file timestamps and state.
		/// </summary>
		private Dictionary<string, CacheEntry> cache;
		
		private readonly ReaderWriterLockSlim/*!*/cacheLock = new ReaderWriterLockSlim();

        /// <summary>Source files watcher. Can be <c>null</c> reference if <c>WatchSourceChanges</c> is disabled.</summary>
		private FileSystemWatcher watcher;

		/// <summary>Searching for precompiled files in ASP.NET temporary files.</summary>
		private static Regex reFileStamp = new Regex(TemporaryFilesSearchPattern, RegexOptions.Compiled);

        /// <summary>
        /// Time of AppCode assembly was created. Any SSA compiled before this time should be recompiled.
        /// </summary>
        private DateTime appCodeAssemblyCreated = DateTime.MinValue;

		#endregion

		#region Construction

		/// <summary>
		/// Creates a new instance of the manager. 
		/// Manager can be instantiated either in dedicated domain or in web AppDomain.
		/// </summary>
		/// <param name="appContext">Application context.</param>
		public WebServerCompilerManager(ApplicationContext/*!*/ appContext)
		{
			Debug.Assert(appContext != null);

            bool isWebApp = HttpContext.Current != null;    // whether we are running web app or an executable app

            this.outDir = isWebApp ? HttpRuntime.CodegenDir : Path.GetTempPath();
			this.events = new Dictionary<PhpSourceFile, ManualResetEvent>();
			this.applicationContext = appContext;

            // On Windows it's case-insensitive, because same file can be accessed with various cases
            cache = new Dictionary<string, CacheEntry>(100, FullPath.StringComparer);

            // install file system watcher to invalidate cache of files that have been modified:
            if (isWebApp &&
                Configuration.Application.Compiler.WatchSourceChanges &&
                !Configuration.Application.Compiler.OnlyPrecompiledCode)
            {
                watcher = new FileSystemWatcher()
                {
                    // TODO: multiple paths (multiple watchers?):
                    Path = Configuration.Application.Compiler.SourceRoot.ToString(),
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                    IncludeSubdirectories = true,
                    EnableRaisingEvents = false,
                };

                watcher.Changed += OnFileChanged;
                watcher.Renamed += OnFileRenamed;
                watcher.Deleted += OnFileChanged;
            }
            else
            {
                watcher = null;
            }

            // look for "App_Code.compiled" file
            if (isWebApp)
                LoadAppCode(Path.Combine(HttpRuntime.CodegenDir, "App_Code.compiled"));
		}

        /// <summary>
        /// Try to load assembly containing App_Code compiled files.
        /// </summary>
        /// <param name="app_code_compiled_path"><c>App_Code.compiled</c> XML containing path to the assembly. The file is generated by ASP.NET.</param>
        private void LoadAppCode(string app_code_compiled_path)
        {
            if (!File.Exists(app_code_compiled_path))
                return;

            var xml = new System.Xml.XmlDocument();
            xml.Load(app_code_compiled_path);
            var node = xml.DocumentElement.SelectSingleNode(@"/preserve[@assembly]");
            if (node != null)
            {
                var assemblyFile = Path.Combine(Path.GetDirectoryName(app_code_compiled_path), node.Attributes["assembly"].Value + ".dll");
                appCodeAssemblyCreated = File.GetLastWriteTime(assemblyFile);
                ApplicationContext.AssemblyLoader.Load(Assembly.LoadFrom(assemblyFile), null);
            }
        }
		
        static WebServerCompilerManager()
        {
            // initialize locks used for locking of SSA loading
            tempLoadLocks = new object[32];
            for (int i = 0; i < tempLoadLocks.Length; ++i)
                tempLoadLocks[i] = new object();
        }

		#endregion

		#region ICompilerManager Members

		/// <summary>
		/// Checks whether a specified source file needs to be (re)compiled and if so locks 
		/// it so that any other compiler from the current app domain will wait until this compilation finishes.
		/// </summary>
		/// <param name="sourceFile">Source file.</param>
		/// <param name="ctx">Compilation context.</param>
		/// <returns>
		/// A compiled module associated with the <paramref name="sourceFile"/> or a <B>null</B> reference
		/// if a compilation of that file is needed.
		/// </returns>
		public PhpModule LockForCompiling(PhpSourceFile/*!*/ sourceFile, CompilationContext/*!*/ ctx)
		{
			Debug.Assert(ctx is WebCompilationContext);
			WebCompilationContext context = (WebCompilationContext)ctx;

            // take a look into script library first
            if (applicationContext.ScriptLibraryDatabase.ContainsScript(sourceFile.FullPath))
            {
                return applicationContext.ScriptLibraryDatabase.GetScriptModule(sourceFile.FullPath);
            }

			for (int i = 0; i < AttemptsToGetCompiledAssembly; i++)
			{
				string ns = ScriptModule.GetSubnamespace(sourceFile.RelativePath, false);

				CacheEntry cache_entry;

				// TODO: Single script assemblies can be loaded and reflected 
				//       but this still have to be done for MSAs
				if (TryLoadCachedEntry(ns, sourceFile, out cache_entry) && !cache_entry.ScriptAssembly.IsMultiScript)
					return cache_entry.ScriptAssembly.GetModule(sourceFile);

				// compilation is in progress or not started yet //
				ManualResetEvent compilation_finished;

				lock (eventsMutex)
				{
					// if compilation of the target file has not started yet:
					if (!events.TryGetValue(sourceFile, out compilation_finished))
					{
						// adds event which others wait on:
						events.Add(sourceFile, new ManualResetEvent(false));

						return null;
					}
				}

				// waits until compilation is finished and assembly has been persisted:
				compilation_finished.WaitOne(CompilationTimeout, false);
			}

			return null;
		}

		/// <summary>
		/// Creates a new instance of <see cref="ScriptBuilder"/> to be used for compilation of the script's assembly.
		/// </summary>
		/// <param name="compiledUnit">Unit being compiled.</param>
		/// <param name="ctx">The current compilation context.</param>
		/// <returns>The script builder.</returns>
		public IPhpModuleBuilder DefineModuleBuilder(CompilationUnitBase/*!*/ compiledUnit, CompilationContext/*!*/ ctx)
		{
			Debug.Assert(compiledUnit is ScriptCompilationUnit && ctx is WebCompilationContext);
			WebCompilationContext context = (WebCompilationContext)ctx;
			ScriptCompilationUnit unit = (ScriptCompilationUnit)compiledUnit;

			// creates an assembly name:
			AssemblyName name = context.GetAssemblyFullName(unit.SourceUnit.SourceFile);

			// creates a script assembly builder:
			SingleScriptAssemblyBuilder builder = new SingleScriptAssemblyBuilder(applicationContext,
				name, outDir, name.Name + AssemblyExt, AssemblyKinds.WebPage, context.Config.Compiler.Debug, false, context.SaveOnlyAssembly, null);
            
			return builder.DefineScript(unit);
		}

		/// <summary>
		/// Persists a built script to a file.
		/// </summary>
		/// <param name="compilationUnit">The unit being compiled.</param>
		/// <param name="ctx">Compilation context.</param>
		public void Persist(CompilationUnitBase/*!*/ compilationUnit, CompilationContext/*!*/ ctx)
		{
			Debug.Assert(compilationUnit is ScriptCompilationUnit && ctx is WebCompilationContext);

			WebCompilationContext context = (WebCompilationContext)ctx;
			ScriptCompilationUnit unit = (ScriptCompilationUnit)compilationUnit;
			SingleScriptAssemblyBuilder assembly_builder = (SingleScriptAssemblyBuilder)unit.ModuleBuilder.AssemblyBuilder;

			assembly_builder.Save();

            string ns = ScriptModule.GetSubnamespace(unit.SourceUnit.SourceFile.RelativePath, false);

            if (SaveOnlyAssembly)
            {
                // assembly not loaded into memory yet (we need to load from fs to not break debugging)
                string file = assembly_builder.Assembly.Path;

                CacheEntry entry;
                LoadSSA(ns, file, out entry);
            }
            else
            {
                // We only add the assembly into the cache, if it was built and loaded into memory.
                // Otherwise the assembly has to be reloaded from the disk.
                // This is because of debugging, since we don't want to load dynamic assemblies into memory, which breaks debug symbols.

                // makes up a list of dependent assembly names:
                string[] includers = new string[unit.Includers.Count];
                int i = 0;
                foreach (StaticInclusion inclusion in unit.Includers)
                    includers[i++] = ScriptModule.GetSubnamespace(inclusion.Includer.SourceUnit.SourceFile.RelativePath, false);

                // what assemblies are included by this one?
                string[] inclusions = new string[unit.Inclusions.Count];
                int j = 0;
                foreach (StaticInclusion inclusion in unit.Inclusions)
                    inclusions[j++] = ScriptModule.GetSubnamespace(new RelativePath(0, inclusion.Includee.RelativeSourcePath), false);

                // adds dependencies on the source file and the included assemblies:
                SetCacheEntry(ns,
                    new CacheEntry(
                        assembly_builder.SingleScriptAssembly.GetScriptType(),
                        assembly_builder.SingleScriptAssembly, context.RequestTimestamp, includers, inclusions, true), true, true);
            }
		}

		/// <summary>
		/// Wakes up threads waiting for a script compilation finish.
		/// </summary>
		/// <param name="sourceFile">The compiled script's source file.</param>
		/// <param name="successful">Whether compilation has been successful.</param>
		/// <param name="ctx">A compilation context.</param>
		/// <remarks>Should be called after <see cref="Persist"/>.</remarks>
		public void UnlockForCompiling(PhpSourceFile/*!*/ sourceFile, bool successful, CompilationContext ctx)
		{
			Debug.Assert(sourceFile != null && ctx is WebCompilationContext);

			ManualResetEvent compilation_finished;

			lock (eventsMutex)
			{
				if (events.TryGetValue(sourceFile, out compilation_finished))
					events.Remove(sourceFile);
			}

			// any waiting thread can access the compiled assembly now:
			if (compilation_finished != null)
				compilation_finished.Set();
		}

		/// <summary>
		/// Called by compiler when information about compiling progress is available.
		/// </summary>
		/// <remarks>Ignored by this manager.</remarks>
		public void Info(PhpSourceFile/*!*/ sourceFile, CompilationContext ctx)
		{
			// nop //
		}

		public void Finish(bool successfull)
		{
			// nop //
		}

		#endregion
		
		#region Cache
		
		/// <summary>
		/// Structure that contains loaded scripts. The cache is built while application is 
		/// running and is lost when the AppDomain is reloaded.
		/// </summary>
		internal class CacheEntry
		{
			#region Members

            public ScriptInfo/*!*/ ScriptInfo { get { return scriptInfo; } }
			private readonly ScriptInfo/*!*/ scriptInfo;

			public ScriptAssembly/*!*/ ScriptAssembly { get { return scriptAssembly; } }
			private readonly ScriptAssembly/*!*/ scriptAssembly;

			public DateTime BuildTimestamp { get { return buildTimeStamp; } }
			private readonly DateTime buildTimeStamp;

			/// <summary>
			/// Collection of scripts that include this script - if the item is invalidated
			/// all includers must be invalidated too
			/// </summary>
			public List<string>/*!*/ Includers { get { return includers; } }
			private List<string>/*!*/ includers;

			/// <summary>
			/// Collection of included scripts - after loading (from temp files or when 
			/// application starts) we need to check whether all included files are up-to-date
			/// </summary>
			public List<string>/*!*/ Includees { get { return includees; } }
			private List<string>/*!*/ includees;

			/// <summary>
			/// Whether the source file time-stamp has been checked. Precompiled source files need to be checked because
			/// thay may have been modified prior to the start of the watcher. Although it would be possible to do so during 
			/// assembly load, it could make the load quite slow and files that are even not used may be checked unnecessarily. 
			/// Therefore the check is done lazily. 
			/// 
			/// Source files compiled while the watcher is active needn't to be checked. 
			/// </summary>
			public bool FileTimeChecked { get { return fileTimeChecked; } set { fileTimeChecked = value; } }
			private bool fileTimeChecked;

            public CacheEntry(Type/*!*/ scriptType, ScriptAssembly/*!*/ scriptAssembly, DateTime buildTimeStamp,
				string[]/*!*/ includers, bool fileTimeChecked)
			{
                Debug.Assert(scriptType != null && scriptAssembly != null && includers != null);

                this.scriptInfo = new ScriptInfo(scriptType);
				this.scriptAssembly = scriptAssembly;
				this.buildTimeStamp = buildTimeStamp;
				this.includers = new List<string>(includers);
				this.fileTimeChecked = fileTimeChecked;
				this.includees = new List<string>();
			}

            public CacheEntry(Type/*!*/ scriptType, ScriptAssembly/*!*/ scriptAssembly, DateTime buildTimeStamp,
				string[]/*!*/ includers, string[]/*!*/ includees, bool fileTimeChecked)
			{
                Debug.Assert(scriptType != null && scriptAssembly != null && includers != null);
			
				this.scriptInfo = new ScriptInfo(scriptType);
				this.scriptAssembly = scriptAssembly;
				this.buildTimeStamp = buildTimeStamp;
				this.includers = new List<string>(includers);
				this.fileTimeChecked = fileTimeChecked;
				this.includees = new List<string>(includees);
			}

			#endregion
		}
		
		/// <summary>
		/// Invalidate cache entry for specified file
		/// </summary>
		private void OnFileChanged(object source, FileSystemEventArgs e)
		{
			InvalidateCacheEntry(e.FullPath);
		}

		/// <summary>
		/// Invalidate cache entry for specified file
		/// </summary>
		private void OnFileRenamed(object source, RenamedEventArgs e)
		{
			InvalidateCacheEntry(e.FullPath);
		}

		/// <summary>
		/// Removes entry for specified script (and for all includers of this file) from cache.
		/// </summary>
		/// <param name="fullPath">Path of the modified file</param>
		private void InvalidateCacheEntry(string/*!*/ fullPath)
		{
			FullPath root = Configuration.Application.Compiler.SourceRoot;
			string ns = ScriptModule.GetSubnamespace(new RelativePath(root, new FullPath(fullPath)), false);

			Debug.WriteLine("WATCHER", "Checking cache entry '{0}'.", ns);

			CacheEntry entry;
			if (TryGetCachedEntry(ns, out entry))
			{
				// remove entry:
				List<ScriptAssembly> removed_assemblies = RemoveCachedEntry(ns, entry);

				// removes assembly files (do not remove a multiscript assembly):  
				foreach (ScriptAssembly removed_assembly in removed_assemblies)
				{
                    if (!removed_assembly.IsMultiScript && !string.IsNullOrEmpty(removed_assembly.Path))
					{
						Debug.WriteLine("WATCHER", "Deleting file '{0}'.", removed_assembly.Path);

						try
						{
							File.Delete(removed_assembly.Path);
							File.Delete(Path.ChangeExtension(removed_assembly.Path, ".pdb"));
						}
						catch (Exception)
						{
							// nop //
						}
					}
				}		
			}					
		}

        /// <summary>
        /// Objects used for locking loading of single SSA from ASP.NET temp.
        /// </summary>
        private static readonly object[] tempLoadLocks;

		/// <summary>
		/// Loads script from cache (in-memory tables) or from 
		/// previously compiled dll in ASP.NET Temporary files.
		/// </summary>
		private bool TryLoadCachedEntry(string/*!*/ ns, PhpSourceFile/*!*/ sourceFile, out CacheEntry cache_entry)
		{
            // first try in-memory cache
            if (TryGetCachedEntry(ns, out cache_entry))
            {
                Debug.WriteLine("WSSM", "Cache hit.");
                if (CheckEntryFileTime(ns, cache_entry) || !Configuration.Application.Compiler.WatchSourceChanges)
                    return true;
            }

            lock(tempLoadLocks[unchecked((uint)ns.GetHashCode()) % tempLoadLocks.Length])
            {
                // double checked lock
                if (TryGetCachedEntry(ns, out cache_entry))
                    return true;    // do not check source time, since it was currently created

                // try to find previously compiled assembly in temporary files
                if (TryLoadTemporaryCompiledNoLock(ns, sourceFile, out cache_entry))
                {
                    Debug.WriteLine("WSSM", "Loaded from Temporary files.");
                    return true;
                }
            }

            return false;
        }


		/// <summary>
		/// Get script for specified namespace from in-memory cache
		/// </summary>
		private bool TryGetCachedEntry(string/*!*/ ns, out CacheEntry entry)
		{
            cacheLock.EnterReadLock();
			try
			{
				return cache.TryGetValue(ns, out entry);
			}
			finally
			{
                cacheLock.ExitReadLock();
			}
		}


		/// <summary>
		/// Store entry in the cache
		/// </summary>
		/// <param name="ns">Key (namespace)</param>
		/// <param name="entry">Value (entry)</param>
		/// <param name="setIncludees">Set this file as included in every includer</param>
		/// <param name="setIncluders">Set this file as includer for every included script</param>
		private void SetCacheEntry(string/*!*/ ns, CacheEntry entry, bool setIncludees, bool setIncluders)
		{
            cacheLock.EnterWriteLock();
			try
			{
				SetCacheEntryNoLock(ns, entry, setIncludees, setIncluders);
			}
			finally
			{
                cacheLock.ExitWriteLock();
			}
		}

		private void SetCacheEntryNoLock(string/*!*/ ns, CacheEntry entry, bool setIncludees, bool setIncluders)
		{
			// new entry -> includers need to know about it:
			if (setIncludees && !cache.ContainsKey(ns))
			{
				foreach (string includer in entry.Includers)
				{
					CacheEntry incl_entry;
					if (cache.TryGetValue(includer, out incl_entry)) incl_entry.Includees.Add(ns);
				}
			}

			if (setIncluders)
			{
				foreach (string includee in entry.Includees)
				{
					CacheEntry incl_entry;
					if (cache.TryGetValue(includee, out incl_entry)) incl_entry.Includers.Add(ns);
				}
			}
			cache[ns] = entry;
		}

		private List<ScriptAssembly>/*!*/ RemoveCachedEntry(string/*!*/ ns, CacheEntry entry)
		{
			List<ScriptAssembly> removed_assemblies = new List<ScriptAssembly>();

            cacheLock.EnterWriteLock();
			try
			{
				RemoveCachedEntryNoLock(ns, entry, removed_assemblies);
			}
			finally
			{
                cacheLock.ExitWriteLock();
			}

			return removed_assemblies;
		}

		private void RemoveCachedEntryNoLock(string/*!*/ ns, CacheEntry entry, List<ScriptAssembly> removedAssemblies)
		{
			if (cache.Remove(ns))
			{
				Debug.WriteLine("WATCHER", "Cache entry '{0}' removed.", ns);
				
				if (removedAssemblies != null)
					removedAssemblies.Add(entry.ScriptAssembly);
				
				foreach (string includer in entry.Includers)
				{
					if (cache.TryGetValue(includer, out entry))
						RemoveCachedEntryNoLock(includer, entry, removedAssemblies);
				}
			}
		}


		/// <summary>
		/// Updates 'Includee' fields of cache items (it is not possible
		/// to set includees while loading, beacause the key (script namespace) 
		/// of includer might not exist when the includee is loaded).
		/// </summary>
		/// <param name="inclusionDict">
		/// Dictionary containing list of includers for every loaded file (key)
		/// </param>
		private void UpdateCacheInclusions(Dictionary<string, string[]>/*!*/ inclusionDict)
		{
			foreach (string entry in inclusionDict.Keys)
			{
				foreach (string includer in inclusionDict[entry])
				{
					cache[includer].Includees.Add(entry);
				}
			}
		}		


		/// <summary>
		/// Checks whether timestamp of the file in the cache matches timestamp
		/// loded from the compiled assembly - this prevents us from using compiled
		/// assembly when the source file was modified (when the watcher was not running)
		/// </summary>
		private bool CheckEntryFileTime(string/*!*/ ns, CacheEntry entry)
		{
			if (entry.FileTimeChecked)
                return true;

            return CheckEntryFileTimeInternal(ns, entry);
		}

        private bool CheckEntryFileTimeInternal(string/*!*/ ns, CacheEntry entry)
        {
            cacheLock.EnterWriteLock();
            try
            {
                return CheckEntryFileTimeNoLock(ns, entry);
            }
            finally
            {
                cacheLock.ExitWriteLock();
            }
        }


		private bool CheckEntryFileTimeNoLock(string/*!*/ ns, CacheEntry entry)
		{
			if (entry.FileTimeChecked) return true;
			try
			{
				FullPath source_path = ScriptModule.GetPathFromSubnamespace(ns).ToFullPath(Configuration.Application.Compiler.SourceRoot);
				entry.FileTimeChecked = true;

				// is compilation obsolete?
				if (entry.BuildTimestamp < File.GetLastWriteTime(source_path))
				{
					RemoveCachedEntryNoLock(ns, entry, null);
					return false;
				}	
			}
			catch
			{
				RemoveCachedEntryNoLock(ns, entry, null);
				return false;
			}
				
			// (When checking file first time after application starts)
			// Make sure that all included files (Includees) are up to date
			foreach (string includee in entry.Includees)
			{
				if (cache.TryGetValue(includee, out entry) && !CheckEntryFileTimeNoLock(includee, entry))
				{
					RemoveCachedEntryNoLock(ns, entry, null);
					return false;
				}
			}
		  return true;
		}

		#endregion

		#region Temporary files



        /// <summary>
        /// Tries to load script from ASP.NET Temporary files - this is useful when 
        /// web is not precompiled (so it is compiled into SSAs) and appdomain is reloaded
        /// (which means that we loose the cache)
        /// </summary>
        private bool TryLoadTemporaryCompiledNoLock(string ns, PhpSourceFile/*!*/ sourceFile, out CacheEntry cache_entry)
        {
            CompilerConfiguration config = new CompilerConfiguration(Configuration.Application);
            string name = WebCompilationContext.GetAssemblyCodedName(sourceFile, config);

            string sourcePath = sourceFile.FullPath.ToString();
            bool sourceExists = File.Exists(sourcePath);
            DateTime sourceTime = sourceExists ? File.GetLastWriteTime(sourcePath) : DateTime.UtcNow.AddYears(1);   // If file does not exist, fake the sourceTime to NOT load any SSA DLL. Delete them instead.
            DateTime configTime = Configuration.LastConfigurationModificationTime;

            // here find the max modification of all dependant files (configuration, script itself, other DLLs):
            long sourceStamp = Math.Max(Math.Max(sourceTime.Ticks, configTime.Ticks), appCodeAssemblyCreated.Ticks);

            // Find specified file in temporary files

            if (Directory.Exists(outDir))

            foreach (string file in Directory.GetFiles(outDir, name + "*.dll"))
            {
                Match match = reFileStamp.Match(file);
                if (!match.Success) continue;

                long fileStamp;
                if (!Int64.TryParse((string)match.Groups["Stamp"].Value, NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out fileStamp)) continue;

                // File is up-to-date
                if (sourceStamp < fileStamp)
                {
                    Debug.WriteLine("WSSM", "Loading from ASP.NET Temporary files.");

                    return LoadSSA(ns, file, out cache_entry);
                }
                else
                {
                    // do "some" cleanup:
                    try
                    {
                        File.Delete(file);
                        File.Delete(Path.ChangeExtension(file, ".pdb"));
                    }
                    catch{ /*nop*/ }
                }
            }
            cache_entry = default(CacheEntry);
            return false;
        }

        private bool LoadSSA(string ns, string assemblyfile, out CacheEntry cache_entry)
        {
            // load assembly (ssa)
            Assembly assembly = Assembly.LoadFrom(assemblyfile);
            SingleScriptAssembly ssa = (SingleScriptAssembly)ScriptAssembly.LoadFromAssembly(applicationContext, assembly);

            // find type <Script>
            var scriptType = ssa.GetScriptType();

            if (scriptType != null)
            {
                // recursively check (and load) included assemblies
                // (includees and includers are set for all loaded CacheEntries except the 
                // inclusion to the currently loaded script - this is set later)
                Dictionary<string, CacheEntry> temporaryCache = new Dictionary<string, CacheEntry>();
                if (LoadIncludeesRecursive(ns, scriptType, ssa.RealModule, false, null, temporaryCache))
                {
                    cache_entry = temporaryCache[ns];   // cached SSA is OK, reuse it

                    foreach (KeyValuePair<string, CacheEntry> entryTmp in temporaryCache)
                        if (entryTmp.Value != null)
                            SetCacheEntry(entryTmp.Key, entryTmp.Value, false, false);

                    return true;
                }
            }

            // otherwise
            cache_entry = default(CacheEntry);
            return false;
        }


        /// <summary>
        /// Recursive function that loads (SSA) assembly and all included assemblies
        /// (included assemblies are required because we need to check whether included files are up-to-date)
        /// </summary>
        /// <param name="ns">Namespace of the script to be loaded (namespace is encoded file name)</param>
        /// <param name="type">Type of the &lt;Script&gt; class</param>
        /// <param name="module">Module of the type - used for token resolving </param>
        /// <param name="checkStamp">Should we check timestamp?</param>
        /// <param name="includer">Namespace of the includer (can be null)</param>
        /// <param name="tempCache">Temporary cache - used only while loading</param>
        /// <returns>Success?</returns>
        private bool LoadIncludeesRecursive(string/*!*/ ns, Type type/*!*/, Module/*!*/ module,
            bool checkStamp, string includer, Dictionary<string, CacheEntry>/*!*/ tempCache)
        {
            //File already processed?
            if (tempCache.ContainsKey(ns))
                return true;

            tempCache[ns] = null;   // just recursion prevention

            // find [Script] attribute
            ScriptAttribute script_attr = ScriptAttribute.Reflect(type);
            if (script_attr == null) return false;

            // check source file timestamp
            if (checkStamp)
            {
                string path = ScriptModule.GetPathFromSubnamespace(ns).
                    ToFullPath(Configuration.Application.Compiler.SourceRoot).ToString();
                DateTime writeStamp = File.GetLastWriteTime(path);  // note: it does not fail if the file does not exists, in such case it returns 12:00 midnight, January 1, 1601 A.D.
                if (writeStamp > script_attr.SourceTimestamp) return false;
            }

            // find [ScriptIncludees] attribute
            ScriptIncludeesAttribute script_includees = ScriptIncludeesAttribute.Reflect(type);
            string[] inclusionNames;
            if (script_includees != null)
            {
                Type[] inclusionScripts;

                inclusionNames = new string[script_includees.Inclusions.Length];
                inclusionScripts = new Type[script_includees.Inclusions.Length];

                // resolve included Scripts tokens:
                for (int i = 0; i < inclusionNames.Length; i++)
                {
                    try
                    {
                        inclusionScripts[i] = module.ResolveType(script_includees.Inclusions[i]);
                        ScriptAttribute sa = ScriptAttribute.Reflect(inclusionScripts[i]);
                        if (sa == null) return false;
                        inclusionNames[i] = ScriptModule.GetSubnamespace(new RelativePath(sa.RelativePath), false);
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                }

                // Try to load all included scripts and check whether files weren't changed
                for (int i = 0; i < inclusionNames.Length; i++)
                {
                    if (!LoadIncludeesRecursive(inclusionNames[i], inclusionScripts[i], inclusionScripts[i].Module, true, ns, tempCache)) return false;
                }
            }
            else
            {
                inclusionNames = ArrayUtils.EmptyStrings;
            }

            // Load SSA assembly
            SingleScriptAssembly ssa = ScriptAssembly.LoadFromAssembly(applicationContext, type.Assembly) as SingleScriptAssembly;

            if (ssa != null)
            {
                // Save only to temp cache (other calls to LoadIncludeesRecursive may fail!)
                string[] includers = includer == null ? (ArrayUtils.EmptyStrings) : (new string[] { includer });
                CacheEntry entry = new CacheEntry(type, ssa, script_attr.SourceTimestamp, includers, inclusionNames, true);
                tempCache[ns] = entry;
            }
            else
            {
                // script in MSA was included from SSA, MSA scripts should not be in cache[]
                // leave null in tempCache[ns] (as recursion prevention), it will not process into cache[]
            }

            return true;
        }


		#endregion

		#region Precompiled Assembly


		internal MultiScriptAssembly GetPrecompiledAssembly()
		{
			if (!precompiledAssemblyProbed)
			{
				lock (precompiledLoadMutex)
				{
					if (!precompiledAssemblyProbed)
					{
						precompiledAssembly = LoadPrecompiledAssemblyNoLock();
                        precompiledAssemblyProbed = true;
                    }
				}
			}
			return precompiledAssembly;
		}

		private MultiScriptAssembly LoadPrecompiledAssemblyNoLock()
		{
            if (HttpContext.Current == null)
                return null;    // following is only for web app

			string path = Path.Combine(HttpRuntime.BinDirectory, PhpScript.CompiledWebAppAssemblyName);
			if (!File.Exists(path)) return null;

			Assembly assembly = Assembly.LoadFrom(path);
            MultiScriptAssembly result = (MultiScriptAssembly)ScriptAssembly.LoadFromAssembly(applicationContext, assembly);

			// populate precompiled script cache, start watching for the source code:
			Debug.WriteLine("WSSM", "Starting precompiled script cache population.");

			// we need to load all modules before we can set includers
			// (because we also need to set includees so we need all types first)
			Dictionary<string, string[]> includersDict = new Dictionary<string, string[]>();
			result.RealModule.FindTypes(delegate(Type type, object _)
			{
				if (type.Name == ScriptModule.ScriptTypeName)
				{
                    ScriptAttribute script_attr = ScriptAttribute.Reflect(type);
                    if (script_attr == null)
                        throw new ReflectionException(CoreResources.GetString("precompiled_assembly_missing_script_attribute", assembly)); // unexpected: there is a <Script> class without ScriptAttribute

                    ScriptIncludersAttribute script_includers = ScriptIncludersAttribute.Reflect(type);
                    string[] includers;
                    if (script_includers != null)
                    {
                        includers = new string[script_includers.Includers.Length];
                        
                        // resolve includers' tokens:
                        for (int i = 0; i < includers.Length; i++)
                        {
                            try
                            {
                                includers[i] = result.RealModule.ResolveType(script_includers.Includers[i]).Namespace;
                            }
                            catch (ArgumentException)
                            {
                                throw new ReflectionException(
                                    CoreResources.GetString("precompiled_assembly_corrupted", assembly, script_includers.Includers[i]));
                            }
                        }
                    }
                    else
                    {
                        includers = ArrayUtils.EmptyStrings;
                    }

                    // store includers in dictionary, so we can set includees later
                    includersDict[type.Namespace] = includers;
                    SetCacheEntryNoLock(
                        type.Namespace,
                        new CacheEntry(type, result, script_attr.SourceTimestamp, includers, false), false, false);
				}

				return false;
			}, null);
			UpdateCacheInclusions(includersDict);

			Debug.WriteLine("WSSM", "Precompiled script cache population finished (#entries = {0}).", cache.Count);
				
			return result;
		}

		#endregion
		
		#region Compilation

		/// <summary>
		/// Retrives a compiled script.
        /// 
        /// The method check scripts in following order:
        /// 1. Script Library database.
        /// 2. Modified source file on the file system.
        /// 3. Unmodified source file in precompiled WebPages.dll.
		/// </summary>
		/// <param name="sourceFile">Script source file.</param>
		/// <param name="requestContext">The current HTTP context. Can be <c>null</c> in case of desktop app.</param>
		/// <returns>The script type or a <B>null</B> reference on failure.</returns>
        /// <remarks>The method do check the script library database.</remarks>
        public ScriptInfo GetCompiledScript(PhpSourceFile/*!*/ sourceFile, RequestContext requestContext)
        {
            Debug.Assert(sourceFile != null);

            // try to get the script from precompiled script library first
            var scriptLibraryModule = applicationContext.ScriptLibraryDatabase.GetScriptModule(sourceFile.FullPath);
            if (scriptLibraryModule != null)
                return scriptLibraryModule.ScriptInfo;

            // loads precompiled assembly if exists and not loaded yet:
            GetPrecompiledAssembly();

            // enables source code watcher if not enabled yet:
            if (watcher != null && !watcher.EnableRaisingEvents)
            {
                Debug.WriteLine("WSSM", "Source code watcher is starting.");

                watcher.EnableRaisingEvents = true;
            }

            string ns = ScriptModule.GetSubnamespace(sourceFile.RelativePath, false);

            CacheEntry cache_entry;

            if (Configuration.Application.Compiler.OnlyPrecompiledCode)
            {
                // Load script from cache (WebPages.dll)
                if (TryGetCachedEntry(ns, out cache_entry))
                    return cache_entry.ScriptInfo;
                else
                    return null;
            }
            else
            {
                // Load script from cache or from ASP.NET Temporary files
                if (TryLoadCachedEntry(ns, sourceFile, out cache_entry))
                    return cache_entry.ScriptInfo;

                lock (this)
                {
                    // double checked lock, CompileScript should not be called on more threads
                    if (TryGetCachedEntry(ns, out cache_entry))
                        return cache_entry.ScriptInfo;

                    Debug.WriteLine("WSSM", "Compile script '{0}'.", sourceFile.ToString());
                    return CompileScriptNoLock(ns, sourceFile, requestContext);
                }
            }
        }


		/// <summary>
		/// Compiles a script.
		/// Called when the script cannot be loaded from pre-compiled assembly and it should be compiled.
		/// </summary>
		/// <returns>The compiled script type.</returns>
        private ScriptInfo CompileScriptNoLock(string ns, PhpSourceFile/*!*/ sourceFile, RequestContext requestContext)
		{
            Debug.Assert(sourceFile != null);

			CompilerConfiguration config = new CompilerConfiguration(Configuration.Application);
			WebCompilationContext context = new WebCompilationContext(applicationContext, this, config, sourceFile.Directory, 
				(requestContext != null) ? requestContext.HttpContext.Timestamp : DateTime.UtcNow);

			try
			{
				CacheEntry cache_entry;
                if (ScriptAssemblyBuilder.CompileScripts(new PhpSourceFile[] { sourceFile }, context))
                {
                    // assembly should be already added into the cache by Persist() method
                    if (TryGetCachedEntry(ns, out cache_entry))
                        return cache_entry.ScriptInfo;
                }

                return null;
			}
			catch (CompilerException)
			{
				return null;
			}
			catch (Exception)
			{
				// record stack info to the message if the manager resides in a dedicated domain:
				throw;
			}
		}
		
		#endregion
	}

	#endregion
}
