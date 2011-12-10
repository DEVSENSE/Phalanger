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

#if !SILVERLIGHT
            this.scriptLibraryDatabase = new ScriptLibraryDatabase(this);
#endif

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
            addType(typeof(Library.SPL.SplFixedArray));
            
            addType(typeof(Library.SPL.Serializable));
            addType(typeof(Library.SPL.Countable));
            addType(typeof(Library.SPL.Reflector));
            addType(typeof(Library.SPL.SplObjectStorage));
            addType(typeof(Library.SPL.SplObserver));
            addType(typeof(Library.SPL.SplSubject));

            // Iterators:
            addType(typeof(Library.SPL.Traversable));
            addType(typeof(Library.SPL.Iterator));
            addType(typeof(Library.SPL.IteratorAggregate));
            addType(typeof(Library.SPL.SeekableIterator));
            addType(typeof(Library.SPL.OuterIterator));
            addType(typeof(Library.SPL.RecursiveIterator));
            addType(typeof(Library.SPL.ArrayIterator));
            addType(typeof(Library.SPL.EmptyIterator));
            addType(typeof(Library.SPL.IteratorIterator));
            addType(typeof(Library.SPL.AppendIterator));
            addType(typeof(Library.SPL.FilterIterator));
            addType(typeof(Library.SPL.RecursiveArrayIterator));
            addType(typeof(Library.SPL.RecursiveIteratorIterator));
            
            // Exception:
            addType(typeof(Library.SPL.Exception));
            addType(typeof(Library.SPL.RuntimeException));
            addType(typeof(Library.SPL.ErrorException));
            addType(typeof(Library.SPL.LogicException));
            addType(typeof(Library.SPL.InvalidArgumentException));
            addType(typeof(Library.SPL.BadFunctionCallException));
            addType(typeof(Library.SPL.BadMethodCallException));
            addType(typeof(Library.SPL.LengthException));
            addType(typeof(Library.SPL.RangeException));
            addType(typeof(Library.SPL.OverflowException));
            addType(typeof(Library.SPL.UnderflowException));
            addType(typeof(Library.SPL.DomainException));

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
			functions[fullName] = new PhpRoutineDesc(memberAttributes, arglessStub, true);
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

	public sealed partial class AssemblyLoader
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


	}

	#endregion

}
