/*

 Copyright (c) 2004-2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

/* 
	NOTES:
		- compact, extract functions are implemented in PhpVariables class
    
	TODO:
		- array_map depends on call-time ampersand modification (see bug #33940)
		- array_diff_assoc - strict equality comparison
		- array_pop - it is probably buggy in PHP 5.1.2, submitted bug report.
*/

using System;
using System.Collections;
using System.Text;
using System.IO;
using System.Runtime.Serialization;
using System.ComponentModel;
using PHP.Core;
using System.Collections.Generic;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Library
{
	#region Enumerations

	/// <summary>
	/// Type of sorting.
	/// </summary>
	public enum ComparisonMethod
	{
		/// <summary>Regular comparison.</summary>
		[ImplementsConstant("SORT_REGULAR")]
		Regular = 0,

		/// <summary>Numeric comparison.</summary>
		[ImplementsConstant("SORT_NUMERIC")]
		Numeric = 1,

		/// <summary>String comparison.</summary>
		[ImplementsConstant("SORT_STRING")]
		String = 2,

		/// <summary>String comparison respecting to locale.</summary>
		[ImplementsConstant("SORT_LOCALE_STRING")]
		LocaleString = 5,

		/// <summary>Undefined comparison.</summary>
		Undefined = -1
	};

	/// <summary>
	/// Sort order.
	/// </summary>
	public enum SortingOrder
	{
		/// <summary>Descending</summary>
		[ImplementsConstant("SORT_DESC")]
		Descending = 3,

		/// <summary>Ascending</summary>
		[ImplementsConstant("SORT_ASC")]
		Ascending = 4,

		/// <summary>Undefined</summary>
		Undefined = -1
	}

	/// <summary>
	/// Whether or not the sort is case-sensitive.
	/// </summary>
	public enum LetterCase
	{
		/// <summary>Lower case.</summary>
		[ImplementsConstant("CASE_LOWER")]
		Lower = 0,

		/// <summary>Upper case.</summary>
		[ImplementsConstant("CASE_UPPER")]
		Upper = 1
	}

	#endregion

	/// <summary>
	/// Manipulates arrays and collections. 
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpArrays
	{
		/// <summary>
		/// Array which is always empty. Nobody should add any item to it.
		/// </summary>
		internal static readonly PhpArray EmptyArray = new PhpArray();

		#region reset, pos, prev, next, key, end, each

		/// <summary>
		/// Retrieves a value being pointed by an array intrinsic enumerator.
		/// </summary>
		/// <param name="array">The array which current value to return.</param>
		/// <returns><b>False</b>, if the intrinsic enumerator is behind the last item of <paramref name="array"/>, 
		/// otherwise the value being pointed by the enumerator (beware of values which are <b>false</b>!).</returns>
		/// <remarks>The value returned is dereferenced.</remarks>
		[ImplementsFunction("current")]
		[return: PhpDeepCopy]
		public static object Current([PhpRw] IPhpEnumerable array)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return null;
			}

			if (array.IntrinsicEnumerator.AtEnd) return false;

			// dereferences result since enumerator doesn't do so:
			return PhpVariable.Dereference(array.IntrinsicEnumerator.Value);
		}

		/// <summary>
		/// Retrieves a value being pointed by an array intrinsic enumerator.
		/// </summary>
		/// <param name="array">The array which current value to return.</param>
		/// <returns>
		/// <b>False</b> if the intrinsic enumerator is behind the last item of <paramref name="array"/>, 
		/// otherwise the value being pointed by the enumerator (beware of values which are <b>false</b>!).
		/// </returns>
		/// <remarks>
		/// Alias of <see cref="Current"/>. The value returned is dereferenced.
		/// </remarks>
		[ImplementsFunction("pos")]
		[return: PhpDeepCopy]
		public static object Pos([PhpRw] IPhpEnumerable array)
		{
			return Current(array);
		}

		/// <summary>
		/// Retrieves a key being pointed by an array intrinsic enumerator.
		/// </summary>
		/// <param name="array">The array which current key to return.</param>
		/// <returns>
		/// <b>Null</b>, if the intrinsic enumerator is behind the last item of <paramref name="array"/>, 
		/// otherwise the key being pointed by the enumerator.
		/// </returns>
		[ImplementsFunction("key")]
		public static object Key([PhpRw] IPhpEnumerable array)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return null;
			}

			if (array.IntrinsicEnumerator.AtEnd)
				return null;

			// note, key can't be of type PhpReference, hence no dereferencing follows:
			return ((IntStringKey)array.IntrinsicEnumerator.Key).Object;
		}

		/// <summary>
		/// Advances array intrinsic enumerator one item forward.
		/// </summary>
		/// <param name="array">The array which intrinsic enumerator to advance.</param>
		/// <returns>
		/// The value being pointed by the enumerator after it has been advanced
		/// or <b>false</b> if the enumerator has moved behind the last item of <paramref name="array"/>.
		/// </returns>
		/// <remarks>The value returned is dereferenced.</remarks>
		/// <include file='Doc/Arrays.xml' path='docs/intrinsicEnumeration/*'/>
		[ImplementsFunction("next")]
		[return: PhpDeepCopy]
		public static object Next([PhpRw] IPhpEnumerable array)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return null;
			}
			
			// moves to the next item and returns false if there is no such item:
			if (!array.IntrinsicEnumerator.MoveNext()) return false;

			// dereferences result since enumerator doesn't do so:
			return PhpVariable.Dereference(array.IntrinsicEnumerator.Value);
		}

		/// <summary>
		/// Moves array intrinsic enumerator one item backward.
		/// </summary>
		/// <param name="array">The array which intrinsic enumerator to move.</param>
		/// <returns>
		/// The value being pointed by the enumerator after it has been moved
		/// or <b>false</b> if the enumerator has moved before the first item of <paramref name="array"/>.
		/// </returns>
		/// <remarks>The value returned is dereferenced.</remarks>
		/// <include file='Doc/Arrays.xml' path='docs/intrinsicEnumeration/*'/>
		[ImplementsFunction("prev")]
		[return: PhpDeepCopy]
		public static object Prev([PhpRw] IPhpEnumerable array)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return null;
			}
			
			// moves to the previous item and returns false if there is no such item:
			if (!array.IntrinsicEnumerator.MovePrevious()) return false;

			// gets a value:
			object result = array.IntrinsicEnumerator.Value;

			// dereferences result since enumerator doesn't do so:
			return PhpVariable.Dereference(result);
		}

		/// <summary>
		/// Moves array intrinsic enumerator so it will point to the last item of the array.
		/// </summary>
		/// <param name="array">The array which intrinsic enumerator to move.</param>
		/// <returns>The last value in the <paramref name="array"/> or <b>false</b> if <paramref name="array"/> 
		/// is empty.</returns>
		/// <remarks>The value returned is dereferenced.</remarks>
		[ImplementsFunction("end")]
		[return: PhpDeepCopy]
		public static object End([PhpRw] IPhpEnumerable array)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return null;
			}
			
			// moves to the last item and returns false if there is no such item:
			if (!array.IntrinsicEnumerator.MoveLast()) return false;

			// gets a value:
			object result = array.IntrinsicEnumerator.Value;

			// dereferences result since enumerator doesn't do so:
			return PhpVariable.Dereference(result);
		}

		/// <summary>
		/// Moves array intrinsic enumerator so it will point to the first item of the array.
		/// </summary>
		/// <param name="array">The array which intrinsic enumerator to move.</param>
		/// <returns>The first value in the <paramref name="array"/> or <b>false</b> if <paramref name="array"/> 
		/// is empty.</returns>
		/// <remarks>The value returned is dereferenced.</remarks>
		[ImplementsFunction("reset")]
		[return: PhpDeepCopy]
		public static object Reset([PhpRw] IPhpEnumerable array)
		{
			if (array == null)
			{
				//PhpException.ReferenceNull("array");
				return null;
			}
			
			// moves to the last item and returns false if there is no such item:
			if (!array.IntrinsicEnumerator.MoveFirst()) return false;

			// gets a value:
			object result = array.IntrinsicEnumerator.Value;

			// dereferences result since enumerator doesn't do so:
			return PhpVariable.Dereference(result);
		}

		/// <summary>
		/// Retrieves the current entry and advances array intrinsic enumerator one item forward.
		/// </summary>
		/// <param name="array">The array which entry get and which intrinsic enumerator to advance.</param>
		/// <returns>
		/// The instance of <see cref="PhpArray"/>(0 =&gt; key, 1 =&gt; value, "key" =&gt; key, "value" =&gt; value)
		/// where key and value are pointed by the enumerator before it is advanced
		/// or <b>false</b> if the enumerator has been behind the last item of <paramref name="array"/>
		/// before the call.
		/// </returns>
		/// <include file='Doc/Arrays.xml' path='docs/intrinsicEnumeration/*'/>
		[ImplementsFunction("each")]
		[return: CastToFalse, PhpDeepCopy]
		public static PhpArray Each([PhpRw] IPhpEnumerable array)
		{
			if (array == null)
			{
				//PhpException.ReferenceNull("array");
				return null;
			}

			if (array.IntrinsicEnumerator.AtEnd)
				return null;

			DictionaryEntry entry = array.IntrinsicEnumerator.Entry;
			array.IntrinsicEnumerator.MoveNext();

			// dereferences result since enumerator doesn't do so:
			object key = ((IntStringKey)entry.Key).Object;
			object value = PhpVariable.Dereference(entry.Value);

			// creates the resulting array:
			PhpArray result = new PhpArray();
			result.Add(1, value);
			result.Add("value", value);
			result.Add(0, key);
			result.Add("key", key);

			// keys and values should be inplace deeply copied:
			result.InplaceCopyOnReturn = true;
			return result;
		}

		#endregion


		#region array_pop, array_push, array_shift, array_unshift, array_reverse


		/// <summary>
		/// Removes the last item from an array and returns it.
		/// </summary>
		/// <param name="array">The array whcih item to pop.</param>
		/// <returns>The last item of <paramref name="array"/> or a <b>null</b> reference if it is empty.</returns>
		/// <remarks>Resets intrinsic enumerator.</remarks>
		[ImplementsFunction("array_pop")]
		[return: PhpDeepCopy]
		public static object Pop([PhpRw] PhpArray array)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return null;
			}

			if (array.Count == 0) return null;

			// dereferences result since the array doesn't do so:
			object result = PhpVariable.Dereference(array.RemoveLast().Value);

			array.RefreshMaxIntegerKey();

			array.RestartIntrinsicEnumerator();
			return result;
		}

		/// <summary>
		/// Adds multiple items into an array.
		/// </summary>
		/// <param name="array">The array where to add values.</param>
		/// <param name="vars">The array of values to add.</param>
		/// <returns>The number of items in array after all items was added.</returns>
		[ImplementsFunction("array_push")]
		public static int Push([PhpRw] PhpArray array, [PhpDeepCopy] params object[] vars)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return 0;
			}

			// adds copies variables (if called by PHP):
			for (int i = 0; i < vars.Length; i++)
			{
				array.Add(vars[i]);
			}

			return array.Count;
		}

		/// <summary>
		/// Removes the first item of an array and reindex integer keys starting from zero.
		/// </summary>
		/// <param name="array">The array to be shifted.</param>
		/// <returns>The removed object.</returns>
		/// <remarks>Resets intrinsic enumerator.</remarks>
		[ImplementsFunction("array_shift")]
		[return: PhpDeepCopy]
		public static object Shift([PhpRw] PhpArray array)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return null;
			}

			if (array.Count == 0) return null;

			// dereferences result since the array doesn't do so:
			object result = PhpVariable.Dereference(array.RemoveFirst().Value);

			// reindexes integer keys starting from zero:
			array.ReindexIntegers(0);

			array.RestartIntrinsicEnumerator();
			return result;
		}

		/// <summary>
		/// Inserts specified items before the first item of an array and reindex integer keys starting from zero.
		/// </summary>
		/// <param name="array">The array to be unshifted.</param>
		/// <param name="vars">Variables to be inserted.</param>
		/// <returns>The number of items in resulting array.</returns>
		[ImplementsFunction("array_unshift")]
		public static int Unshift([PhpRw] PhpArray array, [PhpDeepCopy] params object[] vars)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return 0;
			}

			// reindexes integer keys starting from the number of items to be prepended:
			array.ReindexIntegers(vars.Length);

			// prepends items indexing keys from 0 to the number of items - 1:
			for (int i = vars.Length - 1; i >= 0; i--)
			{
				array.Prepend(i, vars[i]);
			}

			return array.Count;
		}

		/// <summary>
		/// Returns array which elements are taken from a specified one in reversed order.
		/// Integer keys are reindexed starting from zero.
		/// </summary>
		/// <param name="array">The array to be reversed.</param>
		/// <returns>The array <paramref name="array"/> with items in reversed order.</returns>
		[ImplementsFunction("array_reverse")]
		[return: PhpDeepCopy]
		public static PhpArray Reverse(PhpArray array)
		{
			return Reverse(array, false);
		}

		/// <summary>
		/// Returns array which elements are taken from a specified one in reversed order.
		/// </summary>
		/// <param name="array">The array to be reversed.</param>
		/// <param name="preserveKeys">Whether keys should be left untouched. 
		/// If set to <b>false</b> then integer keys are reindexed starting from zero.</param>
		/// <returns>The array <paramref name="array"/> with items in reversed order.</returns>
		[ImplementsFunction("array_reverse")]
		[return: PhpDeepCopy]
		public static PhpArray Reverse(PhpArray array, bool preserveKeys)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			PhpArray result = new PhpArray();

			if (preserveKeys)
			{
				// changes only the order of elements:
				foreach (KeyValuePair<IntStringKey, object> entry in array)
					result.Prepend(entry.Key, entry.Value);
			}
			else
			{
				// changes the order of elements and reindexes integer keys:
				int i = array.IntegerCount;
				foreach (KeyValuePair<IntStringKey, object> entry in array)
				{
					if (entry.Key.IsString)
						result.Prepend(entry.Key.String, entry.Value);
					else
						result.Prepend(--i, entry.Value);
				}
			}

			// if called by PHP languge then all items in the result should be inplace deeply copied:
			result.InplaceCopyOnReturn = true;
			return result;
		}

		#endregion


		#region array_slice, array_splice

		/// <summary>
		/// Retrieves a slice of specified array.
		/// </summary>
		/// <param name="array">The array which slice to get.</param>
		/// <param name="offset">The ordinal number of a first item of the slice.</param>
		/// <returns>The slice of <paramref name="array"/>.</returns>
		/// <remarks>
		/// The same as <see cref="Slice(PhpArray,int,int)"/> where <c>length</c> is infinity. 
		/// <seealso cref="PhpMath.AbsolutizeRange"/>. Resets integer keys.
		/// </remarks>
		[ImplementsFunction("array_slice")]
		[return: PhpDeepCopy]
		public static PhpArray Slice(PhpArray array, int offset)
		{
			return Slice(array, offset, int.MaxValue, false);
		}

		/// <summary>
		/// Retrieves a slice of specified array.
		/// </summary>
		/// <param name="array">The array which slice to get.</param>
		/// <param name="offset">The relativized offset of the first item of the slice.</param>
		/// <param name="length">The relativized length of the slice.</param>
		/// <returns>The slice of <paramref name="array"/>.</returns>
		/// <remarks>
		/// See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and 
		/// <paramref name="length"/>. Resets integer keys.
		/// </remarks>
		[ImplementsFunction("array_slice")]
		[return: PhpDeepCopy]
		public static PhpArray Slice(PhpArray array, int offset, int length)
		{
			return Slice(array, offset, length, false);
		}

		/// <summary>
		/// Retrieves a slice of specified array.
		/// </summary>
		/// <param name="array">The array which slice to get.</param>
		/// <param name="offset">The relativized offset of the first item of the slice.</param>
		/// <param name="length">The relativized length of the slice.</param>
		/// <param name="preserveKeys">Whether to preserve integer keys. If <B>false</B>, the integer keys are reset.</param>
		/// <returns>The slice of <paramref name="array"/>.</returns>
		/// <remarks>
		/// See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and <paramref name="length"/>.
		/// </remarks>
		[ImplementsFunction("array_slice")]
		[return: PhpDeepCopy]
		public static PhpArray Slice(PhpArray array, int offset, int length, bool preserveKeys)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			// absolutizes range:
			PhpMath.AbsolutizeRange(ref offset, ref length, array.Count);

			var iterator = array.GetBaseEnumerator();
			
			// moves iterator to the first item of the slice;
			// starts either from beginning or from the end (which one is more efficient):
			if (offset < array.Count - offset)
			{
				for (int i = -1; i < offset; i++) iterator.MoveNext();
			}
			else
			{
				for (int i = array.Count; i > offset; i--) iterator.MovePrevious();
			}

			// copies the slice:
            PhpArray result = new PhpArray(length);
            int ikey = 0;
			for (int i = 0; i < length; i++)
			{
				KeyValuePair<IntStringKey, object> entry = iterator.Current;

				// integer keys are reindexed if preserveKeys is false, string keys are not touched:
				if (entry.Key.IsString)
				{
					result.Add(entry.Key, entry.Value);
				}
				else
				{
					if (!preserveKeys)
						result.Add(ikey++, entry.Value);
					else
						result.Add(entry.Key, entry.Value);
				}

				iterator.MoveNext();
			}

			result.InplaceCopyOnReturn = true;
			return result;
		}


		/// <summary>
		/// Removes a slice of an array.
		/// </summary>
		/// <param name="array">The array which slice to remove.</param>
		/// <param name="offset">The relativized offset of a first item of the slice.</param>
		/// <remarks>
		/// <para>Items from <paramref name="offset"/>-th to the last one are removed from <paramref name="array"/>.</para>
		/// </remarks>
		/// <para>See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/>.</para>
		[ImplementsFunction("array_splice")]
		public static PhpArray Splice([PhpRw] PhpArray array, int offset)
		{
			// Splice would be equivalent to SpliceDc if no replacelent is specified (=> no SpliceDc):
			return Splice(array, offset, int.MaxValue, null);
		}

		/// <summary>
		/// Removes a slice of an array.
		/// </summary>
		/// <param name="array">The array which slice to remove.</param>
		/// <param name="offset">The relativized offset of a first item of the slice.</param>
		/// <param name="length">The relativized length of the slice.</param>
		/// <remarks>
		/// <para><paramref name="length"/> items are removed from <paramref name="array"/> 
		/// starting with the <paramref name="offset"/>-th one.</para>
		/// </remarks>
		/// <para>See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/>.</para>
		[ImplementsFunction("array_splice")]
		public static PhpArray Splice([PhpRw] PhpArray array, int offset, int length)
		{
			// Splice would be equivalent to SpliceDc if no replacement is specified (=> no SpliceDc):
			return Splice(array, offset, length, null);
		}

		/// <summary>
		/// Replaces a slice of an array with specified item(s).
		/// </summary>
		/// <remarks>
		/// <para>The same as <see cref="Splice(PhpArray,int,int,object)"/> except for that
		/// replacement items are deeply copied to the <paramref name="array"/>.</para>
		/// </remarks>
		[ImplementsFunction("array_splice")]
		public static PhpArray SpliceDc([PhpRw] PhpArray array, int offset, int length, object replacement)
		{
			if (array == null)
			{
				PhpException.ReferenceNull("array");
				return null;
			}

			return SpliceInternal(array, offset, length, replacement, true);
		}

		/// <summary>
		/// Replaces a slice of an array with specified item(s).
		/// </summary>
		/// <param name="array">The array which slice to replace.</param>
		/// <param name="offset">The relativized offset of a first item of the slice.</param>
		/// <param name="length">The relativized length of the slice.</param>
		/// <param name="replacement"><see cref="PhpArray"/> of items to replace the splice or a single item.</param>
		/// <returns>The <see cref="PhpArray"/> of replaced items indexed by integers starting from zero.</returns>
		/// <remarks>
		/// <para>See <see cref="PhpMath.AbsolutizeRange"/> for details about <paramref name="offset"/> and <paramref name="length"/>.</para>
		/// <para>Reindexes all integer keys in resulting array.</para>
		/// </remarks>
		public static PhpArray Splice(PhpArray array, int offset, int length, object replacement)
		{
			if (array == null)
				throw new ArgumentNullException("array");

			return SpliceInternal(array, offset, length, replacement, false);
		}

		/// <summary>
		/// Implementation of <see cref="Splice(PhpArray,int,int,object)"/> and <see cref="SpliceDc(PhpArray,int,int,object)"/>.
		/// </summary>
		/// <remarks>Whether to make a deep-copy of items in the replacement.</remarks>
		internal static PhpArray SpliceInternal(PhpArray array, int offset, int length, object replacement, bool deepCopy)
		{
			Debug.Assert(array != null);
			int count = array.Count;

			// converts offset and length to interval [first,last]:
			PhpMath.AbsolutizeRange(ref offset, ref length, count);

            PhpArray result = new PhpArray(length);
			PhpArray r_array = replacement as PhpArray;

			// replacement is an array:
			if (r_array != null)
			{
				// provides deep copies:
				IEnumerable<object> e;

				if (deepCopy)
					e = PhpVariable.EnumerateDeepCopies<object>(r_array.Values);
				else
					e = r_array.Values;

				// does replacement:
				array.ReindexAndReplace(offset, length, e, result);
			}
			else if (replacement != null)
			{
				// replacement is another type //

				// creates a deep copy:
				if (deepCopy) replacement = PhpVariable.DeepCopy(replacement);

				// does replacement:
				array.ReindexAndReplace(offset, length, new object[] { replacement }, result);
			}
			else
			{
				// replacement is null:

				array.ReindexAndReplace(offset, length, null, result);
			}

			return result;
		}

		#endregion


		#region shuffle, array_rand

		/// <summary>
		/// Randomizes the order of elements in the array using PhpMath random numbers generator.
		/// </summary>
		/// <exception cref="PhpException">Thrown if the <paramref name="array"/> argument is null.</exception>
		/// <remarks>Reindexes all keys in the resulting array.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("shuffle")]
		public static bool Shuffle([PhpRw] PhpArray array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return false;
			}

			array.Shuffle(PhpMath.Generator);
			array.ReindexAll();

            return true;
		}

		/// <summary>
		/// Returns a key of an entry chosen at random using PhpMath random numbers generator.
		/// </summary>
		/// <param name="array">The array which to choose from.</param>
		/// <returns>The chosen key.</returns>
        /// <exception cref="System.NullReferenceException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		[ImplementsFunction("array_rand")]
		[return: PhpDeepCopy]
		public static object RandomKeys(PhpArray array)
		{
			return RandomKeys(array, 1);
		}

		/// <summary>
		/// Chooses specified number of keys from an array at random.
		/// </summary>
		/// <param name="array">The <see cref="PhpArray"/> from which to choose.</param>
		/// <param name="count">The number of items to choose.</param>
		/// <returns>Either <see cref="PhpArray"/> of chosen keys (<paramref name="count"/> &gt; 1) or a single key.</returns>
		/// <remarks>
		/// Items are chosen uniformly in time <I>O(n)</I>, where <I>n</I> is the number of items in the 
		/// <paramref name="array"/> using conveyor belt sampling. 
		/// </remarks>
        /// <exception cref="NullReferenceException"><paramref name="array"/>  is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException"><paramref name="count"/> is not positive and less 
		/// than the number of items in <paramref name="array"/>. (Warning)</exception>
		[ImplementsFunction("array_rand")]
		[return: PhpDeepCopy]
		public static object RandomKeys(PhpArray array, int count)
		{
			if (count == 1)
			{
				ArrayList result = new ArrayList(1);
				return RandomSubset(((IDictionary)array).Keys, result, count, PhpMath.Generator) ? result[0] : null;
			}
			else
			{
				PhpArray result = new PhpArray(count > 0 ? count : 0, 0);
				if (RandomSubset(((IDictionary)array).Keys, result, count, PhpMath.Generator))
				{
					result.InplaceCopyOnReturn = true;
					return result;
				}
				else
				{
					return null;
				}
			}
		}

		/// <summary>
		/// Chooses specified number of items from a collection at random.
		/// </summary>
		/// <param name="source">The <see cref="ICollection"/> from which to choose.</param>
		/// <param name="result">The <see cref="IList"/> where to add chosen items.</param>
		/// <param name="count">The number of items to choose.</param>
		/// <param name="generator">The initialized random numbers generator.</param>
		/// <remarks>
		/// Items are chosen uniformly in time <I>O(n)</I>, where <I>n</I> is the number of items in the collection
		/// using conveyor belt sampling. 
		/// </remarks>
		/// <returns><B>false</B> on failure.</returns>
		/// <exception cref="PhpException">Either <paramref name="source"/> or <paramref name="result"/> or 
		/// <paramref name="generator"/> is a <B>null</B> reference (Warning)</exception>
		/// <exception cref="PhpException"><paramref name="count"/> is not positive and less 
		/// than the number of items in <paramref name="source"/>. (Warning)</exception>
		public static bool RandomSubset(ICollection source, IList result, int count, Random generator)
		{
			#region requires (source && result && generator && count>=1 && count<=source.Count)

			if (source == null)
			{
				PhpException.ArgumentNull("array");
				return false;
			}
			if (result == null)
			{
				PhpException.ArgumentNull("result");
				return false;
			}
			if (generator == null)
			{
				PhpException.ArgumentNull("generator");
				return false;
			}
			if (count < 1 || count > source.Count)
			{
				PhpException.InvalidArgument("count", LibResources.GetString("number_of_items_not_between_one_and_item_count",
					count, source.Count));
				return false;
			}

			#endregion

			int n = source.Count;
			IEnumerator iterator = source.GetEnumerator();
			while (iterator.MoveNext())
			{
				// adds item to result with probability count/n:
				if ((double)count > generator.NextDouble() * n)
				{
					result.Add(iterator.Current);
					if (--count == 0) break;
				}
				n--;
			}

			return true;
		}

		#region Unit Test

#if DEBUG

		[Test]
		private static void TestRandomKeys()
		{
			PhpArray a = PhpArray.Keyed("Server1", 1, "Server2", 2, "Server3", 3);
			PhpVariable.Dump(a);
			string result = RandomKeys(a) as string;
			Debug.Assert(result == "Server1" || result == "Server2" || result == "Server3");
		}

#endif

		#endregion

		#endregion


		#region array_key_exists, in_array, array_search

		/// <summary>
		/// Checks if a key exists in the array.
		/// </summary>
		/// <param name="key">The key to be searched for.</param>
		/// <param name="array">The array where to search for the key.</param>
		/// <returns>Whether the <paramref name="key"/> exists in the <paramref name="array"/>.</returns>
		/// <remarks><paramref name="key"/> is converted by <see cref="PHP.Core.Convert.ObjectToArrayKey"/> before the search.</remarks>
		/// <exception cref="PhpException"><paramref name="array"/> argument is a <B>null</B> reference (Warning).</exception>
		/// <exception cref="PhpException"><paramref name="key"/> has type which is illegal for array key.</exception>
		[ImplementsFunction("array_key_exists")]
		public static bool KeyExists(object key, PhpArray array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return false;
			}

			IntStringKey array_key;
			if (PHP.Core.Convert.ObjectToArrayKey(key, out array_key))
				return array.ContainsKey(array_key);
				
			PhpException.Throw(PhpError.Warning, CoreResources.GetString("illegal_offset_type")); 
			return false;
		}

		/// <summary>
		/// Alias of <see cref="KeyExists"/>.
		/// </summary>
		[ImplementsFunction("key_exists"), EditorBrowsable(EditorBrowsableState.Never)]
		public static bool KeyExistsObsolete(object key, PhpArray array)
		{
			return KeyExists(key, array);
		}

		/// <summary>
		/// Checks if a value exists in an array.
		/// </summary>
		/// <param name="needle">The value to search for.</param>
		/// <param name="haystack">The <see cref="PhpArray"/> where to search.</param>
		/// <returns>Whether there is the <paramref name="needle"/> value in the <see cref="PhpArray"/>.</returns>
		/// <remarks>Regular comparison (<see cref="PhpComparer.CompareEq(object,object)"/>) is used for comparing values.</remarks>
		/// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
		[ImplementsFunction("in_array")]
		public static bool InArray(object needle, PhpArray haystack)
		{
			object b = Search(needle, haystack, false);
			return !(b is bool) || (bool)b;
		}

		/// <summary>
		/// Checks if a value exists in an array.
		/// </summary>
		/// <param name="needle">The value to search for.</param>
		/// <param name="haystack">The <see cref="PhpArray"/> where to search.</param>
		/// <param name="strict">Whether strict comparison method (operator ===) is used for comparing values.</param>
		/// <returns>Whether there is the <paramref name="needle"/> value in the <see cref="PhpArray"/>.</returns>
		/// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
		[ImplementsFunction("in_array")]
		public static bool InArray(object needle, PhpArray haystack, bool strict)
		{
			object b = Search(needle, haystack, strict);
			return !(b is bool) || (bool)b;
		}

		/// <summary>
		/// Searches the array for a given value and returns the corresponding key if successful.
		/// </summary>
		/// <param name="needle">The value to search for.</param>
		/// <param name="haystack">The <see cref="PhpArray"/> where to search.</param>
		/// <returns>The key associated with the <paramref name="needle"/> or <B>false</B> if there is no such key.</returns>
		/// <remarks>Regular comparison (<see cref="PhpComparer.CompareEq(object,object)"/>) is used for comparing values.</remarks>
		/// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
		[ImplementsFunction("array_search")]
		public static object Search(object needle, PhpArray haystack)
		{
			return Search(needle, haystack, false);
		}

		/// <summary>
		/// Searches the array for a given value and returns the corresponding key if successful.
		/// </summary>
		/// <param name="needle">The value to search for.</param>
		/// <param name="haystack">The <see cref="PhpArray"/> where to search.</param>
		/// <param name="strict">Whether strict comparison method (operator ===) is used for comparing values.</param>
		/// <returns>The key associated with the <paramref name="needle"/> or <B>false</B> if there is no such key.</returns>
		/// <exception cref="PhpException"><paramref name="haystack"/> is a <B>null</B> reference (Warning).</exception>
		[ImplementsFunction("array_search")]
		public static object Search(object needle, PhpArray haystack, bool strict)
		{
			// result needn't to be deeply copied because it is a key of an array //

			if (haystack == null)
			{
				PhpException.ArgumentNull("haystack");
				return false;
			}

			// using operator ===:
			if (strict)
			{
                using (var enumerator = haystack.GetFastEnumerator())
                    while (enumerator.MoveNext())
                    {
                        // dereferences value (because of StrictEquality operator):
                        object val = PhpVariable.Dereference(enumerator.CurrentValue);

                        if (Operators.StrictEquality(needle, val))
                            return enumerator.CurrentKey.Object;
                    }
			}
			else
			{
				// using operator ==:

                using (var enumerator = haystack.GetFastEnumerator())
                    while (enumerator.MoveNext())
                    {
                        // comparator manages references well:
                        if (PhpComparer.CompareEq(needle, enumerator.CurrentValue))
                            return enumerator.CurrentKey.Object;
                    }
			}

			// not found:
			return false;
		}

		#endregion


        #region array_fill, array_fill_keys, array_pad

        /// <summary>
		/// Creates a new array filled with a specified value.
		/// </summary>
		/// <param name="startIndex">The value of the key of the first item in the array.</param>
		/// <param name="count">The number of items in the array.</param>
		/// <param name="value">The value copied to all items in the array.</param>
		/// <returns>The array.</returns>
		/// <exception cref="PhpException">Thrown if <paramref name="count"/> is not positive.</exception>
		[ImplementsFunction("array_fill")]
		[return: PhpDeepCopy]
		public static PhpArray Fill(int startIndex, int count, object value)
		{
			if (count <= 0)
			{
				PhpException.InvalidArgument("count", LibResources.GetString("arg:negative_or_zero"));
				return null;
			}

			PhpArray result = new PhpArray(count, 0);
			int last = startIndex + count;
			for (int i = startIndex; i < last; i++)
				result.Add(i, value);

			// makes deep copies of all added items:
			result.InplaceCopyOnReturn = true;
			return result;
		}

        [ImplementsFunction("array_fill_keys")]
        [return: PhpDeepCopy]
        public static PhpArray FillKeys(PhpArray keys, object value)
        {
            PhpArray result = new PhpArray(keys.Count);

            if (keys != null)
                foreach (var x in keys)
                {
                    IntStringKey key;
                    if (!PHP.Core.Convert.ObjectToArrayKey(x.Value, out key))
                        continue;

                    if (!result.ContainsKey(key))
                        result.Add(key, value);
                }

            // makes deep copies of all added items:
            result.InplaceCopyOnReturn = true;
            return result;
        }

		/// <summary>
		/// Pads array to the specified length with a value.
		/// If the length is negative adds |length| elements at beginning otherwise adds elements at the end.
		/// Values with integer keys that are contained in the source array are inserted to the resulting one with new 
		/// integer keys counted from zero (or from |length| if length negative).
		/// </summary>
		/// <param name="array">The source array.</param>
		/// <param name="length">The length of the resulting array.</param>
		/// <param name="value">The value to add in array.</param>
		/// <returns>Padded array.</returns>
		/// <exception cref="PhpException">The <paramref name="array"/> argument is a <B>null</B> reference.</exception>
		[ImplementsFunction("array_pad")]
		[return: PhpDeepCopy]
		public static PhpArray Pad(PhpArray array, int length, object value)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			// number of items to add:
			int remains = Math.Abs(length) - array.Count;

			// returns unchanged array (or its deep copy if called from PHP):
			if (remains <= 0) return array;

			PhpArray result = new PhpArray(array.IntegerCount + remains, array.StringCount);

			// prepends items:
			if (length < 0)
			{
				while (remains-- > 0) result.Add(value);
			}

			// inserts items from source array
			// if a key is a string inserts it unchanged otherwise inserts value with max. integer key:  
			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
				if (entry.Key.IsString)
					result.Add(entry.Key.String, entry.Value);
				else
					result.Add(entry.Value);
			}

			// appends items:
			if (length > 0)
			{
				while (remains-- > 0) result.Add(value);
			}

			// the result is inplace deeply copied on return to PHP code:
			result.InplaceCopyOnReturn = true;
			return result;
		}

		#endregion


		#region range

		/// <summary>
		/// Creates an array containing range of integers from the [low;high] interval with arbitrary step.
		/// </summary>
		/// <param name="low">Lower bound of the interval.</param>
		/// <param name="high">Upper bound of the interval.</param>
		/// <param name="step">The step. An absolute value is taken if step is zero.</param>
		/// <returns>The array.</returns>
		public static PhpArray RangeOfInts(int low, int high, int step)
		{
			if (step == 0)
			{
				PhpException.InvalidArgument("step", LibResources.GetString("arg:zero"));
				return null;
			}

			if (step < 0) step = -step;

			PhpArray result = new PhpArray(Math.Abs(high - low) / step + 1);

			if (high >= low)
			{
				for (int i = 0; low <= high; i++, low += step) result.Add(i, low);
			}
			else
			{
				for (int i = 0; low >= high; i++, low -= step) result.Add(i, low);
			}

			return result;
		}

		/// <summary>
		/// Creates an array containing range of long integers from the [low;high] interval with arbitrary step.
		/// </summary>
		/// <param name="low">Lower bound of the interval.</param>
		/// <param name="high">Upper bound of the interval.</param>
		/// <param name="step">The step. An absolute value is taken if step is zero.</param>
		/// <returns>The array.</returns>
		public static PhpArray RangeOfLongInts(long low, long high, long step)
		{
			if (step == 0)
			{
				PhpException.InvalidArgument("step", LibResources.GetString("arg:zero"));
				return null;
			}

			if (step < 0) step = -step;

			PhpArray result = new PhpArray(unchecked((int)(Math.Abs(high - low) / step + 1)));

			if (high >= low)
			{
				for (int i = 0; low <= high; i++, low += step) result.Add(i, low);
			}
			else
			{
				for (int i = 0; low >= high; i++, low -= step) result.Add(i, low);
			}

			return result;
		}

		/// <summary>
		/// Creates an array containing range of doubles from the [low;high] interval with arbitrary step.
		/// </summary>
		/// <param name="low">Lower bound of the interval.</param>
		/// <param name="high">Upper bound of the interval.</param>
		/// <param name="step">The step. An absolute value is taken if step is less than zero.</param>
		/// <returns>The array.</returns>
		/// <exception cref="PhpException">Thrown if the <paramref name="step"/> argument is zero.</exception>
		public static PhpArray RangeOfDoubles(double low, double high, double step)
		{
			if (step == 0)
			{
				PhpException.InvalidArgument("step", LibResources.GetString("arg:zero"));
				return null;
			}

			if (step < 0) step = -step;

			PhpArray result = new PhpArray(System.Convert.ToInt32(Math.Abs(high - low) / step) + 1);

			if (high >= low)
			{
				for (int i = 0; low <= high; i++, low += step) result.Add(i, low);
			}
			else
			{
				for (int i = 0; low >= high; i++, low -= step) result.Add(i, low);
			}

			return result;
		}

		/// <summary>
		/// Creates an array containing range of characters from the [low;high] interval with arbitrary step.
		/// </summary>
		/// <param name="low">Lower bound of the interval.</param>
		/// <param name="high">Upper bound of the interval.</param>
		/// <param name="step">The step.</param>
		/// <returns>The array.</returns>
		/// <exception cref="PhpException">Thrown if the <paramref name="step"/> argument is zero.</exception>
		public static PhpArray RangeOfChars(char low, char high, int step)
		{
			if (step == 0)
			{
				PhpException.InvalidArgument("step", LibResources.GetString("arg:zero"));
				step = 1;
			}

			if (step < 0) step = -step;

			PhpArray result = new PhpArray(Math.Abs(high - low) / step + 1, 0);
			if (high >= low)
			{
				for (int i = 0; low <= high; i++, low = unchecked((char)(low + step))) result.Add(i, low.ToString());
			}
			else
			{
				for (int i = 0; low >= high; i++, low = unchecked((char)(low - step))) result.Add(i, low.ToString());
			}

			return result;
		}

		/// <summary>
		/// Creates an array containing range of elements with step 1.
		/// </summary>
		/// <param name="low">Lower bound of the interval.</param>
		/// <param name="high">Upper bound of the interval.</param>
		/// <returns>The array.</returns>
		[ImplementsFunction("range")]
		public static PhpArray Range(object low, object high)
		{
			return Range(low, high, 1);
		}

		/// <summary>
		/// Creates an array containing range of elements with arbitrary step.
		/// </summary>
		/// <param name="low">Lower bound of the interval.</param>
		/// <param name="high">Upper bound of the interval.</param>
		/// <param name="step">The step.</param>
		/// <returns>The array.</returns>
		/// <remarks>
		/// Implements PHP awful range function. The result depends on types and 
		/// content of parameters under the following rules:
		/// <list type="number">
		/// <item>
		///   <description>
		///   If at least one parameter (low, high or step) is of type double or is a string wholly representing 
		///       double value (i.e. whole string is converted to a number and no chars remains, 
		///       e.g. "1.5" is wholly representing but the value "1.5x" is not)
		///    than
		///       range of double values is generated with a step treated as a double value
		///       (e.g. <c>range("1x","2.5x","0.5") = array(1.0, 1.5, 2.0, 2.5)</c> etc.)
		///    otherwise 
		///   </description>
		/// </item>
		/// <item>
		///   <description>
		///    if at least one bound (i.e. low or high parameter) is of type int or is a string wholly representing
		///       integer value 
		///    than 
		///       range of integer values is generated with a step treated as integer value
		///       (e.g. <c>range("1x","2","1.5") = array(1, 2, 3, 4)</c> etc.)
		///    otherwise
		///   </description>
		/// </item>
		/// <item>
		///   <description>
		///    low and high are both non-empty strings (otherwise one of the two previous conditions would be true),
		///    so the first characters of these strings are taken and a sequence of characters is generated.
		///   </description>     
		/// </item>
		/// </list>
		/// Moreover, if <paramref name="low"/> is greater than <paramref name="high"/> then descending sequence is generated 
		/// and ascending one otherwise. If <paramref name="step"/> is less than zero than an absolute value is used.
		/// </remarks>
		/// <exception cref="PhpException">Thrown if the <paramref name="step"/> argument is zero (or its absolute value less than 1 in the case 2).</exception>
		[ImplementsFunction("range")]
		public static PhpArray Range(object low, object high, object step)
		{
			double d_step, d_low, d_high;
			int i_step, i_low, i_high;
			long l_step, l_low, l_high;
			Core.Convert.NumberInfo info_step, info_low, info_high;

			bool is_step_double, is_low_double, is_high_double, w_step, w_low, w_high;

			if (low == null || String.Empty.Equals(low)) low = "0";
			if (high == null || String.Empty.Equals(high)) high = "0";

			// converts each parameter to a number, determines what type of number it is (int/double)
			// and whether it wholly represents that number:
			info_step = Core.Convert.ObjectToNumber(step, out i_step, out l_step, out d_step);
			info_low = Core.Convert.ObjectToNumber(low, out i_low, out l_low, out d_low);
			info_high = Core.Convert.ObjectToNumber(high, out i_high, out l_high, out d_high);

			is_step_double = (info_step & PHP.Core.Convert.NumberInfo.Double) != 0;
			is_low_double = (info_low & PHP.Core.Convert.NumberInfo.Double) != 0;
			is_high_double = (info_high & PHP.Core.Convert.NumberInfo.Double) != 0;

			w_step = (info_step & PHP.Core.Convert.NumberInfo.IsNumber) != 0;
			w_low = (info_low & PHP.Core.Convert.NumberInfo.IsNumber) != 0;
			w_high = (info_high & PHP.Core.Convert.NumberInfo.IsNumber) != 0;

			// at least one parameter is a double or its numeric value is wholly double:
			if (is_low_double && w_low || is_high_double && w_high || is_step_double && w_step)
			{
				return RangeOfDoubles(d_low, d_high, d_step);
			}

			// at least one bound is wholly integer (doesn't matter what the step is):
			if (!is_low_double && w_low || !is_high_double && w_high)
			{
				// at least one long integer:
				if (((info_step | info_low | info_high) & PHP.Core.Convert.NumberInfo.LongInteger) != 0)
					return RangeOfLongInts(l_low, l_high, l_step);
				else
					return RangeOfInts(i_low, i_high, i_step);
			}

			// both bounds are strings which are not wholly representing numbers (other types wholly represents a number):

			string slow = Core.Convert.ObjectToString(low);
			string shigh = Core.Convert.ObjectToString(high);

			// because each string doesn't represent a number it isn't empty:
			Debug.Assert(slow != "" && shigh != "");

			return RangeOfChars(slow[0], shigh[0], i_step);
		}

		#endregion


		#region GetComparer

		/// <summary>
		/// Gets an instance of PHP comparer parametrized by specified method, order, and compared item type.
		/// </summary>
		/// <param name="method">The <see cref="ComparisonMethod"/>.</param>
		/// <param name="order">The <see cref="SortingOrder"/>.</param>
		/// <param name="keyComparer">Whether to compare keys (<B>false</B> for value comparer).</param>
		/// <returns>A comparer (either a new instance or existing singleton instance).</returns>
		public static IComparer<KeyValuePair<IntStringKey, object>>/*!*/ GetComparer(ComparisonMethod method, SortingOrder order, bool keyComparer)
		{
			if (keyComparer)
			{
				switch (method)
				{
					case ComparisonMethod.Numeric:
						return (order == SortingOrder.Descending) ? KeyComparer.ReverseNumeric : KeyComparer.Numeric;

					case ComparisonMethod.String:
						return (order == SortingOrder.Descending) ? KeyComparer.ReverseString : KeyComparer.String;

					case ComparisonMethod.LocaleString:
						return new KeyComparer(Locale.GetStringComparer(false), order == SortingOrder.Descending);

					default:
						return (order == SortingOrder.Descending) ? KeyComparer.Reverse : KeyComparer.Default;
				}
			}
			else
			{
				switch (method)
				{
					case ComparisonMethod.Numeric:
						return (order == SortingOrder.Descending) ? ValueComparer.ReverseNumeric : ValueComparer.Numeric;

					case ComparisonMethod.String:
						return (order == SortingOrder.Descending) ? ValueComparer.ReverseString : ValueComparer.String;

					case ComparisonMethod.LocaleString:
						return new ValueComparer(Locale.GetStringComparer(false), order == SortingOrder.Descending);

					default:
						return (order == SortingOrder.Descending) ? ValueComparer.Reverse : ValueComparer.Default;
				}
			}
		}

		#endregion


		#region sort,asort,ksort,rsort,arsort,krsort

		/// <summary>
		/// Sorts an array using regular comparison method for comparing values.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("sort")]
		public static bool Sort([PhpRw] PhpArray array)
		{
            return Sort(array, ComparisonMethod.Regular);
		}

		/// <summary>
		/// Sorts an array using specified comparison method for comparing values.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <param name="comparisonMethod">The method to be used for comparison of values.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("sort")]
		public static bool Sort([PhpRw] PhpArray array, ComparisonMethod comparisonMethod)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }

			array.Sort(GetComparer(comparisonMethod, SortingOrder.Ascending, false));
			array.ReindexAll();
			array.RestartIntrinsicEnumerator();

            return true;
		}

		/// <summary>
		/// Sorts an array using regular comparison method for comparing values preserving key-value associations.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("asort")]
		public static bool AssocSort([PhpRw] PhpArray array)
		{
			return AssocSort(array, ComparisonMethod.Regular);
		}

		/// <summary>
		/// Sorts an array using specified comparison method for comparing values preserving key-value associations.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <param name="comparisonMethod">The method to be used for comparison of values.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("asort")]
		public static bool AssocSort([PhpRw] PhpArray array, ComparisonMethod comparisonMethod)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }

			array.Sort(GetComparer(comparisonMethod, SortingOrder.Ascending, false));
			array.RestartIntrinsicEnumerator();

            return true;
		}

		/// <summary>
		/// Sorts an array using regular comparison method for comparing keys.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("ksort")]
		public static bool KeySort([PhpRw] PhpArray array)
		{
			return KeySort(array, ComparisonMethod.Regular);
		}

		/// <summary>
		/// Sorts an array using specified comparison method for comparing keys.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <param name="comparisonMethod">The method to be used for comparison of keys.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("ksort")]
		public static bool KeySort([PhpRw] PhpArray array, ComparisonMethod comparisonMethod)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }

			array.Sort(GetComparer(comparisonMethod, SortingOrder.Ascending, true));
			array.RestartIntrinsicEnumerator();

            return true;
		}

		/// <summary>
		/// Sorts an array using regular comparison method for comparing values in reverse order.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("rsort")]
		public static bool ReverseSort([PhpRw] PhpArray array)
		{
			return ReverseSort(array, ComparisonMethod.Regular);
		}

		/// <summary>
		/// Sorts an array using specified comparison method for comparing values in reverse order.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <param name="comparisonMethod">The method to be used for comparison of keys.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("rsort")]
		public static bool ReverseSort([PhpRw] PhpArray array, ComparisonMethod comparisonMethod)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }

			array.Sort(GetComparer(comparisonMethod, SortingOrder.Descending, false));
			array.ReindexAll();
			array.RestartIntrinsicEnumerator();

            return true;
		}

		/// <summary>
		/// Sorts an array using regular comparison method for comparing values in reverse order 
		/// preserving key-value associations.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("arsort")]
		public static bool AssocReverseSort([PhpRw] PhpArray array)
		{
			return AssocReverseSort(array, ComparisonMethod.Regular);
		}

		/// <summary>
		/// Sorts an array using specified comparison method for comparing values in reverse order
		/// preserving key-value associations.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <param name="comparisonMethod">The method to be used for comparison of values.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("arsort")]
		public static bool AssocReverseSort([PhpRw] PhpArray array, ComparisonMethod comparisonMethod)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }

			array.Sort(GetComparer(comparisonMethod, SortingOrder.Descending, false));
			array.RestartIntrinsicEnumerator();

            return true;
		}

		/// <summary>
		/// Sorts an array using regular comparison method for comparing keys in reverse order.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("krsort")]
		public static bool KeyReverseSort([PhpRw] PhpArray array)
		{
			return KeyReverseSort(array, ComparisonMethod.Regular);
		}

		/// <summary>
		/// Sorts an array using specified comparison method for comparing keys in reverse order.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <param name="comparisonMethod">The method to be used for comparison of keys.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("krsort")]
		public static bool KeyReverseSort([PhpRw] PhpArray array, ComparisonMethod comparisonMethod)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }

			array.Sort(GetComparer(comparisonMethod, SortingOrder.Descending, true));
			array.RestartIntrinsicEnumerator();

            return true;
		}

		#endregion


		#region usort,uasort,uksort

		/// <summary>
		/// Sorts an array using user comparison callback for comparing values.
		/// </summary>
        /// <param name="caller">The class context used to bind the callback.</param>
        /// <param name="array">The array to be sorted.</param>
		/// <param name="compare">The user callback to be used for comparison of values.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("usort", FunctionImplOptions.NeedsClassContext)]
		public static bool UserSort(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpArray array, PhpCallback compare)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }
			if (!PhpArgument.CheckCallback(compare, caller, "compare", 0, false)) return false;

			// sorts array using callback for comparisons:
			array.Sort(new ValueComparer(new PhpUserComparer(compare), false));

			array.ReindexAll();
			array.RestartIntrinsicEnumerator();

            return true;
		}

		/// <summary>
		/// Sorts an array user comparison callback method for comparing values preserving key-value associations.
		/// </summary>
        /// <param name="caller">The class context used to bind the callback.</param>
        /// <param name="array">The array to be sorted.</param>
		/// <param name="compare">The user callback to be used for comparison of values.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        [ImplementsFunction("uasort", FunctionImplOptions.NeedsClassContext)]
        public static bool UserAssocSort(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpArray array, PhpCallback compare)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }
			if (!PhpArgument.CheckCallback(compare, caller, "compare", 0, false)) return false;

			// sorts array using callback for comparisons:
			array.Sort(new ValueComparer(new PhpUserComparer(compare), false));

            return true;
		}

		/// <summary>
		/// Sorts an array using user comparison callback for comparing keys.
		/// </summary>
        /// <param name="caller">The class context used to bind the callback.</param>
        /// <param name="array">The array to be sorted.</param>
		/// <param name="compare">The user callback to be used for comparison of values.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
        [ImplementsFunction("uksort", FunctionImplOptions.NeedsClassContext)]
        public static bool UserKeySort(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpArray array, PhpCallback compare)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }
            if (!PhpArgument.CheckCallback(compare, caller, "compare", 0, false)) return false;

			array.Sort(new KeyComparer(new PhpUserComparer(compare), false));

            return true;
		}

		#endregion


		#region natsort,natcasesort

		/// <summary>
		/// Sorts an array using case sensitive natural comparison method for comparing 
		/// values preserving key-value association.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("natsort")]
		public static bool NaturalSort([PhpRw] PhpArray array)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }

			array.Sort(new ValueComparer(PhpNaturalComparer.Default, false));

            return true;
		}

		/// <summary>
		/// Sorts an array using case insensitive natural comparison method for 
		/// comparing values preserving key-value association.
		/// </summary>
		/// <param name="array">The array to be sorted.</param>
		/// <remarks>Resets <paramref name="array"/>'s intrinsic enumerator.</remarks>
        /// <returns>True on success, False on failure.</returns>
		[ImplementsFunction("natcasesort")]
		public static bool NaturalCaseInsensitiveSort([PhpRw] PhpArray array)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return false; }

			array.Sort(new ValueComparer(PhpNaturalComparer.CaseInsensitive, false));

            return true;
		}

		#endregion


		#region array_multisort

		/// <summary>
		/// Resolves arguments passed to <see cref="MultiSort"/> method according to PHP manual for <c>array_multisort</c> function.
		/// </summary>
		/// <param name="first">The first argument of <see cref="MultiSort"/>.</param>
		/// <param name="args">The rest of arguments of <see cref="MultiSort"/>.</param>
		/// <param name="arrays">An array to be filled with arrays passed in all arguments.</param>
		/// <param name="comparers">An array to be filled with comparers defined by arguments.</param>
		/// <remarks>
		/// Arrays and comparers can be a <B>null</B> reference. In such a case only number of arrays to be sorted
		/// is returned. Otherwise, <paramref name="arrays"/> is filled with these arrays and <paramref name="comparers"/>
		/// with comparers defined by appropriate arguments.
		/// </remarks>
		private static int MultiSortResolveArgs(
			PhpArray first,
			object[] args,
			PhpArray[] arrays,
			IComparer<KeyValuePair<IntStringKey, object>>[] comparers)
		{
			PhpArray array;
			int col_count = 1;
			int row_count = first.Count;
			ComparisonMethod method = ComparisonMethod.Undefined;
			SortingOrder order = SortingOrder.Undefined;

			if (arrays != null)
			{
				arrays[0] = first;
			}

			for (int i = 0; i < args.Length; i++)
			{
				if ((array = args[i] as PhpArray) != null)
				{
					// checks whether the currently processed array has the same length as the first one:
					if (array.Count != row_count)
					{
						PhpException.Throw(PhpError.Warning, CoreResources.GetString("lengths_are_different", "the first array",
							String.Format("{0}-th array", col_count)));
						return 0;
					}
					// sets next array:
					if (arrays != null)
						arrays[col_count] = array;

					// sets comparer of the previous array:
					if (comparers != null)
						comparers[col_count - 1] = GetComparer(method, order, false);

					// resets values:
					method = ComparisonMethod.Undefined;
					order = SortingOrder.Undefined;

					col_count++;
				}
				else if (args[i] is int)
				{
					switch ((int)args[i])
					{
						case (int)ComparisonMethod.Numeric:
						case (int)ComparisonMethod.Regular:
						case (int)ComparisonMethod.String:
						case (int)ComparisonMethod.LocaleString:
							if (method != ComparisonMethod.Undefined)
							{
								PhpException.Throw(PhpError.Warning, LibResources.GetString("sorting_flag_already_specified", i));
								return 0;
							}
							else
							{
								method = (ComparisonMethod)args[i];
							}
							break;

						case (int)SortingOrder.Ascending:
						case (int)SortingOrder.Descending:
							if (order != SortingOrder.Undefined)
							{
								PhpException.Throw(PhpError.Warning, LibResources.GetString("sorting_flag_already_specified", i));
								return 0;
							}
							else
							{
								order = (SortingOrder)args[i];
							}
							break;

						default:
							PhpException.Throw(PhpError.Warning, LibResources.GetString("argument_not_array_or_sort_flag", i));
							return 0;
					}
				}
				else
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("argument_not_array_or_sort_flag", i));
					return 0;
				}
			}

			// sets comparer of the previous array:
			if (comparers != null)
				comparers[col_count - 1] = GetComparer(method, order, false);
			return col_count;
		}

		/// <summary>
		/// Sort multiple arrays.
		/// </summary>
		/// <param name="first">The first array to be sorted.</param>
		/// <param name="args">Arrays to be sorted along with flags affecting sort order and 
		/// comparison methods to be used. See PHP manual for more details.</param>
		/// <returns>Whether arrays were sorted successfully.</returns>
		/// <remarks>Reindexes integer keys in the sorted arrays and restarts their intrinsic enumerators.</remarks>
		/// <exception cref="PhpException"><paramref name="first"/> is a <B>null</B> reference (Warning).</exception>
        /// <exception cref="PhpException">Arrays has different lengths (Warning).</exception>
        /// <exception cref="PhpException">Invalid sorting flags (Warning).</exception>
        /// <exception cref="PhpException">Multiple sorting flags applied on single array (Warning).</exception>
		[ImplementsFunction("array_multisort")]
		public static bool MultiSort([PhpRw] PhpArray first, params object[] args)
		{
			// some "args" are also [PhpRw] but which ones is compile time unknown
			// but it is not neccessary to mark them since this attribute has no important effect

			if (first == null)
			{
				PhpException.ArgumentNull("first");
			}

			IComparer<KeyValuePair<IntStringKey, object>>[] comparers;
			PhpArray[] arrays;
			int length = MultiSortResolveArgs(first, args, null, null);

			if (length == 0)
			{
				return false;
			}
			if (length == 1)
			{
				comparers = new IComparer<KeyValuePair<IntStringKey, object>>[1];
				MultiSortResolveArgs(first, args, null, comparers);
				first.Sort(comparers[0]);
				first.ReindexIntegers(0);
				first.RestartIntrinsicEnumerator();
				return true;
			}

			arrays = new PhpArray[length];
			comparers = new IComparer<KeyValuePair<IntStringKey, object>>[length];
			MultiSortResolveArgs(first, args, arrays, comparers);
			PhpHashtable.Sort(arrays, comparers);

			for (int i = 0; i < length; i++)
			{
				arrays[i].ReindexIntegers(0);
				arrays[i].RestartIntrinsicEnumerator();
			}

			return true;
		}

		#endregion


		#region array_u?(diff|intersect)(_u?assoc)?, array_(diff|intersect)_u?key

		/// <summary>
		/// Internal method common for all functions.
		/// </summary>
		private static PhpArray SetOperation(SetOperations op, PhpArray array, PhpArray[] arrays,
			IComparer<KeyValuePair<IntStringKey, object>> comparer)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			if (arrays == null || arrays.Length == 0)
			{
				PhpException.InvalidArgumentCount(null, null);
				return null;
			}

			Debug.Assert(comparer != null);

			PhpArray result = new PhpArray();
			array.SetOperation(op, arrays, comparer, result);

			// the result is inplace deeply copied on return to PHP code:
			result.InplaceCopyOnReturn = true;
			return result;
		}

		/// <summary>
		/// There have to be at least 1 value in <paramref name="vars"/>.
		/// The last is converted to callback, the rest to arrays.
		/// </summary>
		private static bool SplitArraysAndComparers(int comparerCount, PhpArray array, object[] vars,
		  out PhpArray[] arrays, out PhpCallback cmp1, out PhpCallback cmp2)
		{
			arrays = null;
			cmp1 = cmp2 = null;

			if (vars == null || vars.Length == 0)
			{
				PhpException.InvalidArgumentCount(null, null);
				return false;
			}

			// the first callback:
			cmp1 = Core.Convert.ObjectToCallback(vars[vars.Length - comparerCount]);
            if (!PhpArgument.CheckCallback(cmp1, PHP.Core.Reflection.UnknownTypeDesc.Singleton/*(J): TBD pass caller from library func when this will be performance issue*/, null, vars.Length - comparerCount + 3, false))
				return false;

			// the second callback:
			if (comparerCount > 1)
			{
				cmp2 = Core.Convert.ObjectToCallback(vars[vars.Length - 1]);
                if (!PhpArgument.CheckCallback(cmp2, PHP.Core.Reflection.UnknownTypeDesc.Singleton/*(J): TBD pass caller from library func when this will be performance issue*/, null, vars.Length - comparerCount + 3, false))
					return false;
			}

			// remaining arguments should be arrays:
			arrays = new PhpArray[vars.Length - comparerCount + 1];
			arrays[0] = array;
			for (int i = 0; i < vars.Length - comparerCount; i++)
			{
				arrays[i + 1] = vars[i] as PhpArray;
				if (arrays[i + 1] == null)
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("argument_not_array", i + 3));
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Computes the difference of arrays.
		/// </summary>
		/// <param name="array">The array from which to take items away.</param>
		/// <param name="arrays">The arrays to be differentiated.</param>
		/// <returns>The array containing all the entries of <paramref name="array"/> that are not present 
		/// in any of the <paramref name="arrays"/>.</returns>
		/// <remarks>Keys are preserved. Entries are considered to be equal iff values compared by  
		/// by string comparison method are the same (see <see cref="ValueComparer.String"/>).</remarks>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException"><paramref name="arrays"/> is a <B>null</B> reference or an empty array.</exception>
		[ImplementsFunction("array_diff")]
		[return: PhpDeepCopy]
		public static PhpArray Diff(PhpArray array, params PhpArray[] arrays)
		{
			return SetOperation(SetOperations.Difference, array, arrays, ValueComparer.String);
		}

		/// <summary>
		/// Computes the intersection of arrays.
		/// </summary>
		[ImplementsFunction("array_intersect")]
		[return: PhpDeepCopy]
		public static PhpArray Intersect(PhpArray array, params PhpArray[] arrays)
		{
			return SetOperation(SetOperations.Intersection, array, arrays, ValueComparer.String);
		}

		/// <summary>
		/// Computes the difference of arrays.
		/// </summary>
		/// <param name="array">The array from which to take items away.</param>
		/// <param name="arrays">The arrays to be differentiated.</param>
		/// <returns>The array containing all the entries of <paramref name="array"/> that are not present 
		/// in any of the <paramref name="arrays"/>.</returns>
		/// <remarks>Keys are preserved. Entries are considered to be equal iff they has the same keys and values
		/// according to string method comparison (see <see cref="EntryComparer"/> and <see cref="PhpStringComparer"/>).</remarks>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException"><paramref name="arrays"/> is a <B>null</B> reference or an empty array.</exception>
		[ImplementsFunction("array_diff_assoc")]
		[return: PhpDeepCopy]
		public static PhpArray DiffAssoc(PhpArray array, params PhpArray[] arrays)
		{
			return SetOperation(SetOperations.Difference, array, arrays,
				new EntryComparer(PhpStringComparer.Default, false, PhpStringComparer.Default, false));
		}

		/// <summary>
		/// Computes the intersection of arrays.
		/// </summary>
		[ImplementsFunction("array_intersect_assoc")]
		[return: PhpDeepCopy]
		public static PhpArray IntersectAssoc(PhpArray array, params PhpArray[] arrays)
		{
			return SetOperation(SetOperations.Intersection, array, arrays,
				new EntryComparer(PhpStringComparer.Default, false, PhpStringComparer.Default, false));
		}

		/// <summary>
		/// Computes the difference of arrays.
		/// </summary>
		/// <param name="array">The array from which to take items away.</param>
		/// <param name="arrays">The arrays to be differentiated.</param>
		/// <returns>The array containing all the entries of <paramref name="array"/> that are not present 
		/// in any of the <paramref name="arrays"/>.</returns>
		/// <remarks>Entries are considered to be equal iff keys compared by  
		/// by string comparison method are the same (see <see cref="KeyComparer.String"/>).</remarks>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException"><paramref name="arrays"/> is a <B>null</B> reference or an empty array.</exception>
		[ImplementsFunction("array_diff_key")]
		[return: PhpDeepCopy]
		public static PhpArray DiffKey(PhpArray array, params PhpArray[] arrays)
		{
			return SetOperation(SetOperations.Difference, array, arrays, KeyComparer.String);
		}

		/// <summary>
		/// Computes the intersection of arrays.
		/// </summary>
		[ImplementsFunction("array_intersect_key")]
		[return: PhpDeepCopy]
		public static PhpArray IntersectKey(PhpArray array, params PhpArray[] arrays)
		{
			return SetOperation(SetOperations.Intersection, array, arrays, KeyComparer.String);
		}

		/// <summary>
		/// Computes the difference of arrays using a specified key comparer.
		/// </summary>
		[ImplementsFunction("array_diff_ukey")]
		[return: PhpDeepCopy]
		public static PhpArray DiffDiffUser(PhpArray array, PhpArray array0, params object[] arraysAndComparer)
		{
			PhpArray[] arrays;
			PhpCallback key_comparer, cmp;

			if (!SplitArraysAndComparers(1, array0, arraysAndComparer, out arrays, out key_comparer, out cmp)) return null;

			return SetOperation(SetOperations.Difference, array, arrays,
				new KeyComparer(new PhpUserComparer(key_comparer), false));
		}

		/// <summary>
		/// Computes the intersection of arrays using a specified key comparer.
		/// </summary>
		[ImplementsFunction("array_intersect_ukey")]
		[return: PhpDeepCopy]
		public static PhpArray IntersectUserKey(PhpArray array, PhpArray array0, params object[] arraysAndComparer)
		{
			PhpArray[] arrays;
			PhpCallback key_comparer, cmp;

			if (!SplitArraysAndComparers(1, array0, arraysAndComparer, out arrays, out key_comparer, out cmp)) return null;

			return SetOperation(SetOperations.Intersection, array, arrays,
				new KeyComparer(new PhpUserComparer(key_comparer), false));
		}

		/// <summary>
		/// Computes the difference of arrays using a specified comparer.
		/// </summary>
		[ImplementsFunction("array_udiff")]
		[return: PhpDeepCopy]
		public static PhpArray UserDiff(PhpArray array, PhpArray array0, params object[] arraysAndComparer)
		{
			PhpArray[] arrays;
			PhpCallback value_comparer, cmp;

			if (!SplitArraysAndComparers(1, array0, arraysAndComparer, out arrays, out value_comparer, out cmp)) return null;

			return SetOperation(SetOperations.Difference, array, arrays,
				new ValueComparer(new PhpUserComparer(value_comparer), false));
		}

		/// <summary>
		/// Computes the intersection of arrays using a specified comparer.
		/// </summary>
		[ImplementsFunction("array_uintersect")]
		[return: PhpDeepCopy]
		public static PhpArray UserIntersect(PhpArray array, PhpArray array0, params object[] arraysAndComparer)
		{
			PhpArray[] arrays;
			PhpCallback value_comparer, cmp;

			if (!SplitArraysAndComparers(1, array0, arraysAndComparer, out arrays, out value_comparer, out cmp)) return null;

			return SetOperation(SetOperations.Intersection, array, arrays,
				new ValueComparer(new PhpUserComparer(value_comparer), false));
		}

		/// <summary>
		/// Computes the difference of arrays using a specified comparer.
		/// </summary>
		[ImplementsFunction("array_udiff_assoc")]
		[return: PhpDeepCopy]
		public static PhpArray UserDiffAssoc(PhpArray array, PhpArray array0, params object[] arraysAndComparer)
		{
			PhpArray[] arrays;
			PhpCallback value_comparer, cmp;

			if (!SplitArraysAndComparers(1, array0, arraysAndComparer, out arrays, out value_comparer, out cmp)) return null;

			return SetOperation(SetOperations.Difference, array, arrays,
				new EntryComparer(PhpStringComparer.Default, false, new PhpUserComparer(value_comparer), false));
		}

		/// <summary>
		/// Computes the intersection of arrays using a specified comparer.
		/// </summary>
		[ImplementsFunction("array_uintersect_assoc")]
		[return: PhpDeepCopy]
		public static PhpArray UserIntersectAssoc(PhpArray array, PhpArray array0, params object[] arraysAndComparer)
		{
			PhpArray[] arrays;
			PhpCallback value_comparer, cmp;

			if (!SplitArraysAndComparers(1, array0, arraysAndComparer, out arrays, out value_comparer, out cmp)) return null;

			return SetOperation(SetOperations.Intersection, array, arrays,
				new EntryComparer(PhpStringComparer.Default, false, new PhpUserComparer(value_comparer), false));
		}


		/// <summary>
		/// Computes the difference of arrays using a specified comparer.
		/// </summary>
		[ImplementsFunction("array_diff_uassoc")]
		[return: PhpDeepCopy]
		public static PhpArray DiffUserAssoc(PhpArray array, PhpArray array0, params object[] arraysAndComparer)
		{
			PhpArray[] arrays;
			PhpCallback key_comparer, cmp;

			if (!SplitArraysAndComparers(1, array0, arraysAndComparer, out arrays, out key_comparer, out cmp)) return null;

			return SetOperation(SetOperations.Difference, array, arrays,
				new EntryComparer(new PhpUserComparer(key_comparer), false, PhpStringComparer.Default, false));
		}

		/// <summary>
		/// Computes the intersection of arrays using a specified comparer.
		/// </summary>
		[ImplementsFunction("array_intersect_uassoc")]
		[return: PhpDeepCopy]
		public static PhpArray IntersectUserAssoc(PhpArray array, PhpArray array0, params object[] arraysAndComparer)
		{
			PhpArray[] arrays;
			PhpCallback key_comparer, cmp;

			if (!SplitArraysAndComparers(1, array0, arraysAndComparer, out arrays, out key_comparer, out cmp)) return null;

			return SetOperation(SetOperations.Intersection, array, arrays,
				new EntryComparer(new PhpUserComparer(key_comparer), false, PhpStringComparer.Default, false));
		}

		/// <summary>
		/// Computes the difference of arrays using specified comparers.
		/// </summary>
		[ImplementsFunction("array_udiff_uassoc")]
		[return: PhpDeepCopy]
		public static PhpArray UserDiffUserAssoc(PhpArray array, PhpArray array0, params object[] arraysAndComparers)
		{
			PhpArray[] arrays;
			PhpCallback key_comparer, value_comparer;

			if (!SplitArraysAndComparers(2, array0, arraysAndComparers, out arrays, out value_comparer, out key_comparer))
				return null;

			return SetOperation(SetOperations.Difference, array, arrays,
				new EntryComparer(new PhpUserComparer(key_comparer), false, new PhpUserComparer(value_comparer), false));
		}

		/// <summary>
		/// Computes the intersection of arrays using specified comparers.
		/// </summary>
		[ImplementsFunction("array_uintersect_uassoc")]
		[return: PhpDeepCopy]
		public static PhpArray UserIntersectUserAssoc(PhpArray array, PhpArray array0, params object[] arraysAndComparers)
		{
			PhpArray[] arrays;
			PhpCallback key_comparer, value_comparer;

			if (!SplitArraysAndComparers(2, array0, arraysAndComparers, out arrays, out value_comparer, out key_comparer))
				return null;

			return SetOperation(SetOperations.Intersection, array, arrays,
				new EntryComparer(new PhpUserComparer(key_comparer), false, new PhpUserComparer(value_comparer), false));
		}



		#endregion


		#region array_merge, array_merge_recursive

		/// <summary>
		/// Merges one or more arrays. Integer keys are changed to new ones, string keys are preserved.
		/// Values associated with existing string keys are be overwritten.
		/// </summary>
		/// <param name="arrays">Arrays to be merged.</param>
		/// <returns>The <see cref="PhpArray"/> containing items from all <paramref name="arrays"/>.</returns>
		[ImplementsFunction("array_merge")]
		[return: PhpDeepCopy]
		public static PhpArray Merge(params PhpArray[] arrays)
		{
			// "arrays" argument is PhpArray[] => compiler generates code converting any value to PhpArray.
			// Note, PHP does reject non-array arguments.

			if (arrays == null || arrays.Length == 0)
			{
				PhpException.InvalidArgument("arrays", LibResources.GetString("arg:null_or_empty"));
				return null;
			}

			PhpArray result = new PhpArray(arrays[0].IntegerCount, arrays[0].StringCount);

			for (int i = 0; i < arrays.Length; i++)
			{
                if (arrays[i] != null)
                {
                    using (var enumerator = arrays[i].GetFastEnumerator())
                        while (enumerator.MoveNext())
                        {
                            if (enumerator.CurrentKey.IsString)
                                result[enumerator.CurrentKey] = enumerator.CurrentValue;
                            else
                                result.Add(enumerator.CurrentValue);
                        }
                }
			}

			// results is inplace deeply copied if returned to PHP code:
			result.InplaceCopyOnReturn = true;
			return result;
		}

		/// <summary>
		/// Merges arrays recursively.
		/// </summary>
		/// <param name="array">The first array to merge.</param>
		/// <param name="arrays">The next arrays to merge.</param>
		/// <returns>An array containing items of all specified arrays.</returns>
		/// <remarks>
		/// Integer keys are reset so there cannot be a conflict among them. 
		/// Conflicts among string keys are resolved by merging associated values into arrays. 
		/// Merging is propagated recursively. Merged values are dereferenced. References are 
		/// preserved in non-merged values.
		/// </remarks>
		/// <exception cref="PhpException">Some array is a <B>null</B> reference (Warning).</exception>
		[ImplementsFunction("array_merge_recursive")]
		public static PhpArray MergeRecursiveDc(PhpArray array, params PhpArray[] arrays)
		{
			if (array == null || arrays == null)
			{
				PhpException.ArgumentNull((array == null) ? "array" : "arrays");
				return null;
			}

			for (int i = 0; i < arrays.Length; i++)
			{
				if (arrays[i] == null)
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("argument_not_array", i + 2));
					return null;
				}
			}

			return MergeRecursive(array, true, arrays);
		}

		/// <summary>
		/// Merges arrays recursively.
		/// </summary>
		/// <param name="array">The first array to merge.</param>
		/// <param name="arrays">The next arrays to merge.</param>
		/// <param name="deepCopy">Whether to deep copy merged items.</param>
		/// <returns>An array containing items of all specified arrays.</returns>
		public static PhpArray MergeRecursive(PhpArray array, bool deepCopy, params PhpArray[] arrays)
		{
			if (array == null) return null;

			PhpArray result = new PhpArray();
			array.AddTo(result, deepCopy);

			if (arrays != null)
			{
				for (int i = 0; i < arrays.Length; i++)
				{
					if (arrays[i] != null)
					{
						if (!MergeRecursiveInternal(result, arrays[i], deepCopy))
							PhpException.Throw(PhpError.Warning, LibResources.GetString("recursion_detected"));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Adds items of "array" to "result" merging those whose string keys are the same.
		/// </summary>
		private static bool MergeRecursiveInternal(PhpArray/*!*/ result, PhpArray/*!*/ array, bool deepCopy)
		{
			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
				if (entry.Key.IsString)
				{
					if (result.ContainsKey(entry.Key))
					{
						// the result array already contains the item => merging take place
						object xv = result[entry.Key];
						object yv = entry.Value;

						// source item:
						object x = PhpVariable.Dereference(xv);
						object y = PhpVariable.Dereference(yv);
						PhpArray ax = x as PhpArray;
						PhpArray ay = y as PhpArray;

						// if x is not a reference then we can reuse the ax array for the result
						// since it has been deeply copied when added to the resulting array:
						PhpArray item_result = (deepCopy && x == xv && ax != null) ? ax : new PhpArray();

						if (ax != null && ay != null)
						{
							if (ax != item_result)
								ax.AddTo(item_result, deepCopy);

							if (ax.Visited && ay.Visited) return false;

							ax.Visited = true;
							ay.Visited = true;

							// merges ay to the item result (may lead to stack overflow, 
							// but only with both arrays recursively referencing themselves - who cares?):
							bool finite = MergeRecursiveInternal(item_result, ay, deepCopy);

							ax.Visited = false;
							ay.Visited = false;

							if (!finite) return false;
						}
						else
						{
							if (ax != null)
							{
								if (ax != item_result)
									ax.AddTo(item_result, deepCopy);
							}
							else
							{
								/*if (x != null)*/
									item_result.Add((deepCopy) ? PhpVariable.DeepCopy(x) : x);
							}

							if (ay != null) ay.AddTo(item_result, deepCopy);
							else /*if (y != null)*/ item_result.Add((deepCopy) ? PhpVariable.DeepCopy(y) : y);
						}

						result[entry.Key] = item_result;
					}
					else
					{
						// PHP does no dereferencing when items are not merged:
						result.Add(entry.Key, (deepCopy) ? PhpVariable.DeepCopy(entry.Value) : entry.Value);
					}
				}
				else
				{
					// PHP does no dereferencing when items are not merged:
					result.Add((deepCopy) ? PhpVariable.DeepCopy(entry.Value) : entry.Value);
				}
			}

			return true;
		}

		#endregion


		#region array_change_key_case

		/// <summary>
		/// Converts string keys in <see cref="PhpArray"/> to lower case.
		/// </summary>
		/// <param name="array">The <see cref="PhpArray"/> to be converted.</param>
		/// <returns>The copy of <paramref name="array"/> with all string keys lower cased.</returns>
		/// <remarks>Integer keys as well as all values remain unchanged.</remarks>
		public static PhpArray StringKeysToLower(PhpArray/*!*/ array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			PhpArray result = new PhpArray();

            System.Globalization.TextInfo textInfo = null; // cache current culture to avoid repetitious CurrentCulture.get

			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
                if (entry.Key.IsString)
                {
                    if (textInfo == null) textInfo = System.Globalization.CultureInfo.CurrentCulture.TextInfo;
                    result[textInfo.ToLower(entry.Key.String)] = entry.Value;
                }
                else
                    result[entry.Key] = entry.Value;
			}
			return result;
		}

		/// <summary>
		/// Converts string keys in <see cref="PhpArray"/> to upper case.
		/// </summary>
		/// <param name="array">The <see cref="PhpArray"/> to be converted.</param>
		/// <returns>The copy of <paramref name="array"/> with all string keys upper cased.</returns>
		/// <remarks>Integer keys as well as all values remain unchanged.</remarks>
		public static PhpArray StringKeysToUpper(PhpArray/*!*/ array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			PhpArray result = new PhpArray();
			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
				if (entry.Key.IsString)
					result[entry.Key.String.ToUpper()] = entry.Value;
				else
					result[entry.Key] = entry.Value;
			}
			return result;
		}

		/// <summary>
		/// Converts string keys in <see cref="PhpArray"/> to lower case.
		/// </summary>
		/// <param name="array">The <see cref="PhpArray"/> to be converted.</param>
		/// <returns>The copy of <paramref name="array"/> with all string keys lower cased.</returns>
		/// <remarks>Integer keys as well as all values remain unchanged.</remarks>
		[ImplementsFunction("array_change_key_case")]
		[return: PhpDeepCopy]
		public static PhpArray ChangeKeyCase(PhpArray/*!*/ array)
		{
			PhpArray result = StringKeysToLower(array);
			result.InplaceCopyOnReturn = true;
			return result;
		}

		/// <summary>
		/// Converts string keys in <see cref="PhpArray"/> to specified case.
		/// </summary>
		/// <param name="array">The <see cref="PhpArray"/> to be converted.</param>
		/// <param name="keyCase">The <see cref="LetterCase"/> to convert keys to.</param>
		/// <returns>The copy of <paramref name="array"/> with all string keys lower cased.</returns>
		/// <remarks>Integer keys as well as all values remain unchanged.</remarks>
		[ImplementsFunction("array_change_key_case")]
		[return: PhpDeepCopy]
		public static PhpArray ChangeKeyCase(PhpArray array, LetterCase keyCase)
		{
			PhpArray result;
			switch (keyCase)
			{
				case LetterCase.Lower: result = StringKeysToLower(array); break;
				case LetterCase.Upper: result = StringKeysToUpper(array); break;

				default:
					PhpException.InvalidArgument("keyCase");
					goto case LetterCase.Upper;
			}
			result.InplaceCopyOnReturn = true;
			return result;
		}

		#endregion


		#region array_chunk

		/// <summary>
		/// Splits an array into chunks.
		/// </summary>
		/// <param name="array">The array to be split.</param>
		/// <param name="size">The number of items in each chunk (except for the last one where can be lesser items).</param>
		/// <returns>The array containing chunks indexed by integers starting from zero, 
		/// all keys in chunks are reindexed starting from zero.</returns>
		/// <remarks>Chunks will contain deep copies of <paramref name="array"/> items.</remarks>
		[ImplementsFunction("array_chunk")]
		public static PhpArray ChunkDc(PhpArray array, int size)
		{
			return ChunkInternal(array, size, false, true);
		}

		/// <summary>
		/// Splits an array into chunks.
		/// </summary>
		/// <param name="array">The array to be split.</param>
		/// <param name="size">The number of items in each chunk (except for the last one where can be lesser items).</param>
		/// <param name="preserveKeys">Whether to preserve keys in chunks.</param>
		/// <returns>The array containing chunks indexed by integers starting from zero.</returns>
		/// <remarks>Chunks will contain deep copies of <paramref name="array"/> items.</remarks>
		[ImplementsFunction("array_chunk")]
		public static PhpArray ChunkDc(PhpArray array, int size, bool preserveKeys)
		{
			return ChunkInternal(array, size, preserveKeys, true);
		}

		/// <summary>
		/// Splits an array into chunks.
		/// </summary>
		/// <param name="array">The array to be split.</param>
		/// <param name="size">The number of items in each chunk (except for the last one where can be lesser items).</param>
		/// <returns>The array containing chunks indexed by integers starting from zero, 
		/// all keys in chunks are reindexed starting from zero.</returns>
		public static PhpArray Chunk(PhpArray array, int size)
		{
			return ChunkInternal(array, size, false, false);
		}

		/// <summary>
		/// Splits an array into chunks.
		/// </summary>
		/// <param name="array">The array to be split.</param>
		/// <param name="size">The number of items in each chunk (except for the last one where can be lesser items).</param>
		/// <param name="preserveKeys">Whether to preserve keys in chunks.</param>
		/// <returns>The array containing chunks indexed by integers starting from zero.</returns>
		public static PhpArray Chunk(PhpArray array, int size, bool preserveKeys)
		{
			return ChunkInternal(array, size, preserveKeys, false);
		}

		/// <summary>
		/// Internal version of <see cref="Chunk"/> with deep-copy option.
		/// </summary>
		internal static PhpArray ChunkInternal(PhpArray array, int size, bool preserveKeys, bool deepCopy)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}
			if (size <= 0)
			{
				PhpException.InvalidArgument("array", LibResources.GetString("arg:negative_or_zero"));
				return null;
			}

			// nothing to do:
			if (array.Count == 0)
				return new PhpArray();

			// number of chunks:
			int count = (array.Count - 1) / size + 1; // = ceil(Count/size):

			PhpArray chunk;
			PhpArray result = new PhpArray(count, 0);

			IEnumerator<KeyValuePair<IntStringKey, object>> iterator;

			// if deep-copies are required, wrapp iterator by enumerator making deep copies:
			if (deepCopy)
				iterator = PhpVariable.EnumerateDeepCopies(array).GetEnumerator();
			else
				iterator = array.GetEnumerator();

			iterator.MoveNext();

			// all chunks except for the last one:
			for (int i = 0; i < count - 1; i++)
			{
				chunk = new PhpArray(size, 0);

				if (preserveKeys)
				{
					for (int j = 0; j < size; j++, iterator.MoveNext())
						chunk.Add(iterator.Current.Key, iterator.Current.Value);
				}
				else
				{
					for (int j = 0; j < size; j++, iterator.MoveNext())
						chunk.Add(iterator.Current.Value);
				}

				result.Add(chunk);
			}

			// the last chunk:
			chunk = new PhpArray((size <= array.Count) ? size : array.Count, 0);

			if (preserveKeys)
			{
				do { chunk.Add(iterator.Current.Key, iterator.Current.Value); } while (iterator.MoveNext());
			}
			else
			{
				do { chunk.Add(iterator.Current.Value); } while (iterator.MoveNext());
			}

			result.Add(chunk);

			// no deep copy is needed since it has already been done on chunks:
			return result;
		}

		#endregion


		#region array_count_values, array_unique

		/// <summary>
		/// Counts frequency of each value in an array.
		/// </summary>
		/// <param name="array">The array which values to count.</param>
		/// <returns>The array which keys are values of <paramref name="array"/> and values are their frequency.</returns>
		/// <remarks>
		/// Only <see cref="string"/> and <see cref="int"/> values are counted.
		/// Note, string numbers (e.g. "10") and their integer equivalents (e.g. 10) are counted separately.
		/// </remarks>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException">A value is neither <see cref="string"/> nor <see cref="int"/>.</exception>
		[ImplementsFunction("array_count_values")]
		public static PhpArray CountValues(PhpArray array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			string skey;
			PhpArray result = new PhpArray();

			IEnumerator iterator = array.Values.GetEnumerator();
			while (iterator.MoveNext())
			{
				// dereferences value:
				object val = PhpVariable.Dereference(iterator.Current);

				if (val is int)
				{
					int ikey = (int)val;
					object q = result[ikey];
					result[ikey] = (q == null) ? 1 : (int)q + 1;
				}
				else if ((skey = PhpVariable.AsString(val)) != null)
				{
					object q = result[skey];
					result[skey] = (q == null) ? 1 : (int)q + 1;
				}
				else
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("neither_string_nor_integer_value", "count"));
				}
			}

			// no need to deep copy (values are ints):
			return result;
		}

        public enum ArrayUniqueSortFlags
	    {
            /// <summary>
            /// compare items normally (don't change types)
            /// </summary>
            [ImplementsConstant("SORT_REGULAR")]
            Regular = 0,

            /// <summary>
            /// compare items numerically
            /// </summary>
            [ImplementsConstant("SORT_NUMERIC")]
            Numeric = 1,
        
            /// <summary>
            /// compare items as strings, default
            /// </summary>
            [ImplementsConstant("SORT_STRING")]
            String = 2,
        
            /// <summary>
            /// compare items as strings, based on the current locale.
            /// Added in PHP 4.4.0 and 5.0.2. Before PHP 6, it uses the system locale,
            /// which can be changed using setlocale(). Since PHP 6, you must use the i18n_loc_set_default() function.
            /// </summary>
            [ImplementsConstant("SORT_LOCALE_STRING")]
            LocaleString = 5,
		
        }

        /// <summary>
		/// Removes duplicate values from an array.
		/// </summary>
		/// <param name="array">The array which duplicate values to remove.</param>
		/// <returns>A copy of <paramref name="array"/> without duplicated values.</returns>
		/// <remarks>
		/// Values are compared using string comparison method (<see cref="ValueComparer.String"/>).  
		/// </remarks>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		[ImplementsFunction("array_unique")]
		[return: PhpDeepCopy]
		public static PhpArray Unique(PhpArray array)
        {
            return Unique(array, ArrayUniqueSortFlags.String);
        }

		/// <summary>
		/// Removes duplicate values from an array.
		/// </summary>
		/// <param name="array">The array which duplicate values to remove.</param>
        /// <param name="sortFlags">Specifies how the values are compared to be identical.</param>
		/// <returns>A copy of <paramref name="array"/> without duplicated values.</returns>
		/// <remarks>
		/// Values are compared using string comparison method (<see cref="ValueComparer.String"/>).  
		/// </remarks>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		[ImplementsFunction("array_unique")]
		[return: PhpDeepCopy]
		public static PhpArray Unique(PhpArray array, ArrayUniqueSortFlags sortFlags /*= String*/)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			IComparer comparer;
            switch(sortFlags)
            {
                case ArrayUniqueSortFlags.Regular:
                    comparer = PhpComparer.Default; break;
                case ArrayUniqueSortFlags.Numeric:
                    comparer = PhpNumericComparer.Default; break;
                case ArrayUniqueSortFlags.String:
                    comparer = PhpStringComparer.Default; break;
                case ArrayUniqueSortFlags.LocaleString:
                default:
                    PhpException.ArgumentValueNotSupported("sortFlags", (int)sortFlags);
                    return null;
            }

            PhpArray result = new PhpArray(array.Count);

            HashSet<object>/*!*/identitySet = new HashSet<object>(new ObjectEqualityComparer(comparer));

            // get only unique values - first found
            using (var enumerator = array.GetFastEnumerator())
                while (enumerator.MoveNext())
                {
                    if (identitySet.Add(PhpVariable.Dereference(enumerator.CurrentValue)))
                        result.Add(enumerator.Current);
                }

			result.InplaceCopyOnReturn = true;
			return result;
		}

		#endregion


		#region array_flip

		/// <summary>
		/// Swaps all keys and their associated values in an array.
		/// </summary>
		/// <param name="array">The array.</param>
		/// <returns>An array containing entries which keys are values from the <paramref name="array"/>
		/// and which values are the corresponding keys.</returns>
		/// <remarks>
		/// <para>
		/// Values which are not of type <see cref="string"/> nor <see cref="int"/> are skipped 
		/// and for each such value a warning is reported. If there are more entries with the same 
		/// value in the <paramref name="array"/> the last key is considered others are ignored.
		/// String keys are converted using <see cref="Core.Convert.StringToArrayKey"/>.
		/// </para>
		/// <para>
		/// Unlike PHP this method doesn't return <B>false</B> on failure but a <B>null</B> reference.
		/// This is because it fails only if <paramref name="array"/> is a <B>null</B> reference.
		/// </para>
		/// </remarks>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference (Warning).</exception>
		/// <exception cref="PhpException">A value is neither <see cref="string"/> nor <see cref="int"/> (Warning).</exception>     
		[ImplementsFunction("array_flip")]
		public static PhpArray Flip(PhpArray array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			string skey;
			PhpArray result = new PhpArray();

			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
				// dereferences value:
				object val = PhpVariable.Dereference(entry.Value);

				if (val is int)
				{
					result[(int)val] = entry.Key.Object;
				}
				else if ((skey = PhpVariable.AsString(val)) != null)
				{
					result[Core.Convert.StringToArrayKey(skey)] = entry.Key.Object;
				}
				else
				{
					PhpException.Throw(PhpError.Warning, LibResources.GetString("neither_string_nor_integer_value", "flip"));
				}
			}

			// no need to deep copy because values are ints/strings only (<= keys were int/strings only):
			return result;
		}

		#endregion


		#region array_keys, array_values, array_combine

		/// <summary>
		/// Retrieves an array of keys contained in a given array.
		/// </summary>
		/// <param name="array">An array which keys to get.</param>
		/// <returns><see cref="PhpArray"/> of <paramref name="array"/>'s keys.
		/// Keys in returned array are successive integers starting from zero.</returns>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		[ImplementsFunction("array_keys")]
		public static PhpArray Keys(PhpArray array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			// no need to make a deep copy since keys are immutable objects (strings, ints):
            var result = new PhpArray(array.Count);
            using (var enumerator = array.GetFastEnumerator())
                while (enumerator.MoveNext())
                    result.AddToEnd(enumerator.CurrentKey.Object);

            return result;
		}

		/// <summary>
		/// Retrieves an array of some keys contained in a given array.
		/// </summary>
		/// <param name="array">An array which keys to get.</param>
		/// <param name="searchValue">Only the keys for this value are returned. 
		/// Values are compared using regular comparison method (<see cref="PhpComparer.CompareEq"/>).</param>
		/// <returns>An array of keys being associated with specified value. 
		/// Keys in returned array are successive integers starting from zero.</returns>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		[ImplementsFunction("array_keys")]
		public static PhpArray Keys(PhpArray array, object searchValue)
		{
            return Keys(array, searchValue, false);
		}

        /// <summary>
        /// Retrieves an array of some keys contained in a given array.
        /// </summary>
        /// <param name="array">An array which keys to get.</param>
        /// <param name="searchValue">Only the keys for this value are returned. 
        /// Values are compared using regular comparison method (<see cref="PhpComparer.CompareEq"/>).</param>
        /// <param name="strict">If true, uses strict comparison method (operator "===").</param>
        /// <returns>An array of keys being associated with specified value. 
        /// Keys in returned array are successive integers starting from zero.</returns>
        /// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
        [ImplementsFunction("array_keys")]
        public static PhpArray Keys(PhpArray array, object searchValue, bool strict)
        {
            if (array == null)
            {
                PhpException.ArgumentNull("array");
                return null;
            }

            PhpArray result = new PhpArray();

            if (!strict)
            {
                using (var enumerator = array.GetFastEnumerator())
                    while (enumerator.MoveNext())
                    {
                        if (PhpComparer.CompareEq(enumerator.CurrentValue, searchValue))
                            result.AddToEnd(enumerator.CurrentKey.Object);
                    }
            }
            else
            {
                using (var enumerator = array.GetFastEnumerator())
                    while (enumerator.MoveNext())
                    {
                        if (Operators.StrictEquality(enumerator.CurrentValue, searchValue))
                            result.AddToEnd(enumerator.CurrentKey.Object);
                    }
            }

            // no need to make a deep copy since keys are immutable objects (strings, ints):
            return result;
        }

		/// <summary>
		/// Retrieves an array of values contained in a given array.
		/// </summary>
		/// <param name="array">An array which values to get.</param>
		/// <returns>A copy of <paramref name="array"/> with all keys indexed starting from zero.</returns>
		/// <exception cref="PhpException"><paramref name="array"/> is a <B>null</B> reference.</exception>
		/// <remarks>Doesn't dereference PHP references.</remarks>
		[ImplementsFunction("array_values")]
		[return: PhpDeepCopy]
		public static PhpArray Values(PhpArray array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return null;
			}

			// references are not dereferenced:
            PhpArray result = new PhpArray(array.Count);
            using (var enumerator = array.GetFastEnumerator())
                while (enumerator.MoveNext())
                    result.AddToEnd(enumerator.CurrentValue);
                
			// result is inplace deeply copied on return to PHP code:
			result.InplaceCopyOnReturn = true;
			return result;
		}

		/// <summary>
		/// Creates an array using one array for its keys and the second for its values.
		/// </summary>
		/// <param name="keys">The keys of resulting array.</param>
		/// <param name="values">The values of resulting array.</param>
		/// <returns>An array with keys from <paramref name="keys"/> values and values 
		/// from <paramref name="values"/> values.</returns>
		/// <remarks>
		/// <paramref name="keys"/> and <paramref name="values"/> should have the same length (zero is 
		/// adminssible - an empty array is returned).
		/// Keys are converted using <see cref="PHP.Core.Convert.ObjectToArrayKey"/> before hashed to resulting array.
		/// If more keys has the same value after conversion the last one is used.
		/// If a key is not a legal array key it is skipped.
		/// </remarks>
		/// <exception cref="PhpException"><paramref name="keys"/> or <paramref name="values"/> is a <B>null</B> reference.</exception>
		/// <exception cref="PhpException"><paramref name="keys"/> and <paramref name="values"/> has different length.</exception>
		/// <remarks>Doesn't dereference PHP references.</remarks>
		[ImplementsFunction("array_combine")]
		[return: PhpDeepCopy]
		public static PhpArray Combine(PhpArray keys, PhpArray values)
		{
			if (keys == null)
			{
				PhpException.ArgumentNull("keys");
				return null;
			}
			if (values == null)
			{
				PhpException.ArgumentNull("values");
				return null;
			}
			if (keys.Count != values.Count)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("lengths_are_different", "keys", "values"));
				return null;
			}

			PhpArray result = new PhpArray();
			IEnumerator k_iterator = keys.Values.GetEnumerator();
			IEnumerator v_iterator = values.Values.GetEnumerator();
			while (k_iterator.MoveNext())
			{
				v_iterator.MoveNext();

				// invalid keys are skipped, values are not dereferenced:
				IntStringKey key;
				if (PHP.Core.Convert.ObjectToArrayKey(k_iterator.Current, out key))
					result[key] = v_iterator.Current;
			}

			// result is inplace deeply copied on return to PHP code:
			result.InplaceCopyOnReturn = true;
			return result;
		}

		#endregion


		#region array_sum, array_product, array_reduce

		/// <summary>
		/// Sums all values in an array. Each value is converted to a number in the same way it is done by PHP.
		/// </summary>
		/// <exception cref="PhpException">Thrown if the <paramref name="array"/> argument is null.</exception>
		/// <returns>
		/// An integer, if all items are integers or strings converted to integers and the result is in integer range.
		/// A double, otherwise.
		/// </returns>
		[ImplementsFunction("array_sum")]
		public static object Sum(PhpArray array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return 0.0;
			}

			if (array.Count == 0)
				return 0;

			double dval;
			int ival;
			long lval;

			double dresult = 0.0;
			long lresult = 0;
			bool overflown = false;

			Core.Convert.NumberInfo info_result = 0;

			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
				Core.Convert.NumberInfo info;

				info = Core.Convert.ObjectToNumber(entry.Value, out ival, out lval, out dval) & Core.Convert.NumberInfo.TypeMask;

				try
				{
					dresult += dval;
					if (!overflown) lresult += lval;
				}
				catch (OverflowException)
				{
					overflown = true;
				}

				info_result |= info;
			}

			if ((info_result & Core.Convert.NumberInfo.Double) != 0)
				return dresult;

			if ((info_result & Core.Convert.NumberInfo.LongInteger) != 0
			 || lresult < Int32.MinValue || lresult > Int32.MaxValue)
			{
				if (overflown) return dresult; else return lresult;
			}

			return (int)lresult;
		}

		/// <summary>
		/// Computes a product of all values in an array. 
		/// Each value is converted to a number in the same way it is done by PHP.
		/// </summary>
		/// <exception cref="PhpException">Thrown if the <paramref name="array"/> argument is null.</exception>
		/// <returns>
		/// An integer, if all items are integers or strings converted to integers and the result is in integer range.
		/// A double, otherwise.
		/// </returns>
		[ImplementsFunction("array_product")]
		public static object Product(PhpArray array)
		{
			if (array == null)
			{
				PhpException.ArgumentNull("array");
				return 0;
			}

			if (array.Count == 0)
				return 0;

			double dval;
			int ival;
			long lval;

			double dresult = 1.0;
			long lresult = 1;
			bool overflown = false;

			Core.Convert.NumberInfo info_result = 0;

			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
				Core.Convert.NumberInfo info;

				info = Core.Convert.ObjectToNumber(entry.Value, out ival, out lval, out dval) & Core.Convert.NumberInfo.TypeMask;

				try
				{
					dresult *= dval;
					if (!overflown) lresult *= lval;
				}
				catch (OverflowException)
				{
					overflown = true;
				}

				info_result |= info;
			}

			if ((info_result & Core.Convert.NumberInfo.Double) != 0)
				return dresult;

			if ((info_result & Core.Convert.NumberInfo.LongInteger) != 0
			 || lresult < Int32.MinValue || lresult > Int32.MaxValue)
			{
				if (overflown) return dresult; else return lresult;
			}

			return (int)lresult;
		}

		[ImplementsFunction("array_reduce", FunctionImplOptions.NeedsClassContext)]
        public static object Reduce(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpArray array, PhpCallback function)
		{
			return Reduce(caller, array, function, null);
		}

		[ImplementsFunction("array_reduce", FunctionImplOptions.NeedsClassContext)]
        public static object Reduce(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpArray array, PhpCallback function, [PhpDeepCopy] object initialValue)
		{
			if (array == null) { PhpException.ReferenceNull("array"); return null; }
			if (!PhpArgument.CheckCallback(function, caller, "function", 0, false)) return null;
			if (array.Count == 0) return initialValue;

			object[] args = new object[] { initialValue, null };
			PhpReference holder = new PhpReference();

			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
				object item = entry.Value;
				PhpReference ref_item = item as PhpReference;

				// array item is a reference:
				if (ref_item != null)
				{
					args[1] = item;
					args[0] = function.Invoke(args);
				}
				else
				{
					// array item is not a reference:

					holder.Value = item;
					args[1] = holder;
					args[0] = function.Invoke(args);

					// updates an item if it has been changed:
					if (item != holder.Value)
						array[entry.Key] = holder.Value;
				}
			}

			// dereferences the last returned value:
			return PhpVariable.Dereference(args[0]);
		}

		#endregion


		#region array_walk, array_walk_recursive

		/// <summary>
		/// Applies a user function or method on each element of a specified array or dictionary.
		/// </summary>
		/// <returns><B>true</B>.</returns>
        /// <remarks>See <see cref="Walk(PHP.Core.Reflection.DTypeDesc,PhpHashtable,PhpCallback,object)"/> for details.</remarks>
		/// <exception cref="PhpException"><paramref name="function"/> or <paramref name="array"/> are <B>null</B> references.</exception>
		[ImplementsFunction("array_walk", FunctionImplOptions.NeedsClassContext)]
        public static bool Walk(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpHashtable array, PhpCallback function)
		{
			return Walk(caller, array, function, null);
		}

		/// <summary>
		/// Applies a user function or method on each element (value) of a specified dictionary.
		/// </summary>
        /// <param name="caller">Current class context.</param>
		/// <param name="array">The array (or generic dictionary) to walk through.</param>
		/// <param name="callback">
		/// The callback called for each element of <paramref name="array"/>.
		/// The callback is assumed to have two or three parameters:
		/// <list type="number">
		///   <item>
		///     <term>
		///       A value of dictionary entry. Can be specified with &amp; modifier which propagates any changes
		///       make to the argument back to the entry. The dictionary can be changed in this way.
		///     </term>
		///   </item>
		///   <item>A key of dictionary entry.</item>
		///   <item>
		///     Value of <paramref name="data"/> parameter if it is not a <B>null</B> reference.
		///     Otherwise, the callback is assumed to have two parameters only.
		///   </item>
		/// </list>
		/// </param>
		/// <param name="data">An additional parameter passed to <paramref name="callback"/> as its third parameter.</param>
		/// <returns><B>true</B>.</returns>
		/// <exception cref="PhpException"><paramref name="callback"/> or <paramref name="array"/> are <B>null</B> references.</exception>
		[ImplementsFunction("array_walk", FunctionImplOptions.NeedsClassContext)]
        public static bool Walk(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpHashtable array, PhpCallback callback, object data)
		{
			object[] args = PrepareWalk(array, callback, data);
			if (args == null) return false;

			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
				VisitEntryOnWalk(caller, entry, array, callback, args);
			}

			return true;
		}

		/// <summary>
		/// Applies a user function or method on each element of a specified array recursively.
		/// </summary>
		/// <returns><B>true</B>.</returns>
        /// <remarks>See <see cref="Walk(PHP.Core.Reflection.DTypeDesc,PhpHashtable,PhpCallback,object)"/> for details.</remarks>
        /// <exception cref="PhpException"><paramref name="callback"/> or <paramref name="array"/> are <B>null</B> references.</exception>
        [ImplementsFunction("array_walk_recursive", FunctionImplOptions.NeedsClassContext)]
		public static bool WalkRecursive(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpHashtable array, PhpCallback callback)
		{
			return WalkRecursive(caller, array, callback, null);
		}

		/// <summary>
		/// Applies a user function or method on each element (value) of a specified dictionary recursively.
		/// </summary>
        /// <param name="caller">Current class context.</param>
		/// <param name="array">The array to walk through.</param>
		/// <param name="callback">The callback called for each element of <paramref name="array"/>.</param>
		/// <param name="data">An additional parameter passed to <paramref name="callback"/> as its third parameter.</param>
        /// <exception cref="PhpException"><paramref name="callback"/> or <paramref name="array"/> are <B>null</B> references.</exception>
		/// <remarks><seealso cref="Walk"/>.</remarks>
		[ImplementsFunction("array_walk_recursive", FunctionImplOptions.NeedsClassContext)]
		public static bool WalkRecursive(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpHashtable array, PhpCallback callback, object data)
		{
			object[] args = PrepareWalk(array, callback, data);
			if (args == null) return false;

			using (PhpHashtable.RecursiveEnumerator iterator = array.GetRecursiveEnumerator(true,false))
			{
				while (iterator.MoveNext())
				{
					// visits the item unless it is an array or a reference to an array:
					PhpReference ref_value = iterator.Current.Value as PhpReference;
					if (!(iterator.Current.Value is PhpHashtable || (ref_value != null && ref_value.Value is PhpHashtable)))
						VisitEntryOnWalk(caller, iterator.Current, iterator.CurrentTable, callback, args);
				}
			}
			return true;
		}

		/// <summary>
		/// Prepares a walk for <see cref="Walk"/> and <see cref="WalkRecursive"/> methods.
		/// </summary>
        /// <exception cref="PhpException"><paramref name="callback"/> or <paramref name="array"/> are <B>null</B> references.</exception>
		private static object[] PrepareWalk(IDictionary array, PhpCallback callback, object data)
		{
			if (callback == null) { PhpException.ArgumentNull("callback"); return null; }
			if (array == null) { PhpException.ArgumentNull("array"); return null; }

			// prepares an array of callback's arguments (no deep copying needed because it is done so in callback):
			if (data != null)
				return new object[3] { new PhpReference(), null, data };
			else
				return new object[2] { new PhpReference(), null };
		}

		/// <summary>
		/// Visits an entyr of array which <see cref="Walk"/> or <see cref="WalkRecursive"/> is walking through.
		/// </summary>
		private static void VisitEntryOnWalk(PHP.Core.Reflection.DTypeDesc caller, KeyValuePair<IntStringKey, object> entry, IDictionary<IntStringKey, object> array,
			PhpCallback callback, object[] args)
		{
			PhpReference ref_item = entry.Value as PhpReference;

			// fills arguments for the callback:
			((PhpReference)args[0]).Value = (ref_item != null) ? ref_item.Value : entry.Value;
			args[1] = entry.Key.Object;

			// invoke callback:
            Core.Convert.ObjectToBoolean(callback.Invoke(caller, args));

			// loads a new value from a reference:
			if (ref_item != null)
			{
				ref_item.Value = ((PhpReference)args[0]).Value;
			}
			else
			{
				array[entry.Key] = ((PhpReference)args[0]).Value;
			}
		}

		#endregion


		#region array_filter

		/// <summary>
		/// Retuns the specified array.
        /// see http://php.net/manual/en/function.array-filter.php
		/// </summary>
        /// <remarks>The caller argument is here just because of the second Filter() method. Phalanger shares the function properties over the overloads.</remarks>
        [ImplementsFunction("array_filter", FunctionImplOptions.NeedsClassContext)]
        [return: PhpDeepCopy]
        public static PhpArray Filter(PHP.Core.Reflection.DTypeDesc _, PhpArray array)
        {
            var _result = new PhpArray();

            using (var enumerator = array.GetFastEnumerator())
                while (enumerator.MoveNext())
                    if (Core.Convert.ObjectToBoolean(enumerator.CurrentValue))
                        _result.Add(enumerator.CurrentKey, enumerator.CurrentValue);

            return _result;
        }

		/// <summary>
		/// Filters an array using a specified callback.
		/// </summary>
        /// <param name="caller">Current class context.</param>
		/// <param name="array">The array to be filtered.</param>
		/// <param name="callback">
		/// The callback called on each value in the <paramref name="array"/>. 
		/// If the callback returns value convertible to <B>true</B> the value is copied to the resulting array.
		/// Otherwise, it is ignored.
		/// </param>
		/// <returns>An array of unfiltered items.</returns>
		[ImplementsFunction("array_filter", FunctionImplOptions.NeedsClassContext)]
		[return: PhpDeepCopy]
		public static PhpArray Filter(PHP.Core.Reflection.DTypeDesc caller, PhpArray array, PhpCallback callback)
		{
			if (callback == null) { PhpException.ArgumentNull("callback"); return null; }
			if (array == null) { PhpException.ArgumentNull("array"); return null; }

			PhpArray result = new PhpArray();
			object[] args = new object[1];

			foreach (KeyValuePair<IntStringKey, object> entry in array)
			{
				// no deep copying needed because it is done so in callback:
				args[0] = entry.Value;

				// adds entry to the resulting array if callback returns true:
                if (Core.Convert.ObjectToBoolean(callback.Invoke(caller, args)))
				{
					result.Add(entry.Key, entry.Value);
				}
			}

			// values should be inplace deeply copied:
			result.InplaceCopyOnReturn = true;
			return result;
		}

		#endregion


		#region array_map

		/// <summary>
		/// Default callback for <see cref="Map"/>.
		/// </summary>
		/// <param name="instance">Unused.</param>
		/// <param name="stack">A PHP stack.</param>
		/// <returns>A <see cref="PhpArray"/> containing items on the stack (passed as arguments).</returns>
		private static object MapIdentity(object instance, PhpStack stack)
		{
			PhpArray result = new PhpArray(stack.ArgCount, 0);

			for (int i = 1; i <= stack.ArgCount; i++)
			{
				result.Add(PhpVariable.Copy(stack.PeekValueUnchecked(i), CopyReason.PassedByCopy));
			}
			stack.RemoveFrame();

			return result;
		}

		/// <summary>
		/// Applies a callback function on specified tuples one by one storing its results to an array.
		/// </summary>
        /// <param name="caller">The class context used to resolve given callback.</param>
		/// <param name="map">
		/// A callback to be called on tuples. The number of arguments should be the same as
		/// the number of arrays specified by <pramref name="arrays"/>.
		/// Arguments passed by reference modifies elements of <pramref name="arrays"/>.
		/// A <B>null</B> means default callback which makes integer indexed arrays from the tuples is used. 
		/// </param>
		/// <param name="arrays">Arrays where to load tuples from. </param>
		/// <returns>An array of return values of the callback
		/// keyed by keys of the <paramref name="arrays"/> if it
		/// is a single array or by integer keys starting from 0.</returns>
		/// <remarks>
		/// <para>
		/// In the <I>i</I>-th call the <I>j</I>-th parameter of the callback will be 
		/// the <I>i</I>-th value of the <I>j</I>-the array or a <B>null</B> if that array 
		/// has less then <I>i</I> entries.
		/// </para>
		/// <para>
		/// If the callback assigns a value to a parameter passed by reference in the <I>i</I>-the call 
		/// and the respective array contains at least <I>i</I> elements the assigned value is propagated 
		/// to the array.
		/// </para>
		/// </remarks>
		[ImplementsFunction("array_map", FunctionImplOptions.NeedsClassContext)]
        public static PhpArray Map(PHP.Core.Reflection.DTypeDesc caller, PhpCallback map, [PhpRw] params PhpArray[] arrays)
		{
			if (!PhpArgument.CheckCallback(map, caller, "map", 0, true)) return null;
			if (arrays == null || arrays.Length == 0)
			{
				PhpException.InvalidArgument("arrays", LibResources.GetString("arg:null_or_emtpy"));
				return null;
			}

			// if callback has not been specified uses the default one:
			if (map == null)
				map = new PhpCallback(new RoutineDelegate(MapIdentity), ScriptContext.CurrentContext);

			int count = arrays.Length;
			bool preserve_keys = count == 1;
			PhpReference[] args = new PhpReference[count];
			IEnumerator<KeyValuePair<IntStringKey, object>>[] iterators = new IEnumerator<KeyValuePair<IntStringKey, object>>[count];
			PhpArray result;

			// initializes iterators and args array, computes length of the longest array:
			int max_count = 0;
			for (int i = 0; i < arrays.Length; i++)
			{
                var array = arrays[i];

                if (array == null)
                {
                    PhpException.Throw(PhpError.Warning, LibResources.GetString("argument_not_array", i + 2));// +2 (first arg is callback) 
                    return null;
                }

				args[i] = new PhpReference();
                iterators[i] = array.GetEnumerator();
                if (array.Count > max_count) max_count = array.Count;
			}

			// keys are preserved in a case of a single array and re-indexed otherwise:
			if (preserve_keys)
				result = new PhpArray(arrays[0].IntegerCount, arrays[0].StringCount);
			else
				result = new PhpArray(max_count, 0);

			for (; ; )
			{
				// fills args[] with items from arrays:
				for (int i = 0; i < arrays.Length; i++)
				{
					if (iterators[i] != null)
					{
						// an element is available:
						if (iterators[i].MoveNext())
						{
							// note: deep copy is not necessary since a function copies its arguments if needed:
                            object value = iterators[i].Current.Value;
                            PhpReference valueref = (value != null) ? value as PhpReference : null;
                            args[i].Value = (valueref != null) ? valueref.value : value;
                            //args[i].Value = iterators[i].Current.Value; // TODO: throws if the current Value is PhpReference
						}
						else
						{
							// the i-th iterator has stopped:
							count--;
							iterators[i] = null;
							args[i].Value = null;
						}
					}
				}

				if (count == 0) break;

				// invokes callback:
				object return_value = map.Invoke(args);

				// return value is not deeply copied:
				if (preserve_keys)
					result.Add(iterators[0].Current.Key, return_value);
				else
					result.Add(return_value);

				// loads new values (callback may modify some by ref arguments):
				for (int i = 0; i < arrays.Length; i++)
				{
					if (iterators[i] != null)
					{
						object item = iterators[i].Current.Value;
						PhpReference ref_item = item as PhpReference;
						if (ref_item != null)
						{
							ref_item.Value = args[i].Value;
						}
						else
						{
							arrays[i][iterators[i].Current.Key] = args[i].Value;
						}
					}
				}
			}

			return result;
		}

		#endregion

        #region array_replace, array_replace_recursive

        /// <summary>
        /// array_replace() replaces the values of the first array with the same values from
        /// all the following arrays. If a key from the first array exists in the second array,
        /// its value will be replaced by the value from the second array. If the key exists in
        /// the second array, and not the first, it will be created in the first array. If a key
        /// only exists in the first array, it will be left as is. If several arrays are passed
        /// for replacement, they will be processed in order, the later arrays overwriting the
        /// previous values.
        ///  
        /// array_replace() is not recursive : it will replace values in the first array by
        /// whatever type is in the second array. 
        /// </summary>
        /// <param name="array">The array in which elements are replaced. </param>
        /// <param name="arrays">The arrays from which elements will be extracted. </param>
        /// <returns>Deep copy of array with replacements. Returns an array, or NULL if an error occurs. </returns>
        [ImplementsFunction("array_replace")]
        //[return: PhpDeepCopy]
        public static PhpArray ArrayReplace([PhpRw] PhpArray array, params PhpArray[] arrays)
        {
            return ArrayReplaceImpl(array, arrays, false);
        }

        /// <summary>
        ///  array_replace_recursive() replaces the values of the first array with the same values
        ///  from all the following arrays. If a key from the first array exists in the second array,
        ///  its value will be replaced by the value from the second array. If the key exists in the
        ///  second array, and not the first, it will be created in the first array. If a key only
        ///  exists in the first array, it will be left as is. If several arrays are passed for
        ///  replacement, they will be processed in order, the later array overwriting the previous
        ///  values.
        ///  
        /// array_replace_recursive() is recursive : it will recurse into arrays and apply the same
        /// process to the inner value.
        /// 
        /// When the value in array is scalar, it will be replaced by the value in array1, may it be
        /// scalar or array. When the value in array and array1 are both arrays, array_replace_recursive()
        /// will replace their respective value recursively. 
        /// </summary>
        /// <param name="array">The array in which elements are replaced. </param>
        /// <param name="arrays">The arrays from which elements will be extracted.</param>
        /// <returns>Deep copy of array with replacements. Returns an array, or NULL if an error occurs. </returns>
        [ImplementsFunction("array_replace_recursive")]
        //[return: PhpDeepCopy]
        public static PhpArray ArrayReplaceRecursive([PhpRw] PhpArray array, params PhpArray[] arrays)
        {
            return ArrayReplaceImpl(array, arrays, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrays"></param>
        /// <param name="recursive"></param>
        /// <returns></returns>
        /// <remarks>Performs deep copy of array, return array with replacements.</remarks>
        internal static PhpArray ArrayReplaceImpl(PhpArray array, PhpArray[] arrays, bool recursive)
        {
            PhpArray result = (PhpArray)array.DeepCopy();

            if (arrays != null)
                foreach (PhpArray array1 in arrays)
                    ArrayReplaceImpl(result, array1, recursive);
            
            //// if called by PHP language then all items in the result should be in place deeply copied:
            //result.InplaceCopyOnReturn = true;
            return result;
        }

        /// <summary>
        /// Performs replacements on deeply-copied array. Performs deep copies of replace values.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="replaceWith"></param>
        /// <param name="recursive"></param>
        internal static void ArrayReplaceImpl(PhpArray array, PhpArray replaceWith, bool recursive)
        {
            if (array != null && replaceWith != null)
                foreach (var x in replaceWith)
                {
                    PhpArray xArrayValue, resultArrayValue;
                    if (recursive && (xArrayValue = x.Value as PhpArray) != null && (resultArrayValue = array[x.Key] as PhpArray) != null)
                    {
                        ArrayReplaceImpl(resultArrayValue, xArrayValue, true);
                    }
                    else
                    {
                        array[x.Key] = PhpVariable.DeepCopy(x.Value);
                    }
                }
        }

        #endregion
    }
}
