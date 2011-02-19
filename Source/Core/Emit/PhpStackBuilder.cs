/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace PHP.Core.Emit
{
	/// <summary>
	/// Methods used for emitting <see cref="PhpStack"/> operations.
	/// </summary>
	internal static class PhpStackBuilder
	{
		public static void EmitAddFrame(ILEmitter/*!*/ il, IPlace/*!*/ scriptContextPlace, int typeArgCount, int argCount,
		  Action<ILEmitter, int> typeArgEmitter, Action<ILEmitter, int>/*!*/ argEmitter)
		{
			Debug.Assert(typeArgCount == 0 || typeArgEmitter != null);

			// type args:
			if (typeArgCount > 0)
			{
				scriptContextPlace.EmitLoad(il);
				il.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);

				il.EmitOverloadedArgs(Types.DTypeDesc[0], typeArgCount, Methods.PhpStack.AddTypeFrame.ExplicitOverloads, typeArgEmitter);
			}

			// args:
			scriptContextPlace.EmitLoad(il);
			il.Emit(OpCodes.Ldfld, Fields.ScriptContext_Stack);

			il.EmitOverloadedArgs(Types.Object[0], argCount, Methods.PhpStack.AddFrame.ExplicitOverloads, argEmitter);

			il.Emit(OpCodes.Call, Methods.PhpStack.AddFrame.Overload(argCount));

			// AddFrame adds empty type frame by default, so if there are no type parameters, we can skip AddTypeFrame call:
			if (typeArgCount > 0)
				il.Emit(OpCodes.Call, Methods.PhpStack.AddTypeFrame.Overload(typeArgCount));
		}

		public static void EmitArgFullPreCall(ILEmitter/*!*/ il, IPlace/*!*/ stack, bool argsAware,
		  int formalParamCount, int formalTypeParamCount, out LocalBuilder locArgsCount)
		{
			if (argsAware)
			{
				locArgsCount = il.DeclareLocal(typeof(int));

				// locArgsCount = stack.MakeArgsAware(<formal tpye param count | formal param count>);
				stack.EmitLoad(il);
				il.LdcI4((formalTypeParamCount << 16) | formalParamCount);
				il.Emit(OpCodes.Call, Methods.PhpStack.MakeArgsAware);
				il.Stloc(locArgsCount);
			}
			else
			{
				locArgsCount = null;

				// CALL stack.RemoveFrame();
				stack.EmitLoad(il);
				il.Emit(OpCodes.Call, Methods.PhpStack.RemoveFrame);
			}
		}

		public static void EmitArgFullPostCall(ILEmitter/*!*/ il, IPlace/*!*/ stack, LocalBuilder locArgsCount)
		{
			// args-aware:
			if (locArgsCount != null)
			{
				// CALL stack.RemoveArgsAwareFrame(count);
				stack.EmitLoad(il);
				il.Ldloc(locArgsCount);
				il.Emit(OpCodes.Call, Methods.PhpStack.RemoveArgsAwareFrame);
			}
		}

		//public static MethodCallPlace/*!*/ MakePeekValuePlace(IPlace/*!*/ stack, IPlace/*!*/ index)
		//{
		//  Debug.Assert(stack != null && index != null);
		//  return new MethodCallPlace(Methods.PhpStack.PeekValue, false, stack, index);
		//}

		//public static MethodCallPlace/*!*/ MakeValuePeekUncheckedPlace(IPlace/*!*/ stack, IPlace/*!*/ index)
		//{
		//  Debug.Assert(stack != null && index != null);
		//  return new MethodCallPlace(Methods.PhpStack.PeekValueUnchecked, false, stack, index);
		//}

		//public static MethodCallPlace/*!*/ MakeReferencePeekPlace(IPlace/*!*/ stack, IPlace/*!*/ index)
		//{
		//  Debug.Assert(stack != null && index != null);
		//  return new MethodCallPlace(Methods.PhpStack.PeekReference, false, stack, index);
		//}

		//public static MethodCallPlace/*!*/ MakeReferencePeekUncheckedPlace(IPlace/*!*/ stack, IPlace/*!*/ index)
		//{
		//  Debug.Assert(stack != null && index != null);
		//  return new MethodCallPlace(Methods.PhpStack.PeekReferenceUnchecked, false, stack, index);
		//}


		public static object EmitValuePeek(ILEmitter/*!*/ il, IPlace/*!*/ stack, IPlace/*!*/ index)
		{
			Debug.Assert(il != null && stack != null && index != null);

			// CALL stack.PeekValue(<index+1>);
			stack.EmitLoad(il);
			index.EmitLoad(il);
			il.Emit(OpCodes.Call, Methods.PhpStack.PeekValue);

			return typeof(object);
		}

		public static object EmitValuePeekUnchecked(ILEmitter/*!*/ il, IPlace/*!*/ stack, IPlace/*!*/ index)
		{
			Debug.Assert(il != null && stack != null && index != null);

			// CALL stack.PeekValueUnchecked(<index+1>);
			stack.EmitLoad(il);
			index.EmitLoad(il);
			il.Emit(OpCodes.Call, Methods.PhpStack.PeekValueUnchecked);

			return typeof(object);
		}

		public static object EmitReferencePeek(ILEmitter/*!*/ il, IPlace/*!*/ stack, IPlace/*!*/ index)
		{
			Debug.Assert(il != null && stack != null && index != null);

			// LOAD stack.PeekReference(<index+1>);
			stack.EmitLoad(il);
			index.EmitLoad(il);
			il.Emit(OpCodes.Call, Methods.PhpStack.PeekReference);

			return typeof(PhpReference);
		}

		public static object EmitReferencePeekUnchecked(ILEmitter/*!*/ il, IPlace/*!*/ stack, IPlace/*!*/ index)
		{
			Debug.Assert(il != null && stack != null && index != null);

			// LOAD stack.PeekReferenceUnchecked(<index+1>);
			stack.EmitLoad(il);
			index.EmitLoad(il);
			il.Emit(OpCodes.Call, Methods.PhpStack.PeekReferenceUnchecked);

			return typeof(PhpReference);
		}

		public static object EmitValuePeek(ILEmitter/*!*/ il, int index, object/*!*/ stackPlace)
		{
			Debug.Assert(il != null && stackPlace != null);

			// CALL stack.PeekValue(<index+1>);
			((IPlace)stackPlace).EmitLoad(il);
			il.LdcI4(index + 1);
			il.Emit(OpCodes.Call, Methods.PhpStack.PeekValue);

			return typeof(object);
		}

		public static object EmitValuePeekUnchecked(ILEmitter/*!*/ il, int index, object/*!*/ stackPlace)
		{
			Debug.Assert(il != null && stackPlace != null);

			// CALL stack.PeekValueUnchecked(<index+1>);
			((IPlace)stackPlace).EmitLoad(il);
			il.LdcI4(index + 1);
			il.Emit(OpCodes.Call, Methods.PhpStack.PeekValueUnchecked);

			return typeof(object);
		}

		public static object EmitReferencePeek(ILEmitter/*!*/ il, int index, object/*!*/ stackPlace)
		{
			Debug.Assert(il != null && stackPlace != null);

			// LOAD stack.PeekReference(<index+1>);
			((IPlace)stackPlace).EmitLoad(il);
			il.LdcI4(index + 1);
			il.Emit(OpCodes.Call, Methods.PhpStack.PeekReference);

			return typeof(PhpReference);
		}

		public static object EmitReferencePeekUnchecked(ILEmitter/*!*/ il, int index, object/*!*/ stackPlace)
		{
			Debug.Assert(il != null && stackPlace != null);

			// LOAD stack.PeekReferenceUnchecked(<index+1>);
			((IPlace)stackPlace).EmitLoad(il);
			il.LdcI4(index);
			il.Emit(OpCodes.Call, Methods.PhpStack.PeekReferenceUnchecked);

			return typeof(PhpReference);
		}

		/// <summary>
		/// Emits load of an array where all optional arguments are stored.
		/// Each optional argument is peeked from the PHP stack and converted before stored to the array.
		/// The resulting array is pushed on evaluation stack so it can be later passed as an argument to a method.
		/// </summary>
		/// <param name="builder">The builder.</param>
		/// <param name="start">The index of the first argument to be loaded.</param>
		/// <param name="param">The last parameter of the overload (should be an array).</param>
		/// <param name="optArgCount">The place where the number of optional arguments is stored.</param>
		/// <remarks>Assumes that the non-negative number of optional arguments has been stored to 
		/// <paramref name="optArgCount"/> place.</remarks>
		public static void EmitPeekAllArguments(OverloadsBuilder/*!*/ builder, int start, ParameterInfo param, IPlace optArgCount)
		{
			Debug.Assert(start >= 0 && optArgCount != null && param != null);

			ILEmitter il = builder.IL;
			Type elem_type = param.ParameterType.GetElementType();
			Type array_type = Type.GetType(elem_type.FullName + "[]", true);
			Type actual_type;

			// declares aux. variables:
			LocalBuilder loc_array = il.DeclareLocal(array_type);
			LocalBuilder loc_i = il.DeclareLocal(typeof(int));
			LocalBuilder loc_elem = il.DeclareLocal(elem_type);

			// creates an array for the arguments 
			// array = new <elem_type>[opt_arg_count]:
			optArgCount.EmitLoad(il);
			il.Emit(OpCodes.Newarr, elem_type);
			il.Stloc(loc_array);

			Label for_end_label = il.DefineLabel();
			Label condition_label = il.DefineLabel();

			// i = 0;
			il.Emit(OpCodes.Ldc_I4_0);
			il.Stloc(loc_i);

			// FOR (i = 0; i < opt_arg_count; i++)
			if (true)
			{
				il.MarkLabel(condition_label);

				// condition (i < opt_arg_count):
				il.Ldloc(loc_i);
				optArgCount.EmitLoad(il);
				il.Emit(OpCodes.Bge, for_end_label);

				// LOAD stack, i + start+1>:
				builder.Stack.EmitLoad(il);
				il.Ldloc(loc_i);
				il.LdcI4(start + 1);
				il.Emit(OpCodes.Add);

				if (elem_type == typeof(PhpReference))
				{
					// CALL stack.PeekReferenceUnchecked(STACK);
					il.Emit(OpCodes.Call, Methods.PhpStack.PeekReferenceUnchecked);
					actual_type = typeof(PhpReference);
				}
				else
				{
					// CALL stack.PeekValueUnchecked(STACK);
					il.Emit(OpCodes.Call, Methods.PhpStack.PeekValueUnchecked);
					actual_type = typeof(object);
				}

				// emits a conversion stuff (loads result into "elem" local variable):
				builder.EmitArgumentConversion(elem_type, actual_type, false, param);
				il.Stloc(loc_elem);

				// array[i] = elem;
				il.Ldloc(loc_array);
				il.Ldloc(loc_i);
				il.Ldloc(loc_elem);
				il.Stelem(elem_type);

				// i = i + 1;
				il.Ldloc(loc_i);
				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Add);
				il.Stloc(loc_i);

				// GOTO condition;
				il.Emit(OpCodes.Br, condition_label);
			}
			// END FOR

			il.MarkLabel(for_end_label);

			// loads array to stack - consumed by the method call:
			il.Ldloc(loc_array);
		}


	}
}
