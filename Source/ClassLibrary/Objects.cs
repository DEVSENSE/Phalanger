/*

 Copyright (c) 2004-2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
	/// <summary>
	/// Contains object-related class library functions.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpObjects
	{
		/// <summary>
		/// Calls the method referred by <paramref name="methodName"/> from the user defined
		/// object <paramref name="classNameOrObject"/> with parameters <paramref name="args"/>.
		/// </summary>
        /// <param name="caller">DTypeDesc of the caller's class context. Can be UnknownTypeDesc.</param>
        /// <param name="methodName">The name of the method.</param>
		/// <param name="classNameOrObject">An instance to invoke the method on or a class name.</param>
		/// <param name="args">Parameters to invoke the method with.</param>
		/// <returns>The method's return value (always dereferenced).</returns>
        internal static object CallUserMethodInternal(DTypeDesc caller, string methodName, object classNameOrObject, ICollection args)
		{
			PhpException.Throw(PhpError.Notice, LibResources.GetString("call_user_method_deprecated"));

			object ret_val = false;
			DObject obj;
			string class_name;

			ScriptContext context = ScriptContext.CurrentContext;

            //DTypeDesc classContext = PhpStackTrace.GetClassContext();  // TODO: GetClassContext only if needed by context.ResolveType
            if (caller != null && caller.IsUnknown) caller = PhpStackTrace.GetClassContext();

			if ((obj = classNameOrObject as DObject) != null)
			{
				// push arguments on stack
				context.Stack.AddFrame(args);
				ret_val = obj.InvokeMethod(methodName, caller, context);
			}
			else if ((class_name = PhpVariable.AsString(classNameOrObject)) != null)
			{
				// push arguments on stack
				context.Stack.AddFrame(args);
				
				ResolveTypeFlags flags = ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors;
                DTypeDesc type = PHP.Core.Convert.ObjectToTypeDesc(class_name, flags, caller, context, null, null);

                ret_val = Operators.InvokeStaticMethod(type, methodName, null, caller, context);
			}
			else
			{
				PhpException.InvalidArgument("classNameOrObject", LibResources.GetString("arg:not_object_or_class_name"));
			}

			return PhpVariable.Dereference(ret_val);
		}

		/// <summary>
		/// Calls the method referred by <paramref name="methodName"/> from the user defined
		/// object <paramref name="classNameOrObject"/> with parameters <paramref name="args"/>.
		/// </summary>
        /// <param name="caller">DTypeDesc of the caller's class context. Can be UnknownTypeDesc.</param>
        /// <param name="methodName">The name of the method.</param>
		/// <param name="classNameOrObject">An instance to invoke the method on or a class name.</param>
		/// <param name="args">Parameters to invoke the method with.</param>
		/// <returns>The method's return value (always dereferenced).</returns>
        [ImplementsFunction("call_user_method", FunctionImplOptions.NeedsClassContext)]
		public static object CallUserMethod(DTypeDesc caller, string methodName, object classNameOrObject, params object[] args)
		{
            return CallUserMethodInternal(caller, methodName, classNameOrObject, args);
		}

		/// <summary>
		/// Calls the method referred by <paramref name="methodName"/> from the user defined
		/// object <paramref name="classNameOrObject"/> with parameters <paramref name="args"/>.
		/// </summary>
        /// <param name="caller">DTypeDesc of the caller's class context. Can be UnknownTypeDesc.</param>
		/// <param name="methodName">The name of the method.</param>
		/// <param name="classNameOrObject">An instance to invoke the method on or a class name.</param>
		/// <param name="args">Parameters to invoke the method with.</param>
		/// <returns>The method's return value.</returns>
		[ImplementsFunction("call_user_method_array", FunctionImplOptions.NeedsClassContext)]
		public static object CallUserMethodArray(DTypeDesc caller, string methodName, object classNameOrObject, PhpArray args)
		{
            return CallUserMethodInternal(caller, methodName, classNameOrObject, ((IDictionary)args).Values);
		}

		/// <summary>
		/// Returns a <see cref="PhpArray"/> of default fields of a class.
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
		/// <param name="className">The name of the class.</param>
		/// <param name="parentsFirst">Whether to list vars in PHP order (child vars then parent vars) or logical (parent vars then child).</param>
		/// <param name="includeStatic">Specifies whether static variables should be listed as well</param>
		/// <returns>Array of <paramref name="className"/>'s default fields.</returns>
		/// <remarks>
		/// <para>
		/// The resulting array elements are in the form of <c>varname => value</c>, where <c>value</c>
		/// is the default value of the field.
		/// </para>
		/// <para>
		/// This method returns fields declared in <paramref name="className"/> and all its parent classes.
		/// </para>
		/// </remarks>
		[ImplementsFunction("get_class_vars", FunctionImplOptions.NeedsClassContext)]
		[return: PhpDeepCopy]
		[return: CastToFalse]
        public static PhpArray GetClassVars(DTypeDesc caller, string className, bool parentsFirst, bool includeStatic)
		{
			ScriptContext script_context = ScriptContext.CurrentContext;
			DTypeDesc type = script_context.ResolveType(className);
			if (type == null) return null;

			// determine the calling type
            //DTypeDesc caller = PhpStackTrace.GetClassContext();
            if (caller != null && caller.IsUnknown) caller = PhpStackTrace.GetClassContext();
			PhpArray result = new PhpArray();

			// add instance properties
			bool have_instance_props = false;
			if (!type.IsAbstract)
			{
				// the class has to be instantiated in order to discover default instance property values
				// (the constructor will initialize default properties, user defined constructor will not be called)
				DObject obj = type.New(script_context) as DObject;
				if (obj == null) return null;

				// populate the resulting array taking into account current caller
				IDictionaryEnumerator enumerator = obj.GetEnumerator(caller);
				while (enumerator.MoveNext())
				{
					result.Add(enumerator.Key, enumerator.Value);
				}

				have_instance_props = true;
			}

			// add static fields (static and instance fields if the type is abstract)
			if (includeStatic)
			{
				foreach (KeyValuePair<VariableName, DPropertyDesc> pair in type.EnumerateProperties(caller))
				{
					if (pair.Value.IsStatic)
					{
						result.Add(pair.Key.ToString(), pair.Value.Get(null));
					}
					else if (!have_instance_props)
					{
						result.Add(pair.Key.ToString(), null);
					}
				}
			}

			result.InplaceCopyOnReturn = true;
			return result;
		}


		[ImplementsFunction("get_class_vars", FunctionImplOptions.NeedsClassContext)]
		[return: PhpDeepCopy]
		[return: CastToFalse]
		public static PhpArray GetClassVars(DTypeDesc caller, string className)
		{
			return GetClassVars(caller, className, false, true);
		}

        		/// <summary>
		/// Returns a <see cref="PhpArray"/> of defined fields for the specified object <paramref name="obj"/>. 
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
		/// <param name="obj">The object whose fields are requested.</param>
		/// <returns>Array of <paramref name="obj"/>'s fields (name => value pairs).</returns>
		/// <remarks>
		/// This method returns default fields (those declared in the class using &quot;var&quot;) declared in the
		/// class and all its parent classes) as well as fields added at runtime.
		/// </remarks>
        [ImplementsFunction("get_object_vars", FunctionImplOptions.NeedsClassContext)]
        //[return: PhpDeepCopy]
        public static PhpArray GetObjectVars(DTypeDesc caller, DObject obj)
        {
            return GetObjectVars(caller,obj,false);
        }

		/// <summary>
		/// Returns a <see cref="PhpArray"/> of defined fields for the specified object <paramref name="obj"/>. 
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
		/// <param name="obj">The object whose fields are requested.</param>
        /// <param name="IgnoreReferences">References will be omited from the result</param>
		/// <returns>Array of <paramref name="obj"/>'s fields (name => value pairs).</returns>
		/// <remarks>
		/// This method returns default fields (those declared in the class using &quot;var&quot;) declared in the
		/// class and all its parent classes) as well as fields added at runtime.
		/// </remarks>
		[ImplementsFunction("get_object_vars", FunctionImplOptions.NeedsClassContext)]
        //[return: PhpDeepCopy] // already deep copied
		public static PhpArray GetObjectVars(DTypeDesc caller, DObject obj, bool IgnoreReferences)
		{
			if (obj == null) return null;

            Converter<object, object> copy = null;

            ///////////////////////////////////////
            // This is hot fix for a reference counting problem when reference aren't released in same way as in PHP. 
            // Hence, we need to perform deep copy ignoring references
            if (IgnoreReferences)
                copy = (value) =>
                    {
                        PhpReference refValue = value as PhpReference;
                        if (refValue != null)
                            return copy(refValue.Value);

                        PhpArray array = value as PhpArray;
                        if (array != null)
                        {
                            PhpArray dst = new PhpArray(array.IntegerCount, array.StringCount);

                            foreach (KeyValuePair<IntStringKey, object> entry in array)
                            {
                                // checks whether a value is a reference pointing to the instance itself:
                                refValue = entry.Value as PhpReference;
                                if (refValue != null && refValue.Value == array)
                                {
                                    // copies the value so that it will self-reference the new instance (not the old one):
                                    dst.Add(entry.Key, new PhpReference(dst));
                                }
                                else
                                {
                                    dst.Add(entry.Key, copy(entry.Value));
                                }
                            }
                            return dst;
                        }

                        return value;
                    };
            else
                copy = (value) => { return PhpVariable.DeepCopy(value); };  // perform InplaceDeepCopy() here to save one more iteration through the array
            ///////////////////////////////////////

            PhpArray result = new PhpArray(0, obj.Count);
            var foreachEnumerator = obj.GetEnumerator((caller != null && caller.IsUnknown) ? PhpStackTrace.GetClassContext() : caller);
            while (foreachEnumerator.MoveNext())
			//foreach (DictionaryEntry pair in obj)
			{
                DictionaryEntry pair = (DictionaryEntry)foreachEnumerator.Current;
				result.Add((string)pair.Key, copy(pair.Value));
			}

            //result.InplaceCopyOnReturn = true;    // already deep copied

			return result;
		}

		/// <summary>
		/// Verifies whether the method given by <paramref name="methodName"/> has been defined for the given
		/// object <paramref name="obj"/>. 
		/// </summary>
        /// <param name="caller">Current class context.</param>
		/// <param name="obj">The object to test.</param>
		/// <param name="methodName">The name of the method.</param>
		/// <returns><B>True</B> if the method given by <paramref name="methodName"/> has been defined for the given
		/// object <paramref name="obj"/>, <B>false</B> otherwise.</returns>
        [ImplementsFunction("method_exists", FunctionImplOptions.NeedsClassContext)]
		public static bool MethodExists(DTypeDesc caller, object obj, string methodName)
		{
			if (obj == null || string.IsNullOrEmpty(methodName)) return false;

            DTypeDesc dtype;
            DObject dobj;
            string str;

            if ((dobj = (obj as DObject)) != null)
            {
                dtype = dobj.TypeDesc;
                if (dtype == null)
                {
                    Debug.Fail("DObject.TypeDesc should not be null");
                    return false;
                }
            }
            else if ((str = PhpVariable.AsString(obj)) != null)
            {
                ScriptContext script_context = ScriptContext.CurrentContext;
                dtype = script_context.ResolveType(str, null, caller, null, ResolveTypeFlags.UseAutoload);
                if (dtype == null)
                    return false;
            }
            else
            {
                // other type names are not handled
                return false;
            }

			DRoutineDesc method;
            return (dtype.GetMethod(new Name(methodName), dtype, out method) != GetMemberResult.NotFound);
		}

		/// <summary>
		/// Converts a class name or class instance to <see cref="DTypeDesc"/> object.
		/// </summary>
		/// <param name="scriptContext">Current <see cref="ScriptContext"/>.</param>
		/// <param name="namingContext">Current <see cref="NamingContext"/>.</param>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
        /// <param name="classNameOrObject">The class name or class instance (<see cref="DObject"/>).</param>
		/// <param name="useAutoload"><B>True</B> iff the <c>__autoload</c> magic function should be used.</param>
		/// <returns>The type desc that corresponds to <paramref name="classNameOrObject"/> or <B>null</B>
		/// if the type could not be found or <paramref name="classNameOrObject"/> is neither a string
		/// nor <see cref="DObject"/>.</returns>
		internal static DTypeDesc ClassNameOrObjectToType(ScriptContext/*!*/ scriptContext, NamingContext namingContext,
			DTypeDesc caller, object classNameOrObject, bool useAutoload)
		{
			string class_name = PhpVariable.AsString(classNameOrObject);
			if (class_name != null)
			{
				// lookup the Type
				return scriptContext.ResolveType(class_name, namingContext, caller, null,
					(useAutoload ? ResolveTypeFlags.UseAutoload : ResolveTypeFlags.None));
			}
			else
			{
				DObject obj = classNameOrObject as DObject;
				if (obj != null) return obj.TypeDesc;
			}

			return null;
		}

		/// <summary>
		/// Verifies whether the property given by <paramref name="propertyName"/> has been defined for the given
		/// object object or class. 
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
		/// <param name="classNameOrObject">The object (<see cref="DObject"/>) or the name of a class
		/// (<see cref="String"/>).</param>
		/// <param name="propertyName">The name of the property.</param>
		/// <returns><B>True</B> if the property given by <paramref name="propertyName"/> has been defined for the
		/// given class or object and is accessible from current class context.</returns>
		/// <remarks>
		/// This function has different semantics than <see cref="MethodExists"/>, which ignores visibility.
		/// If an object is passed in the first parameter, the property is searched among runtime fields as well.
		/// </remarks>
		[ImplementsFunction("property_exists", FunctionImplOptions.NeedsClassContext)]
		public static bool PropertyExists(DTypeDesc caller, object classNameOrObject, string propertyName)
		{
			ScriptContext context = ScriptContext.CurrentContext;
			DTypeDesc type = ClassNameOrObjectToType(context, null, caller, classNameOrObject, true);
			if (type == null) return false;

			// determine the calling class context
			//DTypeDesc caller = PhpStackTrace.GetClassContext();
            if (caller != null && caller.IsUnknown) caller = PhpStackTrace.GetClassContext();

			DPropertyDesc property;
			if (type.GetProperty(new VariableName(propertyName), caller, out property) == GetMemberResult.OK)
			{
				// CT property was found
				return true;
			}
			else
			{
				// search RT fields, if possible
				DObject obj = classNameOrObject as DObject;
				return (obj != null && obj.RuntimeFields != null && obj.RuntimeFields.ContainsKey(propertyName));
			}
		}

		/// <summary>
		/// Returns all methods defined in the specified class or class of specified object, and its predecessors.
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
		/// <param name="classNameOrObject">The object (<see cref="DObject"/>) or the name of a class
		/// (<see cref="String"/>).</param>
		/// <returns>Array of all methods defined in <paramref name="classNameOrObject"/>.</returns>
		[ImplementsFunction("get_class_methods", FunctionImplOptions.NeedsClassContext)]
		public static PhpArray GetClassMethods(DTypeDesc caller, object classNameOrObject)
		{
			ScriptContext context = ScriptContext.CurrentContext;
			DTypeDesc type = ClassNameOrObjectToType(context, null, caller, classNameOrObject, true);
			if (type == null) return null;

			// determine the calling type
			//DTypeDesc caller = PhpStackTrace.GetClassContext();
            if (caller != null && caller.IsUnknown) caller = PhpStackTrace.GetClassContext();

			PhpArray result = new PhpArray();

			foreach (KeyValuePair<Name, DRoutineDesc> pair in type.EnumerateMethods(caller))
			{
				result.Add(pair.Key.ToString());
			}

			return result;
		}

		/// <summary>
		/// Returns a <see cref="PhpArray"/> with names of all defined classes (system and user).
		/// </summary>
		/// <returns><see cref="PhpArray"/> of class names.</returns>
		[ImplementsFunction("get_declared_classes")]
		public static PhpArray GetDeclaredClasses()
		{
			return (PhpArray)ScriptContext.CurrentContext.GetDeclaredClasses(new PhpArray());
		}

		/// <summary>
		/// Returns a <see cref="PhpArray"/> with names of all defined interfaces (system and user).
		/// </summary>
		/// <returns><see cref="PhpArray"/> of interface names.</returns>
		[ImplementsFunction("get_declared_interfaces")]
		public static PhpArray GetDeclaredInterfaces()
		{
			return (PhpArray)ScriptContext.CurrentContext.GetDeclaredInterfaces(new PhpArray());
		}

		/// <summary>
		/// Tests whether the class given by <paramref name="classNameOrObject"/> is derived from a class given
		/// by <paramref name="baseClassName"/>.
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
        /// <param name="classNameOrObject">The object (<see cref="DObject"/>) or the name of a class
		/// (<see cref="String"/>).</param>
		/// <param name="baseClassName">The name of the (base) class.</param>
		/// <returns><B>true</B> if <paramref name="classNameOrObject"/> implements or extends <paramref name="baseClassName"/>,
		/// <B>false</B> otherwise.</returns>
		[ImplementsFunction("is_subclass_of", FunctionImplOptions.NeedsClassContext)]
		public static bool IsSubclassOf(DTypeDesc caller, object classNameOrObject, string baseClassName)
		{
			ScriptContext context = ScriptContext.CurrentContext;

			DTypeDesc type = ClassNameOrObjectToType(context, null, caller, classNameOrObject, true);
			if (type == null) return false;

			// look for the class, do not use autoload (since PHP 5.1):
            DTypeDesc base_type = context.ResolveType(baseClassName, null, caller, null, ResolveTypeFlags.None); // do not call autoload [workitem:26664]
			if (base_type == null) return false;

			return (base_type.RealType.IsAssignableFrom(type.RealType) && base_type != type);
		}

		/// <summary>
		/// Tests whether a given class is defined.
		/// </summary>
        /// <param name="caller">The caller class context to resolve visibility.</param>
		/// <param name="className">The name of the class.</param>
		/// <returns><B>true</B> if the class given by <paramref name="className"/> has been defined,
		/// <B>false</B> otherwise.</returns>
		[ImplementsFunction("class_exists", FunctionImplOptions.NeedsClassContext)]
        [PureFunction(typeof(PhpObjects), "ClassExists_Analyze_1")]
        public static bool ClassExists(DTypeDesc caller, string className)
		{
			return ClassExists(caller, className, true);
		}

		/// <summary>
		/// Tests whether a given class is defined.
		/// </summary>
        /// <param name="caller">The caller class context to resolve visibility.</param>
        /// <param name="className">The name of the class.</param>
		/// <param name="autoload">Whether to attempt to call <c>__autoload</c>.</param>
		/// <returns><B>true</B> if the class given by <paramref name="className"/> has been defined,
		/// <B>false</B> otherwise.</returns>
		[ImplementsFunction("class_exists", FunctionImplOptions.NeedsClassContext)]
        [PureFunction(typeof(PhpObjects), "ClassExists_Analyze_2")]
        public static bool ClassExists(DTypeDesc caller, string className, bool autoload)
		{
			DTypeDesc type = ScriptContext.CurrentContext.ResolveType(className, null, caller, null, autoload ? ResolveTypeFlags.UseAutoload : ResolveTypeFlags.None);
			return type != null;
        }

        #region analyzer of class_exists

        public static PHP.Core.AST.DirectFcnCall.EvaluateInfo ClassExists_Analyze_2(Analyzer analyzer, string name, bool autoload)
        {
            // ignore autoload at the compile time
            return ClassExists_Analyze_1(analyzer, name);
        }

        public static PHP.Core.AST.DirectFcnCall.EvaluateInfo ClassExists_Analyze_1(Analyzer analyzer, string name)
        {
            QualifiedName? alias;

            DType type = analyzer.SourceUnit.ResolveTypeName(
                new QualifiedName(new Name(name)),
                analyzer.CurrentScope,
                out alias,
                null,
                PHP.Core.Parsers.Position.Invalid,
                false);

            if (type == null || type.IsUnknown)
                return null;  // type is not known at the compilation time. However it can be defined at the runtime (dynamic include, script library, etc).

            return new PHP.Core.AST.DirectFcnCall.EvaluateInfo()
            {
                value = true    // type is definitely known the the compilation time
            };
        }

        #endregion

        /// <summary>
		/// Tests whether a given interface is defined.
		/// </summary>
        /// <param name="caller">The class context of the caller.</param>
		/// <param name="ifaceName">The name of the interface.</param>
		/// <returns><B>true</B> if the interface given by <paramref name="ifaceName"/> has been defined,
		/// <B>false</B> otherwise.</returns>
		[ImplementsFunction("interface_exists", FunctionImplOptions.NeedsClassContext)]
		public static bool InterfaceExists(DTypeDesc caller, string ifaceName)
		{
			return InterfaceExists(caller, ifaceName, true);
		}

		/// <summary>
		/// Tests whether a given interface is defined.
		/// </summary>
        /// <param name="caller">The class context of the caller.</param>
        /// <param name="ifaceName">The name of the interface.</param>
		/// <param name="autoload">Whether to attempt to call <c>__autoload</c>.</param>
		/// <returns><B>true</B> if the interface given by <paramref name="ifaceName"/> has been defined,
		/// <B>false</B> otherwise.</returns>
		[ImplementsFunction("interface_exists", FunctionImplOptions.NeedsClassContext)]
		public static bool InterfaceExists(DTypeDesc caller, string ifaceName, bool autoload)
		{
            DTypeDesc type = ScriptContext.CurrentContext.ResolveType(ifaceName, null, caller, null, autoload ? ResolveTypeFlags.UseAutoload : ResolveTypeFlags.None);
			return type != null && type.IsInterface;
		}

        /// <summary>
        /// Returns the name of the current class.
        /// </summary>
        /// <param name="caller">Current class context.</param>
        /// <returns>Current class name.</returns>
        [ImplementsFunction("get_class", FunctionImplOptions.NeedsClassContext)]
        [return: CastToFalse]
        public static string GetClass(DTypeDesc caller)
        {
            if (caller == null || caller.IsUnknown)
                return null;

            return caller.MakeFullName();
        }

		/// <summary>
		/// Returns the name of the class of which the object <paramref name="var"/> is an instance.
		/// </summary>
        /// <param name="caller">Current class context.</param>
		/// <param name="var">The object whose class is requested.</param>
		/// <returns><paramref name="var"/>'s class name or current class name if <paramref name="var"/> is
		/// <B>null</B>.</returns>
		[ImplementsFunction("get_class", FunctionImplOptions.NeedsClassContext)]
		[return: CastToFalse]
		public static string GetClass(DTypeDesc caller, object var)
		{
            if (var == null)
                return GetClass(caller);

			DObject obj = var as DObject;
			return (obj != null) ? obj.TypeName : null;
		}

        [ImplementsFunction("get_called_class", FunctionImplOptions.NeedsLateStaticBind)]
		[return: CastToFalse]
		public static string GetCalledClass(DTypeDesc caller)
		{
            if (caller == null || caller.IsUnknown)
                return null;

            return caller.MakeFullName();
		}

		/// <summary>
		/// Gets the name of the class from which class given by <paramref name="classNameOrObject"/>
		/// inherits.
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
        /// <param name="classNameOrObject">The object (<see cref="DObject"/>) or the name of a class
		/// (<see cref="String"/>).</param>
		/// <returns>If <paramref name="classNameOrObject"/> is an <see cref="DObject"/>, returns the name
		/// of the parent class of the class of which <paramref name="classNameOrObject"/> is an instance.
		/// If <paramref name="classNameOrObject"/> is a <see cref="String"/>, returns the name of the parent
		/// class of the class with that name.</returns>
		/// <remarks>
		/// If the class given by <paramref name="classNameOrObject"/> has no parent in PHP class hierarchy,
		/// this method returns <B>null</B>.
		/// </remarks>
		[ImplementsFunction("get_parent_class", FunctionImplOptions.NeedsClassContext)]
		[return: CastToFalse]
        [PureFunction(typeof(PhpObjects), "GetParentClass_Analyze")]
		public static string GetParentClass(DTypeDesc caller, object classNameOrObject)
		{
			ScriptContext context = ScriptContext.CurrentContext;
            DTypeDesc type = ClassNameOrObjectToType(context, null, caller, classNameOrObject, true);
			if (type == null || type.IsInterface) return null;

			DTypeDesc parent_type = type.Base;
			return (parent_type == null ? null : parent_type.MakeFullName());
		}

        #region analyzer of get_parent_class

        [return: CastToFalse]
        public static PHP.Core.AST.DirectFcnCall.EvaluateInfo GetParentClass_Analyze(Analyzer analyzer, string name)
        {
            QualifiedName? alias;

            DType type = analyzer.SourceUnit.ResolveTypeName(
                new QualifiedName(new Name(name)),
                analyzer.CurrentScope,
                out alias,
                null,
                PHP.Core.Parsers.Position.Invalid,
                false);

            if (type == null || type.IsUnknown)
                return null;  // type is not known at the compilation time. However it can be defined at the runtime (dynamic include, script library, etc).

            // type is definitely known the the compilation time
            var parent_type = type.Base;
            return new PHP.Core.AST.DirectFcnCall.EvaluateInfo()
            {
                value = (parent_type == null ? null : parent_type.FullName)
            };
        }

        #endregion

		/// <summary>
		/// Tests whether <paramref name="obj"/>'s class is derived from a class given by <paramref name="className"/>.
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
        /// <param name="obj">The object to test.</param>
		/// <param name="className">The name of the class.</param>
		/// <returns><B>true</B> if the object <paramref name="obj"/> belongs to <paramref name="className"/> class or
		/// a class which is a subclass of <paramref name="className"/>, <B>false</B> otherwise.</returns>
        [ImplementsFunction("is_a", FunctionImplOptions.NeedsClassContext)]
		public static bool IsA(DTypeDesc caller, object obj, string className)
		{
			if (obj == null || !(obj is DObject)) return false;

            DObject dobj = (DObject)obj;
            DTypeDesc type = ScriptContext.CurrentContext.ResolveType(className, null, caller, null, ResolveTypeFlags.None);    // do not call autoload [workitem:26664]
			if (type == null) return false;

			return type.IsAssignableFrom(dobj.TypeDesc);
		}

		/// <summary>
		/// Returns a <see cref="PhpArray"/> with keys and values being names of a given class's
		/// base classes.
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
        /// <param name="classNameOrObject">The <see cref="DObject"/> or class name to get base classes of.</param>
		/// <param name="useAutoload"><B>True</B> if the magic <c>__autoload</c> function should be used.</param>
		/// <returns>The <see cref="PhpArray"/> with base class names.</returns>
		[ImplementsFunction("class_parents", FunctionImplOptions.NeedsClassContext)]
		[return: CastToFalse]
        public static PhpArray GetClassParents(DTypeDesc caller, object classNameOrObject, bool useAutoload)
		{
			ScriptContext context = ScriptContext.CurrentContext;
			DTypeDesc type = ClassNameOrObjectToType(context, null, caller, classNameOrObject, useAutoload);
			if (type == null || type.IsInterface) return null;

			PhpArray result = new PhpArray();

			while ((type = type.Base) != null)
			{
				string class_name = type.MakeFullName();
				result.Add(class_name, class_name);
			}

			return result;
		}

		/// <summary>
		/// Returns a <see cref="PhpArray"/> with keys and values being names of a given class's
		/// base classes.
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
        /// <param name="classNameOrObject">The <see cref="DObject"/> or class name to get base classes of.</param>
		/// <returns>The <see cref="PhpArray"/> with base class names.</returns>
        [ImplementsFunction("class_parents", FunctionImplOptions.NeedsClassContext)]
		[return: CastToFalse]
        public static PhpArray GetClassParents(DTypeDesc caller, object classNameOrObject)
		{
			return GetClassParents(caller, classNameOrObject, true);
		}

		/// <summary>
		/// Returns a <see cref="PhpArray"/> with keys and values being names of interfaces implemented by a given
		/// class.
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
        /// <param name="classNameOrObject">The <see cref="DObject"/> or class name to get implemented interfaces of.
		/// <param name="useAutoload"><B>True</B> if the magic <c>__autoload</c> function should be used.</param>
		/// </param>
		/// <returns>The <see cref="PhpArray"/> with interface names.</returns>
        [ImplementsFunction("class_implements", FunctionImplOptions.NeedsClassContext)]
		[return: CastToFalse]
        public static PhpArray GetClassInterfaces(DTypeDesc caller, object classNameOrObject, bool useAutoload)
		{
			ScriptContext context = ScriptContext.CurrentContext;
			DTypeDesc type = ClassNameOrObjectToType(context, null, caller, classNameOrObject, useAutoload);
			if (type == null) return null;

			PhpArray result = new PhpArray();

			DTypeDesc[] interfaces = type.GetImplementedInterfaces();
			for (int i = 0; i < interfaces.Length; i++)
			{
				string iface_name = interfaces[i].MakeFullName();
				result[iface_name] = iface_name;
			}

			return result;
		}

		/// <summary>
		/// Returns a <see cref="PhpArray"/> with keys and values being names of interfaces implemented by a given
		/// class.
		/// </summary>
        /// <param name="caller">The caller of the method to resolve visible properties properly. Can be UnknownTypeDesc.</param>
        /// <param name="classNameOrObject">The <see cref="DObject"/> or class name to get implemented interfaces of.
		/// </param>
		/// <returns>The <see cref="PhpArray"/> with interface names.</returns>
        [ImplementsFunction("class_implements", FunctionImplOptions.NeedsClassContext)]
		[return: CastToFalse]
        public static PhpArray GetClassInterfaces(DTypeDesc caller, object classNameOrObject)
		{
			return GetClassInterfaces(caller, classNameOrObject, true);
		}
	}
}
