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

using PHP.Core;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

/*
  FUTURE VERSION:
   - inline callbacks if evaluable at compile time (create a new callback with given delegate)
   - save return values into variable so that emitted code would be nicer (disassembler would be happier)
   
*/

namespace PHP.Core.Emit
{
	/// <summary>
	/// Builder of overloads-aware library functions indirect calls.
	/// </summary>
	internal class OverloadsBuilder
    {
        #region Nested Class: OverloadTreeNode
        /// <summary>
        /// Node in overload decision tree
        /// </summary>
        private class OverloadTreeNode
        {
            /// <summary>
            /// Object representing overload of a library function. This is null if the node is not a leaf in the tree.
            /// </summary>
            private PhpLibraryFunction.Overload overload;

            /// <summary>
            /// Dictionary of child node pairs.
            /// </summary>
            private Dictionary<Type, OverloadTreeNode> childNodes;

            /// <summary>
            /// Initializes new decision tree using supplied overload array. 
            /// </summary>
            /// <param name="overloads"></param>
            public OverloadTreeNode(PhpLibraryFunction.Overload[] overloads) : this(0, overloads)
            {
            }

            /// <summary>
            /// Initializes new decisin tree using supplied overload array and decision index.
            /// </summary>
            /// <param name="index">Argument index which to start at.</param>
            /// <param name="overloads">Array of overloads.</param>
            private OverloadTreeNode(int index, PhpLibraryFunction.Overload[] overloads)
            {            
                //assumptions
                Debug.Assert(index >= 0);
                Debug.Assert(overloads != null && overloads.Length > 0);
                Debug.Assert(index < overloads[0].ParamCount - 1 || overloads.Length == 1);

                childNodes = new Dictionary<Type,OverloadTreeNode>();

                if (overloads.Length == 1)
                {
                    overload = overloads[0];
                    return;
                }

                while(true)
                {
                    Debug.Assert(index < overloads[0].ParamCount);

                    bool decisionPoint = TestDecisionPoint(index, overloads);

                    if (decisionPoint)
                    {
                        foreach(var branch in DivideOverloads(index, overloads))
                        {
                            childNodes.Add(branch.Key, new OverloadTreeNode(index + 1, SortOverloads(index, branch.Value)));
                        }
                    }
                    else
                    {
                        index++;

                        if (index == overloads[0].ParamCount)
                        {
                            Debug.Fail();
                            overload = overloads[0];
                            childNodes = new Dictionary<Type,OverloadTreeNode>();                            
                            break;
                        }
                    }
                }
            }

            /// <summary>
            /// Tests whether an argument index is a decision point on a set of overloads.
            /// </summary>
            /// <param name="index">Argument index.</param>
            /// <param name="overloads">Array of overloads.</param>
            /// <returns>True if decision is present on the index, otherwise false.</returns>
            private bool TestDecisionPoint(int index, PhpLibraryFunction.Overload[] overloads)
            {
                Type first = null;

                foreach (var overload in overloads)
                {
                    Type paramType = overload.RealParameters[index].ParameterType;                    

                    if (first == null) first = paramType;
                    else if (first != paramType) return true;
                }

                return false;
            }

            /// <summary>
            /// Topological sort of types for their partial order. Sort is in-place. Specialized types come first, generic last.
            /// </summary>
            /// <param name="index">Index of argument which will be used for sorting.</param>
            /// <param name="overloads">Array of overloads.</param>
            /// <returns>Returns the same reference as it gets in "overloads" argument.</returns>
            private PhpLibraryFunction.Overload[] SortOverloads(int index, PhpLibraryFunction.Overload[] overloads)
            {
                for (int i = 0; i >= overloads.Length; i++)
                {
                    int k = i;

                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (overloads[j].RealParameters[index].ParameterType.IsSubclassOf(overloads[k].RealParameters[index].ParameterType))
                        {
                            PhpLibraryFunction.Overload temp = overloads[j];
                            overloads[j] = overloads[k];
                            overloads[k] = temp;
                            k = j;
                        }
                    }
                }

                return overloads;
            }

            /// <summary>
            /// Takes array of overloads and divides them into groups. Takes into account argument index (depth) of this node.
            /// </summary>
            /// <param name="index"></param>
            /// <param name="overloads">Array of overload descriptors.</param>
            /// <returns>Dictionary of </returns>
            private Dictionary<Type, PhpLibraryFunction.Overload[]> DivideOverloads(int index, PhpLibraryFunction.Overload[] overloads)
            {
                var dict = new Dictionary<Type,List<PhpLibraryFunction.Overload>>();

                foreach (var overload in overloads)
                {
                    List<PhpLibraryFunction.Overload> list;
                    Type paramType = overload.RealParameters[index].ParameterType;

                    if (dict.ContainsKey(paramType))
                    {
                        list = dict[paramType];
                    }
                    else
                    {
                        list = new List<PhpLibraryFunction.Overload>();
                        dict.Add(paramType, list);
                    }

                    list.Add(overload);
                }

                var ret = new Dictionary<Type, PhpLibraryFunction.Overload[]>();

                foreach(Type t in dict.Keys)
                {
                    ret.Add(t, dict[t].ToArray());
                }

                return ret;
            }

            private void Emit(ILEmitter il)
            {
            }
        }
        #endregion

        #region Nested Class: OverloadForest
        /// <summary>
        /// Represents collection of overload decision trees, each representing one count of arguments.
        /// </summary>
        private class OverloadForest
        {
            private Dictionary<int, OverloadTreeNode> overloadTrees;

            public OverloadForest(List<PhpLibraryFunction.Overload>/*!!*/ overloads)
            {
                // we divide overloads by argument count
                var dividedOverloads = new Dictionary<int, List<PhpLibraryFunction.Overload>>();

                foreach (var overload in overloads)
                {
                    if (!dividedOverloads.ContainsKey(overload.ParamCount))
                    {
                        dividedOverloads.Add(overload.ParamCount, new List<PhpLibraryFunction.Overload>());
                    }

                    dividedOverloads[overload.ParamCount].Add(overload);
                }

                overloadTrees = new Dictionary<int, OverloadTreeNode>();

                foreach (var division in dividedOverloads)
                {
                    overloadTrees.Add(division.Key, new OverloadTreeNode(division.Value.ToArray()));
                }
            }
        }
        #endregion

        /// <summary>
		/// A delegate used to load a parameter to evaluation stack.
		/// </summary>
		internal delegate object ParameterLoader(ILEmitter/*!*/ il, int/*!*/ index, object aux);

		internal delegate void ParametersLoader(OverloadsBuilder builder, int start, ParameterInfo param, IPlace argCount);

		#region Construction

		/// <summary>
		/// Creates a new instance of <see cref="OverloadsBuilder"/>.
		/// </summary>
		/// <param name="debug">
		/// Whether the emitted code is to be debuggable.
		/// </param>
		/// <param name="stack">
		/// Place where the <see cref="PhpStack"/> instance can be loaded from.
		/// </param>
		/// <param name="loadValueParam">
		/// Delegate called when value parameter is to be loaded on evaluation stack.
		/// The target method should guarantee that a value is loaded on evaluation stack.
		/// </param>
		/// <param name="loadReferenceParam">
		/// Delegate called when PHP reference parameter is to be loaded on evaluation stack.
		/// The target method should guarantee that the object reference of type <see cref="PhpReference"/> 
		/// is loaded on the evaluation stack. This object reference should not be a <B>null</B>.
		/// </param>
		/// <param name="loadOptParams">
		/// Delegate called when an array of optional arguments is to be loaded on evaluation stack.
		/// The target method should load that array on the evaluation stack.
		/// </param>
		public OverloadsBuilder(bool debug, IPlace stack,
			ParameterLoader loadValueParam, ParameterLoader loadReferenceParam, ParametersLoader loadOptParams)
		{
			this.loadValueParam = loadValueParam;
			this.loadReferenceParam = loadReferenceParam;
			this.loadOptParams = loadOptParams;
			this.stack = stack;
			this.debug = debug;
		}

		#endregion

		#region Fields and Properties

		private bool debug;

		public IPlace Stack { get { return stack; } }
		private IPlace stack;

		// parameter loaders:
		private ParameterLoader loadValueParam;
		private ParameterLoader loadReferenceParam;
		private ParametersLoader loadOptParams;

		/// <summary>
		/// An auxiliary object which builder doesn't care about.
		/// </summary>
		public object Aux { get { return aux; } set { aux = value; } }
		private object aux;

		/// <summary>
		/// The IL generator used to emit code.
		/// </summary>
		public ILEmitter IL { get { return il; } set { il = value; } }
		private ILEmitter il;

		/// <summary>
        /// An action used to emit jump onto the end of overload call - after the overload method call.
        /// A value must be put onto the evaluation stack and jump must be performed.
        /// </summary>
        private Action<ILEmitter> overloadCallSkipEmitter = null;

		/// <summary>
		/// The number of arguments that has been pushed on the evaluation stack so far.
		/// </summary>
		private int pushedArgsCount;

		/// <summary>
		/// The name of the function currently emitted.
		/// </summary>
		public Name FunctionName { get { return functionName; } set { functionName = value; } }
		private Name functionName;

		/// <summary>
		/// A list of local variable builders holding values of arguments passed by reference.
		/// </summary>
		private ArrayList refHolders = new ArrayList(3);   // GENERICS: <LocalBuilder>

		/// <summary>
		/// A list of local variable builders where arguments passed by reference are stored.
		/// </summary>
		private ArrayList refReferences = new ArrayList(3);   // GENERICS: <LocalBuilder>

		#endregion

		#region Call Switch Emitter

		/// <summary>
		/// Emits calls to specified overloads and a switch statement which calls appropriate overload 
		/// according to the current value of <see cref="PhpStack.ArgCount"/> field of the current stack. 
		/// </summary>
        /// <param name="thisRef">Reference to self.</param>
        /// <param name="script_context">Current script context.</param>
		/// <param name="rtVariables">
		/// Place where a run-time variables table can be loaded from.
		/// </param>
		/// <param name="namingContext">Naming context load-from place.</param>
        /// <param name="classContext">Class context load.</param>
		/// <param name="overloads">The overload list.</param>
		/// <remarks>
		/// Example: given overloads (2,5,7,9+), i.e. there are four overloads having 2, 5, 7 and 9 PHP parameters,
		/// respectively, and the last overload is marked as vararg,
		/// the method emits the following code:
		/// <code>
		/// switch(ArgCount - 2)                  // 2 = minimum { arg count of overload }
		/// {
		///   case 0: return call #2;             // call to the 2nd overload with appropriate arg. and return value handling
		///   case 1: goto case error;
		///   case 2: goto case error;
		///   case 3: return call #5;
		///   case 4: goto case error;
		///   case 5: return call #7;
		///   case 6: goto case error;
		/// 
		/// #if vararg 
		///   case 7: goto default; 
		///   default: return call #vararg (9 mandatory args,optional args);break;
		/// #elif
		///   case 7: return call #9;
		///   default: goto case error;
		/// #endif
		///
		///   case error: PhpException.InvalidArgumentCount(null, functionName); break;
		/// }
		/// </code>
		/// </remarks>
		public void EmitCallSwitch(IPlace/*!*/ thisRef, IPlace/*!*/script_context, IPlace/*!*/ rtVariables, IPlace/*!*/ namingContext, IPlace/*!*/classContext, List<PhpLibraryFunction.Overload>/*!!*/ overloads)
		{
			Debug.AssertAllNonNull(overloads);

			int last = overloads.Count - 1;
			int min = overloads[0].ParamCount;
			int max = overloads[last].ParamCount;

            var flags = overloads[last].Flags;

            // if function is not supported, just throw the warning:
            if ((flags & PhpLibraryFunction.OverloadFlags.NotSupported) != 0)
            {
                // stack.RemoveFrame();
                if (stack != null)
                {
                    stack.EmitLoad(il);
                    il.Emit(OpCodes.Call, Methods.PhpStack.RemoveFrame);
                }

                // PhpException.FunctionNotSupported( <FullName> );
                il.Emit(OpCodes.Ldstr, FunctionName.Value);
                il.Emit(OpCodes.Call, Methods.PhpException.FunctionNotSupported_String);
                if (debug) il.Emit(OpCodes.Nop);

                // load methods default value
                il.EmitBoxing(OverloadsBuilder.EmitLoadDefault(il, overloads[last].Method));
                return;
            }

            bool is_vararg = (flags & PhpLibraryFunction.OverloadFlags.IsVararg) != 0;

            if ((flags & PhpLibraryFunction.OverloadFlags.NeedsScriptContext) == 0)
                script_context = null;

			if ((flags & PhpLibraryFunction.OverloadFlags.NeedsThisReference) == 0)
				thisRef = null;

			if ((flags & PhpLibraryFunction.OverloadFlags.NeedsVariables) == 0)
				rtVariables = null;

			if ((flags & PhpLibraryFunction.OverloadFlags.NeedsNamingContext) == 0)
				namingContext = null;

            if ((flags & (PhpLibraryFunction.OverloadFlags.NeedsClassContext | PhpLibraryFunction.OverloadFlags.NeedsLateStaticBind)) == 0)
                classContext = null;

			Label end_label = il.DefineLabel();
			Label error_label = il.DefineLabel();
			Label[] cases = new Label[max - min + 1];
			MethodInfo method;

			// fills cases with "goto case error":
			for (int i = 0; i < cases.Length; i++)
				cases[i] = error_label;

			// define labels for valid cases:
			for (int i = 0; i < overloads.Count; i++)
			{
				int count = overloads[i].ParamCount;
				cases[count - min] = il.DefineLabel();
			}

			// LOAD(stack.ArgCount - min);
			stack.EmitLoad(il);
			il.Emit(OpCodes.Ldfld, Fields.PhpStack_ArgCount);
			il.LdcI4(min);
			il.Emit(OpCodes.Sub);

			// SWITCH(tmp)
			il.Emit(OpCodes.Switch, cases);

			// CASE >=N or <0 (underflows);
			// if the last overload is vararg:
			if (is_vararg)
			{
				LocalBuilder opt_arg_count_local = il.DeclareLocal(typeof(int));

				// CASE N: 
				il.MarkLabel(cases[cases.Length - 1]);

				// opt_arg_count = stack.ArgCount - max;
				stack.EmitLoad(il);
				il.Emit(OpCodes.Ldfld, Fields.PhpStack_ArgCount);
				il.LdcI4(max);
				il.Emit(OpCodes.Sub);
				il.Stloc(opt_arg_count_local);

				// IF(tmp<0) GOTO CASE error;
				il.Ldloc(opt_arg_count_local);
				il.Emit(OpCodes.Ldc_I4_0);
				il.Emit(OpCodes.Blt, error_label);

				// emits argument loading, stack frame removal, method call, return value conversion:
				method = overloads[last].Method;
                Type return_type = EmitOverloadCall(method, overloads[last].RealParameters, max, script_context,
                    rtVariables, namingContext, classContext, new Place(opt_arg_count_local), thisRef, false);

				// loads boxed return value:
                if (return_type != Types.Void)
                {
                    //il.LoadBoxed(return_value);
                    if (return_type.IsValueType)
                        il.Emit(OpCodes.Box, return_type);
                }
                else
                    il.Emit(OpCodes.Ldnull);

				// RETURN;
				il.Emit(OpCodes.Ret);  //bug in Reflector: il.Emit(OpCodes.Br,end_label);
			}
			else
			{
				// GOTO CASE error;
				il.Emit(OpCodes.Br, error_label);
			}

			// emits all valid cases which are not vararg:
			int j = 0;
			for (int i = min; i <= max - (is_vararg ? 1 : 0); i++)
			{
				if (overloads[j].ParamCount == i)
				{
					// CASE <i>;
					il.MarkLabel(cases[i - min]);

					// emits argument loading, stack frame removal, method call, return value conversion:
					method = overloads[j].Method;
                    Type return_type = EmitOverloadCall(method, overloads[j].RealParameters, i, script_context, rtVariables, namingContext, classContext, null, thisRef, false);

					// loads boxed return value:
                    if (return_type != Types.Void)
                    {
                        //il.LoadBoxed(return_value);
                        if (return_type.IsValueType)
                            il.Emit(OpCodes.Box, return_type);
                    }
                    else
                        il.Emit(OpCodes.Ldnull);

					// RETURN;
					il.Emit(OpCodes.Ret);  //bug in Reflector: il.Emit(OpCodes.Br,end_label);

					j++;
				}
			}
			Debug.Assert(j + (is_vararg ? 1 : 0) == overloads.Count);

			// ERROR:
			il.MarkLabel(error_label);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Ldstr, this.functionName.ToString());
			il.Emit(OpCodes.Call, Methods.PhpException.InvalidArgumentCount);
			if (debug) il.Emit(OpCodes.Nop);

			// RETURN null:
			il.Emit(OpCodes.Ldnull);
			il.MarkLabel(end_label);
		}

		#endregion

		#region Overload Call Emitter

		/// <summary>
		/// Emits call to specified overload.
		/// </summary>
		/// <param name="method">The overload implementor.</param>
		/// <param name="ps">Formal parameters of the <paramref name="method"/>.</param>
		/// <param name="phpParamCount">The number of PHP arguments of the overload.</param>
        /// <param name="scriptContext">A place where current script context should be loaded from.</param>
		/// <param name="rtVariables">A place where run-time variables table should be loaded from.</param>
		/// <param name="namingContext">A place where the naming can be load from.</param>
        /// <param name="classContext">A place where the class context can be load from.</param>
		/// <param name="optArgs">A place where the number of optional arguments should be loaded from.</param>
		/// <param name="selfRef">A place where reference to 'self' ($this) can be loaded from.</param>
        /// <param name="ignoringReturnValue">True if the return value of the function call is not used then.</param>
		/// <returns>A type of value stored on the top of the evaluation stack. In case of value type, it is NOT boxed.</returns>
		public Type EmitOverloadCall(
			MethodInfo method,
			ParameterInfo[] ps,
			int phpParamCount,
            IPlace scriptContext,
			IPlace rtVariables,
			IPlace namingContext,
            IPlace classContext,
			IPlace optArgs,
			IPlace selfRef,
            bool ignoringReturnValue)
		{
			pushedArgsCount = 0;

            Label overloadCallEndLabel = il.DefineLabel();
            Type/*!*/return_type = method.ReturnType;

            // the routine used to skip the method call in case of invalid parameter cast
            overloadCallSkipEmitter = (ile) =>
                {
                    if (return_type != Types.Void)
                    {
                        // emit the value; because the method call was skipped, value must be loaded here
                        if (return_type.IsValueType)
                            ile.LoadLiteral(Activator.CreateInstance(return_type), false);    // value is not boxed
                        else
                            ile.Emit(OpCodes.Ldnull);
                    }

                    // goto the end label
                    il.Emit(OpCodes.Br, overloadCallEndLabel);
                };

            if (scriptContext != null)
            {
                // LOAD(<context>);
                scriptContext.EmitLoad(il);
                pushedArgsCount++;
            }

			if (selfRef != null)
			{
				// LOAD(<this>);
				selfRef.EmitLoad(il);
				pushedArgsCount++;
			}

			if (rtVariables != null)
			{
				// LOAD(<defined variables>);
				rtVariables.EmitLoad(il);
				pushedArgsCount++;
			}

			if (namingContext != null)
			{
				// LOAD(<naming context>);
				namingContext.EmitLoad(il);
				pushedArgsCount++;
			}

            if (classContext != null)
            {
                // LOAD(<class_context>)
                classContext.EmitLoad(il);
                pushedArgsCount++;
            }

			// loads mandatory arguments:
			for (int i = 0; i < phpParamCount; i++)
			{
				EmitMandatoryArgumentLoad(i, ps[pushedArgsCount]);
				pushedArgsCount++;
			}

			// loads optional arguments:
			if (optArgs != null)
			{
				loadOptParams(this, phpParamCount, ps[ps.Length - 1], optArgs);
				pushedArgsCount++;
			}

			Debug.Assert(pushedArgsCount == ps.Length);

			// all class library functions are args-unaware => remove frame if using stack:
			if (stack != null)
			{
				stack.EmitLoad(il);
				il.Emit(OpCodes.Call, Methods.PhpStack.RemoveFrame);
			}

			// CALL STATIC <overload>(items on STACK);
			il.Emit(OpCodes.Call, method);

            // the result value is on the top of the evaluation stack

			//IPlace return_value = null;

			// stores return value an tmp. variable:
            //if (method.ReturnType != Types.Void)
            //{
            //    LocalBuilder loc = il.DeclareLocal(method.ReturnType);
            //    return_value = new Place(loc);

            //    // stores the result of a call to local:
            //    il.Stloc(loc);
            //}

			// updates arguments passed by reference:
			EmitReferencesLoad();

            // An argument loader can jump here if method call should be skipped.
			// In such a case return_value local will have default value (since locals are initialized).
			il.MarkLabel(overloadCallEndLabel);

            // [CastToFalse] or [PhpDeepCopy]
            if (!ignoringReturnValue)
            {
                // converts return value (deep copy, cast to false):
                EmitReturnValueConversion(method, ref return_type);
            }
            
            // free the skip emitter, to not be used incidentally again
            overloadCallSkipEmitter = null;

            return return_type;
		}

		#endregion

		#region Argument Loading

		/// <summary>
		/// Emits load of a specified mandatory argument and appropriate conversions on it.
		/// Leaves the result on the evaluation stack so it can be later passed as an argument to a method.
		/// </summary>
		/// <param name="index">The index of the parameter counted from zero.</param>
		/// <param name="param">The parameter.</param>
		/// <remarks>
		/// Arguments passed by object reference (ref/out) are handled in the manner of in/out semantics.
		/// If the argument is passed by object reference a holder local variable of the smae type is created.
		/// A reference passed as the actual argument is peeked from PHP stack, converted to the target type by 
		/// <see cref="EmitArgumentConversion"/> and stored in the holder. Then the holder's address is passed
		/// to the overload. On the return from the overload each holder's value is stored back to 
		/// the actual argument by <see cref="EmitReferencesLoad"/>. In order to do so holder local variable builders
		/// and the local variables builders storing the actual argument are added to <see cref="refHolders"/> and 
		/// <see cref="refReferences"/>, respectively, by this method.
		/// </remarks>
		private void EmitMandatoryArgumentLoad(int index, ParameterInfo param)
		{
			Type formal_type = param.ParameterType;

			if (formal_type.IsByRef)
			{
				// declares holder:
				Type elem_type = formal_type.GetElementType();
				LocalBuilder ref_loc = il.DeclareLocal(Types.PhpReference[0]);
				LocalBuilder holder_loc = il.DeclareLocal(elem_type);

				// emits reference argument peeking:
				// ref = <load reference parameter>
				loadReferenceParam(il, index, this.aux);
				il.Stloc(ref_loc);

				// loads a value to the holder if parameter is not out-only:
				if (!param.IsOut)
				{
					// gets referenced value (loadReferenceParam guarantees that the reference is not null):
					// LOAD(ref.value);
					il.Ldloc(ref_loc);
					il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);

					// emits conversion stuff, loads to holder (actual type is always object);
					// implicit cast of types PhpArray, PhpObject, and PhpResource is allowed and not checked:
					EmitArgumentConversion(elem_type, typeof(object), true, param);
					il.Stloc(holder_loc);
				}

				// LOAD(&holder);
				il.Ldloca(holder_loc);

				// adds holder to the list of holders:
				refHolders.Add(holder_loc);
				refReferences.Add(ref_loc);
			}
			else
			{
				object type_or_value;
				if (formal_type == typeof(PhpReference))
				{
					// LOAD <load reference parameter>
					type_or_value = loadReferenceParam(il, index, this.aux);
				}
				else
				{
					// LOAD <load value parameter>
					type_or_value = loadValueParam(il, index, this.aux);
				}

				// emits conversion stuff:
				EmitArgumentConversion(formal_type, type_or_value, false, param);
			}
		}

		/// <summary>
		/// Emits code loading values stored in holder variables into respective references.
		/// </summary>
		public void EmitReferencesLoad()
		{
			Debug.Assert(refHolders.Count == refReferences.Count);

			for (int i = 0; i < refHolders.Count; i++)
			{
				LocalBuilder holder_loc = (LocalBuilder)refHolders[i];
				LocalBuilder ref_loc = (LocalBuilder)refReferences[i];

				// ref.value = {holder | BOX(holder)};
				il.Ldloc(ref_loc);
				il.Ldloc(holder_loc);

				if (holder_loc.LocalType.IsValueType)
					il.Emit(OpCodes.Box, holder_loc.LocalType);

				il.Emit(OpCodes.Stfld, Fields.PhpReference_Value);
			}

			refHolders.Clear();
			refReferences.Clear();
		}

		#endregion

		#region Argument Conversion

		/// <summary>
		/// Emits code converting argument on the evaluation stack to a specified type using PHP.NET library conversions. 
		/// Used for conversion of elements of params array optional arguments,
		/// for conversion of a content of mandatory by-ref holder, and
		/// for conversion of mandatory in argument.
		/// </summary>
		/// <param name="dstType">
		/// The type of the formal argument. Shouldn't be <see cref="Type.IsByRef"/>.
		/// </param>
		/// <param name="srcTypeOrValue">
		/// The type of the formal argument or its value if it is a literal.
		/// </param>
		/// <param name="allowImplicitCast">Whether to allow implicit cast of types PhpArray, PhpObject, PhpResource.</param>
		/// <param name="param">The formal argument description.</param>
		internal void EmitArgumentConversion(Type dstType, object srcTypeOrValue, bool allowImplicitCast, ParameterInfo param)
		{
			Debug.Assert(!dstType.IsByRef);

			Type src_type = srcTypeOrValue as Type;

			// passing void parameter is the same as passing null reference:
			if (src_type == Types.Void)
			{
				srcTypeOrValue = null;
				src_type = null;
			}

			// unites treatment of enums and ints:
			if (src_type != null && src_type.IsEnum) src_type = typeof(int);
			if (dstType.IsEnum) dstType = typeof(int);

			// no conversions needed:
			if (dstType == src_type)
			{
				// deep copy if needed (doesn't produce unnecessary copying):
				if (param.IsDefined(typeof(PhpDeepCopyAttribute), false))
				{
					// CALL (<dstType>)PhpVariable.Copy(STACK,CopyReason.PassedByCopy);
					il.LdcI4((int)CopyReason.PassedByCopy);
					il.Emit(OpCodes.Call, Methods.PhpVariable.Copy);

					if (dstType != typeof(object))
						il.Emit(OpCodes.Castclass, dstType);
				}
				return;
			}

			// if dst type is reference then src type should be also a reference 
			// (reference - reference combination was eliminated in previous statement):
			Debug.Assert(dstType != typeof(PhpReference), "Formal type cannot be reference if actual is not.");

			// dereferences a reference:
			if (src_type == typeof(PhpReference))
			{
				il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
				src_type = typeof(object);
			}

			#region dst is integer, long integer, bool, double, string, PhpBytes, char

			// to integer (can be loaded from literal):
			if (dstType == typeof(int))
			{
				if (src_type == null)
				{
					il.LdcI4(Convert.ObjectToInteger(srcTypeOrValue));
				}
				else
				{
					// boxing and conversion:
					if (src_type.IsValueType) il.Emit(OpCodes.Box, src_type);
					il.Emit(OpCodes.Call, Methods.Convert.ObjectToInteger);
				}
				return;
			}

			// to long integer (can be loaded from literal):
			if (dstType == typeof(long))
			{
				if (src_type == null)
				{
					il.LdcI8(Convert.ObjectToLongInteger(srcTypeOrValue));
				}
				else
				{
					// boxing and conversion:
					if (src_type.IsValueType) il.Emit(OpCodes.Box, src_type);
					il.Emit(OpCodes.Call, Methods.Convert.ObjectToLongInteger);
				}
				return;
			}

			// to boolean (can be loaded from literal):
			if (dstType == typeof(bool))
			{
				if (src_type == null)
				{
					il.LdcI4(Convert.ObjectToBoolean(srcTypeOrValue) ? 1 : 0);
				}
				else
				{
					// boxing and conversion:
					if (src_type.IsValueType) il.Emit(OpCodes.Box, src_type);
					il.Emit(OpCodes.Call, Methods.Convert.ObjectToBoolean);
				}
				return;
			}

			// to double (can be loaded from literal):
			if (dstType == typeof(double))
			{
				if (src_type == null)
				{
					il.Emit(OpCodes.Ldc_R8, Convert.ObjectToDouble(srcTypeOrValue));
				}
				else
				{
					// boxing and conversion:
					if (src_type.IsValueType) il.Emit(OpCodes.Box, src_type);
					il.Emit(OpCodes.Call, Methods.Convert.ObjectToDouble);
				}
				return;
			}

			// to string (can be loaded from literal):
			if (dstType == typeof(string))
			{
				if (src_type == null)
				{
					il.Emit(OpCodes.Ldstr, Convert.ObjectToString(srcTypeOrValue));
				}
				else
				{
					// boxing and conversion:
					if (src_type.IsValueType) il.Emit(OpCodes.Box, src_type);
					il.Emit(OpCodes.Call, Methods.Convert.ObjectToString);
				}
				return;
			}

            // to bytes:
            if (dstType == typeof(PhpBytes))
            {
                if (src_type == null)
                {
                    il.EmitLoadPhpBytes(Convert.ObjectToPhpBytes(srcTypeOrValue));
                }
                else
                {
                    // boxing and conversion:
                    if (src_type.IsValueType) il.Emit(OpCodes.Box, src_type);
                    il.Emit(OpCodes.Call, Methods.Convert.ObjectToPhpBytes);
                }
                return;
            }

            // to char:
			if (dstType == typeof(char))
			{
				if (src_type == null)
				{
					il.LdcI4((int)Convert.ObjectToChar(srcTypeOrValue));
				}
				else
				{
					// boxing and conversion:
					if (src_type.IsValueType) il.Emit(OpCodes.Box, src_type);
					il.Emit(OpCodes.Call, Methods.Convert.ObjectToChar);
				}
				return;
			}

			#endregion

			// further conversions doesn't work with empty stack => loads literal on eval stack:
			if (src_type == null)
				src_type = il.LoadLiteral(srcTypeOrValue);

			if (src_type.IsValueType)
				il.Emit(OpCodes.Box, src_type);

			// to callback:
			if (dstType == typeof(PhpCallback))
			{
				il.Emit(OpCodes.Call, Methods.Convert.ObjectToCallback);
				return;
			}

			Debug.Assert(!dstType.IsValueType);

			if (param.IsDefined(typeof(PhpDeepCopyAttribute), false))
			{
				// do not copy literals:
				if (!src_type.IsValueType && src_type != typeof(string))
				{
					// CALL (<src_type>)PhpVariable.Copy(STACK,CopyReason.PassedByCopy);
					il.LdcI4((int)CopyReason.PassedByCopy);
					il.Emit(OpCodes.Call, Methods.PhpVariable.Copy);

					// src_type was on the eval. stack before copy was called:
					if (src_type != typeof(object))
						il.Emit(OpCodes.Castclass, src_type);
				}
			}

			// to object:
			if (dstType == typeof(object)) return;

            //
            // cast the value to the target type
            //
            if (allowImplicitCast)
            {
                // cast the value, without the checking of the success
                il.Emit(OpCodes.Isinst, dstType);
            }
            else
            {
                // if implicit cast is not allowed => a condition checking the result of the cast
                // is emitted (conditional call to InvalidImplicitCast)

                // conversion of array, object, and resource:
                string type_name = PhpVariable.GetAssignableTypeName(dstType);

                Label endif_label = il.DefineLabel();

                LocalBuilder loc_typed = null;
                LocalBuilder loc_obj = il.DeclareLocal(typeof(object));
                il.Emit(OpCodes.Dup);
                il.Stloc(loc_obj);
                
                // IF (obj == null) goto ENDIF;
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Beq_S, endif_label);

                //
                il.Ldloc(loc_obj);
                // (obj) on top of eval stack, eat it:

                if (dstType.IsSealed)
                {
                    // if (<obj>.GetType() == typeof(<dstType>)) goto ENDIF;    // little JIT hack
                    il.Emit(OpCodes.Callvirt, Methods.Object_GetType);
                    il.Emit(OpCodes.Ldtoken, dstType);
                    il.Emit(OpCodes.Call, Methods.GetTypeFromHandle);
                    il.Emit(OpCodes.Call, Methods.Equality_Type_Type);
                    il.Emit(OpCodes.Brtrue, endif_label);
                }
                else
                {
                    loc_typed = il.DeclareLocal(dstType);

                    // <loc_typed> = <obj> as <dstType>:
                    il.Emit(OpCodes.Isinst, dstType);
                    il.Emit(OpCodes.Dup);
                    il.Stloc(loc_typed);

                    // (obj as dstType) is on top of the evaluation stack

                    // IF (obj!=null) goto ENDIF;
                    il.Emit(OpCodes.Brtrue_S, endif_label);
                }

                if (true)
                {
                    // pops all arguments already pushed:
                    for (int i = 0; i < pushedArgsCount; ++i)
                        il.Emit(OpCodes.Pop);
                    
                    // CALL PhpException.InvalidImplicitCast(obj,<PhpTypeName>,<functionName>);
                    il.Ldloc(loc_obj);
                    il.Emit(OpCodes.Ldstr, type_name);
                    il.Emit(OpCodes.Ldstr, this.functionName.ToString());
                    il.Emit(OpCodes.Call, Methods.PhpException.InvalidImplicitCast);
                    
                    if (debug)
                        il.Emit(OpCodes.Nop);

                    Debug.Assert(overloadCallSkipEmitter != null);

                    // GOTO <end of call>;
                    //il.Emit(OpCodes.Br, overloadCallEndLabel);
                    overloadCallSkipEmitter(il);
                }

                // ENDIF;
                il.MarkLabel(endif_label);

                // load typed <obj> (already casted in <loc_typed> or boxed in <loc_obj>
                if (loc_typed != null)
                {
                    il.Ldloc(loc_typed);
                }
                else
                {
                    il.Ldloc(loc_obj);
                    il.Emit(OpCodes.Castclass, dstType);
                }
            }
		}

		#endregion

		#region Return Value Conversion

		/// <summary>
		/// Emits code handling return value conversions. The value is on the top of the evaluation stack.
		/// </summary>
		/// <param name="method">The method which return value conversion to emit.</param>
        /// <param name="return_type">A type of return value (that is on the top of evaluation stack) to be converted.</param>
        public void EmitReturnValueConversion(MethodInfo method, ref Type/*!*/return_type)
		{
            if (return_type == null || return_type == Types.Void)
                return; // nothing to be converted

			// whether to emit cast to false:
			if (method.ReturnTypeCustomAttributes.IsDefined(typeof(CastToFalseAttribute), false))
			{
				Label else_label = il.DefineLabel();
				Label endif_label = il.DefineLabel();

				// IF(return_value==-1 or null) THEN
				//returnValue.EmitLoad(il);
                il.Emit(OpCodes.Dup);
                EmitCastShortBranch(return_type, else_label);
				if (true)
				{
                    // pop value
                    il.Emit(OpCodes.Pop);

					// BOX false;
					il.Emit(OpCodes.Ldc_I4_0);
					il.Emit(OpCodes.Box, typeof(bool));

					// BR endif;
					il.Emit(OpCodes.Br_S, endif_label);
				}

				il.MarkLabel(else_label);

				// ELSE

				//il.LoadBoxed(returnValue);
                if (return_type.IsValueType)
                    il.Emit(OpCodes.Box, return_type);

				il.MarkLabel(endif_label);

				// END IF;

				// STORE returnValue,STACK
                //LocalBuilder loc_return_value = il.DeclareLocal(typeof(object));
                //il.Stloc(loc_return_value);
                //returnValue = new Place(loc_return_value);

                return_type = typeof(object);
			}
			else
				// deep copy:
                if (method.ReturnTypeCustomAttributes.IsDefined(typeof(PhpDeepCopyAttribute), false) && !return_type.IsValueType)
				{
					// returnValue = (<returnType>)PhpVariable.Copy(returnValue,CopyReason.ReturnedByCopy);

					//returnValue.EmitLoad(il);
                    il.LdcI4((int)CopyReason.ReturnedByCopy);
					il.Emit(OpCodes.Call, Methods.PhpVariable.Copy);

                    if (return_type != typeof(object))
                        il.Emit(OpCodes.Castclass, return_type);
                    
                    //returnValue.EmitStore(il);
				}
		}

		/// <summary>
		/// Emits casting to false branch. 
		/// </summary>
		/// <param name="returnType">A return type.</param>
		/// <param name="noCastLabel">The label where to jump if the cast doesn't take place.</param>
		private void EmitCastShortBranch(Type returnType, Label noCastLabel)
		{
			if (returnType == typeof(int))
			{
				il.Emit(OpCodes.Ldc_I4_M1);                   // load -1
				il.Emit(OpCodes.Bne_Un_S, noCastLabel);       // branch if not equal
			}
			else
			{
				il.Emit(OpCodes.Brtrue_S, noCastLabel);       // branch if not null
			}
		}

        /// <summary>
        /// Emits load of default value assuming given method fails.
        /// </summary>
        /// <param name="il">ILEmitter.</param>
        /// <param name="method">Method which default return value have to be loaded.</param>
        /// <returns></returns>
        public static PhpTypeCode EmitLoadDefault(ILEmitter/*!*/il, MethodInfo/*!*/method)
        {
            if (method.ReturnType == Types.Bool[0] || method.ReturnTypeCustomAttributes.IsDefined(typeof(CastToFalseAttribute), false))
            {
                il.LoadBool(false);
                return PhpTypeCode.Boolean;
            }

            if (method.ReturnType == Types.Int[0])
            {
                il.LdcI4(0);
                return PhpTypeCode.Integer;
            }

            return PhpTypeCode.Void;
        }

		#endregion
	}
}
