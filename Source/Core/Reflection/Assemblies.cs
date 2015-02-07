/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Xml;
using System.Threading;
using System.Reflection.Emit;
using System.IO;
using System.Diagnostics;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core.Reflection
{
	using ProvidedType = KeyValuePair<string, DTypeDesc>;
	using PHP.Core.Emit;

	#region DAssembly

	public abstract class DAssembly
	{
		/// <summary>
		/// The application context which the assembly has been loaded to.
		/// </summary>
		public ApplicationContext/*!*/ ApplicationContext { get { return applicationContext; } }
		private readonly ApplicationContext/*!*/ applicationContext;

		/// <summary>
		/// Primary real module of the assembly. <B>null</B> only for unknown assembly.
		/// </summary>
		public Module RealModule { get { return realModule; } }
		private Module realModule;

		/// <summary>
		/// Gets the real assembly. <B>null</B> only for unknown assembly.
		/// </summary>
		public Assembly RealAssembly { get { return (realModule != null) ? realModule.Assembly : null; } }

		/// <summary>
		/// Gets the module containing types, functions and constants exported to the referencing assembly.
		/// Returns <B>null</B> if the assembly doesn't export any entries (e.g. script assembly do so).
		/// </summary>
		internal virtual DModule ExportModule { get { return null; } }

		public abstract string/*!*/ DisplayName { get; }

		/// <summary>
		/// Path to the assembly file or <B>null</B> for transient assemblies.
		/// </summary>
		public string Path { get { return path; } }
		private string path;
	
		#region Construction

		protected DAssembly(ApplicationContext/*!*/ applicationContext, Module/*!*/ realModule)
		{
			Debug.Assert(applicationContext != null && realModule != null);

			this.applicationContext = applicationContext;
			this.realModule = realModule;
		}

		protected DAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly)
		{
			Debug.Assert(applicationContext != null && realAssembly != null);

			this.applicationContext = applicationContext;
			this.realModule = realAssembly.ManifestModule;
#if !SILVERLIGHT
			this.path = realAssembly.CodeBase;
#else
			this.path = null;
#endif
		}

		/// <summary>
		/// Used by builders (write-up) and unknown assemblies.
		/// </summary>
		protected DAssembly(ApplicationContext/*!*/ applicationContext)
		{
			this.applicationContext = applicationContext;

			realModule = null;
		}

		internal void WriteUp(Module/*!*/ realModule, string path)
		{
			this.realModule = realModule;
			this.path = path;
		}

		/// <summary>
		/// Called by the loader.
		/// </summary>
		internal static DAssembly/*!*/ CreateNoLock(ApplicationContext/*!*/ applicationContext,
			Assembly/*!*/ realAssembly, LibraryConfigStore config)
		{
			// gets a name of the descriptor:
			DAssemblyAttribute attr = DAssemblyAttribute.Reflect(realAssembly);
			if (attr != null)
			{
				PhpLibraryAttribute lib;
				PurePhpAssemblyAttribute pure;
                
				if ((lib = attr as PhpLibraryAttribute) != null)
				{
					// PHP library or extension: 
					return new PhpLibraryAssembly(applicationContext, realAssembly, lib, config);
				}
				else if ((pure = attr as PurePhpAssemblyAttribute) != null)
				{
#if SILVERLIGHT
					throw new NotSupportedException("Loading of pre-compiled pure assemblies is not supported!");
#else
					// compiled pure PHP assembly:
					return new PureAssembly(applicationContext, realAssembly, pure, config);
#endif
				}
                else 
                {
#if SILVERLIGHT
					throw new NotSupportedException("Loading of pre-compiled script assemblies is not supported!");
#else
                    // compiled PHP script assembly:
                    return ScriptAssembly.Create(applicationContext, realAssembly, (ScriptAssemblyAttribute)attr);
#endif
                }
			}
            else
			{
                // plugin assembly:
                var plugs = PluginAssemblyAttribute.Reflect(realAssembly);
                if (plugs != null)
                {
                    return new PluginAssembly(applicationContext, realAssembly, config, plugs);
                }
                
				// CLR assembly:
				return new ClrAssembly(applicationContext, realAssembly, config);
			}
		}

		#endregion

		/// <summary>
		/// Loads assemblies that are not explicitly referenced by the metadata, yet were referenced by the compiler
		/// when the assembly was being built.
		/// </summary>
		internal virtual void LoadCompileTimeReferencedAssemblies(AssemblyLoader/*!*/ loader)
		{
		}
	}

	#endregion

	#region PhpAssembly

	public abstract class PhpAssembly : DAssembly
	{
		public abstract PhpModule GetModule(PhpSourceFile name);

		public override string/*!*/ DisplayName { get { return RealAssembly.FullName; } }

		public PhpAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly)
			: base(applicationContext, realAssembly)
		{

		}

		public PhpAssembly(ApplicationContext/*!*/ applicationContext, Module/*!*/ realModule)
			: base(applicationContext, realModule)
		{

		}

		/// <summary>
		/// Used by builder.
		/// </summary>
		protected PhpAssembly(ApplicationContext/*!*/ applicationContext)
			: base(applicationContext)
		{
			// to be written-up
		}
	}

	#endregion

	#region TransientAssembly

	[DebuggerNonUserCode]
	public sealed class TransientAssembly : PhpAssembly
	{
		#region Nested Types: Key, Value

		/// <summary>
		/// Compiled code depends on its source code, source file, and position.
		/// </summary>
        [DebuggerNonUserCode]
        private struct Key : IEquatable<Key>
		{
			private readonly string/*!*/ code;
			private readonly SourceCodeDescriptor descriptor;

			public Key(string/*!*/ code, SourceCodeDescriptor descriptor)
			{
				Debug.Assert(code != null);

				this.code = code;
				this.descriptor = descriptor;
			}

			#region IEquatable<Key> Members

			public bool Equals(Key other)
			{
				return this.descriptor.Equals(other.descriptor) && this.code == other.code;
			}

			#endregion

			public override bool Equals(object obj)
			{
				if (!(obj is Key)) return false;
				return Equals((Key)obj);
			}

			public override int GetHashCode()
			{
				return code.GetHashCode() ^ descriptor.GetHashCode();
			}

			#region Debug

			[Conditional("DEBUG")]
			public void Dump(TextWriter o)
			{
				o.WriteLine("{0}#{1}({2},{3})", descriptor.ContainingSourcePath,
					descriptor.ContainingTransientModuleId, descriptor.Line, descriptor.Column);
				o.WriteLine("- code ------------------------------");
				o.Write(code);
				o.WriteLine();
				o.WriteLine("-------------------------------------");
			}

			#endregion
		}

		/// <summary>
		/// Values stored in the cache.
		/// </summary>
        [DebuggerNonUserCode]
        private struct Value
		{
			public readonly TransientModule/*!*/ Module;
			public readonly List<ProvidedType>/*!*/ TypeDependencies;

			public Value(TransientModule/*!*/ module, List<ProvidedType>/*!*/ typeDependencies)
			{
				Debug.Assert(module != null && typeDependencies != null);

				this.Module = module;
				this.TypeDependencies = typeDependencies;
			}
		}

		#endregion

		#region Properties

		internal const string RealAssemblyName = "TransientAssembly";
		internal const string RealModuleName = "TransientModule";

		/// <summary>
		/// An invalid eval id. All eval ids are indices to the eval list so -1 is invalid value indeed.
		/// </summary>
		public const int InvalidEvalId = -1;

		internal override DModule ExportModule { get { return null; } }

		public override string/*!*/ DisplayName { get { return RealAssemblyName; } }

		/// <summary>
		/// Protects both <see cref="cache"/> and <see cref="modules"/>.
		/// </summary>
		private readonly ReaderWriterLockSlim/*!*/rwLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		/// <summary>
		/// Maps transient source code to its compiled form - an instance of <see cref="ScriptModule"/> class.
		/// </summary>
		private readonly Dictionary<Key, Value>/*!*/ cache = new Dictionary<Key, Value>();

		/// <summary>
		/// List of modules. Can contain <B>null</B> slots if two threads are compiling the same code. The slower
		/// may reserve the slot but it doesn't fill it (the redundant compiled code is thrown away).
		/// </summary>
		private readonly List<TransientModule>/*!*/ modules = new List<TransientModule>();

		#endregion

		#region Construction

		internal TransientAssembly(ApplicationContext/*!*/ applicationContext)
			: base(applicationContext)
		{

		}

		#endregion

		#region Modules

		internal TransientModule GetModule(ScriptContext/*!*/ context, DTypeDesc caller, string/*!*/ code, SourceCodeDescriptor descriptor)
		{
			Debug.Assert(context != null && code != null);

			Key key = new Key(code, descriptor);
			Value value;

            rwLock.EnterUpgradeableReadLock();

            try
            {
                if (cache.TryGetValue(key, out value))
                {
                    if (TypesProvider.LoadAndMatch(value.TypeDependencies, context, caller))
                    {
#if !SILVERLIGHT
                        Performance.Increment(Performance.DynamicCacheHits);
#endif
                        return value.Module;
                    }
                    else
                    {
                        // invalidate the cache entry, because type dependencies were changed:
                        rwLock.EnterWriteLock();

                        try
                        {
                            cache.Remove(key);
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }
                    }
                }
            }
            finally
            {
                rwLock.ExitUpgradeableReadLock();
            }            

			return null;
		}

		public override PhpModule GetModule(PhpSourceFile name)
		{
			Debug.Fail();
			throw null;
		}

		/// <summary>
		/// Gets eval info.
		/// </summary>
		public TransientModule GetModule(int id)
		{
			// not synchronized - the modules are not removed from the list

			if (id == InvalidEvalId) return null;

			Debug.Assert(id >= 0 && id < modules.Count, "Eval id has invalid value.");

			return modules[id];
		}

		/// <summary>
		/// Gets a root eval for a specified eval.
		/// </summary>
		/// <param name="id">The eval id.</param>
		/// <returns>An id of root eval.</returns>
		public TransientModule GetRootModule(int id)
		{
			// not synchronized - the modules are not removed

			if (id == InvalidEvalId) return null;

			Debug.Assert(id >= 0 && id < modules.Count, "Eval id has invalid value.");

			TransientModule module = modules[id];

			while (module.ContainingModule != null)
				module = module.ContainingModule;

			return module;
		}

		internal TransientModuleBuilder/*!*/ DefineModule(TransientAssemblyBuilder/*!*/ assemblyBuilder,
			TransientCompilationUnit/*!*/ compilationUnit, int containerId, EvalKinds kind, string sourcePath)
		{
			TransientModule container = GetModule(containerId);

			int new_id;

            rwLock.EnterWriteLock();

            try
			{
				// reserve slot in the module list:
				new_id = modules.Count;
				modules.Add(null);
			}
			finally
			{
                rwLock.ExitWriteLock();
			}

			return new TransientModuleBuilder(new_id, kind, compilationUnit, assemblyBuilder, container, sourcePath);
		}

		internal TransientModule/*!*/ AddModule(TransientModule/*!*/ module, List<ProvidedType>/*!*/ dependentTypes,
			string code, SourceCodeDescriptor descriptor)
		{
			Key key = new Key(code, descriptor);
			Value value = new Value(module, dependentTypes);

			// adds item to the cache and the list if it is not there:
            
            rwLock.EnterWriteLock();
            try
			{
                Value existing;
				if (!cache.TryGetValue(key, out existing))
				{
					cache.Add(key, value);

					Debug.Assert(module.Id < modules.Count, "Slot should have been reserved.");
					modules[module.Id] = module;
				}
				else
				{
					module = existing.Module;
				}
			}
			finally
			{
                rwLock.ExitWriteLock();
			}

			return module;
		}

		/// <summary>
		/// Fills a list by an eval trace starting with an eval of a specified id.
		/// </summary>
		/// <param name="evalId">The id of the eval which to start with.</param>
		/// <param name="result">The list of <see cref="ErrorStackInfo"/> to fill. </param>
		public void GetEvalFullTrace(int evalId, List<ErrorStackInfo>/*!*/ result)
		{
			TransientModule module = GetModule(evalId);

			Debug.Assert(module != null);

			do
			{
				SourceCodeUnit source_unit = module.TransientCompilationUnit.SourceUnit;

				result.Add(new ErrorStackInfo(source_unit.SourceFile.FullPath, module.GetErrorString(),
					source_unit.Line, source_unit.Column, false));

				module = module.ContainingModule;
			}
			while (module != null);
		}

		#endregion

		#region Debug

		/// <summary>
		/// Dumps cache.
		/// </summary>
		[Conditional("DEBUG")]
		internal void Dump(TextWriter o)
		{
            rwLock.EnterReadLock();

            try
			{
                foreach (KeyValuePair<Key, Value> entry in cache)
				{
					entry.Key.Dump(o);
					if (entry.Value.TypeDependencies != null)
					{
						o.WriteLine("dependent types:");
						foreach (ProvidedType type in entry.Value.TypeDependencies)
							o.Write(type.Key.ToString() + " ");
						o.WriteLine();
						o.WriteLine();
					}
				}
			}
			finally
			{
                rwLock.ExitReadLock();
			}
		}

		#endregion
	}

	#endregion

	#region ClrAssembly

	public sealed class ClrAssembly : DAssembly
	{
		internal override DModule ExportModule { get { return module; } }

		public ClrModule/*!*/ Module { get { return module; } }
		private readonly ClrModule/*!*/ module;

		public override string/*!*/ DisplayName { get { return RealAssembly.FullName; } }

		#region Construction

		/// <summary>
		/// Called by the loader.
		/// </summary>
		internal ClrAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly,
			LibraryConfigStore configStore)
			: base(applicationContext, realAssembly)
		{
			this.module = new ClrModule(this);
		}

		#endregion
	}

	#endregion

    #region PluginAssembly

    public sealed class PluginAssembly : DAssembly
    {
        internal override DModule ExportModule { get { return module; } }

        public PluginModule/*!*/ Module { get { return module; } }
        private readonly PluginModule/*!*/ module;

        public override string/*!*/ DisplayName { get { return RealAssembly.FullName; } }

        internal const string LoaderMethod = "Load";
        internal readonly static Type[] LoaderMethodParameters = new Type[] { typeof(ApplicationContext) };

        #region Construction

        /// <summary>
        /// Called by the loader.
        /// </summary>
        internal PluginAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly,
            LibraryConfigStore configStore, IEnumerable<PluginAssemblyAttribute>/*!*/attrs)
            : base(applicationContext, realAssembly)
        {
            this.module = new PluginModule(this);
        }

        #endregion
    }

    #endregion

    #region PhpLibraryAssembly

    public sealed class PhpLibraryAssembly : DAssembly
	{
		internal static int LoadedLibraryCount { get { return uniqueIndex; } }
		private static int uniqueIndex = 0;

		internal override DModule ExportModule { get { return module; } }

		/// <summary>
		/// Library descriptor. Available only if the library is not loaded for reflection only.
		/// </summary>
		public PhpLibraryDescriptor Descriptor { get { return descriptor; } }
		private PhpLibraryDescriptor descriptor;

        #region PhpLibrary properties (the library attribute)

        /// <summary>
        /// The PhpLibrary attribute of the library.
        /// Custom attribute describing library properties.
		/// </summary>
		public PhpLibraryAttribute/*!*/ Properties { get { return properties; } }
		private readonly PhpLibraryAttribute/*!*/ properties;

        /// <summary>
        /// Returns a list of names of extensions which are implemented by the library.
        /// </summary>
        /// <returns>An array of names.</returns>
        /// <remarks>The first item (if any) is considered to be default extension for the library.</remarks>
        public string[]/*!*/ImplementedExtensions { get { return properties.ImplementsExtensions; } }

        /// <summary>
        /// Returns a name of default extension which is implemented by the library.
        /// </summary>
        /// <remarks>The first item (if any) is considered to be default extension for the library.</remarks>
        public string DefaultExtension
        {
            get
            {

                string[] extensions = this.ImplementedExtensions;

                if (extensions.Length > 0)
                    return extensions[0];
                else
                    return null;
            }
        }

        #endregion

        public DModule/*!*/ Module { get { return module; } }
		private readonly DModule/*!*/ module;

		public override string/*!*/ DisplayName
		{
			get 
			{
				return String.Format("{0} ({1})", properties.Name, RealAssembly.GetName().Name); 
			}
		}

		#region Construction

		/// <summary>
		/// Called by the loader. 
		/// Thread unsafe. Has to be called only in a critical section preventing any other calls.
		/// </summary>
		internal PhpLibraryAssembly(ApplicationContext/*!*/ applicationContext, Assembly/*!*/ realAssembly,
			PhpLibraryAttribute/*!*/ properties, LibraryConfigStore configStore)
			: base(applicationContext, realAssembly)
		{
			Debug.Assert(applicationContext != null && realAssembly != null && properties != null);
			
			this.properties = properties;

			if (properties.IsPure)
#if SILVERLIGHT
				throw new NotSupportedException("Loading of pure PHP Libraries on Silverlight is not supported!");
#else
				this.module = new PureModule(this);
#endif
			else
				this.module = new PhpLibraryModule(this);

			if (!applicationContext.AssemblyLoader.ReflectionOnly)
			{
				// creates an instance of library descriptor:
				if (properties.Descriptor != null)
					descriptor = PhpLibraryDescriptor.CreateInstance(properties.Descriptor);
				else
					descriptor = new DefaultLibraryDescriptor();

				descriptor.WriteUp(module, uniqueIndex++);

				try
				{
					// notify descriptor that it has been initialized:
					descriptor.Loaded(properties, configStore);
				}
				catch (Exception e)
				{
					uniqueIndex--;
					descriptor.Invalidate();

					throw new LibraryLoadFailedException(realAssembly.FullName, e);
				}
			}
			else
			{
				descriptor = null;
			}	
		}

		#endregion
	}

	#endregion

	#region UnknownAssembly

	public sealed class UnknownAssembly : DAssembly
	{
		public static readonly UnknownAssembly RuntimeAssembly = new UnknownAssembly();
		public override string/*!*/ DisplayName { get { return String.Empty; } }

		public UnknownAssembly()
			: base(ApplicationContext.Empty)
		{

		}
	}

	#endregion
}
