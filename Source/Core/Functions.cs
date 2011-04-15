/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using PHP.Core.Reflection;

namespace PHP.Core
{
	#region Delegates

	/// <summary>
	/// A delegate used to call functions and methods indirectly.
	/// </summary>
	[Emitted]
	public delegate object RoutineDelegate(object instance, PhpStack/*!*/ stack);

	/// <summary>
    /// The delegate to the Script's Main helper method.
    /// </summary>
    /// <param name="context">A script context.</param>
    /// <param name="localVariables">A table of defined variables.</param>
    /// <param name="self">PHP object context.</param>
    /// <param name="includer">PHP class context.</param>
    /// <param name="isMain">Whether the target script is the main script.</param>
    /// <returns>The return value of the Main method.</returns>
    [Emitted]
	public delegate object MainRoutineDelegate(ScriptContext/*!*/ context, Dictionary<string, object> localVariables,
		DObject self, DTypeDesc includer, bool isMain);

	[Emitted]
	public delegate object GetterDelegate(object instance);

	[Emitted]
	public delegate void SetterDelegate(object instance, object value);

	#endregion

	#region Default Argument Substitute

	/// <summary>
	/// Substitutes for default arguments and default type arguments.
	/// </summary>
	public static class Arg
	{
		/// <summary>
		/// Default type argument.
		/// </summary>
		public static readonly DTypeDesc/*!*/ DefaultType = DTypeDesc.Create(typeof(Arg));

		/// <summary>
		/// Singleton substituting default argument.
		/// </summary>
		public static readonly PhpReference/*!*/ Default = new PhpReference();
	}

	#endregion

	#region PhpFunctionUtils

	/// <summary>
	/// Provides means to work with PHP functions and methods.
	/// </summary>
	public sealed class PhpFunctionUtils
	{
		/// <summary>
		/// SpecialName should be here, but unfortunately CLR blocks it :(
		/// </summary>
		internal const MethodAttributes DynamicStubAttributes = MethodAttributes.Public | MethodAttributes.Static;

		/// <summary>
		/// Assumed maximal number of overloads in all libraries.
		/// </summary>
		internal const int AssumedMaxOverloadCount = 10;

		/// <summary>
		/// Checks whether a mandatory parameter is passed by alias.
		/// </summary>
		/// <param name="paramType">The parameter type.</param>
		/// <returns>
		/// Returns whether the parameter is passed either by object reference (ref/out) or is <see cref="PhpReference"/>.
		/// </returns>
		internal static bool IsParameterByAlias(Type/*!*/ paramType)
		{
			return paramType.IsByRef || paramType == typeof(PhpReference);
		}

		/// <summary>
		/// Reflects a CLR method representing user routine and extracts information about the signature.
		/// </summary>
		/// <param name="method">GetUserEntryPoint info.</param>
		/// <param name="parameters">Parameter infos.</param>
		/// <returns>Count of mandatory parameters.</returns>
		internal static RoutineSignature GetUserRoutineSignature(MethodInfo/*!*/ method, ParameterInfo[]/*!*/ parameters)
		{
			// TODO:
			return null;

			//// static methods has one hidden argument - the script context:
			//int hidden_count = method.IsStatic ? 1 : 0;
			//int param_count = parameters.Length - hidden_count;

			//Debug.Assert(param_count >= 0);
			//Debug.Assert(hidden_count==0 || parameters[0].ParameterType == typeof(ScriptContext));

			//int last_mandatory_param_index = -1;
			//BitArray alias_mask = new BitArray(param_count, false);
			//GenericQualifiedName[] type_hints = new GenericQualifiedName[param_count];

			//int j = 0;
			//for (int i = hidden_count; i < parameters.Length; i++, j++)
			//{
			//  // mandatory:
			//  if (!parameters[i].IsOptional) 
			//    last_mandatory_param_index = j;

			//  // alias:
			//  if (parameters[i].ParameterType == typeof(PhpReference))
			//    alias_mask[j] = true;

			//  // type hint:
			//  object[] attrs = parameters[i].GetCustomAttributes(typeof(TypeHintAttribute), false);
			//  if (attrs.Length > 0) // TODO
			//    type_hints[j] = new GenericQualifiedName(new QualifiedName(new Name(((TypeHintAttribute)attrs[0]).TypeName)), null);  
			//}

			//return new RoutineSignature(
			//  method.ReturnType == Emit.Types.PhpReference[0],
			//  alias_mask,
			//  type_hints,
			//  last_mandatory_param_index + 1);
		}

		/// <summary>
		/// Checks whether a specified library function implies args-aware property of the calling function.
		/// </summary>
		/// <param name="name">The name of the function.</param>
		/// <returns>Whether call to function <paramref name="name"/> implies args-awareness of the caller.</returns>
		/// <exception cref="ArgumentNullException">If <paramref name="name"/> is a <B>null</B> reference.</exception>
		internal static bool ImpliesArgsAwareness(Name name)
		{
			// TODO
			return false;
			// library table lookup (std: func_get_arg, func_get_args, func_num_args):
			// return (Functions.GetFunctionImplOptions(name) & FunctionImplOptions.NeedsFunctionArguments)!=0;
		}

		/// <summary>
		/// Checks whether a specified library function needs defined variables to be passed as its first argument.
		/// </summary>
		/// <param name="name">The name of the function.</param>
		/// <returns>Whether call to function <paramref name="name"/> implies args-awareness of the caller.</returns>
		internal static bool NeedsDefinedVariables(Name name)
		{
			// TODO
			return false;
			// library table lookup (std: extract, compact, get_defined_vars, import_request_variables):
			//  return (Functions.GetFunctionImplOptions(name) & FunctionImplOptions.NeedsVariables) != 0;
		}

		/// <summary>
		/// Checks whether a specified name is valid constant name.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <seealso cref="PhpVariable.IsValidName"/>
		public static bool IsValidName(string name)
		{
			// TODO: qualified names are valid as well
			return PhpVariable.IsValidName(name);
		}

        /// <summary>
        /// Checks whether function name is conditionally defined.
        /// </summary>
        /// <param name="realName">Internal name of the function.</param>
        /// <returns>True if the function name represents conditionally defined function, otherwise false.</returns>
        public static bool IsRealConditionalDefinition(string/*!*/ realName)
        {
            return realName.IndexOf('#') > 0;
        }

		/// <summary>
		/// Determines whether a specified method is an arg-less stub.
		/// </summary>
		/// <param name="method">The method.</param>
		/// <param name="parameters">GetUserEntryPoint parameters (optimization). Can be <B>null</B> reference.</param>
		/// <returns>Whether a specified method is an arg-less stub.</returns>
		internal static bool IsArglessStub(MethodInfo/*!*/ method, ParameterInfo[] parameters)
		{
			Debug.Assert(method != null);

			if (method.ReturnType == Emit.Types.Object[0])
			{
				if (parameters == null) parameters = method.GetParameters();
				return (parameters.Length == 2 &&
					parameters[0].ParameterType == Emit.Types.Object[0] &&
					parameters[1].ParameterType == Emit.Types.PhpStack[0]);
			}
			return false;
		}

		/// <summary>
		/// Determines whether a specified method is an arg-full overload.
		/// </summary>
		/// <param name="method">The method.</param>
		/// <param name="parameters">GetUserEntryPoint parameters (optimization). Can be <B>null</B> reference.</param>
		/// <returns>Whether a specified method is an arg-full overload.</returns>
		internal static bool IsArgfullOverload(MethodInfo/*!*/ method, ParameterInfo[] parameters)
		{
			Debug.Assert(method != null);

			Type type = method.ReturnType;
			if (type != Emit.Types.Object[0] && type != Emit.Types.PhpReference[0]) return false;

			// argfulls should have either EditorBrowsable or ImplementsMethod
			// (on Silverlight the 'EditorBrowsable' is not supported)
#if !SILVERLIGHT
			if (!method.IsDefined(Emit.Types.EditorBrowsableAttribute, false) &&
				!method.IsDefined(Emit.Types.ImplementsMethodAttribute, false)) return false;
#endif

			// check parameters
			if (parameters == null) parameters = method.GetParameters();
			if (parameters.Length == 0 || parameters[0].ParameterType != Emit.Types.ScriptContext[0]) return false;

			bool past_gen_params = false;
			for (int i = 1; i < parameters.Length; i++)
			{
				type = parameters[i].ParameterType;
				if (type != Emit.Types.Object[0] && type != Emit.Types.PhpReference[0])
				{
					if (past_gen_params || type != Emit.Types.DTypeDesc[0]) return false;
				}
				else past_gen_params = true;
			}

			return true;
		}

		#region Routines Enumeration

		// GENERICS: iterator (filter)

		internal delegate void RoutineEnumCallback(MethodInfo/*!*/ argless, MethodInfo/*!*/ argfull,
		  ParameterInfo[]/*!*/ parameters);

		/// <summary>
		/// Enumerates PHP routines contained in the specified method list. Filters out methods that
		/// didn't implement PHP routines (they are not argless or argfull overloads).
		/// </summary>
		internal static void EnumerateRoutines(MethodInfo[]/*!*/ methods, RoutineEnumCallback/*!*/ callback)
		{
			// TODO: can be done in a better way

			Dictionary<string, MethodInfo> arg_less_table = new Dictionary<string, MethodInfo>(
			  methods.Length / 2, // at most one half of all methods are supposed to be user routines
			  StringComparer.OrdinalIgnoreCase);

			// adds arg-less overloads to the hashtable:
			foreach (MethodInfo method in methods)
			{
				if (PhpFunctionUtils.IsArglessStub(method, null))
					arg_less_table[method.Name] = method;
			}

			// searches for matching argfulls:
			foreach (MethodInfo method in methods)
			{
				ParameterInfo[] parameters = method.GetParameters();

				// skips arg-less overloads:
				if (PhpFunctionUtils.IsArglessStub(method, parameters))
					continue;

				// skips methods that hasn't a matching arg-less overload:
				MethodInfo argless;
				if (!arg_less_table.TryGetValue(method.Name, out argless))
					continue;

				// yields the pair:
				callback(argless, method, parameters);
			}
		}

		#endregion

        ///// <summary>
        ///// Invokes a user method (either argless or argfull).
        ///// </summary>
        ///// <param name="method">A method info of the stub.</param>
        ///// <param name="target">An object.</param>
        ///// <param name="args">Arguments.</param>
        ///// <returns>The result of the called method.</returns>
        ///// <exception cref="PhpException">Fatal error.</exception>
        ///// <exception cref="PhpUserException">Uncaught user exception.</exception>
        ///// <exception cref="ScriptDiedException">Script died or exit.</exception>
        ///// <exception cref="TargetInvocationException">An internal error thrown by the target.</exception>
        //internal static object Invoke(MethodInfo method, object target, params object[] args)
        //{
        //    Debug.Assert(method != null && args != null);

        //    try
        //    {
        //        return method.Invoke(target, args);
        //    }
        //    catch (TargetInvocationException e)
        //    {
        //        if (e.InnerException is PhpException ||
        //            e.InnerException is PhpUserException ||
        //            e.InnerException is ScriptDiedException ||
        //            e.InnerException is System.Threading.ThreadAbortException)
        //            throw e.InnerException;

        //        throw;
        //    }
        //}

        #region Constructor invocation

        ///// <summary>
        ///// Creates a new instance of a type by invoking its constructor.
        ///// </summary>
        ///// <param name="type">The type to instantiate.</param>
        ///// <param name="args">Arguments.</param>
        ///// <returns>The result of the called method.</returns>
        ///// <exception cref="PhpException">Fatal error.</exception>
        ///// <exception cref="PhpUserException">Uncaught user exception.</exception>
        ///// <exception cref="ScriptDiedException">Script died or exit.</exception>
        ///// <exception cref="TargetInvocationException">An internal error thrown by the target.</exception>
        //internal static DObject InvokeConstructor(DTypeDesc type, params object[] args)
        //{
        //    Debug.Assert(type != null && args != null);

        //    try
        //    {
        //        return ClrObject.Wrap(Activator.CreateInstance(type.RealType, args));
        //    }
        //    catch (TargetInvocationException e)
        //    {
        //        if (e.InnerException is PhpException ||
        //            e.InnerException is PhpUserException ||
        //            e.InnerException is ScriptDiedException ||
        //            e.InnerException is System.Threading.ThreadAbortException)
        //            throw e.InnerException;

        //        throw;
        //    }
        //}

		/// <summary>
		/// Creates a new instance of a type by invoking its constructor.
		/// </summary>
		/// <param name="type">The type to instantiate.</param>
        /// <param name="context">ScriptContext to be passed to the <c>type</c> constructor.</param>
        /// <param name="newInstance">Bool to be passed to the <c>type</c> constructor.</param>
		/// <returns>New instance of <c>type</c> created using specified constructor.</returns>
		/// <exception cref="PhpException">Fatal error.</exception>
		/// <exception cref="PhpUserException">Uncaught user exception.</exception>
		/// <exception cref="ScriptDiedException">Script died or exit.</exception>
		/// <exception cref="TargetInvocationException">An internal error thrown by the target.</exception>
		internal static DObject InvokeConstructor(DTypeDesc/*!*/type, ScriptContext context, bool newInstance)
		{
			Debug.Assert(type != null);
			
			try
			{
                var newobj = type.RealTypeCtor_ScriptContext_Bool;
                if (newobj == null)
                    lock (type)
                        if ((newobj = type.RealTypeCtor_ScriptContext_Bool) == null)
                        {
                            // emit the type creation:
                            newobj = type.RealTypeCtor_ScriptContext_Bool = (DTypeDesc.Ctor_ScriptContext_Bool)BuildNewObj<DTypeDesc.Ctor_ScriptContext_Bool>(type.RealType, Emit.Types.ScriptContext_Bool);
                        }

				return ClrObject.Wrap(newobj(context, newInstance));
			}
			catch (TargetInvocationException e)
			{
				if (e.InnerException is PhpException ||
					e.InnerException is PhpUserException ||
					e.InnerException is ScriptDiedException ||
					e.InnerException is System.Threading.ThreadAbortException)
					throw e.InnerException;

				throw;
			}
		}

        /// <summary>
        /// Creates a new instance of a type by invoking its constructor.
        /// </summary>
        /// <param name="type">The type to instantiate.</param>
        /// <param name="context">ScriptContext to be passed to the <c>type</c> constructor.</param>
        /// <param name="caller">DTypeDesc to be passed to the <c>type</c> constructor.</param>
        /// <returns>New instance of <c>type</c> created using specified constructor.</returns>
        /// <exception cref="PhpException">Fatal error.</exception>
        /// <exception cref="PhpUserException">Uncaught user exception.</exception>
        /// <exception cref="ScriptDiedException">Script died or exit.</exception>
        /// <exception cref="TargetInvocationException">An internal error thrown by the target.</exception>
        internal static DObject InvokeConstructor(DTypeDesc/*!*/type, ScriptContext context, DTypeDesc caller)
        {
            Debug.Assert(type != null);

            try
            {
                var newobj = type.RealTypeCtor_ScriptContext_DTypeDesc;
                if (newobj == null)
                    lock(type)
                        if ((newobj = type.RealTypeCtor_ScriptContext_DTypeDesc) == null)
                        {
                            // emit the type creation:
                            newobj = type.RealTypeCtor_ScriptContext_DTypeDesc = (DTypeDesc.Ctor_ScriptContext_DTypeDesc)BuildNewObj<DTypeDesc.Ctor_ScriptContext_DTypeDesc>(type.RealType, Emit.Types.ScriptContext_DTypeDesc);
                        }
                
                return ClrObject.Wrap(newobj(context, caller));
            }
            catch (TargetInvocationException e)
            {
                if (e.InnerException is PhpException ||
                    e.InnerException is PhpUserException ||
                    e.InnerException is ScriptDiedException ||
                    e.InnerException is System.Threading.ThreadAbortException)
                    throw e.InnerException;

                throw;
            }
        }

        /// <summary>
        /// Create dynamic method that instantiates given <c>realType</c> using constructor with given <c>types</c>.
        /// If given <c>realType</c> does not define wanted constructor, dynamic method that throws InvalidOperationException is created.
        /// </summary>
        /// <typeparam name="D">The typed delegate the create.</typeparam>
        /// <param name="realType">The type to be instantiated by dynamic method.</param>
        /// <param name="types">Types of parameters of wanted constructor to be called.</param>
        /// <returns>Delegate to dynamic method that creates specified type or throws an exception. The method cannot return null.</returns>
        private static Delegate/*!*/BuildNewObj<D>(Type/*!*/realType, Type[] types) where D : class
        {
            Debug.Assert(realType != null);

            ConstructorInfo ctor_info = realType.GetConstructor(
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                        null, types, null);

            DynamicMethod method = new DynamicMethod(string.Format("<{0}>.ctor", realType.Name), Emit.Types.Object[0], types);
            Emit.ILEmitter il = new PHP.Core.Emit.ILEmitter(method);

            if (ctor_info != null)
            {
                // new T(arg1, arg2, ...);
                for (int i = 0; i < types.Length; ++i)
                    il.Ldarg(i);
                il.Emit(OpCodes.Newobj, ctor_info);
            }
            else
            {
                var invalid_ctor = typeof(InvalidOperationException).GetConstructor(Type.EmptyTypes);
                Debug.Assert(invalid_ctor != null);

                // new InvalidOperationException();
                il.Emit(OpCodes.Newobj, invalid_ctor);
            }

            // return
            il.Emit(OpCodes.Ret);

            //
            return method.CreateDelegate(typeof(D));
        }

        #endregion
    }

	#endregion
}
