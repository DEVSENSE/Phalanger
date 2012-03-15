/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Text;
using System.Diagnostics;
using System.Collections;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
	/// <summary>
	/// Represents binary data in PHP language.
	/// </summary>
	[Serializable]
	[DebuggerNonUserCode]
    [DebuggerDisplay("\"{this.DebugView(),nq}\"", Type = "binary({Length})")]
	public sealed class PhpBytes : IPhpVariable, IPhpObjectGraphNode, ICloneable         // GENERICS: IEquatable<PhpBytes>
	{
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public const string PhpTypeName = PhpVariable.TypeNameString;

        #region DataContainer

        /// <summary>
        /// Internal data structure holds the byte array.
        /// The data can be marked as read only. This tells the runtime if the internal data structure can be reused to avoid of copying.
        /// </summary>
        [Serializable]
        private sealed class DataContainer
        {
            #region Fields

            /// <summary>
            /// True iff the internal data structure is shared and should not be modified.
            /// </summary>
            public bool IsShared
            {
                get { return _refCount > 1; }
            }
            private int _refCount;

            /// <summary>
            /// Internal byte array representing the data.
            /// </summary>
            public byte[]/*!*/Data { get { return _data; } }
            private readonly byte[]/*!*/_data;

            /// <summary>
            /// The length of internal byte array.
            /// </summary>
            public int Length { get { return _data.Length; } }

            #endregion

            #region Constructors

            /// <summary>
            /// Initialize the instance of <see cref="Data"/> with byte array. The data are not marked as <see cref="IsShared"/>.
            /// </summary>
            /// <param name="data">The byte array reference used internally.</param>
            public DataContainer(params byte[]/*!*/ data)
                : this(1, data)
            {
            }

            /// <summary>
            /// Initialize the instance of <see cref="Data"/> with byte array.
            /// </summary>
            /// <param name="refCount">Number of references to this class. By default 1.</param>
            /// <param name="data">The byte array reference used internally.</param>
            public DataContainer(int refCount, params byte[]/*!*/ data)
            {
                Debug.Assert(data != null);
                this._refCount = refCount;
                this._data = data;
            }

            #endregion

            #region Share, Unshare

            /// <summary>
            /// Marks this instance as shared (<see cref="IsShared"/>) and returns itself.
            /// </summary>
            /// <returns></returns>
            internal DataContainer/*!*/Share()
            {
                ++_refCount;
                return this;
            }

            /// <summary>
            /// Get back shared instance of internal <see cref="byte"/> array.
            /// </summary>
            internal void Unshare()
            {
                --_refCount;
            }

            #endregion

            #region this[]

            /// <summary>
            /// Get byte on specified index.
            /// </summary>
            /// <param name="i"></param>
            /// <returns></returns>
            public byte this[int i]
            {
                get
                {
                    return _data[i];
                }
            }

            #endregion
        }

        #endregion

        #region Fields & Properties

        /// <summary>
        /// Empty bytes. Not a single instance with zero length.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public static readonly PhpBytes Empty = new PhpBytes(ArrayUtils.EmptyBytes);

        /// <summary>
        /// Get the internal byte array for read only purposes.
        /// The returned array must not be modified! It is modifiable only because of the performance.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public byte[]/*!*/ReadonlyData { get { return _data.Data; } }

        /// <summary>
		/// Data contained in this instance. If internal byte array is shared with other <see cref="PhpBytes"/> objects,
        /// internal byte array is cloned.
		/// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public byte[]/*!*/ Data
		{
			get
            {
                if (_data.IsShared)
                {   // performs clone of internal byte array
                    _data.Unshare();
                    _data = new DataContainer((byte[])_data.Data.Clone());
                }

                return _data.Data;
            }
			set
            {
                if (value == null) throw new ArgumentNullException("value");

                if (_data.IsShared)
                    _data.Unshare();
                
                _data = new DataContainer(value);
            }
		}
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private DataContainer/*!*/_data;

		/// <summary>
		/// Gets data length.
		/// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public int Length { get { return _data.Length; } }

        /// <summary>
        /// The i-th byte from the internal byte array;
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public byte this[int i] { get { return _data[i]; } }

        #endregion

        #region DebugView, DumpTo

        /// <summary>
        /// Debug view of internal data. Non-ASCII characters are escaped.
        /// </summary>
        /// <returns>Content of this instance.</returns>
        private string DebugView()
        {
            var output = new System.IO.StringWriter();
            DumpTo(output);
            return output.ToString().Replace("\"", "\\\"");
        }

        /// <summary>
        /// Dumps internal data, escapes non-ASCII characters.
        /// </summary>
        /// <param name="output">Output to dump to.</param>
        private void DumpTo(System.IO.TextWriter/*!*/output)
        {
            Debug.Assert(output != null);

            const string hex_digs = "0123456789abcdef";
            char[] patch = new char[4] { '\\', 'x', '0', '0' };

            foreach (byte b in ReadonlyData)
            {
                // printable characters are outputted normally
                if (b < 0x7f)
                {
                    output.Write((char)b);
                }
                else
                {
                    patch[2] = hex_digs[(b & 0xf0) >> 4];
                    patch[3] = hex_digs[(b & 0x0f)];

                    output.Write(patch);
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
		/// Creates a new instance of the <see cref="PhpBytes"/> class.
		/// </summary>
		/// <param name="data">The array of bytes.</param>
		/// <exception cref="ArgumentNullException"><paramref name="data"/> is a <B>null</B> reference.</exception>
		[Emitted]
		public PhpBytes(params byte[]/*!*/ data)
		{
			if (data == null) throw new ArgumentNullException("data");
            this._data = new DataContainer(data);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="PhpBytes"/> class with its data converted from a string using 
		/// encoding from the <see cref="Configuration.Global"/> configuration.
		/// </summary>
		/// <param name="str">The string.</param>
		/// <exception cref="ArgumentNullException"><paramref name="str"/> is a <B>null</B> reference.</exception>
		public PhpBytes(string/*!*/ str)
		{
			if (str == null) throw new ArgumentNullException("str");
            this._data = new DataContainer(Configuration.Application.Globalization.PageEncoding.GetBytes(str));
		}

        /// <summary>
        /// Creates a new instance of the <see cref="PhpBytes"/> class that shares internal byte array
        /// with another <see cref="PhpBytes"/> instance.
        /// </summary>
        /// <param name="data">The original bytes array.</param>
        public PhpBytes(PhpBytes/*!*/data)
        {
            if (data == null) throw new ArgumentNullException("data");
            this._data = data._data.Share();
        }

		#endregion

        #region explicit string conversion

        ///// <summary>
        ///// Converts given PhpBytes object into string using current Phalanger PageEncoding
        ///// (see Configuration.Application.Globalization.PageEncoding).
        ///// </summary>
        ///// <param name="bytes">Object to convert.</param>
        ///// <returns>New string encoded from given PhpBytes.</returns>
        //public static explicit operator string(PhpBytes bytes)
        //{
        //    return ((IPhpConvertible)bytes).ToString();
        //}

        ///// <summary>
        ///// Convert given string into PhpBytes, using ctor of PhpBytes.
        ///// (see Configuration.Application.Globalization.PageEncoding).
        ///// </summary>
        ///// <param name="str">String to convert.</param>
        ///// <returns>Returns new PhpBytes(str).</returns>
        //public static explicit operator PhpBytes(string str)
        //{
        //    return new PhpBytes(str);
        //}

        #endregion

		#region IPhpConvertible Members

		/// <summary>
		/// Retrieves the type code of the Phalanger type.
		/// </summary>
		/// <returns>The <see cref="PhpTypeCode.PhpBytes"/> type code.</returns>
		public PHP.Core.PhpTypeCode GetTypeCode()
		{
			return PhpTypeCode.PhpBytes;
		}

		/// <summary>
		/// Retrives a content of this instance converted to the <see cref="double"/>.
		/// </summary>
		/// <returns>The double precision floating point number.</returns>
		public double ToDouble()
		{
			return Convert.StringToDouble(((IPhpConvertible)this).ToString());
		}

		/// <summary>
		/// Retrives a content of this instance converted to the <see cref="int"/>.
		/// </summary>
		/// <returns>The integer number.</returns>
		public int ToInteger()
		{
            return Convert.StringToInteger(((IPhpConvertible)this).ToString());
		}

		/// <summary>
		/// Retrives a content of this instance converted to the <see cref="long"/>.
		/// </summary>
		/// <returns>The integer number.</returns>
		public long ToLongInteger()
		{
            return Convert.StringToInteger(((IPhpConvertible)this).ToString());
		}

		/// <summary>
		/// Retrives a content of this instance converted to the <see cref="bool"/>.
		/// </summary>
		/// <returns>True iff this instance contains nothing or one zero byte.</returns>
		public bool ToBoolean()
		{
            return !(_data.Length == 0 || (_data.Length == 1 && _data[0] == (byte)'0'));
		}

		/// <summary>
		/// Retrives a content of this instance converted to the <see cref="byte"/>[].
		/// </summary>
		/// <returns>The array of bytes.</returns>
		public PhpBytes ToPhpBytes()
		{
			return this;
		}

		/// <summary>
		/// Converts this instance to a number of type <see cref="int"/> or <see cref="double"/>.
		/// </summary>
		/// <param name="doubleValue">The double value.</param>
		/// <param name="intValue">The integer value.</param>
		/// <param name="longValue">The long integer value.</param>
		/// <returns>Value of <see cref="Convert.NumberInfo"/>.</returns>
		public Convert.NumberInfo ToNumber(out int intValue, out long longValue, out double doubleValue)
		{
			return Convert.StringToNumber(((IPhpConvertible)this).ToString(), out intValue, out longValue, out doubleValue);
		}

        public override string ToString()
        {
            return ((IPhpConvertible)this).ToString();
        }

        string IPhpConvertible.ToString()
        {
            return Configuration.Application.Globalization.PageEncoding.GetString(this.ReadonlyData, 0, this.Length);
        }

        /// <summary>
        /// Converts instance to its string representation according to PHP conversion algorithm.
        /// </summary>
        /// <param name="success">Indicates whether conversion was successful.</param>
        /// <param name="throwOnError">Throw out 'Notice' when conversion wasn't successful?</param>
        /// <returns>The converted value.</returns>
        string IPhpConvertible.ToString(bool throwOnError, out bool success)
        {
            success = false;
            return ((IPhpConvertible)this).ToString();
        }

		#endregion

		#region IPhpPrintable Members

        /// <summary>
		/// Prints a content of this instance.
		/// </summary>
		/// <param name="output">The output text stream.</param>
		public void Print(System.IO.TextWriter output)
		{
			output.Write("\"");
            DumpTo(output);
            output.WriteLine("\"");            
		}

		/// <summary>
		/// Prints a content of this instance along with its type.
		/// </summary>
		/// <param name="output">The output text stream.</param>
		public void Dump(System.IO.TextWriter output)
		{
            output.Write(PhpTypeName + "[binary]({0}) ", this.Length);
            this.Print(output);
            //if (data.Length > 0)
            //    output.WriteLine(PhpTypeName + "[binary]({0}) \"\\x{1}\"", data.Length, StringUtils.BinToHex(data, "\\x"));
            //else
            //    output.WriteLine(PhpTypeName + "[binary](0) \"\"");
		}

		/// <summary>
		/// Prints a PHP declaration of a content of this instance.
		/// </summary>
		/// <param name="output">The output text stream.</param>
		public void Export(System.IO.TextWriter output)
		{
			output.Write("\"\\x{0}\"", StringUtils.BinToHex(this.ReadonlyData, "\\x"));

			if (PhpVariable.PrintIndentationLevel == 0)
				output.WriteLine();
		}

		#endregion

		#region IPhpCloneable Members

		/// <summary>
		/// Creates a lazy deep copy of this instance.
		/// </summary>
		/// <returns>A copy that shares the internal byte array with another <see cref="PhpBytes"/>.</returns>
		public object DeepCopy()
		{
			// duplicates data lazily:
			return new PhpBytes(this);
		}

		public object Copy(CopyReason reason)
		{
			return DeepCopy();
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
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj,comparer)"]/*'/>
		public int CompareTo(object obj, IComparer/*!*/ comparer)
		{
			Debug.Assert(comparer != null);

            // try to compare two PhpBytes instances
            if (obj != null && obj.GetType() == typeof(PhpBytes))
            {
                var other = (PhpBytes)obj;

                // if both PhpByte instances share the same internal byte array:
                if (this._data == other._data) return 0;

                if (object.ReferenceEquals(comparer, PhpComparer.Default) &&
                    !(StringUtils.IsConvertableToNumber(this.ReadonlyData) && StringUtils.IsConvertableToNumber(other.ReadonlyData)))
                {
                    // we don't have to convert bytes to string:
                    return ArrayUtils.Compare(this.ReadonlyData, other.ReadonlyData);
                }
                else
                {
                    // user comparers can handle this operation differently:
                    return comparer.Compare(((IPhpConvertible)this).ToString(), ((IPhpConvertible)other).ToString());
                }
            }

            // compare this as string with obj
            return comparer.Compare(((IPhpConvertible)this).ToString(), obj);
        }

		#endregion

		#region ICloneable Members

		/// <summary>
		/// Creates a shallow copy of this instance.
		/// </summary>
		/// <returns>A shallow copy.</returns>
		public object Clone()
		{
			return new PhpBytes((byte[])ReadonlyData.Clone());
		}

		#endregion

		#region IPhpVariable

		/// <summary>
		/// Defines emptiness on <see cref="PhpBytes"/>.
		/// </summary>
		/// <returns>Whether the inscance contains empty byte array or byte array containing the single zero byte.</returns>
		public bool IsEmpty()
		{
			int length = this.Length;
			return length == 0 || (length == 1 && _data[0] == (byte)'0');
		}

		/// <summary>
		/// Defines whether <see cref="PhpBytes"/> is a scalar.
		/// </summary>
		/// <returns><B>true</B></returns>
		public bool IsScalar()
		{
			return true;
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

		#region Concat

		/// <summary>
		/// Concats two strings of bytes.
		/// </summary>
		/// <param name="x">The first string of bytes to be concatenated. Cannot be <c>null</c>.</param>
        /// <param name="y">The second string of bytes to be concatenated. Cannot be <c>null</c>.</param>
		/// <returns>The concatenation of <paramref name="x"/> and <paramref name="y"/>.</returns>
		/// <remarks>
		/// Bytes are not encoded nor decoded from their respective encodings. 
		/// Instead, data are copied without any changes made and the result's encoding is set to the encoding 
		/// of the <paramref name="x"/>.</remarks>
        [Emitted]
		public static PhpBytes Concat(PhpBytes/*!*/x, PhpBytes/*!*/y)
		{
			if (x == null) throw new ArgumentNullException("x");
			if (y == null) throw new ArgumentNullException("y");

			int lx = x.Length;
			int ly = y.Length;

            byte[] result = new byte[lx + ly];
			
			Buffer.BlockCopy(x.ReadonlyData, 0, result, 0, lx);
            Buffer.BlockCopy(y.ReadonlyData, 0, result, lx, ly);

			return new PhpBytes(result);
		}

        /// <summary>
        /// Concatenates strings or strings of bytes optimized for concatenation with a PhpBytes.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// A concatenation of the <paramref name="x"/> and <paramref name="y"/> (no copy needed).
        /// </returns>
        [Emitted]
        public static PhpBytes Concat(PhpBytes x, object y)
        {
            Debug.Assert(!(y is PhpReference));

            return PhpBytes.Concat(x, Convert.ObjectToPhpBytes(y));
        }

        /// <summary>
        /// Concatenates strings or strings of bytes optimized for concatenation with a PhpBytes.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// A concatenation of the <paramref name="x"/> and <paramref name="y"/> (no copy needed).
        /// </returns>
        [Emitted]
        public static PhpBytes Concat(object x, PhpBytes y)
        {
            Debug.Assert(!(x is PhpReference));

            return PhpBytes.Concat(Convert.ObjectToPhpBytes(x), y);
        }

        /// <summary>
        /// Concatenate list of PhpBytes. Given array can contain nulls.
        /// </summary>
        /// <param name="args">List of PhpArray objects. Can contain null.</param>
        /// <returns>PhpBytes with concatenated args.</returns>
        public static PhpBytes Concat(params PhpBytes[]/*!*/args)
        {
            return Concat(args, 0, args.Length);
        }

        /// <summary>
        /// Concatenate list of PhpBytes. Given array can contain nulls.
        /// </summary>
        /// <param name="args">List of PhpArray objects. Can contain null.</param>
        /// <param name="startIndex">First element in args to start concatenation from.</param>
        /// <param name="count">Amount of element to concatenate from the startIndex index.</param>
        /// <returns>PhpBytes with concatenated args.</returns>
        public static PhpBytes Concat(PhpBytes[]/*!*/args, int startIndex, int count)
        {
            int num = startIndex + count;

            Debug.Assert(args != null);
            Debug.Assert(startIndex >= 0);
            Debug.Assert(count >= 0);
            Debug.Assert(num <= args.Length);

            // computes the length of the result:
            int length = 0;
            for (int i = startIndex; i < num; ++i)
                if (args[i] != null)
                    length += args[i].Length;

            if (length == 0)
                return PhpBytes.Empty;

            var result = new byte[length];

            // copies data to the result array:
            int pos = 0;
            for (int i = startIndex; i < num; ++i)
                if (args[i] != null)
                {
                    byte[] bytes = args[i].ReadonlyData;
                    Buffer.BlockCopy(bytes, 0, result, pos, bytes.Length);
                    pos += bytes.Length;
                }

            return new PhpBytes(result);
        }

		#endregion

        #region Append

        /// <summary>
        /// Concatenates two strings or strings of bytes optimized for concatenation with a string.
        /// </summary>
        /// <param name="x">The first operand.</param>
        /// <param name="y">The second operand.</param>
        /// <returns>
        /// The single-referenced concatenation of the <paramref name="x"/> and <paramref name="y"/> (no copy needed).
        /// </returns>
        [Emitted]
        public static PhpBytes Append(object x, PhpBytes y)
        {
            Debug.Assert(!(x is PhpReference));

            return PhpBytes.Concat(Convert.ObjectToPhpBytes(x), y);
        }

        #endregion

        #region IPhpObjectGraphNode Members

        /// <summary>
		/// Walks the object graph rooted in this node.
		/// </summary>
		/// <param name="callback">The callback method.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		public void Walk(PHP.Core.PhpWalkCallback callback, ScriptContext context)
		{ }

		#endregion

		#region GetHashCode, Equals

		public override int GetHashCode()
		{
			int result = -218974311;

			for (int i = 0; i < this.Length; i++)
				result = unchecked((result << 1) ^ this._data[i]);

			return result;
		}

		public override bool Equals(object obj)
		{
            if (ReferenceEquals(obj, this))
                return true;

            if (obj != null && obj.GetType() == typeof(PhpBytes))
                return Equals((PhpBytes)obj);
            else
                return false;
		}

        public bool Equals(PhpBytes/*!*/other)
        {
            Debug.Assert(other != null);

            return
                this._data == other._data ||    // compare internal data structures if they are shared first
                (
                    this._data.Length == other._data.Length &&  // arrays have to be the same length
                    ArrayUtils.Compare(this.ReadonlyData, other.ReadonlyData) == 0 // compare byte by byte
                );
        }

		#endregion
	}
}
