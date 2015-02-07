/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using PHP.Core.Reflection;

namespace PHP.Core
{
	/// <summary>
	/// A stack used to perform indirect calls of user funcions and to call argument-aware functions.
	/// </summary>
	/// <remarks>
	/// <para>
	/// A stack is used for calling m-decl and args-aware PHP functions/methods.
	/// 1. Caller of such function pushes all arguments into this stack, sets ArgCount
	/// to the argument count pushed and calls argument-less overload. 
	/// If a method is called a caller invokes
	/// Operators.InvokeMethod method and this one calls argument-less overload.
	/// 2. Argument-less overload reads ArgCount arguments from the stack, pushes 
	/// them to the evalution stack and calls argument-full overload.
	/// If the callee is args-aware function/method arguments are kept on the stack
	/// until callee returns and are popped afterwards. Moreover, the number of arguments
	/// is boxed and pushed on the top of the stack in order to be used by 
	/// class library funcions working with arguments (such are func_get_args etc.).
	/// Otherwise, if the function/method is args-unaware, stack frame is deleted before
	/// the function/method is called.  
	/// </para>
	/// <para>
	/// Protocol for args-unaware excluding Class Library Stubs: 
	///   AddFrame; arg-less { (PeekValue + PeekReference + PeekValueOptional + PeekReferenceOptional)*; RemoveFrame; arg-full; }
	/// Protocol for Class Library stubs: 
	///   AddFrame; arg-less { (PeekValueUnchecked + PeekReferenceUnchecked)*; RemoveFrame; arg-full; }
	/// Protocol for args-aware:   
	///   AddFrame; arg-less { (PeekValue + PeekReference + PeekValueOptional + PeekReferenceOptional)*; MakeArgsAware; arg-full; RemoveArgsAwareFrame; }
	/// </para>
	/// </remarks>
	[DebuggerNonUserCode]
	public sealed class PhpStack
	{
		#region Fields, Construction, ResizeItems

		/// <summary>
		/// Array representing stack.
		/// </summary>
		private object[]/*!*/ Items;

		/// <summary>
		/// Array representing generic parameters.
		/// </summary>
		private DTypeDesc[]/*!*/ Types;

		/// <summary>
		/// The <see cref="ScriptContext"/> which the stack is associated to.
		/// </summary>
		[Emitted]
		public readonly ScriptContext/*!*/ Context;

		/// <summary>
		/// An index of the current top item + 1 (points immediately above the top item). 
		/// </summary>
		private int Top;

		private int TypesTop;

		/// <summary>
		/// Creates a new instance of <see cref="PhpStack"/>.
		/// </summary>
		/// <param name="context">The script context.</param>
		internal PhpStack(ScriptContext/*!*/ context)
		{
			Debug.Assert(context != null);

			this.Items = new object[25];
			this.Types = new DTypeDesc[10];
			this.Top = 0;
			this.TypesTop = 0;
			this.Context = context;
		}

		/// <summary>
		/// ResizeItemss args stack to maximum of a given size and a double of the current size.
		/// </summary>
		/// <param name="size">The minimal required size.</param>
		private void ResizeItems(int size)
		{
			int new_size = (size > 2 * Items.Length) ? size : 2 * Items.Length;

			// doubles the items array:
			object[] new_items = new object[new_size];
			Array.Copy(Items, 0, new_items, 0, Top);
			Items = new_items;
		}

		/// <summary>
		/// ResizeItemss types stack to maximum of a given size and a double of the current size.
		/// </summary>
		/// <param name="size">The minimal required size.</param>
		private void ResizeTypes(int size)
		{
			int new_size = (size > 2 * Items.Length) ? size : 2 * Items.Length;

			// doubles the items array:
			DTypeDesc[] new_types = new DTypeDesc[new_size];
			Array.Copy(Types, 0, new_types, 0, Top);
			Types = new_types;
		}

		[Conditional("DEBUG")]
		public void Dump(TextWriter/*!*/ output)
		{
			output.WriteLine("<pre>");
			output.WriteLine("Stack (args_Length = {0}, types_Length = {5}, args_Top = {1}, types_Top = {6}, " +
				"argc = {2}, typec = {7}, callee = {3}, callback = {4}):", Items.Length, Top, ArgCount, CalleeName, Callback,
				Types.Length, TypesTop, TypeArgCount);

			output.WriteLine("Args:");

			for (int i = 0; i < Top; i++)
			{
				output.WriteLine("{0}:", i);
				PhpVariable.Dump(output, Items[i]);
			}

			output.WriteLine("Types:");

			for (int i = 0; i < TypesTop; i++)
			{
				output.WriteLine("{0}:", i);
				output.WriteLine(Types[i]);
			}

			output.WriteLine("</pre>");
		}

		#endregion

		#region Call State

		/// <summary>
		/// Data used in arg-less stub during a single call.
		/// </summary>
		internal struct CallState
		{
			/// <summary>
			/// The number of items in the last stack frame.
			/// </summary>
			public int ArgCount;

			/// <summary>
			/// The number of types in the last stack frame.
			/// </summary>
			public int TypeCount;

			/// <summary>
			/// Defined variables.
			/// </summary>
			public Dictionary<string, object> Variables;

			/// <summary>
			/// Defined variables.
			/// </summary>
			public NamingContext NamingContext;

			/// <summary>
			/// The name of called function or method. Set up before peeking of values or references.
			/// Used for error reporting.
			/// </summary>
			public string CalleeName;

			/// <summary>
			/// Set by PhpCallback.Invoke if a function is called via a callback. 
			/// Changes slightly the behavior of method <see cref="PeekReference"/>.
			/// </summary>
			public bool Callback;

			public bool AllowProtectedCall;

            public DTypeDesc LateStaticBindType;

			public CallState(int argCount, int typeCount, Dictionary<string, object> variables, NamingContext namingContext,
				string calleeName, bool callback, bool allowProtectedCall, DTypeDesc lateStaticBindType)
			{
				this.ArgCount = argCount;
				this.TypeCount = typeCount;
				this.Variables = variables;
				this.NamingContext = namingContext;
				this.CalleeName = calleeName;
				this.Callback = callback;
				this.AllowProtectedCall = allowProtectedCall;
                this.LateStaticBindType = lateStaticBindType;
			}
		}

		/// <summary>
		/// The number of items in the last stack frame.
		/// </summary>
		[Emitted]
		public int ArgCount;

		/// <summary>
		/// The number of items in the last stack frame.
		/// </summary>
		public int TypeArgCount;

		/// <summary>
		/// Defined variables.
		/// </summary>
		[Emitted]
		public Dictionary<string, object> Variables;

		/// <summary>
		/// The name of called function or method. Set up before peeking of values or references.
		/// Used for error reporting.
		/// </summary>
		[Emitted]
		public string CalleeName;

		/// <summary>
		/// Set by PhpCallback.Invoke if a function is called via a callback. 
		/// Changes slightly the behavior of method <see cref="PeekReference"/>.
		/// </summary>
		public bool Callback;

        /// <summary>
        /// Type used to call currently evaluated method.
        /// </summary>
        [Emitted]
        public DTypeDesc LateStaticBindType;

		[Emitted]
		public NamingContext NamingContext;

		[Emitted]
		public bool AllowProtectedCall;

		internal CallState SaveCallState()
		{
			return new CallState(ArgCount, TypeArgCount, Variables, NamingContext, CalleeName, Callback, AllowProtectedCall, LateStaticBindType);
		}

		internal void RestoreCallState(CallState old)
		{
			TypeArgCount = old.TypeCount;
			ArgCount = old.ArgCount;
			Callback = old.Callback;
			CalleeName = old.CalleeName;
			Variables = old.Variables;
			NamingContext = old.NamingContext;
			AllowProtectedCall = old.AllowProtectedCall;
            LateStaticBindType = old.LateStaticBindType;
		}

		#endregion

		#region User Argument Access

		/// <summary>
		/// Retrieves the number of arguments passed to the current user-function.
		/// </summary>
		/// <returns><B>True</B> on success, <B>false</B> if called from outside of user-function context.</returns>
		/// <exception cref="PhpException">If called from outside of user-function context (Warning).</exception>
		public bool GetArgCount(out int argCount, out int typeArgCount)
		{
			// if stack is empty:
			if (Top == 0)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("no_function_context"));
				argCount = typeArgCount = 0;
				return false;
			}

			// fetches the number of arguments from the stack's top;
			// each args-aware function should push it there by MakeArgsAware;
			// Items[Top] = null, Items[Top - 1] = <arg count>, Items[Top - 2] = <1st arg>, ...
			// <arg count> encodes type arg count and regular arg count {type #} << 16 | {regular #};
			int encoded_result = (int)Items[Top - 1];

			argCount = encoded_result & 0xffff;
			typeArgCount = encoded_result >> 16;

			Debug.Assert(argCount >= 0 && argCount <= Top);
			Debug.Assert(typeArgCount >= 0 && typeArgCount <= TypesTop);

			return true;
		}

		/// <summary>
		/// Retrieves an argument passed to the current user-function.
		/// </summary>
		/// <param name="index">The index of the argument to get (starting from zero).</param>
		/// <returns>
		/// The value of the <paramref name="index"/>-th argument or <b>false</b> on error.
		/// The value is returned as is, i.e. no copy is made. That should be done by library function.
		/// </returns>
		/// <exception cref="PhpException">If <paramref name="index"/> is negative (Warning).</exception>
		/// <exception cref="PhpException">If <paramref name="index"/> is greater than the current 
		/// user-function's actual parameter count (Warning).</exception>
		/// <exception cref="PhpException">If called from outside of user-function context (Warning).</exception>
		public object GetArgument(int index)
		{
			// checks correctness of the argument:
			if (index < 0)
			{
				PhpException.InvalidArgument("index", "arg:negative");
				return false;
			}

			int arg_count, type_arg_count;
			if (!GetArgCount(out arg_count, out type_arg_count))
				return false;

			// invalid argument:
			if (index >= arg_count)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("argument_not_passed_to_function", index));
				return false;
			}

			// Items[Top] = null, Items[Top - 1] = <arg count>, Items[Top - 2] = <1st arg>, ...
			return Items[Top - 2 - index];
		}

		public DTypeDesc GetTypeArgument(int index)
		{
			// checks correctness of the argument:
			if (index < 0)
			{
				PhpException.InvalidArgument("index", "arg:negative");
				return null;
			}

			int arg_count, type_arg_count;
			if (!GetArgCount(out arg_count, out type_arg_count))
				return null;

			// invalid argument:
			if (index >= type_arg_count)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("type_argument_not_passed_to_function", index));
				return null;
			}

			// Types[TypesTop] = null, Types[TypesTop - 1] = <1st arg>, Types[TypesTop - 2] = <2nd arg>, ...
			return Types[TypesTop - 1 - index];
		}

		/// <summary>
		/// Returns an array of arguments of the current user-defined function. 
		/// </summary>
		/// <returns>
		/// The array of arguments which values contains arguments' values and keys are their indices or
		/// <b>null</b> on error.
		/// Values in array are returned as is, i.e. no copy is made. That should be done by library function.
		/// </returns>
		/// <exception cref="PhpException">If called from outside of user-function context (Warning).</exception>
		public PhpArray GetArguments()
		{
			int arg_count, type_arg_count;

			if (!GetArgCount(out arg_count, out type_arg_count))
				return null;

			// fills an array: 
			// Items[Top] = null, Items[Top - 1] = <arg count>, Items[Top - 2] = <1st arg>, ...
			PhpArray result = new PhpArray(arg_count, 0);
			for (int i = 0; i < arg_count; i++)
				result[i] = Items[Top - 2 - i];

			return result;
		}

		public DTypeDesc[] GetTypeArguments()
		{
			int arg_count, type_arg_count;

			if (!GetArgCount(out arg_count, out type_arg_count))
				return null;

			// fills an array: 
			// Types[TypesTop] = null, Types[TypesTop - 1] = <1st arg>, Types[TypesTop - 2] = <2nd arg>, ...
			DTypeDesc[] result = new DTypeDesc[type_arg_count];
			for (int i = 0; i < type_arg_count; i++)
				result[i] = Types[TypesTop - 1 - i];

			return result;
		}

		#endregion

		#region AddFrame

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		[Emitted]
		public void AddFrame()
		{
			ArgCount = 0;
			TypeArgCount = 0;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		[Emitted]
		public void AddFrame(object arg)
		{
			int new_top = Top + 1;
			ArgCount = 1;
			TypeArgCount = 0;

			if (new_top > Items.Length) ResizeItems(new_top);
			Items[Top] = arg;
			Top = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		[Emitted]
		public void AddFrame(object arg1, object arg2)
		{
			int new_top = Top + 2;
			ArgCount = 2;
			TypeArgCount = 0;

			if (new_top > Items.Length) ResizeItems(new_top);
			Items[Top + 1] = arg1;
			Items[Top + 0] = arg2;
			Top = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		[Emitted]
		public void AddFrame(object arg1, object arg2, object arg3)
		{
			int new_top = Top + 3;
			ArgCount = 3;
			TypeArgCount = 0;

			if (new_top > Items.Length) ResizeItems(new_top);
			Items[Top + 2] = arg1;
			Items[Top + 1] = arg2;
			Items[Top + 0] = arg3;
			Top = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		[Emitted]
		public void AddFrame(object arg1, object arg2, object arg3, object arg4)
		{
			int new_top = Top + 4;
			ArgCount = 4;
			TypeArgCount = 0;

			if (new_top > Items.Length) ResizeItems(new_top);
			Items[Top + 3] = arg1;
			Items[Top + 2] = arg2;
			Items[Top + 1] = arg3;
			Items[Top + 0] = arg4;
			Top = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		[Emitted]
		public void AddFrame(object arg1, object arg2, object arg3, object arg4, object arg5)
		{
			int new_top = Top + 5;
			ArgCount = 5;
			TypeArgCount = 0;

			if (new_top > Items.Length) ResizeItems(new_top);
			Items[Top + 4] = arg1;
			Items[Top + 3] = arg2;
			Items[Top + 2] = arg3;
			Items[Top + 1] = arg4;
			Items[Top + 0] = arg5;
			Top = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		[Emitted]
		public void AddFrame(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6)
		{
			int new_top = Top + 6;
			ArgCount = 6;
			TypeArgCount = 0;

			if (new_top > Items.Length) ResizeItems(new_top);
			Items[Top + 5] = arg1;
			Items[Top + 4] = arg2;
			Items[Top + 3] = arg3;
			Items[Top + 2] = arg4;
			Items[Top + 1] = arg5;
			Items[Top + 0] = arg6;
			Top = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		[Emitted]
		public void AddFrame(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7)
		{
			int new_top = Top + 7;
			ArgCount = 7;
			TypeArgCount = 0;

			if (new_top > Items.Length) ResizeItems(new_top);
			Items[Top + 6] = arg1;
			Items[Top + 5] = arg2;
			Items[Top + 4] = arg3;
			Items[Top + 3] = arg4;
			Items[Top + 2] = arg5;
			Items[Top + 1] = arg6;
			Items[Top + 0] = arg7;
			Top = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		[Emitted]
		public void AddFrame(object arg1, object arg2, object arg3, object arg4, object arg5, object arg6, object arg7, object arg8)
		{
			int new_top = Top + 8;
			ArgCount = 8;
			TypeArgCount = 0;

			if (new_top > Items.Length) ResizeItems(new_top);
			Items[Top + 7] = arg1;
			Items[Top + 6] = arg2;
			Items[Top + 5] = arg3;
			Items[Top + 4] = arg4;
			Items[Top + 3] = arg5;
			Items[Top + 2] = arg6;
			Items[Top + 1] = arg7;
			Items[Top + 0] = arg8;
			Top = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		/// <param name="args">Arguments to add to a new frame.</param>
		[Emitted]
		public void AddFrame(object[]/*!*/ args)
		{
			ArgCount = args.Length;
			TypeArgCount = 0;
			int new_top = Top + ArgCount;

			if (new_top > Items.Length) ResizeItems(new_top);

			for (int i = 0, stack_offset = new_top - 1; i < args.Length; i++, stack_offset--)
			{
				Items[stack_offset] = args[i];
			}

			Top = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddFrame"]/*'/>
		/// <param name="args">Arguments to add to a new frame.</param>
		public void AddFrame(ICollection args)
		{
			if (args == null) return;

			ArgCount = args.Count;
			TypeArgCount = 0;
			int new_top = Top + ArgCount;

			if (new_top > Items.Length) ResizeItems(new_top);

			int i = new_top;
			IEnumerator iterator = args.GetEnumerator();
			while (iterator.MoveNext())
			{
				Items[--i] = iterator.Current;
			}

			Top = new_top;
		}

		#endregion

		#region AddTypeFrame

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame()
		{
			TypeArgCount = 0;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame(DTypeDesc arg)
		{
			int new_top = TypesTop + 1;
			TypeArgCount = 1;

			if (new_top > Types.Length) ResizeTypes(new_top);
			Types[TypesTop] = arg;
			TypesTop = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame(DTypeDesc arg1, DTypeDesc arg2)
		{
			int new_top = TypesTop + 2;
			TypeArgCount = 2;

			if (new_top > Types.Length) ResizeTypes(new_top);
			Types[TypesTop + 1] = arg1;
			Types[TypesTop + 0] = arg2;
			TypesTop = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame(DTypeDesc arg1, DTypeDesc arg2, DTypeDesc arg3)
		{
			int new_top = TypesTop + 3;
			TypeArgCount = 3;

			if (new_top > Types.Length) ResizeTypes(new_top);
			Types[TypesTop + 2] = arg1;
			Types[TypesTop + 1] = arg2;
			Types[TypesTop + 0] = arg3;
			TypesTop = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame(DTypeDesc arg1, DTypeDesc arg2, DTypeDesc arg3, DTypeDesc arg4)
		{
			int new_top = TypesTop + 4;
			TypeArgCount = 4;

			if (new_top > Types.Length) ResizeTypes(new_top);
			Types[TypesTop + 3] = arg1;
			Types[TypesTop + 2] = arg2;
			Types[TypesTop + 1] = arg3;
			Types[TypesTop + 0] = arg4;
			TypesTop = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame(DTypeDesc arg1, DTypeDesc arg2, DTypeDesc arg3, DTypeDesc arg4, DTypeDesc arg5)
		{
			int new_top = TypesTop + 5;
			TypeArgCount = 5;

			if (new_top > Types.Length) ResizeTypes(new_top);
			Types[TypesTop + 4] = arg1;
			Types[TypesTop + 3] = arg2;
			Types[TypesTop + 2] = arg3;
			Types[TypesTop + 1] = arg4;
			Types[TypesTop + 0] = arg5;
			TypesTop = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame(DTypeDesc arg1, DTypeDesc arg2, DTypeDesc arg3, DTypeDesc arg4, DTypeDesc arg5, DTypeDesc arg6)
		{
			int new_top = TypesTop + 6;
			TypeArgCount = 6;

			if (new_top > Types.Length) ResizeTypes(new_top);
			Types[TypesTop + 5] = arg1;
			Types[TypesTop + 4] = arg2;
			Types[TypesTop + 3] = arg3;
			Types[TypesTop + 2] = arg4;
			Types[TypesTop + 1] = arg5;
			Types[TypesTop + 0] = arg6;
			TypesTop = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame(DTypeDesc arg1, DTypeDesc arg2, DTypeDesc arg3, DTypeDesc arg4, DTypeDesc arg5, DTypeDesc arg6, DTypeDesc arg7)
		{
			int new_top = TypesTop + 7;
			TypeArgCount = 7;

			if (new_top > Types.Length) ResizeTypes(new_top);
			Types[TypesTop + 6] = arg1;
			Types[TypesTop + 5] = arg2;
			Types[TypesTop + 4] = arg3;
			Types[TypesTop + 3] = arg4;
			Types[TypesTop + 2] = arg5;
			Types[TypesTop + 1] = arg6;
			Types[TypesTop + 0] = arg7;
			TypesTop = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame(DTypeDesc arg1, DTypeDesc arg2, DTypeDesc arg3, DTypeDesc arg4, DTypeDesc arg5, DTypeDesc arg6, DTypeDesc arg7, DTypeDesc arg8)
		{
			int new_top = TypesTop + 8;
			TypeArgCount = 8;

			if (new_top > Types.Length) ResizeItems(new_top);
			Items[TypesTop + 7] = arg1;
			Items[TypesTop + 6] = arg2;
			Items[TypesTop + 5] = arg3;
			Items[TypesTop + 4] = arg4;
			Items[TypesTop + 3] = arg5;
			Items[TypesTop + 2] = arg6;
			Items[TypesTop + 1] = arg7;
			Items[TypesTop + 0] = arg8;
			TypesTop = new_top;
		}

		/// <include file='Doc/Common.xml' path='docs/method[@name="PhpStack.AddTypeFrame"]/*'/>
		[Emitted]
		public void AddTypeFrame(DTypeDesc[]/*!*/ args)
		{
			TypeArgCount = args.Length;
			int new_top = TypesTop + TypeArgCount;

			if (new_top > Types.Length) ResizeItems(new_top);

			for (int i = 0, stack_offset = new_top - 1; i < args.Length; i++, stack_offset--)
			{
				Types[stack_offset] = args[i];
			}

			TypesTop = new_top;
		}

		#endregion

        #region ExpandFrame

        /// <summary>
        /// Adds additional arguments before arguments currently on stack.
        /// Used for expanding 'use' parameters of lambda function.
        /// </summary>
        internal void ExpandFrame(PhpArray useParams)
        {
            if (useParams != null && useParams.Count > 0)
            {
                ArgCount += useParams.Count;
                int new_top = Top + useParams.Count;

                if (new_top > Items.Length) ResizeItems(new_top);

                var stack_offset = new_top - 1;

                using (var enumerator = useParams.GetFastEnumerator())
                    while (enumerator.MoveNext())
                    {
                        Items[stack_offset--] = enumerator.CurrentValue;
                    }

                Top = new_top;
            }
        }

        #endregion

        #region RemoveFrame, MakeArgsAware, CollectFrame, AddIndirection

        /// <summary>
		/// Removes the current open args-unaware frame from the stack.
		/// </summary>
		/// <remarks>
		/// Called by args-unaware stubs before executing the arg-full function/method.
		/// </remarks>
		/// <exception cref="PhpException">Some actual arguments are missing (Warning).</exception>
		/// <exception cref="PhpException">Some actual arguments are not references and a function is not called from callback (Error).</exception>
		[Emitted]
		public void RemoveFrame()
		{
			Top -= ArgCount;
			TypesTop -= TypeArgCount;
			ArgCount = 0;
			TypeArgCount = 0;
			Callback = false;
			Variables = null;
			NamingContext = null;
            //LateStaticBindType = null;
		}

		/// <summary>
		/// Removes the closed args-aware frame from the top of the stack.
		/// </summary>
		/// <remarks>
		/// Called by args-aware stubs before returning.
		/// </remarks>
		[Emitted]
		public void RemoveArgsAwareFrame(int encodedActualCount)
		{
			Top -= (encodedActualCount & 0xffff) + 1;  // +1 for encoded args count
			TypesTop -= (encodedActualCount >> 16);
            //LateStaticBindType = null;
		}

		/// <summary>
		/// Sets the stack up so it is prepared for the arg-full overload call.
		/// Called in args-aware stubs after peeking all arguments and before calling the arg-full overload.
		/// </summary>
		/// <param name="encodedFormalCount">{type param count} * 0x1000 + {param count}.</param>
		/// <returns>The number of arguments pushed on the stack.</returns>
		/// <remarks>
		/// An args-aware stub is usually called when the caller doesn't know which arguments 
		/// are references and which not. Therefore, the stub should dereference all that 
		/// arguments which are not references. Those arguments on the stack corresponding 
		/// with formal ones are dereferenced by <see cref="PeekValue"/> and <see cref="PeekValueOptional"/>
		/// methods. Others are dereferenced here.
		/// </remarks>
		[Emitted]
		public int MakeArgsAware(int encodedFormalCount)
		{
			int param_count = encodedFormalCount & 0xffff;

			PeekAllValues(param_count);
			int encoded_args_count = TypeArgCount << 16 | ArgCount;

			// store encoded formal param count on the top of the items stack:
			if (Top + 1 > Items.Length) ResizeItems(Top + 1);
			Items[Top++] = encoded_args_count;

			ArgCount = 0;
			TypeArgCount = 0;
			Callback = false;
			Variables = null;
			NamingContext = null;
            return encoded_args_count;
		}

		/// <summary>
		/// Dereferences all arguments on the stack starting from the given one.
		/// </summary>
		/// <param name="formalParamCount">The number of formal arguments.</param>
		private void PeekAllValues(int formalParamCount)
		{
			for (int i = formalParamCount + 1; i <= ArgCount; i++)
			{
				// peeks the value:
				object result = PeekValueUnchecked(i);

				// stores the value:
				Items[Top - i] = result;
			}
		}

		/// <summary>
		/// Collects arguments of the current open frame to the new instance of <see cref="PhpArray"/> and removes the frame.
		/// Peeks all arguments as values and does no deep-copying.
		/// </summary>
		/// <returns>The array containing all arguments.</returns>
		internal PhpArray CollectFrame()
		{
			PhpArray result = new PhpArray(ArgCount, 0);

			for (int i = 1; i <= ArgCount; i++)
				result.Add(PeekValueUnchecked(i));

			RemoveFrame();

			return result;
		}

		/// <summary>
		/// Adds a level of indirection to a specified argument.
		/// Supresses checks that disables a reference to containe another reference.
		/// </summary>
		/// <param name="i">An index of argument starting from 1.</param>
		internal void AddIndirection(int i)
		{
			// preserves "no duplicate pointers" invariant:
			Items[Top - i] = new PhpReference(Items[Top - i], true);
		}

		#endregion

		#region PeekValue, PeekReference

		/// <summary>
		/// Retrieves an argument from the current frame.
		/// </summary>
		/// <param name="i">The index of the argument starting from 1 (the last pushed argument).</param>
		/// <returns>The value passed as the <paramref name="i"/>-th actual argument.</returns>
		/// <remarks>
		/// If argument is a <see cref="PhpReference"/> then it is dereferenced.
		/// Do set <see cref="CalleeName"/> before calling this method since the name is used for reporting errors.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="i"/> is invalid.</exception>
		[Emitted]
		public object PeekValue(int i)
		{
			object result;

			if (ArgCount >= i)
			{
				// peeks the value:
				result = PeekValueUnchecked(i);

				// stores the value back to the stack so that user args functions can work with it:
				Items[Top - i] = result;
			}
			else
			{
				result = null;

				// warning (can invoke user code => we have to save and restore callstate):
				CallState call_state = SaveCallState();
				PhpException.MissingArgument(i, CalleeName);
				RestoreCallState(call_state);
			}
			return result;
		}

		/// <summary>
		/// Retrieves an optional argument from the current frame.
		/// </summary>
		/// <param name="i">The index of the argument starting from 1 (the last pushed argument).</param>
		/// <returns>The value passed as the <paramref name="i"/>-th actual argument.</returns>
		/// <remarks>If argument is a <see cref="PhpReference"/> then it is dereferenced.</remarks>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="i"/> is invalid.</exception>
		[Emitted]
		public object PeekValueOptional(int i)
		{
			object result;

			if (ArgCount >= i)
			{
				// peeks the value:
				result = PeekValueUnchecked(i);

				// stores the value back to the stack so that user args functions can work with it:
				Items[Top - i] = result;
			}
			else
			{
				// default value:
				result = Arg.Default;
			}
			return result;
		}

		/// <summary>
		/// Retrieves an argument from the current frame without checking range.
		/// Used also by library arg-less stubs.
		/// </summary>
		/// <param name="i">The index of the argument starting from 1 (the last pushed argument).</param>
		/// <returns>The value passed as the <paramref name="i"/>-th actual argument.</returns>
		/// <remarks>If argument is a <see cref="PhpReference"/> then it is dereferenced.</remarks>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="i"/> is invalid.</exception>
		[Emitted]
		public object PeekValueUnchecked(int i)
		{
			PhpRuntimeChain php_chain;

			// caller may have pushed a reference even if a formal argument is not reference => dereference it:
			object result = PhpVariable.Dereference(Items[Top - i]);

			// caller may have pushed a runtime chain => evaluate it:
			if ((php_chain = result as PhpRuntimeChain) != null)
			{
				// call state has to be stored since chain can call arbitrary user code:
				CallState call_state = SaveCallState();
				result = php_chain.GetValue(Context);
				RestoreCallState(call_state);
			}

			return result;
		}

		/// <summary>
		/// Retrieves a reference argument from the current frame.
		/// </summary>
		/// <param name="i">The index of the argument starting from 1 (the last pushed argument).</param>
		/// <returns>The reference passed as the <paramref name="i"/>-th actual argument. Never <B>null</B>.</returns>
		/// <remarks>
		/// Do set <see cref="CalleeName"/> before calling this method since the name is used for reporting errors.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="i"/> is invalid.</exception>
		[Emitted]
		public PhpReference PeekReference(int i)
		{
			PhpReference result;

			if (ArgCount >= i)
			{
				// peeks the reference:
				result = PeekReferenceUnchecked(i);

				// stores the value back to the stack so that user args functions can work with it:
				Items[Top - i] = result;
			}
			else
			{
				result = new PhpReference();

				// warning (can invoke user code => we have to save and restore callstate):
				CallState call_state = SaveCallState();
				PhpException.MissingArgument(i, CalleeName);
				RestoreCallState(call_state);
			}

			return result;
		}

		/// <summary>
		/// Retrieves a reference optional argument from the current frame.
		/// </summary>
		/// <param name="i">The index of the argument starting from 1 (the last pushed argument).</param>
		/// <returns>The reference passed as the <paramref name="i"/>-th actual argument. Never <B>null</B>.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="i"/> is invalid.</exception>
		[Emitted]
		public PhpReference PeekReferenceOptional(int i)
		{
			PhpReference result;

			if (ArgCount >= i)
			{
				// peeks the reference:
				result = PeekReferenceUnchecked(i);

				// stores the value back to the stack so that user args functions can work with it:
				Items[Top - i] = result;
			}
			else
			{
				// default value:
				result = Arg.Default;
			}
			return result;
		}

		/// <summary>
		/// Peeks a reference argument from the current frame without range check. 
		/// Used by library arg-less stubs.
		/// </summary>
		/// <param name="i">The index of the argument starting from 1 (the last pushed argument).</param>
		/// <returns>The reference passed as the <paramref name="i"/>-th actual argument. Never <B>null</B>.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="i"/> is invalid.</exception>
		[Emitted]
		public PhpReference PeekReferenceUnchecked(int i)
		{
			object item = Items[Top - i];
			PhpReference result;
			PhpRuntimeChain php_chain;

			// the caller may not pushed a reference although the formal argument is a reference:
			// it doesn't matter if called by callback:
			if ((result = item as PhpReference) == null)
			{
				// caller may have pushed a runtime chain => evaluate it:
				if ((php_chain = item as PhpRuntimeChain) != null)
				{
					// call state has to be stored since chain can call arbitrary user code:
					CallState call_state = SaveCallState();
					result = php_chain.GetReference(Context);
					RestoreCallState(call_state);
				}
				else
				{
					// the reason of copy is not exactly known (it may be returning by copy as well as passing by copy):
					result = new PhpReference(PhpVariable.Copy(item, CopyReason.Unknown));

					// Reports an error in the case that we are not called by callback.
					// Although, this error is fatal one can switch throwing exceptions off.
					// If this is the case the afterwards behavior will be the same as if callback was called.
					if (!Callback)
					{
						// warning (can invoke user code => we have to save and restore callstate):
						CallState call_state = SaveCallState();

						PhpException.ArgumentNotPassedByRef(i, CalleeName);
						RestoreCallState(call_state);
					}
				}
			}
			return result;
		}

		#endregion

		#region PeekType, PeekTypeOptional

		/// <summary>
		/// Retrieves a type argument from the current frame.
		/// </summary>
		/// <param name="i">The index of the type argument starting from 1 (the last pushed argument).</param>
		/// <returns>The <see cref="DTypeDesc"/> passed as the <paramref name="i"/>-th actual type argument.</returns>
		/// <remarks>
		/// Do set <see cref="CalleeName"/> before calling this method since the name is used for reporting errors.
		/// </remarks>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="i"/> is invalid.</exception>
		[Emitted]
		public DTypeDesc/*!*/ PeekType(int i)
		{
			if (TypeArgCount >= i)
			{
				// peeks the value:
				return Types[TypesTop - i];
			}
			else
			{
				// warning (can invoke user code => we have to save and restore callstate):
				CallState call_state = SaveCallState();
				PhpException.MissingTypeArgument(i, CalleeName);
				RestoreCallState(call_state);

				return DTypeDesc.ObjectTypeDesc;
			}
		}

		/// <summary>
		/// Retrieves an optional type argument from the current frame.
		/// </summary>
		/// <param name="i">The index of the argument starting from 1 (the last pushed argument).</param>
		/// <returns>The value passed as the <paramref name="i"/>-th actual argument.</returns>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="i"/> is invalid.</exception>
		[Emitted]
		public DTypeDesc/*!*/ PeekTypeOptional(int i)
		{
			return (TypeArgCount >= i) ? Types[TypesTop - i] : Arg.DefaultType;
		}

		#endregion

        #region ThrowIfNotArgsaware

        /// <summary>
        /// Check whether current <see cref="CalleeName"/> matches currently called function.
        /// </summary>
        /// <param name="routineName">Currently called function name.</param>
        /// <exception cref="InvalidOperationException">If currently caled function does not match <see cref="CalleeName"/>.</exception>
        public void ThrowIfNotArgsaware(string/*!*/routineName)
        {
            //if (CalleeName != routineName)
            //if (Top == 0 && CalleeName == null)
            //    throw new InvalidOperationException(string.Format(CoreResources.argsaware_routine_needs_args, routineName));
        }

        #endregion
    }
}
