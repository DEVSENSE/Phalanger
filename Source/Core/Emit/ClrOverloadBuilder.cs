/*

 Copyright (c) 2006 Ladislav Prosek and Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

//#if SILVERLIGHT
#define EMIT_VERIFIABLE_STUBS
//#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;
using System.Diagnostics;
using PHP.Core.Reflection;
using PHP.Core.Emit;

namespace PHP.Core.Emit
{
	using Overload = ClrMethod.Overload;
	using ConversionStrictness = PHP.Core.ConvertToClr.ConversionStrictness;
	 
	/// <summary>
	/// </summary>
	/// <remarks><code>
	/// switch (#actuals)
	/// {
	///    case i:
	///				// Conversions for the first overload
	///       overload_i_1:
	///					strictness_i = ImplExactMatch;
	///         formal_i_1_1 = TryConvertTo{typeof(formal_i_1_1)}(actual_1, out strictness_tmp);
	///         if (strictness_tmp == Failed) goto overload_i_2;
	///					strictness_i += strictness_tmp;
	///         formal_i_1_2 = TryConvertTo{typeof(formal_i_1_2)}(actual_2, out strictness_tmp);
	///         if (strictness_tmp == Failed) goto overload_i_2;
	///					strictness_i += strictness_tmp;
	///         ...
	///         if (strictness_i_1 == ImplExactMatch) goto call_overload_i_1;
	///					best_overload = 1;
	///  
	///				// Conversions for the second overload
	///       overload_i_2:
	///         formal_i = TryConvertTo{typeof(formal_i)}(actual_i, out strictness_tmp);
	///         if (strictness_tmp == Failed) goto overload_i_3;
	///					strictness_i_2 += strictness_tmp;
	///         ...
	///         if (strictness_i_2 == ImplExactMatch) goto call_overload_i_2;
	///					if (strictness_i_2 &lt; strictness_i_1)
	///						best_overload = 2;
	/// 
	///       // ... other overloads
	/// 
	///				// select the best overload
	///				call_overload_i:
	///				switch(best_overload)
	///				{
	///					case 1:
	///						return overload_i_1(formal_i_1_1, ..., formal_i_1_k);
	///					case 2:
	///						return overload_i_2(formal_i_2_1, ..., formal_i_2_k);
	///					// ... other calls
	///				}
	///        
	///    case less than #formals:
	///       Warning(to few args);
	///       --- fill missing with default values of their respective types ---
	///       goto case #min_formals;
	/// 
	///    case more than #formals:      
	///        Warning(to many args)
	///       goto case #max_formals;
	/// }
	/// 
	/// error:
	/// NoSuitableOverload()
	/// </code></remarks>
	[DebuggerNonUserCode]
	public sealed class ClrOverloadBuilder
	{
		#region Fields and types

		/// <summary>
		/// A delegate used to load a parameter to evaluation stack.
		/// </summary>
		public delegate object ParameterLoader(ILEmitter/*!*/ il, IPlace/*!*/ stack, IPlace/*!*/ parameter);

		private ILEmitter/*!*/ il;
		private ClrMethod/*!*/ method;
		private ConstructedType constructedType;
		private IPlace/*!*/ stack;
		private IPlace instance;
		private ParameterLoader/*!*/ loadValueArg;
		private ParameterLoader/*!*/ loadReferenceArg;
		private bool emitParentCtorCall;

		private List<Overload>/*!!*/ overloads;
		//private BitArray argCounts;
		private IPlace scriptContext;

		/// <summary>
		/// Case labels used during by-number resolution
		/// </summary>
		private Label[] caseLabels;
		private Label noSuitableOverloadErrorLabel;
		private Label returnLabel;

		/// <summary>
		/// Marked after the argument load block of the overload with minimal arguments.
		/// </summary>
		private Label? minArgOverloadTypeResolutionLabel;

		private int minArgCount;
		private int maxArgCount;
		private LocalBuilder strictness;
		private LocalBuilder[] valLocals;
		private LocalBuilder[] refLocals;
		private LocalBuilder returnValue;

		#endregion

		#region Constructor

		/// <summary>
		/// Generic type definition of Nullable&lt;_&gt; type.
		/// </summary>
		static Type NullableType;

		static ClrOverloadBuilder()
		{
			NullableType = typeof(int?).GetGenericTypeDefinition();
		}

		public ClrOverloadBuilder(ILEmitter/*!*/ il, ClrMethod/*!*/ method, ConstructedType constructedType,
			IPlace/*!*/ stack, IPlace/*!*/ instance, bool emitParentCtorCall,
			ParameterLoader/*!*/ loadValueArg, ParameterLoader/*!*/ loadReferenceArg)
		{
			this.il = il;
			this.method = method;
			this.constructedType = constructedType;
			this.stack = stack;
			this.instance = instance;
			this.loadValueArg = loadValueArg;
			this.loadReferenceArg = loadReferenceArg;
			this.emitParentCtorCall = emitParentCtorCall;

			this.overloads = new List<Overload>(method.Overloads);
			SortOverloads(this.overloads);
		}

		#endregion

		#region Utilities

		/// <summary>
		/// Sorts overloads in the list - so that we guarantee deterministic overload selection.
		/// Most important thing is to test subtypes first - because both conversions are evaluated as ImplExactMatch.
		/// (sorting also allows some performance tweaks)
		/// </summary>
		/// <param name="list">List to sort</param>
		private void SortOverloads(List<Overload> list)
		{
			list.Sort(delegate(Overload first, Overload second)
			{
				// overloads with less parameters are returned first
				if (first.ParamCount != second.ParamCount)
					return first.ParamCount - second.ParamCount;

				// compare parameter types - we want to test subtypes first, so that when we have A :> B 
				// and foo(A), foo(B) we first test foo(B)
				for (int i = 0; i < first.ParamCount; i++)
				{
					Type t1 = first.Parameters[i].ParameterType, t2 = second.Parameters[i].ParameterType;
					if (!t1.Equals(t2)) return CompareTypes(t1, t2); // not equal parameter types - sort by this param
				}
				return 0;
			});
		}

		/// <summary>
		/// Compares two types. Type <paramref name="t1"/> is less if it is subclass
		/// of <paramref name="t2"/>, or it is array of subclasses (covariant arrays).
		/// Otherwise the types are sorted by name in alphabetical order with an exception
		/// of type string which is greather than any other type (because if we allow explicit
		/// conversions, the conversion to string is slooow!).
		/// </summary>
		/// <param name="t1">First type</param>
		/// <param name="t2">Second type</param>
		/// <returns>-1 if first is less, 1 if second is less</returns>
		private int CompareTypes(Type t1, Type t2)
		{
			if (t1.IsSubclassOf(t2)) return -1;
			if (t2.IsSubclassOf(t1)) return +1;

			// array covariance (solves vararg overloads too)
			// array is after non-array or compare array types
			if (t1.IsArray && t2.IsArray) return CompareTypes(t1.GetElementType(), t2.GetElementType());
			else if (t1.IsArray) return 1;
			else if (t2.IsArray) return -1;

			// strings should be at the end .. 
			if (t1.Equals(Types.String[0])) return +1;
			if (t2.Equals(Types.String[0])) return -1;

			// not related types.. sort by type name (to make selection deterministic)
			if (!t1.Equals(t2)) return String.Compare(t1.Name, t2.Name);
			return 0;
		}

		private static void GetOverloadValRefArgCount(Overload/*!*/ overload, out int byValCount, out int byRefCount)
		{
			int byref = 0;

			ParameterInfo[] parameters = overload.Parameters;
			for (int i = 0; i < parameters.Length; i++)
			{
				if (parameters[i].ParameterType.IsByRef) byref++;
			}

			byValCount = parameters.Length - byref;
			byRefCount = byref;
		}

		private void EmitCreateParamsArray(Type/*!*/ elementType, LocalBuilder/*!*/ local, int count)
		{
			il.LdcI4(count);
			il.Emit(OpCodes.Newarr, elementType);
			il.Stloc(local);
		}

        /// <summary>
        /// Emit LOAD <paramref name="instance"/>.
        /// </summary>ILEmiter
        /// <param name="il"><see cref="ILEmitter"/> object instance.</param>
        /// <param name="instance">The place where to load the instance from.</param>
        /// <param name="declaringType">The type of resulting instance.</param>
        /// <remarks>Instance of value types are wrapped in <see cref="ClrValue&lt;T&gt;"/> object instance.</remarks>
        internal static void EmitLoadInstance(ILEmitter/*!*/il, IPlace/*!*/instance, Type/*!*/declaringType)
        {
            Debug.Assert(il != null && instance != null && declaringType != null, "ClrOverloadBuilder.EmitLoadInstance() null argument!");

            // LOAD <instance>
            instance.EmitLoad(il);

            if (declaringType.IsValueType)
            {
                var clrValueType = ClrObject.valueTypesCache.Get(declaringType).Item1;
                Debug.Assert(clrValueType != null, "Specific ClrValue<T> not found!");

                // CAST (ClrValue<T>)
                il.Emit(OpCodes.Castclass, clrValueType);

                // LOAD .realValue
                var realValueField = clrValueType.GetField("realValue");
                Debug.Assert(realValueField != null, "ClrValue<T>.realValue field not found!");
                il.Emit(OpCodes.Ldflda, clrValueType.GetField("realValue"));
            }
            else
            {
                // CAST (T)
                il.Emit(OpCodes.Castclass, declaringType);
            }
        }

		/// <summary>
		/// Emits overload call
		/// </summary>
		private void EmitCall(Overload/*!*/ overload, Label failLabel, LocalBuilder[] formals)
		{
			MethodBase overload_base = overload.Method;

			/* CHECK IS DONE IN THE EARLIER PHASE
			 * (in EmitConversion method)
			 
			if (!emitParentCtorCall && (overload_base.IsFamily || overload_base.IsFamilyOrAssembly))
			{
				// IF (!stack.AllowProtectedCall) THEN GOTO next-overload-or-error;
				stack.EmitLoad(il);
				il.Emit(OpCodes.Ldfld, Fields.PhpStack_AllowProtectedCall);
				il.Emit(OpCodes.Brfalse, failLabel);
			}*/

            //
            // LOAD <instance>
            //

            if ((emitParentCtorCall) // calling .ctor on parent
                ||(!overload_base.IsStatic && !overload_base.IsConstructor)// calling method on non-static object
                //||(overload_base.IsConstructor && overload_base.DeclaringType.IsValueType)// calling .ctor on structure (which initializes fields on existing value) (but the ClrValue does not exist yet :-))
                )
			{
                EmitLoadInstance(il, instance, overload_base.DeclaringType);
			}

            //
            // LOAD {<args>}
            //

			for (int i = 0; i < overload.Parameters.Length; i++)
			{
				if (overload.Parameters[i].ParameterType.IsByRef) il.Ldloca(formals[i]);
				else il.Ldloc(formals[i]);
			}

            //
            // CALL <method> or 
            //

            if (!overload_base.IsConstructor)
			{
                // method
                MethodInfo info = DType.MakeConstructed((MethodInfo)overload_base, constructedType);

				// CALL <method>(args);
				// TODO: il.Emit(info.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, info);
#if SILVERLIGHT
                il.Emit(info.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, info);
#else
				il.Emit(OpCodes.Call, info);
#endif

				// return value conversions:
				if (info.ReturnType != Types.Void)
				{
					il.EmitBoxing(EmitConvertToPhp(il, info.ReturnType/* scriptContext*/));
					il.Stloc(returnValue);
				}
			}
			else
			{
                // .ctor
				ConstructorInfo ctor = DType.MakeConstructed((ConstructorInfo)overload_base, constructedType);

                if (emitParentCtorCall)
				{
					// CALL <ctor>(args);
					il.Emit(OpCodes.Call, ctor);
				}
				else
				{
					// NEW <ctor>(args);
					il.Emit(OpCodes.Newobj, ctor);

                    il.EmitBoxing(EmitConvertToPhp(il, ctor.DeclaringType));    // convert any newly created object to valid PHP object
					/*if (ctor.DeclaringType.IsValueType)
					{
						// box value type:
						il.Emit(OpCodes.Box, ctor.DeclaringType);
					}

					if (!Types.DObject[0].IsAssignableFrom(ctor.DeclaringType))
					{
						// convert to ClrObject if not DObject:
						il.Emit(OpCodes.Call, Methods.ClrObject_Wrap);
					}*/

					il.Stloc(returnValue);
				}
			}

			// store ref/out parameters back to their PhpReferences shells
			int byref_counter = 0;
			for (int i = 0; i < overload.Parameters.Length; i++)
			{
				Type param_type = overload.Parameters[i].ParameterType;
				if (param_type.IsByRef)
				{
					il.Ldloc(refLocals[byref_counter++]);
					il.Ldloc(formals[i]);

                    PhpTypeCode php_type_code = EmitConvertToPhp(
						il,
						param_type.GetElementType()/*,
						scriptContext*/);

					il.EmitBoxing(php_type_code);

					il.Emit(OpCodes.Stfld, Fields.PhpReference_Value);
				}
			}

			il.Emit(OpCodes.Br, returnLabel);
		}

		/// <summary>
		/// Emits code that is invoked when function is passed not enough parameters
		/// </summary>
		/// <param name="gotoLabel">Continue execution at this label</param>
		private void EmitMissingParameterCountHandling(Label gotoLabel)
		{
			// CALL PhpException.MissingArguments(<type name>, <routine name>, <actual count>, <required count>);
			il.Emit(OpCodes.Ldstr, method.DeclaringType.FullName);

			if (method.IsConstructor)
				il.Emit(OpCodes.Ldnull);
			else
				il.Emit(OpCodes.Ldstr, method.FullName);

			stack.EmitLoad(il);
			il.Emit(OpCodes.Ldfld, Fields.PhpStack_ArgCount);
			il.LdcI4(minArgCount);
			il.Emit(OpCodes.Call, Methods.PhpException.MissingArguments);

			// initialize all PhpReferences
			for (int i = 0; i < refLocals.Length; i++)
			{
				il.Emit(OpCodes.Newobj, Constructors.PhpReference_Void);
				il.Stloc(refLocals[i]);
			}

			// GOTO next-overload
			il.Emit(OpCodes.Br, gotoLabel);
		}

		/// <summary>
		/// Emits code that is invoked when function is passed more parameters
		/// </summary>
		/// <param name="gotoLabel">Continue execution at this label</param>
		private void EmitMoreParameterCountHandling(Label gotoLabel)
		{
			// CALL PhpException.InvalidArgumentCount(<type name>, <routine name>);
			il.Emit(OpCodes.Ldstr, method.DeclaringType.FullName);

			if (method.IsConstructor)
				il.Emit(OpCodes.Ldnull);
			else
				il.Emit(OpCodes.Ldstr, method.FullName);

			il.Emit(OpCodes.Call, Methods.PhpException.InvalidArgumentCount);

			// GOTO next-overload
			il.Emit(OpCodes.Br, gotoLabel);
		}

		#endregion

		#region Resolution by number

		private void Prepare(int maxArgCount, int maxValArgCount, int maxRefArgCount)
		{
			Debug.Assert(maxValArgCount + maxRefArgCount >= maxArgCount);

			this.noSuitableOverloadErrorLabel = il.DefineLabel();
			this.scriptContext = new Place(stack, Fields.PhpStack_Context);
			this.returnLabel = il.DefineLabel();

			// locals:
			this.valLocals = new LocalBuilder[maxValArgCount];
			for (int i = 0; i < valLocals.Length; i++)
				valLocals[i] = il.DeclareLocal(Types.Object[0]);

			this.refLocals = new LocalBuilder[maxRefArgCount];
			for (int i = 0; i < refLocals.Length; i++)
				refLocals[i] = il.DeclareLocal(Types.PhpReference[0]);

			this.returnValue = il.DeclareLocal(Types.Object[0]);
			this.strictness = il.DeclareLocal(typeof(ConversionStrictness));
		}

		private void PrepareResolutionByNumber()
		{
			if (overloads.Count == 0) return;

			this.minArgCount = overloads[0].MandatoryParamCount;
			this.maxArgCount = overloads[overloads.Count - 1].ParamCount;

			// determine maximum number of byval and byref parameters:
			int max_val_arg_count = 0;
			int max_ref_arg_count = 0;
			for (int i = 0; i < overloads.Count; i++)
			{
				int byval, byref;
				GetOverloadValRefArgCount(overloads[i], out byval, out byref);

				if (byval > max_val_arg_count) max_val_arg_count = byval;
				if (byref > max_ref_arg_count) max_ref_arg_count = byref;
			}

			int case_count = maxArgCount - minArgCount + 1;

			// labels:
			caseLabels = new Label[case_count];
			for (int i = 0; i < case_count; i++)
				caseLabels[i] = il.DefineLabel();

			Prepare(maxArgCount, max_val_arg_count, max_ref_arg_count);
		}

        /// <summary>
        /// Has to be chosen which method should be called.This method emits the code that 
        /// choses which overload to call by number of arguments. After this it calls 
        /// EmitResolutionByTypes.
        /// </summary>
		public void EmitResolutionByNumber()
		{
			PrepareResolutionByNumber();

			if (overloads.Count > 0)
			{
				// SWITCH (stack.ArgCount - <min param count>)
				stack.EmitLoad(il);
				il.Emit(OpCodes.Ldfld, Fields.PhpStack_ArgCount);
				if (minArgCount > 0)
				{
					il.LdcI4(minArgCount);
					il.Emit(OpCodes.Sub);
				}
				il.Emit(OpCodes.Switch, caseLabels);

				// DEFAULT:
				EmitDefaultCase();

				int last_success_case_index = -1;
				int arg_count = minArgCount;
				for (int case_index = 0; case_index < caseLabels.Length; case_index++, arg_count++)
				{
					// CASE <case_index>:
					il.MarkLabel(caseLabels[case_index]);

					List<Overload> arg_count_overloads = GetOverloadsForArgCount(arg_count);
					if (arg_count_overloads.Count == 0)
					{
						// no overload with arg_count parameters was found
						// report error and jump to the last successful arg count:
						EmitMoreParameterCountHandling(caseLabels[last_success_case_index]);
					}
					else
					{
						EmitResolutionByTypes(arg_count, arg_count_overloads);
						last_success_case_index = case_index;
					}
				}
			}

			EmitEpilogue();
		}

		private void EmitEpilogue()
		{
			// noSuitableOverload:
			il.MarkLabel(noSuitableOverloadErrorLabel);

			// stack.RemoveFrame
			if (!emitParentCtorCall)
			{
				stack.EmitLoad(il);
				il.Emit(OpCodes.Call, Methods.PhpStack.RemoveFrame);
			}

			il.Emit(OpCodes.Ldstr, method.DeclaringType.FullName);
			il.Emit(OpCodes.Ldstr, method.FullName);
			il.Emit(OpCodes.Call, Methods.PhpException.NoSuitableOverload);

			if (emitParentCtorCall)
			{
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Throw);
			}

			// return:
			il.MarkLabel(returnLabel);

			if (!emitParentCtorCall)
			{
				il.Ldloc(returnValue);
				il.Emit(OpCodes.Ret);
			}
		}

		/// <summary>
		/// Returns a list of overloads that can be called with the given argument count on the stack.
		/// </summary>
		/// <remarks>
		/// Vararg overloads are at the end of the returned list and are sorted, so that more general 
		/// overloads are at the end of the list (for example: params object[] is more general than int,params object[]).
		/// </remarks>
		private List<Overload>/*!*/ GetOverloadsForArgCount(int argCount)
		{
			List<Overload> result = new List<Overload>();
			Overload overload;
			int vararg_start_index = 0;

			int i = 0;
			while (i < overloads.Count && (overload = overloads[i]).MandatoryParamCount <= argCount)
			{
				// keep vararg overload at the end of the list - non-vararg overloads should be preferred
				if (overload.MandatoryParamCount == argCount)
					result.Insert(vararg_start_index++, overload);
				else if ((overload.Flags & ClrMethod.OverloadFlags.IsVararg) == ClrMethod.OverloadFlags.IsVararg)
					result.Add(overload);
				i++;
			}
			SortVarArgOverloads(result, vararg_start_index);
			return result;
		}

		/// <summary>
		/// Returns overloads including the "params" option
		/// </summary>
		/// <remarks>
		/// Returned overloads are sorted, so that more general overloads are at the end 
		/// of the list (for example: params object[] is more general than int,params object[]).
		/// </remarks>
		private List<Overload> GetVarArgOverloads(out int maxMandatoryArgCount)
		{
			List<Overload> result = null;
			maxMandatoryArgCount = 0;
			
			for (int i = 0; i < overloads.Count; i++)
			{
				if ((overloads[i].Flags & ClrMethod.OverloadFlags.IsVararg) == ClrMethod.OverloadFlags.IsVararg)
				{
					if (result == null) result = new List<Overload>();
					result.Add(overloads[i]);

					if (overloads[i].MandatoryParamCount > maxMandatoryArgCount)
					{
						maxMandatoryArgCount = overloads[i].MandatoryParamCount;
					}
				}
			}
			if (result != null) SortVarArgOverloads(result, 0);
			return result;
		}
		
		/// <summary>
		/// This function sorts vararg overloads so that more general overloads are at the end.
		/// The only difference to algorithm used for sorting overloads during initialization is
		/// that we need overloads with MORE parameters first (because this means we have some more 
		/// specific type requirements declared by mandatory parameters).
		/// 
		/// Assumes that input is sorted only the blocks with same parameter count needs to be reversed.
		/// 
		/// We need to sort parameters like this to prevent conversions to supertypes (like object[]),
		/// becausethis kind of conversion is treated as ImplExactMatch.
		/// </summary>
		/// <param name="result">List to be sorted</param>
		/// <param name="vararg_start_index">Vararg overloads start at this index</param>
		/// <remarks>
		/// Sorting should be for example:
		/// #parameters = 2 + 1:
		///		int, string, [params] int[]
		///		int, string, [params] object[]
		/// #parameters = 1 + 1:
		///		int, [params] object[]
		/// #parameters = 0 + 1:
		///		[params] int[]
		///		[params] object[]
		/// </remarks>
		private void SortVarArgOverloads(List<Overload>/*!*/ result, int vararg_start_index)
		{
			// most common situation..
			if (result.Count == 1) return;
			if (result.Count == vararg_start_index) return;

			Overload tmp;
			int i, j;

			// reverse the list
			i = vararg_start_index; j = result.Count - 1;
			while (i < j) { tmp = result[i]; result[i] = result[j]; result[j] = tmp; i++; j--; }
			
			// now reverse every single block 
			int pos = 0;
			while (pos < result.Count)
			{
				int block_end = pos, block_pcount = result[pos].ParamCount;
				while (block_end < result.Count && result[block_end].ParamCount == block_pcount) block_end++;

				i = pos; j = block_end - 1;
				while (i < j) { tmp = result[i]; result[i] = result[j]; result[j] = tmp; i++; j--; }
				pos = block_end;
			}	
		}
		
		/// <summary>
		/// Load arguments from stack and save them to valLocals (by-value) or refLocals (by-reference)
		/// </summary>
		/// <returns>Returned bit array specifies whether #i-th parameter is byref or by value</returns>
		private BitArray/*!*/ EmitLoadArguments(int argCount, List<Overload>/*!*/ argCountOverloads, bool removeFrame)
		{
			BitArray aliases = new BitArray(argCount, false);
			for (int i = 0; i < argCountOverloads.Count; i++)
			{
				// "or" the byref mask of all argCountOverloads
				for (int j = 0; j < argCountOverloads[i].ParamCount; j++)
				{
					if (argCountOverloads[i].IsAlias(j)) aliases[j] = true;
				}
			}

			int val_counter = 0;
			int ref_counter = 0;

			for (int i = 0; i < argCount; i++)
			{
				// LOAD <actual arg #arg_index>
				if (aliases[i])
				{
					loadReferenceArg(il, stack, new LiteralPlace(i + 1));
					il.Stloc(refLocals[ref_counter++]);
				}
				else
				{
					loadValueArg(il, stack, new LiteralPlace(i + 1));
					il.Stloc(valLocals[val_counter++]);
				}
			}

			if (removeFrame && !emitParentCtorCall)
			{
				// remove the frame:
				stack.EmitLoad(il);
				il.Emit(OpCodes.Call, Methods.PhpStack.RemoveFrame);
			}

			return aliases;
		}

		/// <summary>
		/// Default case in the by-number resolution switch
		/// </summary>
		private void EmitDefaultCase()
		{
			Label else_label = il.DefineLabel();
			this.minArgOverloadTypeResolutionLabel = il.DefineLabel();

			// IF (stack.ArgCount > <max param count>) THEN
			stack.EmitLoad(il);
			il.Emit(OpCodes.Ldfld, Fields.PhpStack_ArgCount);
			il.LdcI4(maxArgCount);
			il.Emit(OpCodes.Bge, else_label);

			EmitMissingParameterCountHandling(minArgOverloadTypeResolutionLabel.Value);

			// ELSE
			il.MarkLabel(else_label);
			
			int max_mandatory_arg_count;
			List<Overload> vararg_overloads = GetVarArgOverloads(out max_mandatory_arg_count);

			// do we have any vararg overloads?
			if (vararg_overloads != null)
			{
				// load fixed arguments from stack to valLocals and refLocals locals
				BitArray aliases = EmitLoadArguments(max_mandatory_arg_count, vararg_overloads, false);

				Label callSwitch = il.DefineLabel();
				List<LocalBuilder[]> formalOverloadParams = new List<LocalBuilder[]>();

				// bestOverload = -1
				LocalBuilder bestOverload = il.DeclareLocal(Types.Int[0]);
				il.LdcI4(-1);
				il.Stloc(bestOverload);

				// bestStrictness = Int32.MaxValue
				LocalBuilder bestStrictness = il.DeclareLocal(typeof(ConversionStrictness));
				il.LdcI4(Int32.MaxValue);
				il.Stloc(bestStrictness);


				for (int i = 0; i < vararg_overloads.Count; i++)
				{
					Label jump_on_error = il.DefineLabel();
					Overload current_overload = vararg_overloads[i];

					BitArray overload_aliases;
					if (current_overload.MandatoryParamCount < max_mandatory_arg_count)
					{
						overload_aliases = new BitArray(aliases);
						overload_aliases.Length = current_overload.MandatoryParamCount;
					}
					else 
						overload_aliases = aliases;

					// convert mandatory parameters
					// strictness_i = ImplExactMatch;
					LocalBuilder overloadStrictness = il.GetTemporaryLocal(typeof(int), false);
					il.LdcI4(0); // ConversionStrictness.ImplExactMatch
					il.Stloc(overloadStrictness);

					// alloc local variables
					LocalBuilder[] formals = new LocalBuilder[current_overload.ParamCount];
					formalOverloadParams.Add(formals);
					// convert parameters and tests after conversion
					EmitConversions(overload_aliases, current_overload, jump_on_error, overloadStrictness, formals, false);

					// load remaining arguments and construct the params array
					EmitLoadRemainingArgs(current_overload.MandatoryParamCount, 
						current_overload.Parameters[current_overload.MandatoryParamCount].ParameterType, 
						jump_on_error, formals, overloadStrictness);
					EmitConversionEpilogue(callSwitch, bestOverload, bestStrictness, i, i == (vararg_overloads.Count - 1), overloadStrictness);
	
					// reuse locals
					il.ReturnTemporaryLocal(overloadStrictness);
					il.MarkLabel(jump_on_error);
				}

				// stack.RemoveFrame
				if (!emitParentCtorCall)
				{
					stack.EmitLoad(il);
					il.Emit(OpCodes.Call, Methods.PhpStack.RemoveFrame);
				}

				// call the best overload
				EmitBestOverloadSelection(vararg_overloads, callSwitch, bestOverload, bestStrictness, formalOverloadParams);
			}
			else 
				EmitMoreParameterCountHandling(caseLabels[caseLabels.Length - 1]);
			// END IF;
		}

		#endregion

		#region Resolution by type

		/// <summary>
		/// Emits a for loop and constructs the array to be passed as the last 'params' argument.
		/// </summary>
		private void EmitLoadRemainingArgs(int alreadyLoadedArgs, Type/*!*/ argType, Label failLabel, 
			LocalBuilder[] formals, LocalBuilder overloadStrictness)
		{
			Type element_type = argType.GetElementType();

			// array = new argType[stack.ArgCount - alreadyLoadedArgs]
			LocalBuilder array = il.GetTemporaryLocal(argType);
			LocalBuilder item = il.GetTemporaryLocal(element_type);
			
			stack.EmitLoad(il);
			il.Emit(OpCodes.Ldfld, Fields.PhpStack_ArgCount);

			il.LdcI4(alreadyLoadedArgs);
			il.Emit(OpCodes.Sub);

			il.Emit(OpCodes.Newarr, element_type);
			il.Stloc(array);

			// FOR (tmp = 0; tmp <= array.Length; tmp++)
			LocalBuilder tmp = il.GetTemporaryLocal(Types.Int[0]);
			LocalBuilder tmp2 = il.GetTemporaryLocal(Types.Int[0]);

			// tmp = 0
			il.LdcI4(0);
			il.Stloc(tmp);

			Label condition_label = il.DefineLabel();
			il.Emit(OpCodes.Br_S, condition_label);

			Label body_label = il.DefineLabel();
			il.MarkLabel(body_label);

			// FOR LOOP BODY:
			il.Ldloc(tmp);
			il.LdcI4(alreadyLoadedArgs + 1);
			il.Emit(OpCodes.Add);
			il.Stloc(tmp2);
			
			loadValueArg(il, stack, new Place(tmp2));

			// item = CONVERT
			bool ct_ok = EmitConvertToClr(il, PhpTypeCode.Object, element_type, strictness); //!strictness
			il.Stloc(item);

			if (!ct_ok)
			{
				// if (strictness == Failed) goto error;
				// strictness_i += strictness
				il.Ldloc(strictness);
				il.LdcI4((int)ConversionStrictness.Failed);
				il.Emit(OpCodes.Beq, failLabel);

				il.Ldloc(overloadStrictness);
				il.Ldloc(strictness);
				il.Emit(OpCodes.Add);
				il.Stloc(overloadStrictness);
			}

			// array[tmp] = item
			il.Ldloc(array);
			il.Ldloc(tmp);
			il.Ldloc(item);
			il.Stelem(element_type);
			
			// tmp++
			il.Ldloc(tmp);
			il.LdcI4(1);
			il.Emit(OpCodes.Add);
			il.Stloc(tmp);

			// tmp <= array.Length
			il.MarkLabel(condition_label);
			il.Ldloc(tmp);
			il.Ldloc(array);
			il.Emit(OpCodes.Ldlen);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Blt_S, body_label);

			formals[alreadyLoadedArgs] = array;

			il.ReturnTemporaryLocal(tmp);
			il.ReturnTemporaryLocal(tmp2);
			il.ReturnTemporaryLocal(item);
		}

        /// <summary>
        /// Emits code that choses overload method based on argument types
        /// </summary>
        /// <param name="overloadIndex"></param>
		public void EmitResolutionByTypes(int overloadIndex)
		{
			int arg_count = overloads[overloadIndex].MandatoryParamCount;
			int max_byref = 0, max_byval = 0;

			int i = overloadIndex;
			do
			{
				int byval, byref;
				GetOverloadValRefArgCount(overloads[i], out byval, out byref);

				if (byval > max_byval) max_byval = byval;
				if (byref > max_byref) max_byref = byref;
			}
			while (++i < overloads.Count && overloads[i].MandatoryParamCount == arg_count);

			Prepare(arg_count, max_byval, max_byref);
			EmitResolutionByTypes(arg_count, GetOverloadsForArgCount(arg_count));
			EmitEpilogue();
		}


        /// <summary>
        /// Emits code that choses overload method based on argument types
        /// </summary>
        /// <param name="argCount">Count of the arguments</param>
        /// <param name="argCountOverloads">Count of overloadesd methods with <paramref name="argCount"/> count of arguments.</param>
		private void EmitResolutionByTypes(int argCount, List<Overload> argCountOverloads)
		{
			// load arguments from stack to valLocals and refLocals locals
			BitArray aliases = EmitLoadArguments(argCount, argCountOverloads, true);

			// mark the label where missing arguments handler should jump:
			if (minArgOverloadTypeResolutionLabel.HasValue && argCount == minArgCount)
			{
				il.MarkLabel(minArgOverloadTypeResolutionLabel.Value);
			}

			Label callSwitch = il.DefineLabel();

			// bestOverload = -1
			LocalBuilder bestOverload = il.DeclareLocal(Types.Int[0]);
			il.LdcI4(-1);
			il.Stloc(bestOverload);

			// bestStrictness = Int32.MaxValue
			LocalBuilder bestStrictness = il.DeclareLocal(typeof(ConversionStrictness));
			il.LdcI4(Int32.MaxValue);
			il.Stloc(bestStrictness);

			List<LocalBuilder[]> formalOverloadParams = new List<LocalBuilder[]>();
			for (int i = 0; i < argCountOverloads.Count; i++)
			{
				bool is_last = (i + 1 == argCountOverloads.Count);
				Label jump_on_error = il.DefineLabel();
				Overload current_overload = argCountOverloads[i];

				// strictness_i = ImplExactMatch;
				LocalBuilder overloadStrictness = il.GetTemporaryLocal(typeof(int), false);
				il.LdcI4(0); // ConversionStrictness.ImplExactMatch
				il.Stloc(overloadStrictness);

				// alloc local variables
				LocalBuilder[] formals = new LocalBuilder[current_overload.ParamCount];
				formalOverloadParams.Add(formals);
				// convert parameters and tests after conversion
				EmitConversions(aliases, current_overload, jump_on_error, overloadStrictness, formals, true);
				EmitConversionEpilogue(callSwitch, bestOverload, bestStrictness, i, i == (argCountOverloads.Count - 1), overloadStrictness);

				// reuse locals
				il.ReturnTemporaryLocal(overloadStrictness);
				il.MarkLabel(jump_on_error);
			}

			// call the best overload
			EmitBestOverloadSelection(argCountOverloads, callSwitch, bestOverload, bestStrictness, formalOverloadParams);
		}

		private void EmitBestOverloadSelection(List<Overload> argCountOverloads, Label callSwitch, 
			LocalBuilder bestOverload, LocalBuilder bestStrictness, List<LocalBuilder[]> formalOverloadParams)
		{
			il.MarkLabel(callSwitch);
			Label[] cases = new Label[argCountOverloads.Count];
			for (int i = 0; i < argCountOverloads.Count; i++) cases[i] = il.DefineLabel();

			il.Ldloc(bestOverload);
			il.Emit(OpCodes.Switch, cases);
			il.Emit(OpCodes.Br, noSuitableOverloadErrorLabel);

			for (int i = 0; i < argCountOverloads.Count; i++)
			{
				il.MarkLabel(cases[i]);
				Overload current_overload = argCountOverloads[i];

				EmitCall(current_overload, noSuitableOverloadErrorLabel, formalOverloadParams[i]); // TODO: we need to make sure that overload can be called earlier !!! it is too late here

				// release variables
				foreach (LocalBuilder lb in formalOverloadParams[i])
					il.ReturnTemporaryLocal(lb);
			}
		}

		private void EmitConversionEpilogue(Label callSwitch, LocalBuilder bestOverload, 
			LocalBuilder bestStrictness, int i, bool last, LocalBuilder tmpStrictness)
		{
			// if (tmpStrictness < best_strictness) 
			//	{ best_overload = i; best_strictness = tmpStrictness; }
			il.Ldloc(bestStrictness);
			il.Ldloc(tmpStrictness);
			Label endIf = il.DefineLabel();
			il.Emit(OpCodes.Ble, endIf);
			il.Ldloc(tmpStrictness);
			il.Stloc(bestStrictness);
			il.LdcI4(i);
			il.Stloc(bestOverload);
			il.MarkLabel(endIf);

			// test whether we found 'ImplExactMatch' (the best possible)
			// if (tmpStrictness == ImplExactMatch) goto call_overload_i;
			if (!last)
			{
				il.Ldloc(tmpStrictness);
				il.LdcI4((int)ConversionStrictness.ImplExactMatch);
				il.Emit(OpCodes.Beq, callSwitch);
			}
		}

		private void EmitConversions(BitArray/*!*/ aliases, Overload/*!*/ overload, Label failLabel,
			LocalBuilder overloadStrictness, LocalBuilder[] formals, bool loadAllFormals)
		{
			MethodBase overload_base = overload.Method; 
			if (!emitParentCtorCall && (overload_base.IsFamily || overload_base.IsFamilyOrAssembly))
			{
				// IF (!stack.AllowProtectedCall) THEN GOTO next-overload-or-error;
				stack.EmitLoad(il);
				il.Emit(OpCodes.Ldfld, Fields.PhpStack_AllowProtectedCall);
				il.Emit(OpCodes.Brfalse, failLabel);
			}
			if (!emitParentCtorCall && !overload_base.IsStatic && !overload_base.IsConstructor)
			{
				// IF (<instance> == null) THEN GOTO next-overload-or-error;
				instance.EmitLoad(il);
				il.Emit(OpCodes.Brfalse, failLabel);
			}


			ParameterInfo[] parameters = overload.Parameters;
			int val_counter = 0, ref_counter = 0;

			bool overload_is_vararg = ((overload.Flags & ClrMethod.OverloadFlags.IsVararg) == ClrMethod.OverloadFlags.IsVararg);
			bool last_param_is_ambiguous_vararg = (overload_is_vararg && parameters.Length == aliases.Length);

			Type params_array_element_type = null;

			for (int arg_index = 0; arg_index < aliases.Length; arg_index++)
			{
				// ambiguous_vararg = true iff this is the trailing nth [ParamsArray] parameter and we've been
				// given exactly n arguments - we can accept either the array or one array element
				bool vararg = false, ambiguous_vararg = false;

				Type formal_param_type;
				if (arg_index < parameters.Length)
				{
					formal_param_type = parameters[arg_index].ParameterType;
					if (formal_param_type.IsByRef) formal_param_type = formal_param_type.GetElementType();

					// if current parameter is [params] array, set vararg to true
					if (arg_index + 1 == parameters.Length) 
					{
						ambiguous_vararg = last_param_is_ambiguous_vararg;
						vararg = overload_is_vararg;
					}
				}
				else formal_param_type = null;

				// LOAD <actual arg #arg_index>
				#region Load value or reference depending on parameter in/out settings
				PhpTypeCode php_type_code;

				if (aliases[arg_index])
				{
					if (arg_index >= parameters.Length || !parameters[arg_index].IsOut)
					{
						il.Ldloc(refLocals[ref_counter++]);
						php_type_code = PhpTypeCode.PhpReference;
					}
					else
					{
						// TODO: Completely ignoring actual arg type passed to out params - questionable
						formals[arg_index] = il.GetTemporaryLocal(formal_param_type);
						ref_counter++;
						continue;
					}
				}
				else
				{
					il.Ldloc(valLocals[val_counter++]);
					php_type_code = PhpTypeCode.Object;
				}

				#endregion

				// Switch to mode when parameters are stored in [params] array 
				// (unless we need to try conversion to array first - in case of ambigous vararg)
				if (formal_param_type != null && vararg && !ambiguous_vararg)
				{
					Debug.Assert(formal_param_type.IsArray);

					formals[arg_index] = il.GetTemporaryLocal(formal_param_type); // declare local of the vararg array type
					params_array_element_type = formal_param_type.GetElementType();
					EmitCreateParamsArray(params_array_element_type, formals[arg_index], aliases.Length - arg_index);
					formal_param_type = null;
				}

				// formal = CONVERT(stack, out success);
				bool ct_ok = EmitConvertToClr(il, php_type_code, formal_param_type ?? params_array_element_type, strictness);

				#region Store converted value in local variable or [params] array
				// Store returned value in local variable
				if (formal_param_type != null)
				{
					formals[arg_index] = il.GetTemporaryLocal(formal_param_type); // declare local of the formal param type
					il.Stloc(formals[arg_index]);
				}

				// Store returned value in [params] array
				if (formal_param_type == null)
				{
					Debug.Assert(overload_is_vararg);

					// _params[n] = formal
					LocalBuilder temp = il.GetTemporaryLocal(params_array_element_type, true);
					il.Stloc(temp);
					il.Ldloc(formals[parameters.Length - 1]);
					il.LdcI4(arg_index - parameters.Length + 1);
					il.Ldloc(temp);
					il.Stelem(params_array_element_type);
				}
				#endregion

				if (!ct_ok)
				{
					if (ambiguous_vararg)
					{
						// if the conversion to array has failed, we should try to convert it to the array element
						// this bypasses standard "strictness" handling because type can't be convertible to A and A[] at one time..
						Debug.Assert(parameters[arg_index].IsDefined(typeof(ParamArrayAttribute), false));
						EmitConversionToAmbiguousVararg(arg_index, formal_param_type, strictness, php_type_code,
							(php_type_code == PhpTypeCode.PhpReference ? refLocals[ref_counter - 1] : valLocals[val_counter - 1]), formals);
					}

					// if (strictness == Failed) goto error;
					// strictness_i += strictness
					il.Ldloc(strictness);
					il.LdcI4((int)ConversionStrictness.Failed);
					il.Emit(OpCodes.Beq, failLabel);
					
					il.Ldloc(overloadStrictness);
					il.Ldloc(strictness);
					il.Emit(OpCodes.Add);
					il.Stloc(overloadStrictness);
				}
			}

			if (loadAllFormals && parameters.Length > aliases.Length)
			{
				// one more params argument left -> add empty array
				int arg_index = aliases.Length;
				Type formal_param_type = parameters[arg_index].ParameterType;

				Debug.Assert(arg_index + 1 == parameters.Length);
				Debug.Assert(parameters[arg_index].IsDefined(typeof(ParamArrayAttribute), false));
				Debug.Assert(formal_param_type.IsArray);

				formals[arg_index] = il.GetTemporaryLocal(formal_param_type);
				EmitCreateParamsArray(formal_param_type.GetElementType(), formals[arg_index], 0);
			}
		}

		private void EmitConversionToAmbiguousVararg(int argIndex, Type/*!*/ formalParamType, LocalBuilder/*!*/ tmpStrictness,
			PhpTypeCode argLocalTypeCode, LocalBuilder/*!*/ argLocal, LocalBuilder[] formals)
		{
			Debug.Assert(formalParamType.IsArray);
			Type element_type = formalParamType.GetElementType();
			Label success_label = il.DefineLabel();

			// IF (overloadStrictness == ImplExactMatch) GOTO <success_label>
			il.Ldloc(tmpStrictness); 
			il.LdcI4((int)ConversionStrictness.ImplExactMatch);
			il.Emit(OpCodes.Beq, success_label);

			// formal = new ELEMENT_TYPE[1] { CONVERT(stack, out strictness) };
			EmitCreateParamsArray(element_type, formals[argIndex], 1);

			il.Ldloc(formals[argIndex]);
			il.LdcI4(0);

			// reload the argument
			il.Ldloc(argLocal);

			bool ct_ok = EmitConvertToClr(il, argLocalTypeCode, element_type, tmpStrictness);
			il.Stelem(element_type);
			il.MarkLabel(success_label, true);
		}

		#endregion

		#region EmitConvertToClr

		/// <summary>
		/// Converts a PHP value to the given CLR type that is a generic parameter.
		/// </summary>
		private static void EmitConvertToClrGeneric(ILEmitter/*!*/ il, Type/*!*/ formalType, LocalBuilder/*!*/ strictnessLocal)
		{
			Debug.Assert(formalType.IsGenericParameter);

			// f...ing GenericTypeParameterBuilder will not allow us to read its attributes and constraints :(
			if (!(formalType is GenericTypeParameterBuilder))
			{
				GenericParameterAttributes attrs = formalType.GenericParameterAttributes;
				if (Reflection.Enums.GenericParameterAttrTest(attrs, GenericParameterAttributes.NotNullableValueTypeConstraint))
				{
					// we know that we are converting to a value type
					il.Ldloca(strictnessLocal);
					il.Emit(OpCodes.Call, Methods.ConvertToClr.TryObjectToStruct.MakeGenericMethod(formalType));
					return;
				}

				Type[] constraints = formalType.GetGenericParameterConstraints();
				for (int i = 0; i < constraints.Length; i++)
				{
					if (constraints[i].IsClass)
					{
						if (!constraints[i].IsArray && !typeof(Delegate).IsAssignableFrom(constraints[i]))
						{
							// we know that we are converting to a class that is not an array nor a delegate
							il.Ldloca(strictnessLocal);
							il.Emit(OpCodes.Call, Methods.ConvertToClr.TryObjectToClass.MakeGenericMethod(formalType));
							return;
						}
						else break;
					}
				}
			}

			// postpone the conversion to runtime
			il.Ldloca(strictnessLocal);
			il.Emit(OpCodes.Call, Methods.Convert.TryObjectToType.MakeGenericMethod(formalType));
		}

		/// <summary>
		/// Converts a PHP value to the given CLR type (the caller is not interested in the success of the conversion).
		/// </summary>
		public static void EmitConvertToClr(ILEmitter/*!*/ il, PhpTypeCode typeCode, Type/*!*/ formalType)
		{
			EmitConvertToClr(il, typeCode, formalType, il.GetTemporaryLocal(typeof(ConversionStrictness), true));
		}

		/// <summary>
		/// Converts a PHP value to the given CLR type (the caller passes a <paramref name="strictnessLocal"/> that will
		/// receive one of the <see cref="PHP.Core.ConvertToClr.ConversionStrictness"/> enumeration values that
		/// describe the conversion result (the Failed value indicates that conversion was not successful).
		/// </summary>
		/// <returns><B>True</B> if it the conversion will surely succeed.</returns>
		internal static bool EmitConvertToClr(ILEmitter/*!*/ il, PhpTypeCode typeCode,
			Type/*!*/ formalType, LocalBuilder/*!*/ strictnessLocal)
		{
			Debug.Assert(strictnessLocal.LocalType == typeof(ConversionStrictness));

			// preprocess the value according to the PHP type code
			switch (typeCode)
			{
				case PhpTypeCode.PhpReference:
					{
						// dereference
						il.Emit(OpCodes.Ldfld, Fields.PhpReference_Value);
						typeCode = PhpTypeCode.Object;
						break;
					}

				case PhpTypeCode.ObjectAddress:
					{
						// dereference
						il.Emit(OpCodes.Ldind_Ref);
						typeCode = PhpTypeCode.Object;
						break;
					}

				case PhpTypeCode.LinqSource:
				case PhpTypeCode.PhpRuntimeChain:
					{
						Debug.Fail();
						return true;
					}
			}

			// special treatment for generic parameters
			if (formalType.IsGenericParameter)
			{
				EmitConvertToClrGeneric(il, formalType, strictnessLocal);
				return false;
			}

			// convert CLR type
			return EmitConvertObjectToClr(il, typeCode, formalType, strictnessLocal);
		}

		/// <summary>
		/// Converts object to CLR type
		/// </summary>
		private static bool EmitConvertObjectToClr(ILEmitter il, PhpTypeCode typeCode, Type formalType, LocalBuilder strictnessLocal)
		{
			MethodInfo convert_method = null;
			switch (Type.GetTypeCode(formalType))
			{
				case TypeCode.Boolean: if (typeCode != PhpTypeCode.Boolean)
						convert_method = Methods.ConvertToClr.TryObjectToBoolean; break;
				case TypeCode.Int32: if (typeCode != PhpTypeCode.Integer)
						convert_method = Methods.ConvertToClr.TryObjectToInt32; break;
				case TypeCode.Int64: if (typeCode != PhpTypeCode.LongInteger)
						convert_method = Methods.ConvertToClr.TryObjectToInt64; break;
				case TypeCode.Double: if (typeCode != PhpTypeCode.Double)
						convert_method = Methods.ConvertToClr.TryObjectToDouble; break;
				case TypeCode.String: if (typeCode != PhpTypeCode.String)
						convert_method = Methods.ConvertToClr.TryObjectToString; break;

				case TypeCode.SByte: convert_method = Methods.ConvertToClr.TryObjectToInt8; break;
				case TypeCode.Int16: convert_method = Methods.ConvertToClr.TryObjectToInt16; break;
				case TypeCode.Byte: convert_method = Methods.ConvertToClr.TryObjectToUInt8; break;
				case TypeCode.UInt16: convert_method = Methods.ConvertToClr.TryObjectToUInt16; break;
				case TypeCode.UInt32: convert_method = Methods.ConvertToClr.TryObjectToUInt32; break;
				case TypeCode.UInt64: convert_method = Methods.ConvertToClr.TryObjectToUInt64; break;
				case TypeCode.Single: convert_method = Methods.ConvertToClr.TryObjectToSingle; break;
				case TypeCode.Decimal: convert_method = Methods.ConvertToClr.TryObjectToDecimal; break;
				case TypeCode.Char: convert_method = Methods.ConvertToClr.TryObjectToChar; break;
				case TypeCode.DateTime: convert_method = Methods.ConvertToClr.TryObjectToDateTime; break;
				case TypeCode.DBNull: convert_method = Methods.ConvertToClr.TryObjectToDBNull; break;

				case TypeCode.Object:
					{
						if (formalType.IsValueType)
						{
							if (formalType.IsGenericType && NullableType == formalType.GetGenericTypeDefinition())
							{
								// This is an ugly corner case (using generic TryObjectToStruct wouldn't work, because
								// for nullables .IsValueType returns true, but it doesn't match "T : struct" constraint)!
								// We have to try converting object to Nullable<T> first and then to T
								// (which requires a new call to 'EmitConvertObjectToClr') 
								Type nullableArg = formalType.GetGenericArguments()[0];
								Type nullableType = NullableType.MakeGenericType(nullableArg);
								
								LocalBuilder tmpVar = il.DeclareLocal(typeof(object));
								
								// This succeeds only for exact match
								il.Emit(OpCodes.Call, Methods.ConvertToClr.UnwrapNullable);
								il.Emit(OpCodes.Dup);
								il.Stloc(tmpVar);

								// <stack_0> = tmpVar = UnwrapNullable(...)
								// if (<stack_0> != null) 
								Label lblNull = il.DefineLabel(), lblDone = il.DefineLabel();
								il.Emit(OpCodes.Ldnull);
								il.Emit(OpCodes.Beq, lblNull);
								// {

								// Convert tmpVar to T and wrap it into Nullable<T>
								il.Ldloc(tmpVar);
								bool ret = EmitConvertObjectToClr(il, typeCode, nullableArg, strictnessLocal);
								// TODO: use reflection cache?
								il.Emit(OpCodes.Newobj, nullableType.GetConstructors()[0]);
								il.Emit(OpCodes.Br, lblDone);
								
								// } else /* == null */ {
								il.MarkLabel(lblNull);

								// return (T?)null;
								LocalBuilder tmpNull = il.DeclareLocal(nullableType);
								il.Ldloca(tmpNull);
								il.Emit(OpCodes.Initobj, nullableType);
								il.Ldloc(tmpNull);
								// }
								
								il.MarkLabel(lblDone);
								return ret;
							}
							else
								convert_method = Methods.ConvertToClr.TryObjectToStruct.MakeGenericMethod(formalType);
						}
						else
						{
							if (formalType.IsArray)
								convert_method = Methods.ConvertToClr.TryObjectToArray.MakeGenericMethod(formalType.GetElementType());
							else if (typeof(Delegate).IsAssignableFrom(formalType))
								convert_method = Methods.ConvertToClr.TryObjectToDelegate.MakeGenericMethod(formalType);
							else
								convert_method = Methods.ConvertToClr.TryObjectToClass.MakeGenericMethod(formalType);
						}
						break;
					}

				default:
					Debug.Fail();
					return true;
			}

			if (convert_method != null)
			{
				il.Ldloca(strictnessLocal);
				il.Emit(OpCodes.Call, convert_method);
				return false;
			}
			else return true;
		}

		#endregion

		#region EmitConvertToPhp

		/// <summary>
		/// Converts a value of the given CLR type to PHP value.
		/// </summary>
		internal static PhpTypeCode EmitConvertToPhp(ILEmitter/*!*/ il, Type/*!*/ type)
		{
			// box generic parameter
			if (type.IsGenericParameter)
			{
				il.Emit(OpCodes.Box, type);
				type = Types.Object[0];
			}

			switch (Type.GetTypeCode(type))
			{
				// primitives:
				case TypeCode.Boolean: return PhpTypeCode.Boolean;
				case TypeCode.Int32: return PhpTypeCode.Integer;
				case TypeCode.Int64: return PhpTypeCode.LongInteger;
				case TypeCode.Double: return PhpTypeCode.Double;
				case TypeCode.String: return PhpTypeCode.String;

				// coercion:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Byte:
				case TypeCode.UInt16:
					{
						il.Emit(OpCodes.Conv_I4);
						return PhpTypeCode.Integer;
					}

				case TypeCode.UInt32: EmitConstrainedCoercion(il, typeof(int), typeof(long), Int32.MaxValue); return PhpTypeCode.Object;
				case TypeCode.UInt64: EmitConstrainedCoercion(il, typeof(int), typeof(long), Int32.MaxValue); return PhpTypeCode.Object;

				case TypeCode.Single: il.Emit(OpCodes.Conv_R8); return PhpTypeCode.Double;
                case TypeCode.Char:
                    il.Emit(OpCodes.Box, type);
                    il.Emit(OpCodes.Callvirt, Methods.Object_ToString);
                    return PhpTypeCode.String;

				case TypeCode.DBNull:
					{
						il.Emit(OpCodes.Pop);
						il.Emit(OpCodes.Ldnull);
						return PhpTypeCode.Object;
					}

				case TypeCode.Decimal: // TODO: what to do with this guy?
				case TypeCode.DateTime:
					{
						il.Emit(OpCodes.Box, type);
						il.Emit(OpCodes.Call, Methods.ClrObject_Wrap);
						return PhpTypeCode.DObject;
					}

				case TypeCode.Object:
					{
						if (!typeof(IPhpVariable).IsAssignableFrom(type))
						{
							if (type.IsValueType)
								il.Emit(OpCodes.Box, type);

							il.Emit(OpCodes.Call, Methods.ClrObject_WrapDynamic);

							return PhpTypeCode.Object;
						}
						else return PhpTypeCodeEnum.FromType(type);
					}

				default:
					{
						Debug.Fail();
						return PhpTypeCode.Invalid;
					}
			}
		}

		internal static void EmitConstrainedCoercion(ILEmitter/*!*/ il, Type/*!*/ narrow, Type/*!*/ wide, object threshold)
		{
			Label else_label = il.DefineLabel();
			Label endif_label = il.DefineLabel();

			il.Emit(OpCodes.Dup);

			// IF (STACK <= threshold) THEN
			il.LoadLiteral(threshold);
			il.Emit(OpCodes.Bgt_S, else_label);

			// LOAD (narrow)STACK
			il.Conv(narrow, false);
			il.Emit(OpCodes.Box, narrow);

			il.Emit(OpCodes.Br_S, endif_label);

			// ELSE
			il.MarkLabel(else_label);

			// LOAD (wide)STACK

			il.Conv(wide, false);
			il.Emit(OpCodes.Box, wide);

			// ENDIF
			il.MarkLabel(endif_label);
		}

		#endregion
	}
}
