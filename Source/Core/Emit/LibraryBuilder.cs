/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections;
using PHP.Core;
using PHP.Core.Reflection;
using System.Collections.Generic;

namespace PHP.Core.Emit
{
	public static class LibraryBuilder
	{
		private const TypeAttributes DynamicTypeAttributes =
		  TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public;

#if !SILVERLIGHT
		/// <summary>
		/// Creates a dynamic wrapper of a specified assembly.
		/// </summary>
		public static string CreateDynamicWrapper(Assembly/*!*/ assembly, string directory, string filename)
		{
			return CreateDynamicWrapper(null, assembly, directory, filename);
		}

		/// <summary>
		/// Creates a dynamic wrapper of a specified assembly.
		/// </summary>
		/// <param name="assembly">The assembly which wrapper to create.</param>
		/// <param name="directory">A target directory. A <B>null</B> reference means CL dynamic directory.</param>
		/// <param name="attr">Specifies a type of the 'ImplementsFunction' attribute. If it
		/// ios set to null, the type from the current assembly is used.</param>
        /// <param name="filename">File name of the resulting dynamic wrapper.</param>
		/// <returns>The generated assembly file path.</returns>
		/// <remarks>
		/// <para>
		/// Called either by utility or when indirect call or callback is to be made but 
		/// target is not found in cache and dynamic wrapper doesn't exists. 
		/// This implies that functions table has to be populated. 
		/// </para>
		/// <para>
		/// Functions table is traversed and all methods declared in <paramref name="assembly"/>
		/// is added to a new dynamic wrapper.
		/// </para>
		/// </remarks>
		/// <exception cref="Exception">Something went wrong during assembly building.</exception>
		public static string CreateDynamicWrapper(Type/*!*/ attr, Assembly/*!*/ assembly, string directory, string filename)
		{
			return CreateDynamicWrapperInternal(attr, assembly, directory, filename);
		}
#endif

		// Ugly silverlight hacking
#if SILVERLIGHT
		private static Assembly CreateDynamicWrapperInternal(Type/*!*/ attr, Assembly/*!*/ assembly, string directory, string filename)
#else
        private static string CreateDynamicWrapperInternal(Type/*!*/ attr, Assembly/*!*/ assembly, string directory, string filename)
#endif
		{
			string assembly_base_name;
			AssemblyBuilder assembly_builder;
			ModuleBuilder module_builder;
			TypeBuilder type_builder;
			MethodBuilder method_builder;

			IndexedPlace stack_place = new IndexedPlace(PlaceHolder.Argument, 1);
			
			// TODO: if function requires this reference, we need to pass it somehow
			IPlace self_ref = LiteralPlace.Null;
            IPlace script_context = new Place(stack_place, Fields.PhpStack_Context);
			IPlace rt_variables = new Place(stack_place, Fields.PhpStack_Variables);
			IPlace naming_context = new Place(stack_place, Fields.PhpStack_NamingContext);
            IPlace class_context = new Place(null, Fields.UnknownTypeDesc.Singleton);

#if !SILVERLIGHT
			if (directory == null)
				directory = Configuration.GetPathsNoLoad().DynamicWrappers;
			Directory.CreateDirectory(directory);
#endif

			Dictionary<string, List<PhpLibraryFunction.Overload>> functions = GetLibraryFunctions(attr, assembly);

			OverloadsBuilder overloads_builder = new OverloadsBuilder(
				false,
				stack_place,
				new OverloadsBuilder.ParameterLoader(PhpStackBuilder.EmitValuePeekUnchecked),
				new OverloadsBuilder.ParameterLoader(PhpStackBuilder.EmitReferencePeekUnchecked),
				new OverloadsBuilder.ParametersLoader(PhpStackBuilder.EmitPeekAllArguments));

#if SILVERLIGHT
			int commaIdx = assembly.FullName.IndexOf(',');
			assembly_base_name = commaIdx == -1 ? assembly.FullName : assembly.FullName.Substring(0, commaIdx);
#else
			// securitycritical
			assembly_base_name = assembly.GetName().Name;
#endif

			// appends assembly name with the suffix:
			AssemblyName name = new AssemblyName();
#if !SILVERLIGHT
			name.Version = assembly.GetName().Version;
#endif
			name.Name = assembly_base_name + PhpLibraryModule.DynamicAssemblySuffix;

#if SILVERLIGHT

            // defines assembly with storage in the dynamic code path:
            assembly_builder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Run);

            // defines a module:
            module_builder = assembly_builder.DefineDynamicModule(PhpLibraryModule.DynamicWrapperModuleName);

#else
			// defines assembly with storage in the dynamic code path:
			assembly_builder = AppDomain.CurrentDomain.DefineDynamicAssembly(name, AssemblyBuilderAccess.Save, directory);

			// defines a module:
            module_builder = assembly_builder.DefineDynamicModule(PhpLibraryModule.DynamicWrapperModuleName, filename);
#endif

			// defines type which will contain all mathods:  
			type_builder = module_builder.DefineType(Namespaces.LibraryStubs + Type.Delimiter + assembly_base_name, DynamicTypeAttributes);

			try
			{
				foreach (KeyValuePair<string, List<PhpLibraryFunction.Overload>> function in functions)
				{
					// defines method:
					method_builder = type_builder.DefineMethod(function.Key, MethodAttributes.Public | MethodAttributes.Static,
						Types.Object[0], Types.Object_PhpStack);

					ILEmitter il = new ILEmitter(method_builder);
					overloads_builder.IL = il;
					overloads_builder.Aux = stack_place;
					overloads_builder.FunctionName = new Name(function.Key);

					// if run-time variables are needed by the overload sets a place where they are stored up:
                    overloads_builder.EmitCallSwitch(self_ref, script_context, rt_variables, naming_context, class_context, function.Value);

					// RETURN:
					il.Emit(OpCodes.Ret);
				}

			}
			catch (Exception e)
			{
				Debug.WriteLine("A", e.ToString());
			}
			type_builder.CreateType();

#if SILVERLIGHT
			return assembly_builder;
#else
            assembly_builder.Save(filename);
            return Path.Combine(directory, filename);
#endif
		}

		private static Dictionary<string, List<PhpLibraryFunction.Overload>>/*!*/ GetLibraryFunctions(Type attr, Assembly/*!*/ assembly)
		{
			Dictionary<string, List<PhpLibraryFunction.Overload>> result =
				new Dictionary<string, List<PhpLibraryFunction.Overload>>(500);

			foreach (Type type in assembly.GetTypes())
			{
				if (PhpLibraryModule.IsLibraryType(type))
				{
					foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
					{
						if (!method.IsGenericMethodDefinition)
						{
							ImplementsFunctionAttribute impl_func = 
								(attr == null)?
									ImplementsFunctionAttribute.Reflect(method):
									ImplementsFunctionAttribute.ReflectDynamic(attr, method);

							if (impl_func != null)
							{
								List<PhpLibraryFunction.Overload> overloads;
								if (!result.TryGetValue(impl_func.Name, out overloads))
								{
									overloads = new List<PhpLibraryFunction.Overload>();
									result.Add(impl_func.Name, overloads);
								}

								if (PhpLibraryFunction.AddOverload(overloads, method, impl_func.Options) == DRoutine.InvalidOverloadIndex)
									throw new ReflectionException(CoreResources.GetString("invalid_class_library", assembly.CodeBase));

							}
						}
					}
				}
			}

			return result;
		}

#if SILVERLIGHT
		/// <summary>
		/// Generates dynamic wrapper and returns a dynamic assembly.
		/// This is used by Silverlight version where we can't save the assembly to the disk.
		/// </summary>
		/// <param name="real_assembly">Assembly with the library</param>
		/// <returns>Dynamic wrapper generated dynamically</returns>
		internal static Assembly CreateDynamicWrapper(Assembly real_assembly)
		{
			return CreateDynamicWrapperInternal(null, real_assembly);
		}
#endif
	}
}
