/*

 Copyright (c) 2006 Tomas Petricek

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//#define DEBUG_DUCK_EMIT

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using PHP.Core.Emit;
using System.Collections;

namespace PHP.Core
{
	#region Attributes

	/// <summary>
	/// This attribute marks interface that is used with duck typing.
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
	public sealed class DuckTypeAttribute : Attribute
	{
		public bool GlobalFunctions { get; set; }
	}


	/// <summary>
	/// Use this attribute when you want to use different name of property or method.
	/// </summary>
	/// <example>
	/// The following example demonstrates how to rename function from "php_name" to "PhpName":
	/// <code>
	/// [DuckType] 
	/// interface IDemo {
	///   [DuckName("php_name")]
	///   void PhpName();
	/// }
	/// </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class DuckNameAttribute : Attribute
	{
		#region Members

		private string _name;
		public string Name { get { return _name; } set { _name = value; } }
	
		public DuckNameAttribute(string name)
		{
			_name = name;
		}

		#endregion
	}

	#endregion


	/// <summary>
	/// Use this type as a interface of all values wrapped by duck types.
	/// </summary>
	public interface IDuckType
	{
		/// <summary>
		/// Gets original object this IDuckValue was created from. This allows passing of returned duck types as
		/// arguments for another duck type methods.
		/// </summary>
		object OriginalObject { get; }
	}

	/// <summary>
	/// Common base implementation of IDuckType interface.
	/// </summary>
	public abstract class DuckTypeBase : IDuckType
	{
		public object OriginalObject { get { return original; } }
		object original;

		protected DuckTypeBase(object original)
		{
			this.original = original;
		}
	}

	/// <summary>
	/// Class that contains duck typing implementation
	/// </summary>
	public class DuckTyping
	{
		#region Static

		internal const string RealAssemblyName = "DuckTypingAssembly";
		internal const string RealModuleName = "DuckTypingModule";

		private static object initializationMutex = new object();
		private static volatile bool initialized = false;
		private static DuckTyping instance;

		/// <summary>
		/// Singleton - returns instance of the object
		/// </summary>
		public static DuckTyping Instance
		{
			get
			{
				if (!initialized)
				{
					lock (initializationMutex)
					{
						if (!initialized)
						{
							instance = new DuckTyping();
							initialized = true;
						}
					}
				}
				return instance;
			}
		}

		#endregion

		#region Types

		/// <summary>
		/// Represents key for the type cache
		/// </summary>
		struct TypeTuple
		{
			#region Members

			private Type _interfaceType;
			public Type InterfaceType { get { return _interfaceType;} set { _interfaceType = value;} }

			private Type _objectType;
			public Type ObjectType { get { return _objectType;} set { _objectType = value;} }
	
			public TypeTuple(Type interfaceType, Type objectType)
			{
				_interfaceType = interfaceType; _objectType = objectType;
			}	

			public override int  GetHashCode()
			{
				return InterfaceType.GetHashCode() + ObjectType.GetHashCode();
			}

			#endregion
		}

		#endregion

		#region Locals

		ModuleBuilder module_builder;
		AssemblyBuilder assembly_builder;

		Dictionary<TypeTuple, Type> typeCache;
		Dictionary<Type, Type> globalCache;
		int type_counter = 0;
		readonly object moduleLock = new object();
		
		#endregion

		#region Construction

		/// <summary>
		/// Singleton - initialize instance
		/// </summary>
		private DuckTyping()
		{
			AssemblyName assembly_name = new AssemblyName(RealAssemblyName);
			assembly_builder = AppDomain.CurrentDomain.DefineDynamicAssembly
				(assembly_name, AssemblyBuilderAccess.Run);

			module_builder = assembly_builder.DefineDynamicModule(RealModuleName);

			typeCache = new Dictionary<TypeTuple, Type>();
			globalCache = new Dictionary<Type, Type>();
		}


		#endregion

		#region Public


		/// <summary>
		/// Implements duck typed wrapper using interface <typeparamref name="T"/> 
		/// for global functions in the currently loaded PHP source and returns wrapped object.
		/// </summary>
		/// <typeparam name="T">Interface that must be marked using <seealso cref="DuckTypeAttribute"/> attribute
		/// and the attribute must be GlobalFunctions = true.</typeparam>
		/// <returns>Wrapped object</returns>
		public T ImplementGlobalDuckType<T>()
		{
			if (!typeof(T).IsInterface)
				throw new ArgumentException("Type parameter for NewObject must be an interface!");
			object[] attrs = typeof(T).GetCustomAttributes(typeof(DuckTypeAttribute), false);
			if (attrs.Length == 0)
				throw new ArgumentException("Type parameter should have [DuckType] attribute!");
			if (!((DuckTypeAttribute)attrs[0]).GlobalFunctions)
				throw new ArgumentException("Type parameter should have [DuckType(GlobalFunctions=true)] attribute!");

			Type type;

			// cache lookup
			if (!globalCache.TryGetValue(typeof(T), out type))
			{
				string typeName;
				EmitAll(typeof(T), out type, out typeName, true);
				globalCache.Add(typeof(T), type);

#if DEBUG_DUCK_EMIT
			AssemblyName assembly_name = new AssemblyName(RealAssemblyName);
			AssemblyBuilder ab = assembly_builder;
			ModuleBuilder mb = module_builder;

			assembly_builder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				assembly_name, AssemblyBuilderAccess.RunAndSave, "C:\\Temp\\", null, null, null, null, true);
			module_builder = assembly_builder.DefineDynamicModule(RealModuleName, String.Format("test_{0}.dll", type_counter-1), true);

			Type _type; string _string;
			EmitAll(typeof(T), out _type, out _string);

			assembly_builder.Save(String.Format("test_{0}.dll", type_counter-1));
			assembly_builder = ab;
			module_builder = mb;

#endif
			}
            return (T)type.GetConstructor(Type.EmptyTypes).Invoke(ArrayUtils.EmptyObjects);
		}


		/// <summary>
		/// Implements duck typed wrapper using interface <typeparamref name="T"/> 
		/// for the object <paramref name="o"/> and returns wrapped object.
		/// </summary>
		/// <typeparam name="T">Interface that must be marked using <seealso cref="DuckTypeAttribute"/> attribute</typeparam>
		/// <param name="o">Object to be wrapped</param>
		/// <returns>Wrapped object</returns>
		public T ImplementDuckType<T>(object o)
		{
			if (!typeof(T).IsInterface)
				throw new ArgumentException("Type parameter for NewObject must be an interface!");
			object[] attrs = typeof(T).GetCustomAttributes(typeof(DuckTypeAttribute), false);
			if (attrs.Length == 0)
				throw new ArgumentException("Type parameter should have [DuckType] attribute!");

			TypeTuple cacheKey = new TypeTuple(typeof(T), o.GetType());
			Type type;

			// cache lookup
			if (!typeCache.TryGetValue(cacheKey, out type))
			{
				string typeName;
				EmitAll(typeof(T), out type, out typeName, false);
				typeCache.Add(cacheKey, type);

#if DEBUG_DUCK_EMIT
			AssemblyName assembly_name = new AssemblyName(RealAssemblyName);
			AssemblyBuilder ab = assembly_builder;
			ModuleBuilder mb = module_builder;

			assembly_builder = AppDomain.CurrentDomain.DefineDynamicAssembly(
				assembly_name, AssemblyBuilderAccess.RunAndSave, "C:\\Temp\\", null, null, null, null, true);
			module_builder = assembly_builder.DefineDynamicModule(RealModuleName, String.Format("test_{0}.dll", type_counter-1), true);

			Type _type; string _string;
			EmitAll(typeof(T), out _type, out _string);

			assembly_builder.Save(String.Format("test_{0}.dll", type_counter-1));
			assembly_builder = ab;
			module_builder = mb;

#endif
			}
			return (T)type.GetConstructor(Types.Object).Invoke(new object[] { o });
		}

		#endregion

		#region Private 

		/// <summary>
		/// Emit duck type implementation
		/// </summary>
		/// <param name="origType">Interface type</param>
		/// <param name="type">Emitted interface implementation</param>
		/// <param name="typeName">Generated type name</param>
        /// <param name="global">Is global.</param>
		private void EmitAll(Type origType, out Type type, out string typeName, bool global)
		{
			lock (moduleLock)
			{
				typeName = String.Format("<{1}#{0}>", type_counter++, origType.Name);

				Type[] interfaces;

				if (!global)
				{
					interfaces = new Type[] { origType, typeof(IDuckType) };
				}
				else
				{
					interfaces = new Type[] { origType };
				}

				TypeBuilder tb = module_builder.DefineType(typeName, TypeAttributes.Public |
					TypeAttributes.Sealed | TypeAttributes.Class, null, interfaces);

				FieldInfo fld = null;
				if (!global)
				{
					// internal constructor and field to store object
					fld = CreateField(tb);
					CreateConstructor(tb, fld);
					ImplementCommonDuckTypeInterface(tb, fld);
				}
				else
				{
					// only empty constructor for 'GlobalFunctions' object
					CreateEmptyConstructor(tb);
				}

				// methods
				foreach (MethodInfo method in origType.GetMethods())
				{
					if (method.IsSpecialName) continue;
					ImplementMethod(tb, method, fld, global);
				}

				// properties
				foreach (PropertyInfo prop in origType.GetProperties())
				{
					if (global)
						throw new ArgumentException("DuckType interfaces with GlobalFunctions=true can not support properties!");
					ImplementProperty(tb, prop, fld);
				}
				type = tb.CreateType();
			}
		}


		/// <summary> Creates constructor </summary>
		/// <remarks><code>
		/// class A : IDuck {
		///   public A(object o) { _obj = o; }
		/// }
		/// </code></remarks>
		private void CreateConstructor(TypeBuilder tb, FieldInfo fld)
		{
			ConstructorBuilder c = tb.DefineConstructor(MethodAttributes.Public, 
				CallingConventions.Standard, Types.Object);
			ILGenerator il = c.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, Types.Object[0].GetConstructor(Type.EmptyTypes));
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, fld);
			il.Emit(OpCodes.Ret);
		}

		private void ImplementCommonDuckTypeInterface(TypeBuilder tb, FieldInfo fld)
		{
			PropertyBuilder prop = tb.DefineProperty("OriginalObject", PropertyAttributes.HasDefault, typeof(Object), null);
			MethodBuilder method = tb.DefineMethod("get_OriginalObject", MethodAttributes.Private | MethodAttributes.HideBySig |
				MethodAttributes.SpecialName | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.Final, typeof(Object), Type.EmptyTypes);
			ILGenerator il = method.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldfld, fld);
			il.Emit(OpCodes.Ret);

			prop.SetGetMethod(method);
			tb.DefineMethodOverride(method, typeof(IDuckType).GetProperty("OriginalObject").GetGetMethod());
		}


		/// <summary> Creates empty constructor </summary>
		/// <remarks><code>
		/// class A : IDuck {
		///   public A() { }
		/// }
		/// </code></remarks>
		private void CreateEmptyConstructor(TypeBuilder tb)
		{
			ConstructorBuilder c = tb.DefineConstructor(MethodAttributes.Public,
				CallingConventions.Standard, Type.EmptyTypes);
			ILGenerator il = c.GetILGenerator();
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Call, Types.Object[0].GetConstructor(Type.EmptyTypes));
			il.Emit(OpCodes.Ret);
		}


		/// <summary> Creates field to store object </summary>
		/// <remarks><code>
		/// class A : IDuck {
		///   object _obj; 
		/// }
		/// </code></remarks>
		private FieldInfo CreateField(TypeBuilder tb)
		{
			return tb.DefineField("_obj", typeof(object), FieldAttributes.Private);
		}


		/// <summary> Implements property </summary>
		/// <remarks><code>
		/// class A : IDuck {
		///   /*type*/ Prop { 
		///     get {
		/// 			Operators.GetProperty(this._obj, "Foo", null, false);
		///				return /* .. type conversion .. */
		///     }
		///     set {
		///				Operators.SetProperty(
		///					new PhpReference(PhpVariable.Copy(ClrObject.WrapDynamic(argument#i), CopyReason.PassedByCopy)),
		///					ref this._obj, "Foo", null, ScriptContext.Current);
		///			}
		///   } 
		/// }
		/// </code></remarks>
		private void ImplementProperty(TypeBuilder tb, PropertyInfo prop, FieldInfo fld)
		{
			if (prop.GetIndexParameters().Length > 0)
				throw new NotImplementedException("Indexers are not supported!");

			string propName = prop.Name;
			object[] attrs = prop.GetCustomAttributes(typeof(DuckNameAttribute), false);
			if (attrs.Length > 0) propName = ((DuckNameAttribute)attrs[0]).Name;

			// define method
			PropertyBuilder pb = tb.DefineProperty(prop.Name, PropertyAttributes.HasDefault, prop.PropertyType, null);

			if (prop.CanRead)
			{
				MethodBuilder getter = tb.DefineMethod("get_"+prop.Name, MethodAttributes.Private | MethodAttributes.SpecialName |
					MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
					prop.PropertyType, Type.EmptyTypes);
				ILEmitter il = new ILEmitter(getter);

				// emit getter
				// Operators.GetProperty(this.obj, "Foo", null, false);
				il.Ldarg(0);
				il.Load(fld);
				il.LoadLiteral(propName);
				il.LoadLiteral(null);
				il.LoadLiteral(false);
				il.Emit(OpCodes.Call, Methods.Operators.GetProperty);

				EmitReturn(il, prop.PropertyType, false);
				pb.SetGetMethod(getter);
				tb.DefineMethodOverride(getter, prop.GetGetMethod());
			}

			if (prop.CanWrite)
			{
				MethodBuilder setter = tb.DefineMethod("set_" + prop.Name, MethodAttributes.Private | MethodAttributes.SpecialName |
					MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final,
					typeof(void), new Type[] { prop.PropertyType });
				ILEmitter il = new ILEmitter(setter);

				// emit setter
				// new PhpReference(PhpVariable.Copy(ClrObject.WrapDynamic(argument#i), CopyReason.PassedByCopy))
				il.Ldarg(1);
				if (prop.PropertyType.IsValueType)
					il.Emit(OpCodes.Box, prop.PropertyType);

				il.Emit(OpCodes.Call, Methods.ClrObject_WrapDynamic);
				il.LdcI4((int)CopyReason.PassedByCopy);
				il.Emit(OpCodes.Call, Methods.PhpVariable.Copy);
				il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);

				// Operators.SetProperty( ... , ref this._obj, "Foo", null, ScriptContext.Current);
				il.Ldarg(0);
				il.LoadAddress(fld);
				il.LoadLiteral(propName);
				il.LoadLiteral(null);
				il.EmitCall(OpCodes.Call, Methods.ScriptContext.GetCurrentContext, Type.EmptyTypes);
				il.EmitCall(OpCodes.Call, Methods.Operators.SetProperty, Type.EmptyTypes);
				il.Emit(OpCodes.Ret);

				pb.SetSetMethod(setter);
				tb.DefineMethodOverride(setter, prop.GetSetMethod());
			}
		}


		/// <summary> Implements method </summary>
		/// <remarks><code>
		/// class A : IDuck {
		///   /*type*/ Func(/*arguments*/) {
		///     sc = ScriptContext.Current;
		///			// temporary array is created only when arguments.Length > 8 (otherwise AddFrame overload exists)
		///			object[] tmp = new object[arguments.Length];  
		///			tmp[#i] = new PhpReference(PhpVariable.Copy(ClrObject.WrapDynamic(argument#i), CopyReason.PassedByCopy));
		///			sc.Stack.AddFrame(tmp);
		///			return /* .. type conversion .. */
		///   }
		/// }
		/// </code></remarks>
		private void ImplementMethod(TypeBuilder tb, MethodInfo method, FieldInfo fld, bool globalFuncs)
		{
			// get parameters (i want C# 3.0 NOW!!)
			ParameterInfo[] pinfo = method.GetParameters();
			Type[] ptypes = new Type[pinfo.Length];
			for(int i = 0; i < pinfo.Length; i++) ptypes[i] = pinfo[i].ParameterType;
			int argCount = pinfo.Length;

			string methName = method.Name;
			object[] attrs = method.GetCustomAttributes(typeof(DuckNameAttribute), false);
			if (attrs.Length > 0) methName = ((DuckNameAttribute)attrs[0]).Name;

			// define method
			MethodBuilder mb = tb.DefineMethod(method.Name, MethodAttributes.Private | MethodAttributes.HideBySig | 
				MethodAttributes.NewSlot | MethodAttributes.Virtual | MethodAttributes.Final, 
				method.ReturnType, ptypes);
			ILEmitter il = new ILEmitter(mb);

			// Wrap parameters
			// sc = ScriptContext.Current
			LocalBuilder sc = il.DeclareLocal(typeof(ScriptContext));
			il.Emit(OpCodes.Call, Methods.ScriptContext.GetCurrentContext);
			il.Stloc(sc);

			LocalBuilder ar = null;
			if (argCount > 8)
			{
				// tmp = new object[pinfo.Length];
				ar = il.DeclareLocal(typeof(object[]));
				il.Emit(OpCodes.Ldc_I4, pinfo.Length);
				il.Emit(OpCodes.Newarr, typeof(object));
				il.Stloc(ar);
			}

			// sc.Stack.AddFrame(...);
			il.Ldloc(sc);
			il.Load(Fields.ScriptContext_Stack);

			for (int i = 0; i < argCount; i++)
			{
				if (argCount > 8)
				{
					// tmp[i]
					il.Emit(OpCodes.Ldloc, ar);
					il.Emit(OpCodes.Ldc_I4, i);
				}

				// if (param#i is IDuckType)
				//   param#i.OriginalObject
				// else
				//   new PhpReference(PhpVariable.Copy(ClrObject.WrapDynamic(param#i), CopyReason.PassedByCopy));

				Label lblDuckType = il.DefineLabel();
				Label lblEnd = il.DefineLabel();

				if (!ptypes[i].IsValueType)
				{
					il.Ldarg(i + 1);
					il.Emit(OpCodes.Isinst, typeof(IDuckType));
					il.Emit(OpCodes.Brtrue, lblDuckType);
				}

				il.Ldarg(i + 1);

				if (ptypes[i].IsValueType)
					il.Emit(OpCodes.Box, ptypes[i]);

				il.Emit(OpCodes.Call, Methods.ClrObject_WrapDynamic);
				il.LdcI4((int)CopyReason.PassedByCopy);
				il.Emit(OpCodes.Call, Methods.PhpVariable.Copy);
				il.Emit(OpCodes.Newobj, Constructors.PhpReference_Object);

				if (!ptypes[i].IsValueType)
				{
					il.Emit(OpCodes.Br, lblEnd);

					il.MarkLabel(lblDuckType);

					il.Ldarg(i + 1);
					il.Emit(OpCodes.Call, typeof(IDuckType).GetProperty("OriginalObject").GetGetMethod());

					il.MarkLabel(lblEnd);
				}

				if (argCount > 8) il.Emit(OpCodes.Stelem_Ref);
			}
			if (argCount > 8)
				il.Emit(OpCodes.Ldloc, ar);
			il.Emit(OpCodes.Call, Methods.PhpStack.AddFrame.Overload(argCount));

			if (globalFuncs)
			{
				// localVariables = null, namingContext = null
				// ScriptContex.Call(null, null, "Foo", null, ScriptContext.Current).value;
				il.LoadLiteral(null);
				il.LoadLiteral(null);
				il.LoadLiteral(methName);
                il.LoadLiteral(null);
                il.Emit(OpCodes.Ldsflda, il.TypeBuilder.DefineField("<callHint>'lambda", typeof(PHP.Core.Reflection.DRoutineDesc), FieldAttributes.Static | FieldAttributes.Private));
				il.Ldloc(sc);
				il.Emit(OpCodes.Call, Methods.ScriptContext.Call);
			}
			else
			{
				// Operators.InvokeMethod(this.obj, "Foo", null, ScriptContext.Current).value;
				il.Ldarg(0);
				il.Load(fld);
				il.LoadLiteral(methName);
				il.LoadLiteral(null);
				il.Ldloc(sc);
				il.Emit(OpCodes.Call, Methods.Operators.InvokeMethodStr);
			}

			EmitReturn(il, method.ReturnType, true);
			tb.DefineMethodOverride(mb, method);
		}


		/// <summary> Emit PHP to CLR conversion </summary>
		/// <remarks>If the return type is interface marked using <seealso cref="DuckTypeAttribute"/>
		/// it is wrapped again.
		/// <code>
		/// // type is IDuckEnumerable&lt;T&gt;
		/// return new DuckEnumerableWrapper&lt;T&gt;(obj.GetForeachEnumerator(false, false, null))
		/// 
		/// // type is IDuckKeyedEnumerable&lt;T&gt;
		/// return new DuckKeyedEnumerableWrapper&lt;T&gt;(obj.GetForeachEnumerator(true, false, null))
		/// 
		/// // type is marked using [DuckType]
		/// return DuckTyping.Instance.ImplementDuckType&lt;T&gt;(obj);
		/// 
		/// // otherwise uses standard ConvertToClr conversion method
		/// </code>
		/// </remarks>
		private static void EmitReturn(ILEmitter il, Type returnedType, bool isPhpRef)
		{
			Type[] gargs = returnedType.GetGenericArguments();
			object[] attrs = returnedType.GetCustomAttributes(typeof(DuckTypeAttribute), false);

			bool isDuckEnumerable = (gargs.Length == 1 && returnedType.Equals(typeof(IDuckEnumerable<>).MakeGenericType(gargs)));
			bool isDuckKeyedEnumerable = (gargs.Length == 2 && returnedType.Equals(typeof(IDuckKeyedEnumerable<,>).MakeGenericType(gargs)));
			bool isDuckType = attrs != null && attrs.Length > 0;

			if (returnedType.Equals(typeof(void)))
			{
				il.Emit(OpCodes.Pop);
				il.Emit(OpCodes.Ret);
			}
			else if (isDuckType || isDuckEnumerable || isDuckKeyedEnumerable)
			{
				LocalBuilder tmp = il.DeclareLocal(typeof(object));

				//store the value local var (after unwrapping it from the reference)
				if (isPhpRef) il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
				il.Stloc(tmp);

				Label lblTestMinusOne = il.DefineLabel();
				Label lblWrap = il.DefineLabel();
				Label lblInvalidInt = il.DefineLabel();

				// test whether the value is null
				il.Ldloc(tmp);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Brfalse, lblTestMinusOne);
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Ret);

				il.MarkLabel(lblTestMinusOne);

				// test whether value is -1
				il.Ldloc(tmp);
				il.Emit(OpCodes.Isinst, typeof(int));
				il.Emit(OpCodes.Brfalse, lblWrap); // value is not int, so we can wrap the value
				il.Ldloc(tmp);
				il.Emit(OpCodes.Unbox_Any, typeof(int));
				il.Emit(OpCodes.Ldc_I4, -1);
				il.Emit(OpCodes.Ceq);
				il.Emit(OpCodes.Brfalse, lblWrap); // value is int but not -1
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Ret);

				il.MarkLabel(lblWrap);

				// specific duck type wrapping
				if (isDuckEnumerable || isDuckKeyedEnumerable)
				{
					il.Ldloc(tmp);
					il.Emit(OpCodes.Dup);
					// Standard: new DuckEnumerableWrapper<T>(obj.GetForeachEnumerator(false, false, null))
					// Keyed:    new DuckKeyedEnumerableWrapper<T>(obj.GetForeachEnumerator(false, false, null))
					il.LoadLiteral(gargs.Length == 2); // keyed?
					il.LoadLiteral(false);
					il.LoadLiteral(null);
					il.Emit(OpCodes.Callvirt, Methods.IPhpEnumerable_GetForeachEnumerator);
					if (isDuckEnumerable)
						il.Emit(OpCodes.Newobj, typeof(DuckEnumerableWrapper<>).
							MakeGenericType(gargs).GetConstructors()[0]);
					else
						il.Emit(OpCodes.Newobj, typeof(DuckKeyedEnumerableWrapper<,>).
							MakeGenericType(gargs).GetConstructors()[0]);
				}
				else
				{
					il.Emit(OpCodes.Call, typeof(DuckTyping).GetProperty("Instance", BindingFlags.Public | BindingFlags.Static).GetGetMethod());
					il.Ldloc(tmp);
					il.Emit(OpCodes.Call, typeof(DuckTyping).GetMethod("ImplementDuckType", BindingFlags.Public | BindingFlags.Instance).MakeGenericMethod(returnedType));					
				}

				il.Emit(OpCodes.Ret);
			}
			else
			{
                if (returnedType == typeof(object))
                {
                    Label lbl = il.DefineLabel();

                    if (isPhpRef)
                    {
                        il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
                    }
                    
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Isinst, typeof(PhpBytes));
                    il.Emit(OpCodes.Brfalse, lbl);
                    il.EmitCall(OpCodes.Call, typeof(IPhpConvertible).GetMethod("ToString", Type.EmptyTypes), Type.EmptyTypes);
                    il.Emit(OpCodes.Ret);
                    il.MarkLabel(lbl);
                    ClrOverloadBuilder.EmitConvertToClr(il, PhpTypeCode.Object, returnedType);
                    il.Emit(OpCodes.Ret);
                }
                else
                {
                    ClrOverloadBuilder.EmitConvertToClr(il, isPhpRef ? PhpTypeCode.PhpReference : PhpTypeCode.Object, returnedType);
                    il.Emit(OpCodes.Ret);
                }
			}			
		}

		#endregion
	}
}
