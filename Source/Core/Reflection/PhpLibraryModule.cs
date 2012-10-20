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
using System.Reflection;
using System.IO;
using System.Threading;
using PHP.Core.Emit;
using System.Collections;

namespace PHP.Core.Reflection
{
	/// <summary>
	/// Represents loaded PHP library. Provides its configuration, implemented extensions list, etc.
	/// </summary>
	public sealed class PhpLibraryModule : DModule
	{
		#region Constants

        /// <summary>
        /// Suffix for dynamically generated wrapper assemblies.
        /// </summary>
        internal const string DynamicAssemblySuffix = ".dynamic";

		/// <summary>
		/// A name of the primary module of dynamic library wrappers.
		/// </summary>
		internal const string DynamicWrapperModuleName = "DynamicWrapper";

		#endregion

		#region Properties

		public PhpLibraryAssembly/*!*/ PhpLibraryAssembly { get { return (PhpLibraryAssembly)assembly; } }

		/// <summary>
		/// Dynamic wrapper, lazy explicit.
		/// </summary>
		public Assembly DynamicWrapper { get { return dynamicWrapper; } }
		private Assembly dynamicWrapper;

		#endregion

		#region Construction

		/// <summary>
		/// Called by the loader thru <see cref="PhpLibraryAssembly"/>.
		/// </summary>
		internal PhpLibraryModule(PhpLibraryAssembly/*!*/ assembly)
			: base(assembly)
		{
			this.dynamicWrapper = null; // lazy load
		}

		#endregion

		#region Reflection

		public override void Reflect(bool full,
			Dictionary<string, DTypeDesc>/*!*/ types,
			Dictionary<string, DRoutineDesc>/*!*/ functions,
			DualDictionary<string, DConstantDesc>/*!*/ constants)
		{
			// TODO: functions' lazy reflection doesn't work
			full = true;

			if (dynamicWrapper == null)
				this.LoadDynamicWrapper();

            Type[] real_types;

            if (dynamicWrapper == null)
            {
                Debug.Assert(!Configuration.IsLoaded && !Configuration.IsBeingLoaded, "No dynamic wrappers are allowed only for configuration-less reflection!");
                real_types = Assembly.RealAssembly.GetTypes();

                // functions
                // only argfulls
            }
            else
            {
                real_types = dynamicWrapper.GetTypes();

                // functions (scan arglesses in the dynamic wrapper - full reflect needs this info as well):
                foreach (Type type in real_types)
                {
                    if (type.Namespace == Namespaces.LibraryStubs)
                        ReflectArglesses(functions, type);
                }

                // types are in the real assembly
                if (dynamicWrapper != Assembly.RealAssembly)
                    real_types = Assembly.RealAssembly.GetTypes();
            }
            
            foreach (Type type in real_types)
			{
                ReflectArgfulls(types, functions, constants, type, full);
			}

            //// reflect <Module>
            //if (Assembly.RealModule != null)
            //{
            //    ReflectGlobals(functions, constants, Assembly.RealModule);
            //}
		}

        /// <summary>
        /// Reflect argless function stubs from the given <c>type</c>.
        /// </summary>
        /// <param name="functions">Dictionary of functions where newly discovered functions will be placed.</param>
        /// <param name="type">The type to reflect function from.</param>
        private void ReflectArglesses(Dictionary<string, DRoutineDesc>/*!*/functions, Type/*!*/type)
        {
            if (type.IsGenericTypeDefinition)
                throw new ReflectionException(CoreResources.GetString("invalid_dynamic_wrapper_format", dynamicWrapper.CodeBase));

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
                AddArglessStub(functions, method, method.Name);
        }

        /// <summary>
        /// Add the argless stub <c>method</c> into the list of functions.
        /// </summary>
        /// <param name="functions">Dictionary of functions to insert the stub into.</param>
        /// <param name="method">The method of the stub.</param>
        /// <param name="functionName">The PHP name representing the function.</param>
        private DRoutineDesc AddArglessStub(Dictionary<string, DRoutineDesc>/*!*/functions, MethodInfo/*!*/method, string/*!*/functionName)
        {
            RoutineDelegate argless_stub;

            try
            {
                argless_stub = (RoutineDelegate)Delegate.CreateDelegate(typeof(RoutineDelegate), method, true);
            }
            catch (Exception)
            {
                throw new ReflectionException(CoreResources.GetString("invalid_dynamic_wrapper_format", dynamicWrapper.CodeBase));
            }

            DRoutineDesc desc;

            try
            {
                desc = new PhpLibraryFunctionDesc(this, argless_stub);
                functions.Add(functionName, desc);
                return desc;
            }
            catch (ArgumentNullException)
            {
                throw new ArgumentNullException("Function with <null> name passed.");
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("Function '" + method.Name + "' reflected before.");
            }
        }
        
        /// <summary>
        /// Reflect argfull function, PHP types and constants from given <c>type</c>.
        /// </summary>
        /// <param name="types">Dictionary of types where newly discovered PHP types will be placed. (Types having [ImplementsType] attribute.)</param>
        /// <param name="functions">Dictionary of reflected functions.</param>
        /// <param name="constants">Dictionary of reflected constants.</param>
        /// <param name="type">The type to reflect functions from.</param>
        /// <param name="full">Whether to perform full function reflect.</param>
        private void ReflectArgfulls(
            Dictionary<string, DTypeDesc>/*!*/ types,
            Dictionary<string, DRoutineDesc>/*!*/ functions,
            DualDictionary<string, DConstantDesc>/*!*/ constants,
            Type/*!*/type, bool full)
        {
            // skip generic types:
            if (type.IsGenericTypeDefinition)
                return;

            if (PhpType.IsPhpRealType(type))
            {
                var dtype = PhpTypeDesc.Create(type);
                types[dtype.MakeSimpleName()] = dtype;
            }
            
            // reflect even if it is PhpType to find global functions [ImplementsFunction] and constants [ImplementsConstant]
            if (IsLibraryType(type))
            {
                ReflectLibraryType(functions, constants, type, full);
            }
        }

        /// <summary>
        /// Assuming the given <c>type</c> is Library type, reflect argfull function stubs, PHP types and constants from given <c>type</c>.
        /// </summary>
        /// <param name="functions">Dictionary of reflected functions.</param>
        /// <param name="constants">Dictionary of reflected constants.</param>
        /// <param name="type">The type to reflect functions from.</param>
        /// <param name="full">Whether to perform full function reflect.</param>
        private void ReflectLibraryType(
            Dictionary<string, DRoutineDesc>/*!*/ functions,
            DualDictionary<string, DConstantDesc>/*!*/ constants,
            Type/*!*/type, bool full)
        {
            // functions (argfulls):
            if (full && !type.IsEnum)
                FullReflectFunctions(type.GetMethods(BindingFlags.Public | BindingFlags.Static), functions, true);

            // constants:
            ReflectConstants(type.GetFields(BindingFlags.Public | BindingFlags.Static), constants);
        }

        ///// <summary>
        ///// Reflect <c>Module</c> with global declarations.
        ///// </summary>
        ///// <param name="functions">Dictionary of reflected functions.</param>
        ///// <param name="constants">Dictionary of reflected constants.</param>
        ///// <param name="module">The module to reflect functions and constants from.</param>
        //private void ReflectGlobals(
        //    //Dictionary<string, DTypeDesc>/*!*/ types,
        //    Dictionary<string, DRoutineDesc>/*!*/ functions,
        //    DualDictionary<string, DConstantDesc>/*!*/ constants,
        //    Module/*!*/module)
        //{
        //    // functions (arglesses & argfulls):
        //    FullReflectFunctions(module.GetMethods(BindingFlags.Public | BindingFlags.Static), functions, true);

        //    // constants:
        //    ReflectConstants(module.GetFields(BindingFlags.Public | BindingFlags.Static), constants);
        //}

        /// <summary>
        /// Find the MethodInfo representing argless stub for the specified method. If the method
        /// is found, it will be added using <c>AddArglessStub</c>.
        /// </summary>
        /// <param name="realMethods">List of MethodInfos to search from.</param>
        /// <param name="functions">Dictionary of reflected functions.</param>
        /// <param name="method">Argfull overload which argless stub is searched.</param>
        /// <param name="impl_func">ImplementsFunction attribute of tjhe <c>method</c>.</param>
        /// <returns>DRoutineDesc of argless stub or null if it was not found.</returns>
        private DRoutineDesc FindArglessStub(
            MethodInfo[]/*!!*/ realMethods, Dictionary<string, DRoutineDesc>/*!*/ functions,
            MethodInfo/*!*/method, ImplementsFunctionAttribute/*!*/impl_func)
        {
            foreach (var argless in realMethods)
                if (argless.Name == method.Name && argless != method &&
                    PhpFunctionUtils.IsArglessStub(argless, null))
                {
                    // we have found the argless stub for the impl_func in the realMethods list
                    return AddArglessStub(functions, argless, impl_func.Name);
                }

            return null;
        }

        /// <summary>
        /// Add empty argless stub just to allow initialization without dynamic wrappers.
        /// </summary>
        /// <param name="functions"></param>
        /// <param name="functionName"></param>
        /// <returns></returns>
        private DRoutineDesc/*!*/AddEmptyArglessStub(Dictionary<string, DRoutineDesc>/*!*/functions, string functionName)
        {
            var desc = new PhpLibraryFunctionDesc(this, (instance, stack) => { throw new NotImplementedException("empty argless!"); });
            functions.Add(functionName, desc);
            return desc;
        }

        /// <summary>
        /// Reflect argfull stubs from the list of given methods.
        /// </summary>
        /// <param name="realMethods">List of MethodInfos to reflect.</param>
        /// <param name="functions">Dictionary of reflected functions.</param>
        /// <param name="lookForArgless">True to look for argless stub in <c>realMethods</c> if argless stub was not found in <c>functions</c>.</param>
		private void FullReflectFunctions(MethodInfo[]/*!!*/ realMethods, Dictionary<string, DRoutineDesc>/*!*/ functions, bool lookForArgless)
		{
			int i = 0;
			while (i < realMethods.Length)
			{
				MethodInfo method = realMethods[i];

				if (!method.IsGenericMethodDefinition)
				{
					ImplementsFunctionAttribute impl_func = ImplementsFunctionAttribute.Reflect(method);
					if (impl_func != null)
					{
						DRoutineDesc desc;

                        // argless is reflected already
                        // or argless was not reflected yet and can be found in realMethods list,
                        // otherwise an exception is thrown:
                        if (functions.TryGetValue(impl_func.Name, out desc) ||  
                            (lookForArgless && (desc = FindArglessStub(realMethods, functions, method, impl_func)) != null) ||
                            (!Configuration.IsLoaded && !Configuration.IsBeingLoaded && (desc = AddEmptyArglessStub(functions, impl_func.Name)) != null))
                        {
                            if (desc.Member == null)
                            {
                                // first argfull overload occurrence //

                                // estimate overload count by counting subsequent methods with the same CLR name (heuristics):
                                int j = i + 1;
                                while (j < realMethods.Length && realMethods[j].Name == method.Name) j++;
                                int estimated_overload_count = j - i;

                                new PhpLibraryFunction((PhpLibraryFunctionDesc)desc, new Name(impl_func.Name), impl_func.Options,
                                    estimated_overload_count);
                            }

                            PhpLibraryFunction.Overload overload;
                            desc.PhpLibraryFunction.AddOverload(method, out overload);

                            //if (NeedsArglessAttribute.IsSet(method))
                            //{/* function should be called via argless stub, occurs only if library function is defined in PHP library and calls arg-aware function inside */}
                        }
                        else
                        {
                            throw new ReflectionException(CoreResources.GetString("invalid_class_library_wrapper", dynamicWrapper.CodeBase));
                        }
					}
				}

				i++;
			}
		}

		private void ReflectConstants(FieldInfo[]/*!!*/ realFields, DualDictionary<string, DConstantDesc>/*!*/ constants)
		{
			foreach (FieldInfo field in realFields)
			{
                // reflect only fields with [ImplementsConstant] attribute:
                ImplementsConstantAttribute impl_const = ImplementsConstantAttribute.Reflect(field);
				if (impl_const != null)
                {
                    object value;

                    try
                    {
                        // expect the constant have literal value, otherwise crash
                        value = Convert.ClrLiteralToPhpLiteral(field.GetValue(null));
                    }	
                    catch(Exception)
                    {
                        throw new InvalidCastException();
                    }

                    GlobalConstant constant = new GlobalConstant(this ,new QualifiedName(new Name(impl_const.Name)), field);
                    constant.SetValue(value);

					constants[impl_const.Name, impl_const.CaseInsensitive] = constant.ConstantDesc;
                }
                    

                //// accepts literals of PHP/CLR primitive types only:
                //if (field.IsLiteral && (PhpVariable.IsLiteralPrimitiveType(field.FieldType) || field.FieldType.IsEnum))
                //{
                //    if (impl_const != null)								
                //    {
                //        // ...
                //    }
                //}
			}
		}

		/// <summary>
		/// Checks whether a specified type is valid class library type that can contain function declarations.
		/// </summary>
		public static bool IsLibraryType(Type/*!*/ type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

			return (type.IsPublic || type.IsNestedPublic) && type.Namespace != null &&
				type.Namespace.StartsWith(Namespaces.Library) && !type.IsGenericTypeDefinition;
		}

		#endregion

		#region Dynamic Wrapper Loading

		/// <summary>
		/// Loads a dynamic wrapper of a specified library assembly.
		/// </summary>
		/// <returns>The dynamic wrapper assembly.</returns>
		/// <remarks>Thread safe.</remarks>
		public void LoadDynamicWrapper()
		{
			if (dynamicWrapper != null)
				return;

			Assembly real_assembly = PhpLibraryAssembly.RealAssembly;
			
			if (PhpLibraryAssembly.Properties.ContainsDynamicStubs)
			{
				dynamicWrapper = real_assembly;
				return;
			}

#if SILVERLIGHT
			this.dynamicWrapper = LibraryBuilder.CreateDynamicWrapper(real_assembly);
#else
            if (!Configuration.IsLoaded && !Configuration.IsBeingLoaded) { return; } // continue without wrappers !! (VS Integration does not need it)
			string wrappers_dir = Configuration.GetPathsNoLoad().DynamicWrappers;

            // determine wrapper file name,
            // we are looking for an up-to-date wrapper or a writable location:
            string wrapper_file = DetermineDynamicWrapperFileName(wrappers_dir, real_assembly);
            string wrapper_fullfile = Path.Combine(wrappers_dir, wrapper_file);

            // 
			try
			{
				// builds wrapper if it doesn't exist:
                if (!IsDynamicWrapperUpToDate(real_assembly, wrapper_fullfile))
				{
                    Mutex mutex = new Mutex(false, String.Concat(@"Global\", wrapper_fullfile.ToLowerInvariant().Replace('\\', '/').Replace(':', '+')));   // do not use \ and : characters, to not confuse Mutex with file system which may not be accessible in this moment
					mutex.WaitOne();
					try
					{
						// if the file still does not exist, we are in charge!
                        if (!IsDynamicWrapperUpToDate(real_assembly, wrapper_fullfile))
                            LibraryBuilder.CreateDynamicWrapper(real_assembly, wrappers_dir, wrapper_file);
					}
					finally
					{
						mutex.ReleaseMutex();
					}
				}

				// loads wrapper:
                this.dynamicWrapper = System.Reflection.Assembly.LoadFrom(wrapper_fullfile);
			}
			catch (Exception e)
			{
                throw new DynamicWrapperLoadException(wrapper_fullfile, e);
			}
#endif
		}

		/// <summary>
		/// An exception thrown when dynamic wrapper cannot be loaded.
		/// </summary>
		public class DynamicWrapperLoadException : Exception
		{
			internal DynamicWrapperLoadException(string wrapperName, Exception inner)
				: base(CoreResources.GetString("dynamic_wrapper_loading_failed", wrapperName), inner)
			{
			}
		}

		#endregion

		#region Supporting Stuff

		/// <summary>
		/// Checks whether a specified type implements some extension in this library and returns its name if so.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>A name of the extension which is partly or entirely implemented by the <paramref name="type"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="type"/> is a <B>null</B> reference.</exception>
		public string GetImplementedExtension(Type/*!*/ type)
		{
			if (type == null)
				throw new ArgumentNullException("type");

            // make sure this type belongs to this library
            if (!PhpLibraryModule.IsLibraryType(type))
                return null;

            do
            {
			    object[] attrs = type.GetCustomAttributes(typeof(ImplementsExtensionAttribute), false);
			    
                if (attrs.Length == 1) 
                    return ((ImplementsExtensionAttribute)attrs[0]).Name;

                type = type.DeclaringType;
            }
            while (type != null);//loop because of nested enum types ( e.g. in ClassLibrary )

            return PhpLibraryAssembly.DefaultExtension;
		}

        /// <summary>
        /// Get the dynamic wrapper file name based on the given extension assembly.
        /// </summary>
        /// <param name="ass">Extension assembly which dynamic wrapper file is needed.</param>
        /// <param name="version">Wrapper version to to be appended to the file name.</param>
        /// <returns>Dynamic wrapper assembly file name corresponding to given <paramref name="ass"/>.</returns>
        public static string/*!*/DynamicWrapperFileName(Assembly/*!*/ass, int version)
        {
            var name = ass.GetName(false);
            return String.Concat(name.Name, DynamicAssemblySuffix, (version > 0) ? version.ToString() : string.Empty  ,".dll");
        }

        /// <summary>
        /// Determine file name to be use for dynamic wrapper. The resulting file has to not exists, be writable or up-to-date.
        /// </summary>
        /// <param name="wrappers_dir">Directory with dynamic wrappers.</param>
        /// <param name="ass">Class library assembly.</param>
        /// <returns>File name (relatively to <paramref name="wrappers_dir"/>) for dyamic wrapper.</returns>
        private static string/*!*/DetermineDynamicWrapperFileName(string/*!*/wrappers_dir, Assembly/*!*/ass)
        {
            Debug.Assert(!string.IsNullOrEmpty(wrappers_dir));
            Debug.Assert(ass != null);

            for (int version = 0; ; version++)
            {
                var fname = DynamicWrapperFileName(ass, version);
                var fullname = Path.Combine(wrappers_dir, fname);

                if (!File.Exists(fullname) || IsDynamicWrapperUpToDate(ass, fullname) || FileIsWritable(fullname))
                    return fname;
            }
        }

        /// <summary>
        /// Determine whether given <paramref name="filename"/> can be overwritten.
        /// </summary>
        /// <param name="filename">Full file name to be checked.</param>
        /// <returns><c>True</c> if file can be overwritten.</returns>
        private static bool FileIsWritable(string/*!*/filename)
        {
            try
            {
                using (var stream = File.OpenWrite(filename))
                { }
                return true;
            }
            catch(IOException){}    // how else determine the file is writable ??

            return false;
        }

        /// <summary>
        /// Check wheter dynamic wrapper for given <see cref="Assembly"/> <paramref name="ass"/> does exist and is up to date.
        /// </summary>
        /// <param name="ass">Class library assembly.</param>
        /// <param name="wrapper_name">Wrapper file name corresponding to the given assembly <paramref name="ass"/>.</param>
        /// <returns>True iff there is a valid up-to-date dynamic wrapper for given assembly.</returns>
        private static bool IsDynamicWrapperUpToDate(Assembly/*!*/ass, string/*!*/wrapper_name)
        {
            Debug.Assert(ass != null);
            Debug.Assert(wrapper_name != null);

            if (File.Exists(wrapper_name))
            {
                if (File.GetLastWriteTimeUtc(ass.Location) < File.GetLastWriteTimeUtc(wrapper_name))
                    return true;
            }

            return false;
        }

		#endregion
	}
}
