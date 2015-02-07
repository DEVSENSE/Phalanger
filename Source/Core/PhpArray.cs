/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Runtime.Serialization;
using PHP.Core.Reflection;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

/*
  FUTURE VERSION:
   - use OrderedHashtable.SortToList in CompareArrays
   - seal PhpArray (solve better PhpArrayString and SPL.PhpArrayObject)
  
*/

namespace PHP.Core
{
	/// <summary>
	/// Represents PHP associative ordered array.
	/// </summary>
	[Serializable]
	[DebuggerDisplay("Count = {Count}", Type = PhpArray.PhpTypeName)]
#if !SILVERLIGHT
	[DebuggerTypeProxy(typeof(PhpArrayDebugView))]
#endif
	public class PhpArray : PhpHashtable, ICloneable, IPhpVariable, IPhpEnumerable, IPhpObjectGraphNode
    {
        #region Fields, Properties, Constants

        /// <summary>
        /// Used in all PHP functions determining the type name. (var_dump, ...)
        /// </summary>
		public const string PhpTypeName = "array";

        /// <summary>
        /// Used in print_r function.
        /// </summary>
        public const string PrintablePhpTypeName = "Array";

		/// <summary>
		/// Representation of "array" string in a form of bytes.
		/// </summary>
		private static readonly PhpBytes/*!*/toBytes =
			new PhpBytes(new byte[] { (byte)'a', (byte)'r', (byte)'r', (byte)'a', (byte)'y' });

        /// <summary>
		/// If this flag is <B>true</B> the array will be copied inplace by the immediate <see cref="Copy"/> call.
		/// </summary>
        [Emitted]
        public bool InplaceCopyOnReturn { get { return this.table.InplaceCopyOnReturn; } set { this.table.InplaceCopyOnReturn = value; } }
		
		/// <summary>
		/// Intrinsic enumerator associated with the array. Initialized lazily.
		/// </summary>
        protected OrderedDictionary.Enumerator intrinsicEnumerator;

        #endregion

        #region Constructors

        /// <summary>
		/// Creates a new instance of <see cref="PhpArray"/> with specified capacities for integer and string keys respectively.
		/// </summary>
		public PhpArray() : base() { }

		/// <summary>
		/// Creates a new instance of <see cref="PhpArray"/> with specified capacities for integer and string keys respectively.
		/// </summary>
		/// <param name="capacity"></param>
		public PhpArray(int capacity) : base(capacity) { }

		/// <summary>
		/// Creates a new instance of <see cref="PhpArray"/> with specified capacities for integer and string keys respectively.
		/// </summary>
		/// <param name="intCapacity"></param>
		/// <param name="stringCapacity"></param>
		public PhpArray(int intCapacity, int stringCapacity) : base(intCapacity + stringCapacity) { }

		/// <summary>
		/// Creates a new instance of <see cref="PhpArray"/> initialized with all values from <see cref="System.Array"/>.
		/// </summary>
		/// <param name="values"></param>
		public PhpArray(Array values) : base(values) { }

		/// <summary>
		/// Creates a new instance of <see cref="PhpArray"/> initialized with a portion of <see cref="System.Array"/>.
		/// </summary>
		/// <param name="values"></param>
		/// <param name="index"></param>
		/// <param name="length"></param>
		public PhpArray(Array values, int index, int length) : base(values, index, length) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="PhpArray"/> class filled by values from specified array.
		/// </summary>
		/// <param name="values">An array of values to be added to the table.</param>
		/// <param name="start">An index of the first item from <paramref name="values"/> to add.</param>
		/// <param name="length">A number of items to add.</param>
		/// <param name="value">A value to be filtered.</param>
		/// <param name="doFilter">Wheter to add all items but <paramref name="value"/> (<b>true</b>) or 
		/// all items with the value <paramref name="value"/> (<b>false</b>).</param>
		public PhpArray(int[] values, int start, int length, int value, bool doFilter)
			: base(values, start, length, value, doFilter) { }

		/// <summary>
        /// Creates a new instance of <see cref="PhpArray"/> filled by data from an enumerator.
        /// </summary>
        /// <param name="data">The enumerator containing values added to the new instance.</param>
        public PhpArray(IEnumerable data)
            : base((data is ICollection) ? ((ICollection)data).Count : 0)
        {
            if (data != null)
            {
                foreach (object value in data)
                    this.Add(value);
            }
        }

        /// <summary>
        /// Copy constructor. Creates <see cref="PhpArray"/> that shares internal data table with another <see cref="PhpArray"/>.
        /// </summary>
        /// <param name="array">Table to be shared.</param>
        /// <param name="preserveMaxInt">True to copy the <see cref="PhpHashtable.MaxIntegerKey"/> from <paramref name="array"/>.
        /// Otherwise the value will be recomputed when needed. See http://phalanger.codeplex.com/workitem/31484 for more details.</param>
        public PhpArray(PhpArray/*!*/array, bool preserveMaxInt)
            : base(array, preserveMaxInt)
        {

        }

		/// <summary>
		/// Creates an instance of <see cref="PhpArray"/> filled by given values.
		/// </summary>
		/// <param name="values">Values to be added to the new instance. 
		/// Keys will correspond order of values in the array.</param>
		public static PhpArray New(params object[] values)
		{
			PhpArray result = new PhpArray(values.Length, 0);
			foreach (object value in values)
				result.Add(value);
			return result;
		}

		/// <summary>
		/// Creates an instance of <see cref="PhpArray"/> filled by given entries.
		/// </summary>
		/// <param name="keysValues">Keys and values (alternating) or values only.</param>
		/// <remarks>If the length of <paramref name="keysValues"/> is odd then its last item is added without a key.</remarks>
		public static PhpArray Keyed(params object[] keysValues)
		{
			PhpArray result = new PhpArray();
			int length = keysValues.Length;
			int remainder = length % 2;

			for (int i = 0; i < length - remainder; i += 2)
				result.Add(keysValues[i], keysValues[i + 1]);

			if (remainder > 0)
				result.Add(keysValues[length - 1]);

			return result;
		}

        /// <summary>
        /// Cast given <paramref name="arrayobj"/> to <see cref="PhpArray"/>. Depends on current implementation of <see cref="PhpArray"/>.
        /// </summary>
        /// <param name="arrayobj"><see cref="Object"/> to be casted to <see cref="PhpArray"/>.</param>
        /// <returns>Casted object or <c>null</c>.</returns>
        public static PhpArray AsPhpArray(object arrayobj)
        {
            return arrayobj as PhpArray;
        }

		#endregion

		#region IPhpPrintable

		/// <summary>
		/// Prints an array in a human readable form.
		/// </summary>
		public void Print(TextWriter output)
		{
			int len = output.NewLine.Length;
			int count = this.Count;

			// prevents recursion:
			if (this.Visited)
			{
                output.WriteLine(PrintablePhpTypeName);
                output.Write(" *RECURSION*");
			}
			else
			{
				this.Visited = true;

				// handles an empty array:
                //if (count == 0)
                //{
                //    output.Write(PhpTypeName);
                //    output.Write(" [empty]");
                //}
                //else
				{
                    output.WriteLine(PrintablePhpTypeName);

                    //PhpVariable.PrintIndentationLevel += 1;
					PhpVariable.PrintIndentation(output);
					output.WriteLine('(');

                    PhpVariable.PrintIndentationLevel += 2;

					// iterates through array items and prints them:
					//IDictionaryEnumerator iterator = this.GetEnumerator();
					foreach (KeyValuePair<IntStringKey, object> entry in this)
					{
						PhpVariable.PrintIndentation(output);
                        PhpVariable.PrintIndentationLevel += 2;

						// prints a key:
						output.Write("[{0}] => ", entry.Key.ToString());

						// prints a value:
						PhpVariable.Print(output, entry.Value);
                        output.WriteLine();
                        PhpVariable.PrintIndentationLevel -= 2;
					}

                    PhpVariable.PrintIndentationLevel -= 2;
					PhpVariable.PrintIndentation(output);
					output.Write(')');
                    //PhpVariable.PrintIndentationLevel -= 1;
				}

				// prevents recursion - marks the array as visited:
				this.Visited = false;

                output.WriteLine();
			}			
		}

		/// <summary>
		/// Prints an array along with item types in human readable form. 
		/// </summary>
		public void Dump(TextWriter output)
		{
			int len = output.NewLine.Length;
			int count = this.Count;

			// prevents recursion:
			if (this.Visited)
			{
                output.Write(PhpTypeName);
                output.Write("({0}) *RECURSION*", count);
			}
			else
			{
				this.Visited = true;

				// handles an empty array:
                //if (count == 0)
                //{
                //    output.Write(PhpTypeName);
                //    output.Write("(0) [empty]");
                //}
                //else
				{
					output.Write(PhpTypeName);
					output.WriteLine("({0}) {{", count);
					//PhpVariable.PrintIndentation(output);
					//output.WriteLine('{');

					PhpVariable.PrintIndentationLevel++;

					// iterates through array items and dumps them:
					foreach (KeyValuePair<IntStringKey, object> entry in this)
					{
						PhpVariable.PrintIndentation(output);

						// prints key discriminating string and integer ones by quotes:
						if (entry.Key.IsString)
							output.WriteLine("[\"{0}\"]=>", entry.Key.String);
						else
							output.WriteLine("[{0}]=>", entry.Key.Integer);

						// marks a reference by an ampersand:
                        PhpVariable.PrintIndentation(output);
						
                        // dumps a value:
                        PhpVariable.Dump(output, entry.Value);
					}

					PhpVariable.PrintIndentationLevel--;

					PhpVariable.PrintIndentation(output);
					output.Write('}');
				}

				this.Visited = false;
			}
			output.WriteLine();
		}


		/// <summary>
		/// Prints an array in form of declaration in PHP. 
		/// </summary>
		public void Export(TextWriter output)
		{
			int len = output.NewLine.Length;
			int count = this.Count;

			// prevents recursion:
			if (this.Visited)
			{
				output.Write(PhpTypeName);
				output.Write("(/* recursion */)", count);
			}
			else
			{
				this.Visited = true;

				// handles an empty array:
				if (count == 0)
				{
					output.Write(PhpTypeName);
					output.Write("()");
				}
				else
				{
					output.Write(PhpTypeName);
					output.WriteLine(" (");

					PhpVariable.PrintIndentationLevel++;

					// iterates through array items and exports them:
					foreach (KeyValuePair<IntStringKey, object> entry in this)
					{
						PhpVariable.PrintIndentation(output);

						// key:
                        if (entry.Key.IsInteger) output.Write(entry.Key.Integer);
                        else output.Write("'{0}'", StringUtils.AddCSlashes(entry.Key.ToString(), true, false));
                        output.Write(" => ");

						// marks a reference by a comment:
						if (entry.Value is PhpReference)
							output.Write("/* reference */ ");

						// dumps a value:
						PhpVariable.Export(output, entry.Value);

						// prints commas after each item // note: (J) including the last one:
						/*if (--count > 0) */output.Write(',');
						output.WriteLine();
					}

					PhpVariable.PrintIndentationLevel--;

					PhpVariable.PrintIndentation(output);
					output.Write(')');
				}

				// marks array as visited to prevent recursion:
				this.Visited = false;
			}

			// the top of the recursion:
			//if (PhpVariable.PrintIndentationLevel == 0)
			//	output.WriteLine();
		}

		#endregion

		#region IPhpConvertible

		/// <summary>
		/// Retrieves a Phalanger type code of this instance.
		/// </summary>
		/// <returns>The PHP.NET type code.</returns>
		public PhpTypeCode GetTypeCode()
		{
			return PhpTypeCode.PhpArray;
		}

		/// <summary>
		/// Converts this instance to an integer value.
		/// </summary>
		/// <returns>The number of items in this instance.</returns>
		public int ToInteger()
		{
			return Count;
		}

		/// <summary>
		/// Returns <c>0</c>.
		/// </summary>
		/// <returns><c>0</c></returns>
		public long ToLongInteger()
		{
			return 0;
		}

		/// <summary>
		/// Converts this instance to a double value.
		/// </summary>
		/// <returns>The number of items in this instance.</returns>
		public double ToDouble()
		{
			return Count;
		}

		/// <summary>
		/// Converts this instance to a boolean value.
		/// </summary>
		/// <returns>Whether the number of items in this instance is not zero.</returns>
		public bool ToBoolean()
		{
			return Count != 0;
		}

		/// <summary>
		/// Converts this instance to a <see cref="PhpBytes"/> value.
		/// </summary>
		/// <returns>Returns "array" string converted to bytes.</returns>
		public PhpBytes ToPhpBytes()
		{
			return toBytes;
		}

		/// <summary>
		/// Converts instance to a number of type <see cref="int"/>.
		/// </summary>
		/// <param name="doubleValue">The number of items in this instance.</param>
		/// <param name="intValue">The number of items in this instance.</param>
		/// <param name="longValue">The number of items in this instance.</param>
		/// <returns><see cref="Convert.NumberInfo.Integer"/>.</returns>
		public Convert.NumberInfo ToNumber(out int intValue, out long longValue, out double doubleValue)
		{
			doubleValue = Count;
			intValue = Count;
			longValue = Count;
			return Convert.NumberInfo.Integer | Convert.NumberInfo.IsPhpArray;
		}

		/// <summary>
		/// Converts this instance to a string value.
		/// </summary>
		/// <returns>Returns "array" string.</returns>
		string IPhpConvertible.ToString()
		{
			PhpException.Throw(PhpError.Notice, CoreResources.GetString("array_to_string_conversion"));
            return PrintablePhpTypeName;
		}

        /// <summary>
		/// Converts this instance to a string value.
		/// </summary>
		/// <param name="throwOnError">Throw 'notice' when conversion fails?</param>
		/// <param name="success">Indicates whether conversion was successful.</param>
		/// <returns>Returns "array" string.</returns>
		string IPhpConvertible.ToString(bool throwOnError, out bool success)
		{
			if (throwOnError)
				PhpException.Throw(PhpError.Notice, CoreResources.GetString("array_to_string_conversion"));
			success = false;
            return PrintablePhpTypeName;
		}

		public override string ToString()
		{
			return String.Format("array({0})", this.Count);
		}

		#endregion

		#region ICloneable Members

		/// <summary>
		/// Creates a shallow copy of this instance.
		/// </summary>
		/// <returns>The copy.</returns>
		public override object Clone()
		{
            return new PhpArray(this, true);
		}

		#endregion

		#region IPhpCloneable Members

		/// <summary>
		/// Creates a deep copy of this instance.
		/// </summary>
		/// <returns>The copy.</returns>
		public object DeepCopy()
		{
            var clone = new PhpArray(this, true);
            clone.EnsureWritable();
            return clone;
		}

		public object Copy(CopyReason reason)
		{
            if (reason == CopyReason.ReturnedByCopy && this.InplaceCopyOnReturn)
			{
                this.table.InplaceCopyOnReturn = false; // copiesCount = 0
                this.table.Share();                     // copiesCount = 1 => underlaying table is shared and its values will be copied lazily if necessary

				return this;
			}
			else
			{
                // create lazy copied PhpArray,
                // preserve MaxIntegerKey if array was not passed as an argument or within assignment expression:
                return new PhpArray(this, (reason != CopyReason.PassedByCopy && reason != CopyReason.Assigned));
			}
		}

		#endregion

		#region IPhpComparable Members

		/// <summary>
		/// Compares this instance with an object of arbitrary PHP.NET type.
		/// </summary>
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj)"]/*'/>
		public int CompareTo(object obj)
		{
			return CompareTo(obj, PhpComparer.Default);
		}

		/// <summary>
		/// Compares this instance with an object of arbitrary PHP.NET type.
		/// </summary>
		/// <remarks>
		/// <para>Empty array is equal to the null reference.</para>
		/// <para>Non empty array is greater than the null reference.</para>
		/// <para>If <paramref name="obj"/> is of type <see cref="bool"/> then 
		/// the comparison is performed between the boolean term "the number of items in this instance is positive" 
		/// and <paramref name="obj"/>.</para>
		/// <para>If <paramref name="obj"/> is of type <see cref="PhpArray"/> then the item counts are compared at first,
		/// the corresponding keys then and finally the corresponding values are compared. Values comparison 
		/// is performed by specified <paramref name="comparer"/> and can be recursive. Never-ending recursion is prevented.</para>
		/// </remarks>
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj,comparer)"]/*'/>
		public int CompareTo(object obj, IComparer comparer)
		{
			PhpArray array;

			if (obj == null) return Count;
			if (obj is bool) return (Count > 0 ? 2 : 1) - ((bool)obj ? 2 : 1);

			if ((array = obj as PhpArray) != null)
			{
                // compare elements:
				bool incomparable;
				int result = CompareArrays(this, array, comparer, out incomparable);
				if (incomparable)
				{
					//PhpException.Throw(PhpError.Warning, CoreResources.GetString("incomparable_arrays_compared"));
                    throw new ArgumentException();  // according to the IComparable remarks
				}
				return result;
			}

			return 1;
		}

		/// <summary>
		/// Compares two instances of <see cref="PhpArray"/>.
		/// </summary>
		/// <param name="comparer">The comparer.</param>
		/// <param name="incomparable">Whether arrays are incomparable 
		/// (no difference is found before both arrays enters an infinite recursion). 
		/// Returns zero then.</param>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		private static int CompareArrays(PhpArray x, PhpArray y, IComparer comparer, out bool incomparable)
		{
			Debug.Assert(x != null && y != null);

			incomparable = false;

            // if both operands point to the same internal dictionary:
            if (object.ReferenceEquals(x.table, y.table))
                return 0;

            //
			object child_x, child_y;
			PhpArray array_x, array_y;
			PhpArray sorted_x, sorted_y;
			IEnumerator<KeyValuePair<IntStringKey, object>> iter_x, iter_y;

			// if numbers of elements differs:
			int result = x.Count - y.Count;
			if (result != 0) return result;

			// comparing with the same instance:
			if (x == y) return 0;

			// marks arrays as visited (will be always restored to false value before return):
			x.Visited = true;
			y.Visited = true;

			// it will be more effective to implement OrderedHashtable.ToOrderedList method and use it here (in future version):
			sorted_x = (PhpArray)x.Clone();
			sorted_x.Sort(KeyComparer.ArrayKeys);
			sorted_y = (PhpArray)y.Clone();
			sorted_y.Sort(KeyComparer.ArrayKeys);

			iter_x = sorted_x.GetEnumerator();
			iter_y = sorted_y.GetEnumerator();

			result = 0;

			try
			{
				// compares corresponding elements (keys first values then):
				while (iter_x.MoveNext())
				{
					iter_y.MoveNext();

					// compares keys:
					result = iter_x.Current.Key.CompareTo(iter_y.Current.Key);
					if (result != 0) break;

					// dereferences childs if they are references:
					child_x = PhpVariable.Dereference(iter_x.Current.Value);
					child_y = PhpVariable.Dereference(iter_y.Current.Value);

					// compares values:
					if ((array_x = child_x as PhpArray) != null)
					{
						if ((array_y = child_y as PhpArray) != null)
						{
							// at least one child has not been visited yet => continue with recursion:
							if (!array_x.Visited || !array_y.Visited)
							{
								result = CompareArrays(array_x, array_y, comparer, out incomparable);
							}
							else
							{
								incomparable = true;
							}

							// infinity recursion has been detected:
							if (incomparable) break;
						}
						else
						{
							// compares an array with a non-array:
                            array_x.CompareTo(child_y, comparer);
						}
					}
					else
					{
						// compares unknown item with a non-array:
						result = -comparer.Compare(child_y, child_x);
					}

					if (result != 0) break;
				} // while
			}
			finally
			{
				x.Visited = false;
				y.Visited = false;
			}
			return result;
		}

		#endregion

		#region Strict Comparison

		/// <summary>
		/// Compares this instance with another <see cref="PhpArray"/>.
		/// </summary>
		/// <param name="array">The array to be strictly compared.</param>
		/// <returns>Whether this instance strictly equals to <paramref name="array"/>.</returns>
		/// <remarks>
		/// Arrays are strictly equal if all entries are strictly equal and in the same order in both arrays.
		/// Entries are strictly equal if keys are the same and values are strictly equal 
		/// in the terms of operator <see cref="Operators.StrictEquality"/>.
		/// </remarks>
		public bool StrictCompareEq(PhpArray array)
		{
			bool incomparable, result;

            result = StrictCompareArrays(this, array, out incomparable);
			if (incomparable)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("incomparable_arrays_compared"));
			}

			return result;
		}

		/// <summary>
		/// Compares two instances of <see cref="PhpArray"/> for strict equality.
		/// </summary>
		/// <param name="incomparable">Whether arrays are incomparable 
		/// (no difference is found before both arrays enters an infinite recursion). 
		/// Returns <B>true</B> then.</param>
		/// <include file='Doc/Common.xml' path='docs/method[@name="Compare(x,y)"]/*'/>
		private static bool StrictCompareArrays(PhpArray x, PhpArray y, out bool incomparable)
		{
			Debug.Assert(x != null && y != null);

			incomparable = false;

            // if both operands point to the same internal dictionary:
            if (object.ReferenceEquals(x.table, y.table))
                return true;

            //
			object child_x, child_y;
			PhpArray array_x, array_y;
			PhpReference r;
			IEnumerator<KeyValuePair<IntStringKey, object>> iter_x, iter_y;

			// if numbers of elements differs:
			if (x.Count != y.Count) return false;

			// comparing with the same instance:
			if (x == y) return true;

			iter_x = x.GetEnumerator();
			iter_y = y.GetEnumerator();

			// marks arrays as visited (will be always restored to false value before return):
			x.Visited = true;
			y.Visited = true;

			bool result = true;

			try
			{
				// compares corresponding elements (keys first values then):
				while (iter_x.MoveNext())
				{
					iter_y.MoveNext();

					// compares keys:
					if (!iter_x.Current.Key.Equals(iter_y.Current.Key))
					{
						result = false;
						break;
					}

					// dereferences x child if it is a reference:
					child_x = iter_x.Current.Value;
					if ((r = child_x as PhpReference) != null) child_x = r.Value;

					// dereferences y child if it is a reference:
					child_y = iter_y.Current.Value;
					if ((r = child_y as PhpReference) != null) child_y = r.Value;

					// compares values:
					if ((array_x = child_x as PhpArray) != null)
					{
						if ((array_y = child_y as PhpArray) != null)
						{
							// at least one child has not been visited yet => continue with recursion:
							if (!array_x.Visited || !array_y.Visited)
							{
								result = StrictCompareArrays(array_x, array_y, out incomparable);
							}
							else
							{
								incomparable = true;
							}

							// infinity recursion has been detected:
							if (incomparable) break;
						}
						else
						{
							// an array with a non-array comparison:
							result = false;
						}
					}
					else
					{
						// compares unknown item with a non-array:
						result = Operators.StrictEquality(child_x, child_y);
					}

					if (!result) break;
				} // while
			}
			finally
			{
				x.Visited = false;
				y.Visited = false;
			}
			return result;
		}

		#endregion

		#region IPhpVariable

		/// <summary>
		/// Defines emptiness of the <see cref="PhpArray"/>.
		/// </summary>
		/// <returns>Whether this instance contains no element.</returns>
		public bool IsEmpty()
		{
			return Count == 0;
		}

		/// <summary>
		/// Defines whether <see cref="PhpArray"/> is a scalar.
		/// </summary>
		/// <returns><B>false</B></returns>
		public bool IsScalar()
		{
			return false;
		}

		/// <summary>
		/// Returns a name of declaring type.
		/// </summary>
		/// <returns>The name.</returns>
		public string GetTypeName()
		{
			return PhpTypeName;
		}

		#endregion

		#region IPhpEnumerable Members

		/// <summary>
		/// Intrinsic enumerator associated with the array. Initialized lazily when read for the first time.
		/// The enumerator points to the first item of the array immediately after the initialization if exists,
		/// otherwise it points to an invalid item and <see cref="IPhpEnumerator.AtEnd"/> is <B>true</B>.
		/// </summary>
		public IPhpEnumerator/*!*/ IntrinsicEnumerator
		{
			get
			{
				// initializes enumerator:
				if (intrinsicEnumerator == null)
				{
					intrinsicEnumerator = this.GetPhpEnumerator();
					intrinsicEnumerator.MoveNext();
				}
				return intrinsicEnumerator;
			}
		}

		/// <summary>
		/// Restarts intrinsic enumerator - moves it to the first item.
		/// </summary>
		/// <remarks>
		/// If the intrinsic enumerator has never been used on this instance nothing happens.
		/// </remarks>
		public void RestartIntrinsicEnumerator()
		{
			if (intrinsicEnumerator != null)
			    intrinsicEnumerator.MoveFirst();
		}

		/// <summary>
		/// Creates an enumerator used in foreach statement.
		/// </summary>
		/// <param name="keyed">Whether the foreach statement uses keys.</param>
		/// <param name="aliasedValues">Whether the values returned by enumerator are assigned by reference.</param>
		/// <param name="caller">Type <see cref="Reflection.DTypeDesc"/> of the caller (ignored).</param>
		/// <returns>The dictionary enumerator.</returns>
		/// <remarks>Used for internal purposes only!</remarks>
        public virtual IDictionaryEnumerator GetForeachEnumerator(bool keyed, bool aliasedValues, Reflection.DTypeDesc caller)
        {
            if (this.Count == 0)
                return OrderedDictionary.EmptyEnumerator.SingletonInstance;

            if (aliasedValues)
                return new ForeachEnumeratorAliased(this, keyed);
            else
                return new ForeachEnumeratorValues(this/*, keyed*/);
        }

		#endregion

        #region Nested class: ForeachEnumeratorValues

        /// <summary>
		/// An enumerator used (only) for foreach statement.
		/// </summary>
		private sealed class ForeachEnumeratorValues : IDictionaryEnumerator, IDisposable
		{

            /// <summary>
            /// The internal enumerator used to iterate through the read only copy of array.
            /// </summary>
            private readonly OrderedDictionary.Enumerator/*!*/enumerator;

            /// <summary>
            /// Wheter the internal enumerator was disposed.
            /// </summary>
            private bool disposed = false;
 
			/// <summary>
			/// Creates a new instance of the enumerator.
			/// </summary>
			/// <param name="array">The array to iterate over.</param>
			public ForeachEnumeratorValues(PhpArray/*!*/ array)
			{
				Debug.Assert(array != null);

                // share the table to iterate through readonly array,
                // get the enumerator, have to be disposed at the end of enumeration, otherwise deep copy will be performed probably

                // note (J): this will not result in registering the enumerator in the PhpArray object, not needed, faster
                this.enumerator = (OrderedDictionary.Enumerator)array.table.Share().GetEnumerator();
			}

			#region IDictionaryEnumerator Members

			/// <summary>
			/// Gets a current key.
			/// </summary>
			public object Key
			{
				get
				{
					// deep copy is not needed because a key is immutable,
                    // we can access .current directly, the underlaying table is read only:
                    return enumerator.CurrentKey.Object;
				}
			}

			/// <summary>
			/// Gets a current value. Returns either a deep copy of a value if values are not aliased or 
			/// a <see cref="PhpReference"/> otherwise. In the latter case, the reference item is added to the array
			/// if there is not one.
			/// </summary>
			public object Value
			{
				get
				{
					// a deep copy of value stored in the original array should be returned,
                    // we can access .current directly, the underlaying table is read only:
                    return PhpVariable.Copy(PhpVariable.Dereference(enumerator.CurrentValue), CopyReason.Assigned);
				}
			}

            public DictionaryEntry Entry { get { throw new NotSupportedException(); } }

			#endregion

			#region IEnumerator Members

			/// <summary>
			/// Resets enumerator.
			/// </summary>
			public void Reset()
			{
				throw new NotSupportedException();
			}

			/// <summary>
			/// Moves to the next entry.
			/// </summary>
			/// <returns>Whether we can continue.</returns>
			public bool MoveNext()
			{
                // move the internal enumerator forward
                if (!enumerator.MoveNext())
                {
                    // dispose on the end of enumeration
                    Dispose();

                    return false;
                }

                return true;
			}

			/// <summary>
			/// Not supported.
			/// </summary>
			public object Current
			{
				get
				{
					throw new NotSupportedException();
				}
			}

			#endregion

            #region IDisposable Members

            /// <summary>
            /// Unshare the underlaying table and dispose enumerator resources if any.
            /// </summary>
            /// <remarks>If this method is not called at least once, the underlaying table may be lazily copied later in some cases.</remarks>
            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;                    // do not dispose again
                    enumerator.table.Unshare();         // return back the table so it can be writable again in most cases
                    enumerator.Dispose();               // disable the enumerator, free resources if any
                }
            }

            #endregion
        }

		#endregion

        #region Nested class: ForeachEnumeratorAliased

        /// <summary>
        /// An enumerator used (only) for aliased foreach statement.
        /// </summary>
        private sealed class ForeachEnumeratorAliased : IDictionaryEnumerator, IDisposable
        {
            /// <summary>
            /// Array to get values from.
            /// </summary>
            private readonly OrderedDictionary.Enumerator/*!*/enumerator;
            private readonly PhpArray/*!*/array;

            /// <summary>
            /// Remember the last key (right after <see cref="MoveNext"/>) to detect whether current entry has been deleted during foreach body.
            /// </summary>
            private IntStringKey currentKey;

            /// <summary>
            /// Creates a new instance of the enumerator.
            /// </summary>
            /// <param name="array">The array to iterate over.</param>
            /// <param name="keyed">Whether keys are interesting.</param>
            public ForeachEnumeratorAliased(PhpArray/*!*/ array, bool keyed)
            {
                Debug.Assert(array != null);

                this.array = array;
                this.enumerator = new OrderedDictionary.Enumerator(array, true);

                // ForeachEnumeratorAliased can leave an undisposed enumerator registered in the array object
                // (only in case a break; was called inside an aliased foreach loop).
            }

            #region IDictionaryEnumerator Members

            /// <summary>
            /// Gets a current key.
            /// </summary>
            public object Key
            {
                get
                {
                    // deep copy is not needed because a key is immutable:
                    return currentKey.Object;
                }
            }

            /// <summary>
            /// Gets a current value. Returns either a deep copy of a value if values are not aliased or 
            /// a <see cref="PhpReference"/> otherwise. In the latter case, the reference item is added to the array
            /// if there is not one.
            /// </summary>
            public object Value
            {
                get
                {
                    var key = enumerator.CurrentKey;
                    return array.table._ensure_item_ref(ref key, array);
                }
            }

            public DictionaryEntry Entry { get { throw new NotSupportedException(); } }

            #endregion

            #region IEnumerator Members

            /// <summary>
            /// Resets enumerator.
            /// </summary>
            public void Reset()
            {
                throw new NotSupportedException();
            }

            /// <summary>
            /// Moves to the next entry.
            /// </summary>
            /// <returns>Whether we can continue.</returns>
            public bool MoveNext()
            {
                bool hasMore;

                if (enumerator.CurrentKey.Equals(ref currentKey))
                {
                    // advance to the next position
                    hasMore = enumerator.MoveNext();
                }
                else
                {
                    hasMore = !enumerator.AtEnd;   // user deleted current entry and enumerator was already advanced to the next position
                }

                this.currentKey = enumerator.CurrentKey;

                if (!hasMore)
                    this.Dispose(); // dispose underlaying Enumerator so it can be unregistered from active enumerators list
                
                return hasMore;
            }

            /// <summary>
            /// Not supported.
            /// </summary>
            public object Current
            {
                get
                {
                    throw new NotSupportedException();
                }
            }

            #endregion

            #region IDisposable Members

            private bool disposed = false;

            public void Dispose()
            {
                if (!disposed)
                {
                    disposed = true;
                    enumerator.Dispose();
                }
            }

            #endregion
        }

        #endregion

		#region IPhpObjectGraphNode Members

		/// <summary>
		/// Walks the object graph rooted in this node.
		/// </summary>
		/// <param name="callback">The callback method.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		/// <returns>The <paramref name="callback"/>'s result.</returns>
		public void Walk(PHP.Core.PhpWalkCallback callback, ScriptContext context)
		{
			// prevents recursion:
			if (!this.Visited)
			{
				this.Visited = true;

				try
				{
					// iterates through array items and invokes the callback:
					foreach (KeyValuePair<IntStringKey, object> entry in this)
					{
						IPhpObjectGraphNode node = entry.Value as IPhpObjectGraphNode;
						if (node != null)
						{
							object res = callback(node, context);
							if (res != entry.Value) this[entry.Key] = res;

							if ((node = res as IPhpObjectGraphNode) != null) node.Walk(callback, context);
						}
					}
				}
				finally
				{
					this.Visited = false;
				}
			}
		}

		#endregion
		
		#region Operators

		#region GetItem
		
		/// <summary>
		/// Retrieves an item from an array.
		/// </summary>
		/// <param name="key">The key of arbitrary PHP.NET type.</param>
		/// <param name="quiet">Disables reporting of notices and warnings.</param>
		/// <returns>The item.</returns>
		/// <exception cref="PhpException"><paramref name="key"/> is not a legal key (Warning).</exception>
		/// <exception cref="PhpException">The <paramref name="key"/> is not contained in <see cref="PhpArray"/> (Notice).</exception>
		[Emitted]
		public object GetArrayItem(object key, bool quiet)
		{
			Debug.Assert(!(key is PhpReference));

            if (this.GetType() == typeof(PhpArray))
            {
                IntStringKey array_key;
                if (Convert.ObjectToArrayKey(key, out array_key))
                {
                    object value;
                    if (this.table.TryGetValue(array_key, out value))
                        return PhpVariable.Dereference(value);
                }
                else
                {
                    PhpException.IllegalOffsetType();
                    return null;
                }                
            }

            return GetArrayItemOverride(key, quiet);
		}

		[Emitted]
		public object GetArrayItem(int key, bool quiet)
		{
            //if (this.GetType() == typeof(PhpArray))   // otherwise just this.table.TryGetValue returns false
            {
                object value;
                if (this.table.TryGetValue(key, out value))
                    return PhpVariable.Dereference(value);
            }
            
            return GetArrayItemOverride(key, quiet);
		}

        /// <summary>
		/// Gets item of the array associated with a specified key of string type (a conversion to integer key may take place).
		/// </summary>
		[Emitted]
		public object GetArrayItem(string/*!*/ key, bool quiet)
		{
			Debug.Assert(key != null);

            //if (this.GetType() == typeof(PhpArray))   // otherwise just this.table.TryGetValue returns false
            {
                object value;
                if (this.table.TryGetValue(Core.Convert.StringToArrayKey(key), out value))
                    return PhpVariable.Dereference(value);
            }
            
            return GetArrayItemOverride(key, quiet);
		}

        [Emitted]
        public object GetArrayItemExact(string/*!*/ key, bool quiet, int hashcode)
        {
            Debug.Assert(key != null);

            //if (this.GetType() == typeof(PhpArray))   // otherwise just this.table.TryGetValue returns false
            {
                object value;
                if (this.table.TryGetValue(new IntStringKey(key, hashcode), out value))
                    return PhpVariable.Dereference(value);
            }
            
            return GetArrayItemOverride(key, quiet);
        }

        #region protected virtual: GetArrayItemOverride

        /// <summary>
        /// Handles undefined offset when getting a value from the array or derivet PhpArray types. Can be overriden.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="quiet">Whether a notice should not be displayed.</param>
        /// <returns><c>null</c> reference or an actual value in overriden class.</returns>
        protected virtual object GetArrayItemOverride(object key, bool quiet)
        {
            if (!quiet)
                PhpException.UndefinedOffset(key);

            return null;
        }

        #endregion

        #endregion

        #region GetItemRef

        /// <summary>
		/// Retrieves a reference on new item of the array.
		/// </summary>
		[Emitted]
		public PhpReference/*!*/ GetArrayItemRef()
		{
            PhpReference result;
            if (this.GetType() == typeof(PhpArray))
                Add(result = new PhpReference());
            else
                result = GetArrayItemRefOverride();
            
            return result;
		}

        /// <summary>
        /// Retrieves a reference on an item of the array.
        /// </summary>
        /// <param name="key">Key of the item.</param>
        /// <returns><see cref="PhpReference"/> of the item.</returns>
        /// <exception cref="PhpException"><paramref name="key"/> is not a legal key (Warning).</exception>
        [Emitted]
        public PhpReference/*!*/ GetArrayItemRef(object key)
        {
            Debug.Assert(!(key is PhpReference));

            if (this.GetType() == typeof(PhpArray))
            {
                IntStringKey array_key;
                if (!Convert.ObjectToArrayKey(key, out array_key))
                {
                    PhpException.IllegalOffsetType();
                    return new PhpReference();
                }

                return GetArrayItemRef(array_key);
            }
            else
            {
                return GetArrayItemRefOverride(key);
            }
        }

        [Emitted]
		public PhpReference/*!*/ GetArrayItemRef(int key)
		{
            if (this.GetType() == typeof(PhpArray))
                return GetArrayItemRef(new IntStringKey(key));
            else
                return GetArrayItemRefOverride(key);
		}

        [Emitted]
		public PhpReference/*!*/ GetArrayItemRef(string/*!*/ key)
		{
			Debug.Assert(key != null);
			
			// the key cannot be converted by compiler using StringToArrayKey as the compiler doesn't know
			// whether the array is not actually ArrayAccess unless it performs som type analysis

            if (this.GetType() == typeof(PhpArray))
                return GetArrayItemRef(Convert.StringToArrayKey(key));
            else
                return GetArrayItemRefOverride(key);			
		}

        private PhpReference/*!*/ GetArrayItemRef(IntStringKey key)
		{
            return this.table._ensure_item_ref(ref key, this);
		}

        #region protected virtual: GetArrayItemRefOverride

        protected virtual PhpReference/*!*/GetArrayItemRefOverride()
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        protected virtual PhpReference/*!*/ GetArrayItemRefOverride(object key)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        protected virtual PhpReference/*!*/ GetArrayItemRefOverride(int key)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        protected virtual PhpReference/*!*/ GetArrayItemRefOverride(string/*!*/ key)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        #endregion

        #endregion

        #region SetArrayItem

        /// <summary>
		/// Sets a value to an item of a <see cref="PhpArray"/>. Implements the last keyed [] operator in the chain.
		/// </summary>
		/// <param name="value">The value to be set to a new item (value or <see cref="PhpReference"/>).</param>
		[Emitted]
		public void SetArrayItem(object value)
		{
            if (this.GetType() == typeof(PhpArray))
                this.Add(value);
            else
                this.SetArrayItemOverride(value);
		}

		[Emitted]
		public void SetArrayItem(object key, object value)
		{
			Debug.Assert(!(key is PhpReference) && !(value is PhpReference));

            if (this.GetType() == typeof(PhpArray))
            {
                IntStringKey array_key;
                if (Convert.ObjectToArrayKey(key, out array_key))
                    SetArrayItem(array_key, value);
                else
                    PhpException.IllegalOffsetType();
            }
            else
            {
                SetArrayItemOverride(key, value);
            }
		}

		[Emitted]
		public void SetArrayItem(int key, object value)
		{
			Debug.Assert(!(value is PhpReference));

            if (this.GetType() == typeof(PhpArray))
            {
                this.EnsureWritable();
                this.table._add_or_update_preserve_ref(this, key, value);
            }
            else
                SetArrayItemOverride(key, value);
		}

		[Emitted]
		public void SetArrayItem(string/*!*/ key, object value)
		{
			Debug.Assert(key != null && !(value is PhpReference));

            if (this.GetType() == typeof(PhpArray))
                // the key cannot be converted by compiler using StringToArrayKey as the compiler doesn't know
                // whether the array is not actually ArrayAccess unless it performs som type analysis
                SetArrayItem(Convert.StringToArrayKey(key), value);
            else
                SetArrayItemOverride(key, value);
		}

		[Emitted]
		public void SetArrayItemExact(string/*!*/ key, object value, int hashcode)
		{
			Debug.Assert(key != null && !(value is PhpReference));

            if (this.GetType() == typeof(PhpArray))
                SetArrayItem(new IntStringKey(key, hashcode), value);
            else
                SetArrayItemOverride(key, value);
		}

		private void SetArrayItem(IntStringKey key, object value)
		{
            Debug.Assert(this.GetType() == typeof(PhpArray));

            this.EnsureWritable();
            this.table._add_or_update_preserve_ref(this, ref key, value);
		}

        #region protected virtual: SetArrayItemOverride

        protected virtual void SetArrayItemOverride(object value)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        protected virtual void SetArrayItemOverride(object key, object value)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }
        protected virtual void SetArrayItemOverride(int key, object value)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }
        protected virtual void SetArrayItemOverride(string key, object value)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        #endregion

        #endregion

        #region SetArrayItemRef

        [Emitted]
		public void SetArrayItemRef(object key, PhpReference value)
		{
			Debug.Assert(!(key is PhpReference));

            if (this.GetType() == typeof(PhpArray))
            {
                IntStringKey array_key;
                if (Convert.ObjectToArrayKey(key, out array_key))
                    this[array_key] = value;
                else
                    PhpException.IllegalOffsetType();                
            }
            else
                SetArrayItemRefOverride(key, value);
		}

		[Emitted]
		public void SetArrayItemRef(int key, PhpReference value)
		{
            if (this.GetType() == typeof(PhpArray))
                this[key] = value;
            else
                SetArrayItemRefOverride(key, value);
		}

		[Emitted]
		public void SetArrayItemRef(string/*!*/ key, PhpReference value)
		{
			Debug.Assert(key != null);

            if (this.GetType() == typeof(PhpArray))
                // the key cannot be converted by compiler using StringToArrayKey as the compiler doesn't know
                // whether the array is not actually ArrayAccess unless it performs som type analysis
                this[Convert.StringToArrayKey(key)] = value;
            else
                SetArrayItemRefOverride(key, value);
		}

        #region protected virtual: SetArrayItemRefOverride

        protected virtual void SetArrayItemRefOverride(object key, PhpReference value)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        #endregion

        #endregion

        #region Ensure

        /// <summary>
		/// Ensures a specified array item is an instance of <see cref="PhpArray"/>. 
		/// </summary>
		/// <remarks>A new instance of <see cref="PhpArray"/> is assigned to the item if it is empty in a meaning of <see cref="Operators.IsEmptyForEnsure"/>.</remarks>
		/// <returns>The item associated with a key after it might be replaced by a new instance of <see cref="PhpArray"/>.</returns>
		[Emitted]
		public PhpArray EnsureItemIsArray()
		{
            if (this.GetType() == typeof(PhpArray))
            {
                PhpArray result;
                Add(result = new PhpArray());
                return result;
            }
            else
            {
                return EnsureItemIsArrayOverride();
            }			
		}

		/// <summary>
		/// Ensures specified array item is an instance of <see cref="DObject"/>. 
		/// </summary>
		/// <param name="context">The <see cref="ScriptContext"/> in which potential new object will be created.</param>
		/// <remarks>A new instance of <see cref="PHP.Library.stdClass"/> is assigned to the item if it is empty in a meaning of <see cref="Operators.IsEmptyForEnsure"/>.</remarks>
		/// <returns>The item associated with a key after the potential replacement by a new instance of <see cref="PHP.Library.stdClass"/>.</returns>
		[Emitted]
		public DObject EnsureItemIsObject(ScriptContext/*!*/ context)
		{
            if (this.GetType() == typeof(PhpArray))
            {
                PHP.Library.stdClass result;
                Add(result = PHP.Library.stdClass.CreateDefaultObject(context));
                return result;
            }
            else
            {
                return EnsureItemIsObjectOverride(context);
            }
		}

		[Emitted]
		public PhpArray EnsureItemIsArray(object key)
		{
            Debug.Assert(!(key is PhpReference));

            if (this.GetType() == typeof(PhpArray))
            {
                IntStringKey array_key;
                if (!Convert.ObjectToArrayKey(key, out array_key))
                {
                    PhpException.IllegalOffsetType();
                    return null;
                }

                // TODO: set writable only if item is not reference
                this.EnsureWritable();   // if we are not writing here, we can in some child array, MUST be set as writable now

                //OrderedHashtable<IntStringKey>.Element element = GetElement(array_key);

                object item = this.table[array_key];// = (element != null) ? element.Value : null;

                // dereferences item if it is a reference:
                PhpReference ref_item = item as PhpReference;
                if (ref_item != null) item = ref_item.Value;

                // convert obj to array or wrap obj into new array if possible:
                object new_item;
                var wrappedarray = Operators.EnsureObjectIsArray(item, out new_item);
                if (wrappedarray != null)
                {
                    if (new_item != null)
                    {
                        // if there was a reference then its value is replaced, 
                        // the value of element is replaced otherwise:
                        if (ref_item != null) ref_item.Value = new_item;
                        else this[array_key] = new_item;
                    }
                    return wrappedarray;
                }

                // error - the item is a scalar, a DObject:
                PhpException.VariableMisusedAsArray(item, false);
                return null;

            }
            else
            {
                return EnsureItemIsArrayOverride(key);
            }
		}

		[Emitted]
		public DObject EnsureItemIsObject(object key, ScriptContext/*!*/ context)
		{
			Debug.Assert(!(key is PhpReference));

            if (this.GetType() == typeof(PhpArray))
            {
                IntStringKey array_key;
                if (!Convert.ObjectToArrayKey(key, out array_key))
                {
                    PhpException.IllegalOffsetType();
                    return null;
                }

                // TODO: set writable only if item is not reference
                this.EnsureWritable();   // if we are not writing here, we can in some child array, MUST be set as writable now

                //OrderedHashtable<IntStringKey>.Element element = GetElement(array_key);
                object item = table[array_key]; //(element != null) ? element.Value : null;

                // dereferences item if it is a reference:
                PhpReference ref_item = item as PhpReference;
                if (ref_item != null) item = ref_item.Value;

                // the item is already an object:
                DObject object_item = item as DObject;
                if (object_item != null) return object_item;

                // an item is empty => creates a new array:
                if (Operators.IsEmptyForEnsure(item))
                {
                    object_item = PHP.Library.stdClass.CreateDefaultObject(context);

                    // if there was a reference then its value is replaced, the item of array is replaced otherwise:
                    if (ref_item != null)
                        ref_item.Value = object_item;
                    else
                        this[array_key] = object_item;

                    return object_item;
                }

                // error - the item is a scalar, a PhpArray or a non-empty string:
                PhpException.VariableMisusedAsObject(item, false);
                return null;
            }
            else
            {
                return EnsureItemIsObjectOverride(key, context);
            }
        }

        #region protected virtual: EnsureItemIs*Override

        protected virtual PhpArray EnsureItemIsArrayOverride()
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        protected virtual DObject EnsureItemIsObjectOverride(ScriptContext/*!*/ context)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        protected virtual PhpArray EnsureItemIsArrayOverride(object key)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        protected virtual DObject EnsureItemIsObjectOverride(object key, ScriptContext/*!*/ context)
        {
            Debug.Fail("This method has to be overriden!");
            throw new InvalidOperationException();
        }

        #endregion

        #endregion

        #endregion
    }

	#region Debug View

	[DebuggerDisplay("Count = {array.Count}", Type = "array")]
	internal sealed class PhpArrayDebugView
	{
		private readonly PhpArray array;

		public PhpArrayDebugView(PhpArray/*!*/ array)
		{
			if (array == null)
				throw new ArgumentNullException("array");

			this.array = array;
		}

		[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
		public PhpHashEntryDebugView[] Items
		{
			get
			{
				PhpHashEntryDebugView[] result = new PhpHashEntryDebugView[array.Count];

				int i = 0;
				foreach (KeyValuePair<IntStringKey, object> entry in array)
					result[i++] = new PhpHashEntryDebugView(entry.Key, entry.Value);

				return result;
			}
		}
	}

	[DebuggerDisplay("{Value}", Name = "{Key}", Type = "{KeyType,nq} => {ValueType,nq}")]
	internal sealed class PhpHashEntryDebugView
	{
		[DebuggerDisplay("{Key}", Name = "Key", Type = "{KeyType,nq}")]
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public object Key { get { return key.Object; } }

		[DebuggerDisplay("{this.value}", Name = "Value", Type = "{ValueType,nq}")]
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value { get { return value; } }

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private IntStringKey key;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		private object value;

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public string KeyType
		{
			get
			{
				return key.IsInteger ? PhpVariable.TypeNameInteger : PhpVariable.TypeNameString;
			}
		}

		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public string ValueType
		{
			get
			{
				return PhpVariable.GetTypeName(value);
			}
		}

		public PhpHashEntryDebugView(IntStringKey key, object value)
		{
			this.key = key;
			this.value = value;
		}
	}

	#endregion
}

