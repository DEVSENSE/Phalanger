/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using PHP.Core;
using System.Collections.Generic;

namespace PHP.Core
{
	#region Interfaces

    /// <summary>
	/// Defines comparison methods which are used to compare PHP.NET types.
	/// </summary>
	public interface IPhpComparable
	{
        /// <summary>
        /// Compares the current instance with another object using default comparer.
        /// </summary>
        /// <param name="obj">Object to compare with.</param>
        /// <returns>-1,0,+1</returns>
        /// <exception cref="ArgumentException">Incomparable objects have been compared.</exception>
		int CompareTo(object obj);

		/// <summary>
		/// Compares the current instance with another object.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj,comparer)"]/*'/>
        /// <exception cref="ArgumentException">Incomparable objects have been compared.</exception>
		int CompareTo(object obj, IComparer/*!*/ comparer);
	}

	/// <summary>
	/// Defines comparer which can be used to compare entries of a disctionary collection.
	/// </summary>
	public interface IDictionaryComparer
	{
		/// <summary>
		/// Compares two entries of a dictionary collection.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareEntries"]/*'/>
		int Compare(object keyA, object valueA, object keyB, object valueB);
	}

	#endregion

    #region Dictionary Comparers

    /// <summary>
	/// Compares keys of dictionary entries by specified comparer.
	/// </summary>
	public class KeyComparer : IComparer<KeyValuePair<IntStringKey, object>>
	{
		/// <summary>Regular comparer.</summary>
		public static readonly KeyComparer Default = new KeyComparer(PhpComparer.Default, false);
		/// <summary>Numeric comparer.</summary>
		public static readonly KeyComparer Numeric = new KeyComparer(PhpNumericComparer.Default, false);
		/// <summary>String comparer.</summary>
		public static readonly KeyComparer String = new KeyComparer(PhpStringComparer.Default, false);
		/// <summary>Array keys comparer.</summary>
		public static readonly KeyComparer ArrayKeys = new KeyComparer(PhpArrayKeysComparer.Default, false);
		/// <summary>Regular comparer with reverse order.</summary>
		public static readonly KeyComparer Reverse = new KeyComparer(PhpComparer.Default, true);
		/// <summary>Numeric comparer with reverse order.</summary>
		public static readonly KeyComparer ReverseNumeric = new KeyComparer(PhpNumericComparer.Default, true);
		/// <summary>String comparer with reverse order.</summary>
		public static readonly KeyComparer ReverseString = new KeyComparer(PhpStringComparer.Default, true);
		/// <summary>Locale string comparer with reverse order.</summary>
		public static readonly KeyComparer ReverseArrayKeys = new KeyComparer(PhpArrayKeysComparer.Default, true);

		/// <summary>
		/// The comparer which will be used to compare keys.
		/// </summary>
		private readonly IComparer/*!*/ comparer; // TODO: <IntStringKey>

		/// <summary>
		/// Plus or minus 1 depending on whether the comparer compares reversly.
		/// </summary>
		private readonly int reverse;

		/// <summary>
		/// Creates a new instance of the <see cref="KeyComparer"/>.
		/// </summary>
		/// <param name="comparer">The comparer which will be used to compare keys.</param>
		/// <param name="reverse">Whether to compare reversly.</param>
        public KeyComparer(IComparer/*!*/ comparer, bool reverse) // TODO: <IntStringKey>
		{
			if (comparer == null)
				throw new ArgumentNullException("comparer");

			this.comparer = comparer;
			this.reverse = reverse ? -1 : +1;
		}

		///// <summary>
		///// Compares keys only. Values are not used to compare so their order will not change if sorting is stable.
		///// </summary>
		///// <include file='Doc/Common.xml' path='docs/method[@name="CompareEntries"]/*'/>
		//public int Compare(object keyA, object valueA, object keyB, object valueB)
		//{
		//  return reverse * comparer.Compare(keyA, keyB);
		//}

		#region IComparer<KeyValuePair<IntStringKey,object>> Members

		public int Compare(KeyValuePair<IntStringKey, object> x, KeyValuePair<IntStringKey, object> y)
		{
			return reverse * comparer.Compare(x.Key.Object, y.Key.Object);
		}

		#endregion
	}

	/// <summary>
	/// Compares values of dictionary entries by specified comparer.
	/// </summary>
	public class ValueComparer : IComparer<KeyValuePair<IntStringKey, object>>/*!*/
	{
		/// <summary>Regular comparer.</summary>
		public static readonly ValueComparer Default = new ValueComparer(PhpComparer.Default, false);
		/// <summary>Numeric comparer.</summary>
		public static readonly ValueComparer Numeric = new ValueComparer(PhpNumericComparer.Default, false);
		/// <summary>String comparer.</summary>
		public static readonly ValueComparer String = new ValueComparer(PhpStringComparer.Default, false);
		/// <summary>Regular comparer with reverse order.</summary>
		public static readonly ValueComparer Reverse = new ValueComparer(PhpComparer.Default, true);
		/// <summary>Numeric comparer with reverse order.</summary>
		public static readonly ValueComparer ReverseNumeric = new ValueComparer(PhpNumericComparer.Default, true);
		/// <summary>String comparer with reverse order.</summary>
		public static readonly ValueComparer ReverseString = new ValueComparer(PhpStringComparer.Default, true);

		/// <summary>The comparer which will be used to compare values.</summary>
		private IComparer/*!*/ comparer;

		/// <summary>Plus or minus 1 depending on whether the comparer compares reversly.</summary>
		private int reverse;

		/// <summary>
		/// Creates a new instance of the <see cref="ValueComparer"/>.
		/// </summary>
		/// <param name="comparer">The comparer which will be used to compare values.</param>
		/// <param name="reverse">Whether to compare reversly.</param>
		public ValueComparer(IComparer/*!*/ comparer, bool reverse)
		{
			if (comparer == null)
				throw new ArgumentNullException("comparer");

			this.comparer = comparer;
			this.reverse = reverse ? -1 : +1;
		}

		///// <summary>
		///// Compares values only. Keys are not used to compare so their order will not change if sorting is stable.
		///// </summary>
		///// <include file='Doc/Common.xml' path='docs/method[@name="CompareEntries"]/*'/>
		//public int Compare(object keyA,object valueA,object keyB,object valueB)
		//{
		//  return reverse*comparer.Compare(valueA,valueB);
		//}

		#region IComparer<KeyValuePair<IntStringKey,object>> Members

		public int Compare(KeyValuePair<IntStringKey, object> x, KeyValuePair<IntStringKey, object> y)
		{
			return reverse * comparer.Compare(x.Value, y.Value);
		}

		#endregion
	}

	/// <summary>
	/// Compares dictionary entries using specified value and key comparers.
	/// </summary>
	public class EntryComparer : IComparer<KeyValuePair<IntStringKey, object>>
	{
		private readonly IComparer/*!*/ keyComparer; // TODO: <IntStringKey>
		private readonly IComparer/*!*/ valueComparer;
		private readonly int keyReverse;
		private readonly int valueReverse;

		/// <summary>
		/// Creates a new instance of <see cref="EntryComparer"/> with specified value and key comparers.
		/// </summary>
		/// <param name="keyComparer">The comparer used on keys.</param>
		/// <param name="keyReverse">Whether the the result of the key comparer is inversed.</param>
		/// <param name="valueComparer">The comparer used on values.</param>
		/// <param name="valueReverse">Whether the the result of the value comparer is inversed</param>
		public EntryComparer(IComparer/*!*/ keyComparer, bool keyReverse, IComparer/*!*/ valueComparer, bool valueReverse)
		{
			// TODO: key: <IntStringKey>

			if (keyComparer == null)
				throw new ArgumentNullException("keyComparer");

			if (valueComparer == null)
				throw new ArgumentNullException("valueComparer");

			this.keyComparer = keyComparer;
			this.valueComparer = valueComparer;
			this.keyReverse = keyReverse ? -1 : +1;
			this.valueReverse = valueReverse ? -1 : +1;
		}

		///// <summary>
		///// Compares two entries.
		///// </summary>
		///// <param name="keyA">The first entry key.</param>
		///// <param name="valueA">The first entry value.</param>
		///// <param name="keyB">The second entry key.</param>
		///// <param name="valueB">The second entry value.</param>
		///// <returns>-1, 0, +1</returns>
		//public int Compare(object keyA, object valueA, object keyB, object valueB)
		//{
		//  int kcmp = keyReverse*keyComparer.Compare(keyA,keyB);
		//  if (kcmp!=0) return kcmp;
		//  return valueReverse*valueComparer.Compare(valueA,valueB);
		//}

		#region IComparer<KeyValuePair<IntStringKey,object>> Members

		public int Compare(KeyValuePair<IntStringKey, object> x, KeyValuePair<IntStringKey, object> y)
		{
			int kcmp = keyReverse * keyComparer.Compare(x.Key.Object, y.Key.Object);
			if (kcmp != 0) return kcmp;
			return valueReverse * valueComparer.Compare(x.Value, y.Value);
		}

		#endregion
	}

    /// <summary>
    /// Implements equality comparer of objects, using given <see cref="IComparer"/>.
    /// </summary>
    public class ObjectEqualityComparer : IEqualityComparer<object>
    {
        /// <summary>
        /// <see cref="IComparer"/> to use.
        /// </summary>
        private readonly IComparer/*!*/ comparer;

        public ObjectEqualityComparer(IComparer/*!*/ comparer)
        {
            if (comparer == null)
                throw new ArgumentNullException("comparer");

            this.comparer = comparer;
        }

        #region IEqualityComparer<object>

        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return comparer.Compare(x, y) == 0;
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return (obj != null) ? obj.GetHashCode() : 0;
        }

        #endregion
    }
    
	#endregion

	#region Regular Comparer

	/// <summary>
	/// Implements PHP regular comparison.
	/// </summary>
    public class PhpComparer : IComparer // TODO: , IComparer<IntStringKey> 
	{
		/// <summary>Prevents from creating instances of this class.</summary>
		private PhpComparer() { }

		/// <summary>
		/// Default comparer used to compare objects where no other comparer is provided by user.
		/// </summary>
		public static readonly PhpComparer/*!*/ Default = new PhpComparer();

		#region Compare

        /// <summary>
        /// Compares two objects in a manner of the PHP regular comparison.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="throws">If true, functions throws ArgumentException for incomparable objects.</param>
        /// <returns></returns>
        public static int CompareOp(object x, object y, bool throws)
        {
            // the following trick is used when comparing booleans:
            //   F,F = 1-1 = 0  => F==F
            //   F,T = 1-2 = -1 => F<T
            //   T,F = 2-1 = 1  => T>F
            //   T,T = 2-2 = 0  => T==T

            // code marked as OBSOLETE is implementing NULL comparison such that NULL is considered as 0/0.0/false/""
            // actual PHP comparison treats NULL as less or equal to any value
            
            if (x == null)
            {
                if (y == null) return 0; // x == null
                if (y.GetType() == typeof(int)) return ((int)y == 0) ? 0 : -1;     // obsolete: -Math.Sign((int)y);                 // x == 0
                if (y.GetType() == typeof(long)) return ((long)y == 0) ? 0 : -1;
                if (y.GetType() == typeof(double)) return ((double)y == 0.0) ? 0 : -1;// obsolete: CompareDouble(0.0,(double)y);      // x == 0.0
                if (y.GetType() == typeof(string)) return ((string)y == "") ? 0 : -1;        // obsolete: sy==String.Empty ? 0:-1;            // x == ""  
                if (y.GetType() == typeof(bool)) return ((bool)y == false) ? 0 : -1;// obsolete: (bool)y ? 1:0;                      // x == false
            }
            else if (x.GetType() == typeof(int))
            {
                if (y == null) return ((int)x == 0) ? 0 : 1; // obsolete: Math.Sign((int)x); // y == 0
                if (y.GetType() == typeof(int)) return ((int)x < (int)y ? -1 : ((int)x > (int)y ? 1 : 0));
                if (y.GetType() == typeof(long)) return ((int)x < (long)y ? -1 : ((int)x > (long)y ? 1 : 0));
                if (y.GetType() == typeof(double)) return CompareDouble((int)x, (double)y);
                if (y.GetType() == typeof(string)) return -CompareString((string)y, (int)x);
                if (y.GetType() == typeof(bool)) return ((int)x != 0 ? 2 : 1) - ((bool)y ? 2 : 1);
            }
            else if (x.GetType() == typeof(long))
            {
                if (y == null) return ((long)x == 0) ? 0 : 1; // obsolete: Math.Sign((int)x); // y == 0
                if (y.GetType() == typeof(int)) return ((long)x < (int)y ? -1 : ((long)x > (int)y ? 1 : 0));
                if (y.GetType() == typeof(long)) return ((long)x < (long)y ? -1 : ((long)x > (long)y ? 1 : 0));
                if (y.GetType() == typeof(double)) return CompareDouble((long)x, (double)y);
                if (y.GetType() == typeof(string)) return -CompareString((string)y, (long)x);
                if (y.GetType() == typeof(bool)) return ((long)x != 0 ? 2 : 1) - ((bool)y ? 2 : 1);
            }
            else if (x.GetType() == typeof(double))
            {
                if (y == null) return ((double)x == 0.0) ? 0 : 1; // obsolete: CompareDouble((double)x,0.0); // y == 0.0
                if (y.GetType() == typeof(double)) return CompareDouble((double)x, (double)y);
                if (y.GetType() == typeof(int)) return CompareDouble((double)x, (int)y);
                if (y.GetType() == typeof(long)) return CompareDouble((double)x, (long)y);
                if (y.GetType() == typeof(string)) return -CompareString((string)y, (double)x);
                if (y.GetType() == typeof(bool)) return ((double)x != 0.0 ? 2 : 1) - ((bool)y ? 2 : 1);
            }
            else if (x.GetType() == typeof(string))
            {
                if (y == null) return (string)x == "" ? 0 : 1; // y == ""
                if (y.GetType() == typeof(string)) return CompareString((string)x, (string)y);
                if (y.GetType() == typeof(int)) return CompareString((string)x, (int)y);
                if (y.GetType() == typeof(long)) return CompareString((string)x, (long)y);
                if (y.GetType() == typeof(double)) return CompareString((string)x, (double)y);
                if (y.GetType() == typeof(bool)) return (Convert.StringToBoolean((string)x) ? 2 : 1) - ((bool)y ? 2 : 1);
            }
            else if (x.GetType() == typeof(bool))
            {
                return ((bool)x ? 2 : 1) - (Convert.ObjectToBoolean(y) ? 2 : 1);
            }
            
            try
            {
                return CompareOp_Nonliterals(x, y);
            }
            catch (ArgumentException)
            {
                if (throws)
                    throw;

                PhpException.Throw(PhpError.Warning, CoreResources.GetString("incomparable_objects_compared"));
                return 0;
            }           
        }

        /// <summary>
        /// Compare given objects, assuming they are IPhpComparable (PhpReference, PhpArray, PhpObject, PhpResource, PhpBytes). Otherwise it compares references.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException">If x and y are incomparable, ArgumentException is thrown.</exception>
        private static int CompareOp_Nonliterals(object x, object y)
        {
            IPhpComparable cmp;

            // PHP variables:
            if ((cmp = x as IPhpComparable) != null) return cmp.CompareTo(y, Default);

            if ((cmp = y as IPhpComparable) != null) return -cmp.CompareTo(x, Default);

            if (x != y) CompareOp_ThrowHelper(x, y);

            return 0;
        }

        /// <summary>
        /// Throws <see cref="ArgumentException"/> with information about arguments type.
        /// </summary>
        /// <param name="x">Left operand.</param>
        /// <param name="y">Right operand.</param>
        /// <exception cref="ArgumentException">Always throws.</exception>
        private static void CompareOp_ThrowHelper(object x, object y)
        {
            throw new ArgumentException(
                string.Format(CoreResources.incomparable_objects_compared_exception,
                    (x != null) ? x.GetType().ToString() : PhpVariable.TypeNameNull,
                    (y != null) ? y.GetType().ToString() : PhpVariable.TypeNameNull));
        }
        
		public static int CompareOp(int x, int y)
        {
            return (x < y ? -1 : (x > y ? 1 : 0));
        }

        public static int CompareOp(object x, int y, bool throws)
        {
            // copied from CompareOp(object,object,bool)

            if (x == null)
            {
                return ((int)y == 0) ? 0 : -1;     // obsolete: -Math.Sign((int)y);                 // x == 0
            }
            else if (x.GetType() == typeof(int))
            {
                return ((int)x < (int)y ? -1 : ((int)x > (int)y ? 1 : 0));                
            }
            else if (x.GetType() == typeof(long))
            {
                return ((long)x < (int)y ? -1 : ((long)x > (int)y ? 1 : 0));
            }
            else if (x.GetType() == typeof(double))
            {
                return CompareDouble((double)x, (int)y);
            }
            else if (x.GetType() == typeof(string))
            {
                return CompareString((string)x, (int)y);
            }
            else if (x.GetType() == typeof(bool))
            {
                return ((bool)x ? 2 : 1) - (Convert.ObjectToBoolean(y) ? 2 : 1);
            }
            
            try
            {
                return CompareOp_Nonliterals(x, y);
            }
            catch (ArgumentException)
            {
                if (throws)
                    throw;

                PhpException.Throw(PhpError.Warning, CoreResources.GetString("incomparable_objects_compared"));
                return 0;
            }
        }

        public static int CompareOp(int x, object y, bool throws)
        {
            // copied from CompareOp(object,object,bool)

            //if (x is int)
            {
                if (y == null) return ((int)x == 0) ? 0 : 1; // obsolete: Math.Sign((int)x); // y == 0
                if (y.GetType() == typeof(int)) return ((int)x < (int)y ? -1 : ((int)x > (int)y ? 1 : 0));
                if (y.GetType() == typeof(long)) return ((int)x < (long)y ? -1 : ((int)x > (long)y ? 1 : 0));
                if (y.GetType() == typeof(double)) return CompareDouble((int)x, (double)y);
                if (y.GetType() == typeof(string)) return -CompareString((string)y, (int)x);
                if (y.GetType() == typeof(bool)) return ((int)x != 0 ? 2 : 1) - ((bool)y ? 2 : 1);
            }
            
            try
            {
                return CompareOp_Nonliterals(x, y);
            }
            catch (ArgumentException)
            {
                if (throws)
                    throw;

                PhpException.Throw(PhpError.Warning, CoreResources.GetString("incomparable_objects_compared"));
                return 0;
            }
        }

        /// <summary>
        /// Compares two objects in a manner of the PHP regular comparison.
        /// </summary>
        /// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
        public int Compare(object x, object y)
        {
            return CompareOp(x, y, true);
        }

        #endregion

		#region CompareEq

		/// <summary>
		/// Compares two objects for equality in a manner of the PHP regular comparison.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
        /// <returns>Whether the values of operands are the same.</returns>
		/// <remarks>Faster than Compare(x,y) == 0.</remarks>
		[Emitted]
		public static bool CompareEq(object x, object y)
		{
			if (x == null)
			{
				if (y == null) return true;                                // y == null
                if (y.GetType() == typeof(int)) return (int)y == 0;								// y == 0
                if (y.GetType() == typeof(long)) return (long)y == 0;						// y == 0
                if (y.GetType() == typeof(double)) return (double)y == 0.0;                  // y == 0.0
                if (y.GetType() == typeof(string)) return (string)y == String.Empty; // y == ""  
                if (y.GetType() == typeof(bool)) return !(bool)y;                            // y == false
			}
            else if (x.GetType() == typeof(int))
			{
				if (y == null) return (int)x == 0;                     // y == 0
                if (y.GetType() == typeof(int)) return (int)x == (int)y;
				if (y.GetType() == typeof(long)) return (long)(int)x == (long)y;
                if (y.GetType() == typeof(double)) return (double)(int)x == (double)y;
                if (y.GetType() == typeof(string)) return CompareStringEq((string)y, (int)x);
                if (y.GetType() == typeof(bool)) return ((int)x != 0) == (bool)y;
			}
            else if (x.GetType() == typeof(long))
			{
				if (y == null) return (long)x == 0;                     // y == 0
                if (y.GetType() == typeof(long)) return (long)x == (long)y;
                if (y.GetType() == typeof(int)) return (long)x == (long)(int)y;
                if (y.GetType() == typeof(double)) return (double)(long)x == (double)y;
                if (y.GetType() == typeof(string)) return CompareStringEq((string)y, (long)x);
                if (y.GetType() == typeof(bool)) return ((long)x != 0) == (bool)y;
			}
            else if (x.GetType() == typeof(double))
			{
				if (y == null) return (double)x == 0.0;                // y == 0.0
                if (y.GetType() == typeof(double)) return (double)x == (double)y;
                if (y.GetType() == typeof(int)) return (double)x == (double)(int)y;
                if (y.GetType() == typeof(long)) return (double)x == (double)(long)y;
                if (y.GetType() == typeof(string)) return CompareStringEq((string)y, (double)x);
                if (y.GetType() == typeof(bool)) return ((double)x != 0.0) == (bool)y;
			}
            else if (x.GetType() == typeof(string))
			{
				if (y == null) return (string)x == String.Empty;             // y == ""  
                if (y.GetType() == typeof(string)) return CompareString((string)x, (string)y) == 0;
                if (y.GetType() == typeof(int)) return CompareString((string)x, (int)y) == 0;
                if (y.GetType() == typeof(long)) return CompareString((string)x, (long)y) == 0;
                if (y.GetType() == typeof(double)) return CompareStringEq((string)x, (double)y);
                if (y.GetType() == typeof(bool)) return Convert.StringToBoolean((string)x) == (bool)y;
			}
			else if (x.GetType() == typeof(bool))
			{
				return (bool)x == Convert.ObjectToBoolean(y);
			}

			try
            {
                return (CompareOp_Nonliterals(x, y) == 0);
            }
            catch (ArgumentException)
            {
                return false;
            }
		}

        /// <summary>
        /// Compares two objects for equality in a manner of the PHP regular comparison.
        /// </summary>
        /// <param name="x">The first object.</param>
        /// <param name="y">The second object.</param>
        /// <returns>Whether the values of operands are the same.</returns>
        /// <remarks>Faster than Compare(x,y) == 0.</remarks>
        [Emitted]
        public static bool CompareEq(object x, string/*!*/y)
        {
            Debug.Assert(y != null);

            if (x == null)
            {
                if (y == null) return true;
                return string.IsNullOrEmpty(y);
            }
            else if (x.GetType() == typeof(string))
            {
                if (y == null) return (string)x == string.Empty;
                return CompareString((string)x, (string)y) == 0;
            }
            else if (x.GetType() == typeof(int))
            {
                if (y == null) return (int)x == 0;
                return CompareStringEq((string)y, (int)x);
            }
            else if (x.GetType() == typeof(long))
            {
                if (y == null) return (long)x == 0;
                return CompareStringEq((string)y, (long)x);
            }
            else if (x.GetType() == typeof(double))
            {
                if (y == null) return (double)x == 0.0;
                return CompareStringEq((string)y, (double)x);
            }
            else if (x.GetType() == typeof(bool))
            {
                return (bool)x == Convert.StringToBoolean(y);
            }

            try
            {
                return (CompareOp_Nonliterals(x, y) == 0);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>
        /// Compares two objects for equality in a manner of the PHP regular comparison.
        /// </summary>
        /// <param name="x">The first object.</param>
        /// <param name="y">The second object.</param>
        /// <returns>Whether the values of operands are the same.</returns>
        /// <remarks>Faster than Compare(x,y) == 0.</remarks>
        [Emitted]
        public static bool CompareEq(object x, int y)
        {
            if (x == null)
            {
                return y == 0;								// y == 0
            }
            else if (x.GetType() == typeof(int))
            {
                return (int)x == y;
            }
            else if (x.GetType() == typeof(long))
            {
                return (long)x == (long)y;
            }
            else if (x.GetType() == typeof(double))
            {
                return (double)x == (double)y;
            }
            else if (x.GetType() == typeof(string))
            {
                return CompareString((string)x, y) == 0;
            }
            else if (x.GetType() == typeof(bool))
            {
                return (bool)x == (y != 0);
            }

            try
            {
                return (CompareOp_Nonliterals(x, y) == 0);
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

		#endregion

		#region Auxiliary comparisons

		/// <summary>
		/// Compares two double values.
		/// </summary>
		/// <returns>(+1,0,-1)</returns>
		/// <remarks>We cannot used <see cref="Math.Sign"/> on <c>x - y</c> since the result can be NaN.</remarks>
        public static int CompareDouble(double x, double y)
		{
			return (x > y) ? +1 : (x < y ? -1 : 0);
		}

		/// <summary>
		/// Compares two integer values.
		/// </summary>
		/// <returns>(+1,0,-1)</returns>
		/// <remarks>We cannot used <see cref="Math.Sign"/> on <c>x - y</c> since the result can overflow.</remarks>
        public static int CompareInteger(int x, int y)
		{
			return (x > y) ? +1 : (x < y ? -1 : 0);
		}

		/// <summary>
		/// Compares two long integer values.
		/// </summary>
		/// <returns>(+1,0,-1)</returns>
        public static int CompareLongInteger(long x, long y)
		{
			return (x > y) ? +1 : (x < y ? -1 : 0);
		}


		/// <summary>
		/// Compares string in a manner of PHP. 
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		/// <remarks>Note that this comparison is not transitive (e.g. {"2","10","10a"} leads to a contradiction).</remarks>
        public static int CompareString(string/*!*/ x, string/*!*/ y)
		{
			Debug.Assert(x != null && y != null);

			int ix, iy;
			long lx, ly;
			double dx, dy;
			Convert.NumberInfo info_x, info_y;

			info_x = Convert.StringToNumber(x, out ix, out lx, out dx);

			// an operand is not entirely convertable to numbers => string comparison is performed:
			if ((info_x & Convert.NumberInfo.IsNumber) == 0) return String.CompareOrdinal(x, y);

			info_y = Convert.StringToNumber(y, out iy, out ly, out dy);

			// an operand is not entirely convertable to numbers => string comparison is performed:
			if ((info_y & Convert.NumberInfo.IsNumber) == 0) return String.CompareOrdinal(x, y);

			// at least one operand has been converted to double:
			if (((info_x | info_y) & Convert.NumberInfo.Double) != 0)
				return CompareDouble(dx, dy);

			// compare integers:
			return CompareLongInteger(lx, ly);
		}

		/// <summary>
		/// Compares a <see cref="string"/> with <see cref="int"/>.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
        public static int CompareString(string/*!*/ x, int y)
		{
			Debug.Assert(x != null);

			int ix;
			double dx;
			long lx;

			switch (Convert.StringToNumber(x, out ix, out lx, out dx) & Convert.NumberInfo.TypeMask)
			{
				case Convert.NumberInfo.Double: return CompareDouble(dx, y);
				case Convert.NumberInfo.Integer: return CompareInteger(ix, y);
				case Convert.NumberInfo.LongInteger: return CompareLongInteger(lx, y);
				default: Debug.Fail(); throw null;
			}
		}

		/// <summary>
		/// Compares a <see cref="string"/> with <see cref="long"/>.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
        public static int CompareString(string/*!*/ x, long y)
		{
			Debug.Assert(x != null);

			int ix;
			double dx;
			long lx;

			switch (Convert.StringToNumber(x, out ix, out lx, out dx) & Convert.NumberInfo.TypeMask)
			{
				case Convert.NumberInfo.Double: return CompareDouble(dx, y);
				case Convert.NumberInfo.Integer: return CompareLongInteger(ix, y);
				case Convert.NumberInfo.LongInteger: return CompareLongInteger(lx, y);
				default: Debug.Fail(); throw null;
			}
		}

		/// <summary>
		/// Compares a <see cref="string"/> with <see cref="double"/>.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
        public static int CompareString(string/*!*/ x, double y)
		{
			Debug.Assert(x != null);

			int ix;
			double dx;
			long lx;

			switch (Convert.StringToNumber(x, out ix, out lx, out dx) & Convert.NumberInfo.TypeMask)
			{
				case Convert.NumberInfo.Double: return CompareDouble(dx, y);
				case Convert.NumberInfo.Integer: return CompareDouble(ix, y);
				case Convert.NumberInfo.LongInteger: return CompareDouble(lx, y);
				default: Debug.Fail(); throw null;
			}
		}

		/// <summary>
		/// Compares two objects for equality in a manner of the PHP regular comparison.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>Whether the values of operands are the same.</returns>
        public static bool CompareStringEq(string/*!*/ x, int y)
		{
			Debug.Assert(x != null);

			int ix;
			double dx;
			long lx;

			switch (Convert.StringToNumber(x, out ix, out lx, out dx) & Convert.NumberInfo.TypeMask)
			{
				case Convert.NumberInfo.Double: return dx == y;
				case Convert.NumberInfo.Integer: return ix == y;
				case Convert.NumberInfo.LongInteger: return lx == y;
				default: Debug.Fail(); throw null;
			}
		}

		/// <summary>
		/// Compares two objects for equality in a manner of the PHP regular comparison.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>Whether the values of operands are the same.</returns>
        public static bool CompareStringEq(string/*!*/ x, long y)
		{
			Debug.Assert(x != null);

			int ix;
			double dx;
			long lx;

			switch (Convert.StringToNumber(x, out ix, out lx, out dx) & Convert.NumberInfo.TypeMask)
			{
				case Convert.NumberInfo.Double: return dx == y;
				case Convert.NumberInfo.Integer: return ix == y;
				case Convert.NumberInfo.LongInteger: return lx == y;
				default: Debug.Fail(); throw null;
			}
		}

		/// <summary>
		/// Compares two objects for equality in a manner of the PHP regular comparison.
		/// </summary>
		/// <param name="x">The first object.</param>
		/// <param name="y">The second object.</param>
		/// <returns>Whether the values of operands are the same.</returns>
        public static bool CompareStringEq(string/*!*/ x, double y)
		{
			Debug.Assert(x != null);

			int ix;
			double dx;
			long lx;

			switch (Convert.StringToNumber(x, out ix, out lx, out dx) & Convert.NumberInfo.TypeMask)
			{
				case Convert.NumberInfo.Double: return dx == y;
				case Convert.NumberInfo.Integer: return ix == y;
				case Convert.NumberInfo.LongInteger: return lx == y;
				default: Debug.Fail(); throw null;
			}
		}

		#endregion
	}

	#endregion

	#region Numeric Comparer

	/// <summary>
	/// Implements PHP numeric comparison.
	/// </summary>
	public class PhpNumericComparer : IComparer
	{
		/// <summary>Prevents from creating instances of this class.</summary>
		private PhpNumericComparer() { }

		/// <summary>
		/// Default comparer used to compare objects where no other comparer is provided by user.
		/// </summary>
		public static readonly PhpNumericComparer/*!*/ Default = new PhpNumericComparer();

		/// <summary>
		/// Compares two objects in a manner of PHP numeric comparison.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		public int Compare(object x, object y)
		{
			int ix, iy;
			long lx, ly;
			double dx, dy;
			Convert.NumberInfo info_x, info_y;

			info_x = Convert.ObjectToNumber(x, out ix, out lx, out dx);
			info_y = Convert.ObjectToNumber(y, out iy, out ly, out dy);

			// at least one operand has been converted to double:
			if (((info_x | info_y) & Convert.NumberInfo.Double) != 0)
				return PhpComparer.CompareDouble(dx, dy);

			// compare integers:
			return PhpComparer.CompareLongInteger(lx, ly);
		}
	}

	#endregion

	#region String Comparer

	/// <summary>
	/// Implements PHP string comparison.
	/// </summary>
	public class PhpStringComparer : IComparer
	{
		/// <summary>Prevents from creating instances of this class.</summary>
		private PhpStringComparer() { }

		/// <summary>
		/// Default comparer used to compare objects where no other comparer is provided by user.
		/// </summary>
		public static readonly PhpStringComparer Default = new PhpStringComparer();

		/// <summary>
		/// Compares two objects in a manner of PHP string comparison.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		public int Compare(object x, object y)
		{
			return String.CompareOrdinal(Convert.ObjectToString(x), Convert.ObjectToString(y));
		}

	}

	#endregion

	#region Locale String Comparer

	/// <summary>
	/// Implements PHP locale string comparison.
	/// </summary>
	public class PhpLocaleStringComparer : IComparer
	{
		/// <summary>
		/// A culture used for comparison.
		/// </summary>
		public CultureInfo Culture { get { return culture; } }
		private readonly CultureInfo culture;

		/// <summary>
		/// Whether the comparer is ignoring case.
		/// </summary>
		public bool IgnoreCase { get { return ignoreCase; } }
		private readonly bool ignoreCase;

		/// <summary>
		/// Creates a new string comparer with a specified culture.
		/// </summary>
		public PhpLocaleStringComparer(CultureInfo culture, bool ignoreCase)
		{
			this.culture = (culture != null) ? culture : CultureInfo.InvariantCulture;
			this.ignoreCase = ignoreCase;
		}

		/// <summary>
		/// Compares two objects in a manner of PHP string comparison.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		public int Compare(object x, object y)
		{
#if SILVERLIGHT
			return String.Compare(Convert.ObjectToString(x), Convert.ObjectToString(y), culture, ignoreCase?CompareOptions.IgnoreCase:CompareOptions.None);
#else
            return String.Compare(Convert.ObjectToString(x), Convert.ObjectToString(y), ignoreCase, culture);
#endif
		}
	}

	#endregion

	#region ArrayKeys Comparer

	/// <summary>
	/// Implements comparison of PHP array keys.
	/// </summary>
	public class PhpArrayKeysComparer : IComparer<IntStringKey>, IComparer
	{
		/// <summary>Prevents from creating instances of this class.</summary>
		private PhpArrayKeysComparer() { }

		/// <summary>
		/// Default comparer.
		/// </summary>
		public static readonly PhpArrayKeysComparer Default = new PhpArrayKeysComparer();

		/// <summary>
		/// Compares keys of an array.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		/// <remarks>
		/// Keys are compared as strings if at least one of them is a string 
		/// otherwise they have to be integers and so they are compared as integers.
		/// </remarks>
		public int Compare(IntStringKey x, IntStringKey y)
		{
			return x.CompareTo(y);
		}

		#region IComparer Members

		public int Compare(object x, object y)
		{
			IntStringKey keyx = (x is IntStringKey ? (IntStringKey)x : new IntStringKey(x));
			IntStringKey keyy = (y is IntStringKey ? (IntStringKey)y : new IntStringKey(y));

			return Compare(keyx, keyy);
		}

		#endregion
	}

	#endregion

	#region Natural Comparer

	/// <summary>
	/// Implements PHP natural comparison.
	/// </summary>
	public class PhpNaturalComparer : IComparer
	{
		/// <summary>Whether comparisons will be case insensitive.</summary>
		private bool caseInsensitive;

		/// <summary>Default case sensitive comparer.</summary>
		public static readonly PhpNaturalComparer Default = new PhpNaturalComparer(false);

		/// <summary>Case insensitive comparer.</summary>
		public static readonly PhpNaturalComparer CaseInsensitive = new PhpNaturalComparer(true);

		/// <summary>Prevents from creating instances of this class.</summary>
		/// <param name="caseInsensitive">Whether comparisons will be case insensitive.</param>
		public PhpNaturalComparer(bool caseInsensitive)
		{
			this.caseInsensitive = caseInsensitive;
		}

		/// <summary>
		/// Compares two objects using the natural ordering.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		public int Compare(object x, object y)
		{
			return CompareStrings(Convert.ObjectToString(x), Convert.ObjectToString(y));
		}

        /// <summary>
		/// Compares two strings using the natural ordering.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		public int CompareStrings(string x, string y)
		{
			if (x == null) x = String.Empty;
			if (y == null) y = String.Empty;

			int length_l = x.Length, length_g = y.Length;
			if (length_l == 0 || length_g == 0) return length_l - length_g;

			int i = 0, j = 0;
			do
			{
				char lc = x[i], gc = y[j];

				// skip white spaces
				if (Char.IsWhiteSpace(lc))
				{
					i++;
					continue;
				}
				if (Char.IsWhiteSpace(gc))
				{
					j++;
					continue;
				}

				if (Char.IsDigit(lc) && Char.IsDigit(gc))
				{
					// compare numbers
					int result = (lc == '0' || gc == '0') ? CompareLeft(x, y, ref i, ref j) :
						CompareRight(x, y, ref i, ref j);

					if (result != 0) return result;
				}
				else
				{
					// compare letters
					if (caseInsensitive)
					{
						lc = Char.ToLower(lc);
						gc = Char.ToLower(gc);
					}

					if (lc < gc) return -1;
					if (lc > gc) return 1;

					i++; j++;
				}
			}
			while (i < length_l && j < length_g);

			if (i < length_l) return 1;
			if (j < length_g) return -1;
			return 0;
		}

		/// <summary>
		/// Compares two strings with left-aligned numbers, the first to have a different value wins.
		/// </summary>
		/// <param name="x">String that contains the first number.</param>
		/// <param name="y">String that contains the second number.</param>
		/// <param name="i">Index in <paramref name="x"/> where the first number begins. Is set to the index
		/// immediately following the number after returning from this method.</param>
		/// <param name="j">Index in <paramref name="y"/> where the second number begins. Is set to the index
		/// immediately following the number after returning from this method.</param>
		/// <returns>
		/// Negative integer if the first number is less than the second number, 
		/// zero if the two numbers are equal and
		/// positive integer if the first number is greater than the second number.</returns>
		/// <remarks>Assumes neither <paramref name="x"/> nor <paramref name="y"/> parameter is null.</remarks>
		private int CompareLeft(string x, string y, ref int i, ref int j)
		{
			Debug.Assert(x != null && y != null);

			int length_l = x.Length, length_g = y.Length;

			while (true)
			{
				bool bl = (i == length_l || !Char.IsDigit(x[i]));
				bool bg = (j == length_g || !Char.IsDigit(y[j]));

				if (bl && bg) return 0;
				if (bl) return -1;
				if (bg) return 1;

				if (x[i] < y[j]) return -1;
				if (x[i] > y[j]) return 1;

				i++; j++;
			}
		}

		/// <summary>
		/// Compares two strings with right-aligned numbers, The longest run of digits wins.
		/// </summary>
		/// <param name="x">String that contains the first number.</param>
		/// <param name="y">String that contains the second number.</param>
		/// <param name="i">Index in <paramref name="x"/> where the first number begins. Is set to the index
		/// immediately following the number after returning from this method.</param>
		/// <param name="j">Index in <paramref name="y"/> where the second number begins. Is set to the index
		/// immediately following the number after returning from this method.</param>
		/// <returns>
		/// Negative integer if the first number is less than the second number, 
		/// zero if the two numbers are equal and
		/// positive integer if the first number is greater than the second number.</returns>
		/// <remarks>Assumes neither <paramref name="x"/> nor <paramref name="y"/> parameter is null.</remarks>
		internal int CompareRight(string x, string y, ref int i, ref int j)
		{
			Debug.Assert(x != null && y != null);

			int length_l = x.Length, length_g = y.Length;

			// That aside, the greatest value wins, but we can't know that it will until we've scanned both numbers to
			// know that they have the same magnitude, so we remember it in "bias".
			int bias = 0;

			while (true)
			{
				bool bl = (i == length_l || !Char.IsDigit(x[i]));
				bool bg = (j == length_g || !Char.IsDigit(y[j]));

				if (bl && bg) return bias;
				if (bl) return -1;
				if (bg) return 1;

				if (x[i] < y[j])
				{
					if (bias == 0) bias = -1;
				}
				else if (x[i] > y[j])
				{
					if (bias == 0) bias = 1;
				}

				i++; j++;
			}
		}

	}

	#endregion

	#region User Comparer

	/// <summary>
	/// Implements PHP numeric comparison.
	/// </summary>
	public class PhpUserComparer : IComparer
	{
		/// <summary>User defined PHP method used to compare given objects.</summary>
		private PhpCallback compare;

		/// <summary>
		/// Creates a new instance of a comparer using <see cref="PhpCallback"/> for comparisons.
		/// </summary>
		/// <param name="compare">User callback which provides comparing functionality.</param>
		/// <remarks>
		/// <para>
		/// Callback should have the signature <c>object(object,object)</c> and should already be bound.
		/// </para>
		/// <para>
		/// The result of calback's invocation is converted to a double by <see cref="Convert.ObjectToDouble"/>
		/// and than the sign is taken as a result of the comparison.</para>
		/// </remarks>
		/// <exception cref="ArgumentNullException"><paramref name="compare"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ArgumentException"><paramref name="compare"/> callback is not bound.</exception>
		public PhpUserComparer(PhpCallback compare)
		{
			if (compare == null) throw new ArgumentNullException("compare");
			if (!compare.IsBound) throw new ArgumentException(CoreResources.GetString("callback_not_bound"), "compare");
			this.compare = compare;
		}

		/// <summary>
		/// Compares two objects in a manner of PHP numeric comparison.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		public int Compare(object x, object y)
		{
			return PhpComparer.CompareDouble(Convert.ObjectToDouble(compare.Invoke(x, y)), 0.0);
		}
	}

	#endregion

}
