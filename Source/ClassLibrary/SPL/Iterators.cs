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

namespace PHP.Library.SPL
{
	/// <summary>
	/// Contains iterators-related class library functions.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class SplIterators
    {
        #region iterator_apply, iterator_count, iterator_to_array

        /// <summary>
        /// Calls a function for every element in an iterator.
        /// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="caller">Class context provided by compiler.</param>
        /// <param name="iterator">The class to iterate over.</param>
        /// <param name="function">The callback function to call on every element.
        /// Note: The function must return TRUE in order to continue iterating over the iterator.</param>
        /// <returns>Returns the iteration count.</returns>
        /// <exception cref="PhpException"><paramref name="function"/> or <paramref name="iterator"/> are <B>null</B> references.</exception>
        [ImplementsFunction("iterator_apply", FunctionImplOptions.NeedsClassContext)]
        [return: CastToFalse]
        public static int Apply(ScriptContext/*!*/context, PHP.Core.Reflection.DTypeDesc caller, Iterator iterator, PhpCallback function)
        {
            return Apply(context, caller, iterator, function, null);
        }

        /// <summary>
        /// Calls a function for every element in an iterator.
        /// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="caller">Class context provided by compiler.</param>
        /// <param name="iterator">The class to iterate over.</param>
        /// <param name="function">The callback function to call on every element.
        /// Note: The function must return TRUE in order to continue iterating over the iterator.</param>
        /// <param name="args">Arguments to pass to the callback function.</param>
        /// <returns>Returns the iteration count.</returns>
        /// <exception cref="PhpException"><paramref name="function"/> or <paramref name="iterator"/> are <B>null</B> references.</exception>
        [ImplementsFunction("iterator_apply", FunctionImplOptions.NeedsClassContext)]
        [return:CastToFalse]
        public static int Apply(ScriptContext/*!*/context, PHP.Core.Reflection.DTypeDesc caller, Iterator/*!*/iterator, PhpCallback function, PhpArray args)
        {
            // check parameters:
            Debug.Assert(context != null);
            Debug.Assert(iterator != null, "Phalanger should not pass a null here.");

            if (function == null)
            {
                PhpException.ArgumentNull("function");
                return -1;
            }

            // copy args into object array:
            object[] args_array;

            if (args != null)
            {
                args_array = new object[args.Count];
                args.Values.CopyTo(args_array, 0);
            }
            else
            {
                args_array = ArrayUtils.EmptyObjects;
            }

            // iterate through the iterator:
            int n = 0;
            
            iterator.rewind(context);
            
            while (PHP.Core.Convert.ObjectToBoolean(iterator.valid(context)))
            {
                if (!PHP.Core.Convert.ObjectToBoolean(function.Invoke(caller, args_array)))
                    break;
		        n++;

		        iterator.next(context);
	        }

            // return amount of iterated elements:
            return n;
        }

        /// <summary>
        /// Count the elements in an iterator.
        /// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="iterator">The iterator being counted.</param>
        /// <returns>The number of elements in <paramref name="iterator"/>.</returns>
        [ImplementsFunction("iterator_count")]
        public static int Count(ScriptContext/*!*/context, Iterator/*!*/iterator)
        {
            // check parameters:
            Debug.Assert(context != null);
            Debug.Assert(iterator != null, "Phalanger should not pass a null here.");
            
            // iterate through the iterator:
            int n = 0;

            iterator.rewind(context);

            while (PHP.Core.Convert.ObjectToBoolean(iterator.valid(context)))
            {
                n++;
                iterator.next(context);
            }

            // return amount of iterated elements:
            return n;
        }

        /// <summary>
        /// Copy the elements of an iterator into an array.
        /// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="iterator">The iterator being copied.</param>
        /// <returns>An array containing the elements of the <paramref name="iterator"/>.</returns>
        [ImplementsFunction("iterator_to_array")]
        public static PhpArray/*!*/ToArray(ScriptContext/*!*/context, Iterator/*!*/iterator)
        {
            return ToArray(context, iterator, true);
        }

        /// <summary>
        /// Copy the elements of an iterator into an array.
        /// </summary>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <param name="iterator">The iterator being copied.</param>
        /// <param name="use_keys">Whether to use the iterator element keys as index.</param>
        /// <returns>An array containing the elements of the <paramref name="iterator"/>.</returns>
        [ImplementsFunction("iterator_to_array")]
        public static PhpArray/*!*/ToArray(ScriptContext/*!*/context, Iterator/*!*/iterator, bool use_keys/*=true*/)
        {
            // check parameters:
            Debug.Assert(context != null);
            Debug.Assert(iterator != null, "Phalanger should not pass a null here.");

            //
            var array = new PhpArray();

            // iterate through the iterator:
            iterator.rewind(context);

            while (PHP.Core.Convert.ObjectToBoolean(iterator.valid(context)))
            {
                object value = iterator.current(context);   // PHP calls current() first
                if (use_keys)
                    array[iterator.key(context)] = value;
                else
                    array.Add(value);

                iterator.next(context);
            }

            // return amount of iterated elements:
            return array;
        }

        #endregion
    }
}
