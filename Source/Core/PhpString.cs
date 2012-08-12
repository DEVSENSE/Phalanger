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
using PHP.Core.Reflection;
using System.Runtime.Serialization;

namespace PHP.Core
{
	/// <summary>
	/// String representation that uses <see cref="StringBuilder"/> internally to improve
    /// performance of modifications such as Append, Prepend and singe character change.
	/// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DebuggerDisplay("{ToString()}", Type = PhpString.PhpTypeName)]
    public sealed class PhpString : IPhpVariable, IPhpObjectGraphNode, IComparable, ISerializable
	{
        /// <summary>
        /// PhpStrings PHP type name (string).
        /// </summary>
		public const string PhpTypeName = PhpVariable.TypeNameString;

		/// <summary>
		/// Copy-on-write aware string builder.
		/// </summary>
		private CowStringBuilder/*!*/cow;

        /// <summary>
        /// Internal <see cref="StringBuilder"/> containing string data. Note: Can be shared.
        /// </summary>
        internal StringBuilder/*!*/StringBuilder { get { return this.cow.Builder; } }

		#region Nested Class: CowStringBuilder

        /// <summary>
        /// StringBuilder that can be marked both as read only (shared, immutable) or writable.
        /// </summary>
		private sealed class CowStringBuilder
        {
            #region Fields & Properties

            /// <summary>
            /// <see cref="StringBuilder"/> containing the string (Unicode) data.
            /// </summary>
            public StringBuilder/*!*/Builder { get { return _builder; } }
			private readonly StringBuilder/*!*/_builder;

            /// <summary>
            /// True iff the internal data structure is shared and should not be modified.
            /// </summary>
            public bool IsShared
            {
                get
                {
                    return _refCount > 1;
                }
            }

            /// <summary>
            /// Keep track of "reference count". Only increased when copied, and decreased when shared instance is modified.
            /// Sometimes it really avoids copying.
            /// </summary>
            private int _refCount = 1;

            #endregion

            #region Contstruction

            /// <summary>
            /// Initialize the instance with string value.
            /// </summary>
            /// <param name="str">String value.</param>
			public CowStringBuilder(string str)
			{
                _builder = new StringBuilder(str);
			}

            /// <summary>
            /// Initialize the instance with string and expected capacity.
            /// </summary>
            /// <param name="str">String value.</param>
            /// <param name="capacity">Expected capacity.</param>
			public CowStringBuilder(string str, int capacity)
			{
                _builder = new StringBuilder(str, capacity);
			}

            /// <summary>
            /// Initialize the instance with twi string values that will be concatenated.
            /// </summary>
            /// <param name="str1"></param>
            /// <param name="str2"></param>
			public CowStringBuilder(string str1, string str2)
			{
				if (str1 == null)
				{
                    _builder = new StringBuilder(str2);
				}
				else if (str2 == null)
				{
                    _builder = new StringBuilder(str1);
				}
				else
				{
                    _builder = new StringBuilder(str1, str1.Length + str2.Length);
                    _builder.Append(str2);
				}
            }

            #endregion

            #region Share, Unshare

            /// <summary>
            /// Mark this instance as shared (read only, immutable).
            /// </summary>
            /// <returns></returns>
            internal CowStringBuilder/*!*/Share()
            {
                ++_refCount;
                return this;
            }

            /// <summary>
            /// Get back shared instance of <see cref="CowStringBuilder"/>.
            /// </summary>
            internal void Unshare()
            {
                --_refCount;
            }

            #endregion
        }

		#endregion

		#region Construction

		/// <summary>
		/// Lazy copy construction.
		/// </summary>
		/// <param name="phps"></param>
		private PhpString(PhpString phps)
		{
			this.cow = phps.cow.Share();
		}

        /// <summary>
        /// Initialize PhpString with string value.
        /// </summary>
        /// <param name="str">String value.</param>
		public PhpString(string str)
		{
			this.cow = new CowStringBuilder(str);
		}

        /// <summary>
        /// Initialize PhpString with two string values that will be concatenated.
        /// </summary>
        /// <param name="str1">First string value.</param>
        /// <param name="str2">Second string value.</param>
		public PhpString(string str1, string str2)
		{
			this.cow = new CowStringBuilder(str1, str2);
		}

		#endregion

		#region IPhpVariable Members

		public bool IsEmpty()
		{
            int length = this.Length;
            return length == 0 || (length == 1 && cow.Builder[0] == '0');
		}

		public bool IsScalar()
		{
			return true;
		}

		public string GetTypeName()
		{
			return PhpTypeName;
		}

		#endregion

		#region IPhpConvertible Members

		public PHP.Core.PhpTypeCode GetTypeCode()
		{
			return PhpTypeCode.PhpString;
		}

		public double ToDouble()
		{
			return Convert.StringToDouble(cow.Builder.ToString());
		}

		public int ToInteger()
		{
			return Convert.StringToInteger(cow.Builder.ToString());
		}

		public long ToLongInteger()
		{
			return Convert.StringToLongInteger(cow.Builder.ToString());
		}

		public bool ToBoolean()
		{
            int length = this.Length;
			return length != 0 && (length != 1 || cow.Builder[0] != 0);
		}

		public PhpBytes ToPhpBytes()
		{
			return new PhpBytes(cow.Builder.ToString());
		}

		public Convert.NumberInfo ToNumber(out int intValue, out long longValue, out double doubleValue)
		{
			return Convert.StringToNumber(cow.Builder.ToString(), out intValue, out longValue, out doubleValue);
		}

		public override string ToString()
		{
			return cow.Builder.ToString();
		}

		string IPhpConvertible.ToString()
		{
			return ToString();
		}

        /// <summary>
		/// Converts instance to its string representation according to PHP conversion algorithm.
		/// </summary>
		/// <param name="success">Indicates whether conversion was successful.</param>
		/// <param name="throwOnError">Throw out 'Notice' when conversion wasn't successful?</param>
		/// <returns>The converted value.</returns>
		string IPhpConvertible.ToString(bool throwOnError, out bool success)
		{
			success = true;
			return ToString();
		}

		#endregion

		#region IPhpPrintable Members

		public void Print(System.IO.TextWriter output)
		{
			PhpVariable.Print(output, cow.Builder.ToString());
		}

		public void Dump(System.IO.TextWriter output)
		{
			PhpVariable.Dump(output, cow.Builder.ToString());
		}

		public void Export(System.IO.TextWriter output)
		{
			PhpVariable.Export(output, cow.Builder.ToString());
		}

		#endregion

		#region IPhpCloneable Members

		public object DeepCopy()
		{
			return new PhpString(this);
		}

		public object Copy(PHP.Core.CopyReason reason)
		{
			return new PhpString(this);
		}

		public PhpString Copy()
		{
			return new PhpString(this);
		}

		#endregion

		#region IPhpComparable Members

		public int CompareTo(object obj)
		{
			return CompareTo(obj, PhpComparer.Default);
		}

		public int CompareTo(object obj, IComparer/*!*/ comparer)
		{
			Debug.Assert(comparer != null);

            // compare internal structures if possible
            PhpString phps;
            if ((phps = obj as PhpString) != null)
            {
                if (object.ReferenceEquals(this.cow, phps.cow))
                    return 0;

                // as we know the second operand is PhpString, compare as strings directly
                return comparer.Compare(this.cow.Builder.ToString(), phps.cow.Builder.ToString());
            }

            // compare as strings
            return comparer.Compare(cow.Builder.ToString(), obj);
		}

		#endregion

		#region Read Operations

		public int Length
		{
			get
			{
				return cow.Builder.Length;
			}
		}

		internal char GetCharUnchecked(int index)
		{
			Debug.Assert(index >= 0 && index < cow.Builder.Length);
			return cow.Builder[index];
		}

		#endregion

		#region Write Operations

		public PhpString/*!*/ Append(string str)
		{
            if (cow.IsShared)
            {
                cow.Unshare();
            	cow = new CowStringBuilder(cow.Builder.ToString(), str);
			}
			else
			{
				cow.Builder.Append(str);
			}

			return this;
		}

		public PhpString/*!*/ Append(char c)
		{
            if (cow.IsShared)
            {
                cow.Unshare();
				cow = new CowStringBuilder(cow.Builder.ToString(), cow.Builder.Length + 1);
			}

			cow.Builder.Append(c);

			return this;
		}

		public PhpString/*!*/ Append(char c, int count)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException("count");

            if (count > 0)
            {
                if (cow.IsShared)
                {
                    cow.Unshare();
                    cow = new CowStringBuilder(cow.Builder.ToString(), cow.Builder.Length + count);
                }

                cow.Builder.Append(c, count);
            }

			return this;
		}

		public PhpString/*!*/ Prepend(string str)
		{
            if (cow.IsShared)
            {
                cow.Unshare();
				cow = new CowStringBuilder(str, cow.Builder.ToString());
			}
			else
			{
				cow.Builder.Insert(0, str);
			}

			return this;
		}

		internal void SetCharUnchecked(int index, char value)
		{
            Debug.Assert(index >= 0 && index < this.Length);

            if (cow.IsShared)
            {
                cow.Unshare();
				cow = new CowStringBuilder(cow.Builder.ToString());
			}

			cow.Builder[index] = value;
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

        public override bool Equals(object obj)
        {
            if (obj != null && obj.GetType() == typeof(PhpString))
                return this.cow.Builder.Equals(((PhpString)obj).cow.Builder);
            return false;
        }
        public override int GetHashCode()
        {
            return this.cow.Builder.Length;
        }

        #region ISerializable (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Handles serialization and deserialization of <see cref="PhpString"/>.
        /// </summary>
        /// <remarks>Deserialization converts this object into <see cref="string"/>.</remarks>
        [Serializable]
        private class SerializationHelper : ISerializable, IDeserializationCallback, IObjectReference
        {
            /// <summary>
            /// Name of value field within <see cref="SerializationInfo"/> containing serialized string.
            /// </summary>
            private const string InfoValueName = "Value";

            /// <summary>
            /// Deserialized string value.
            /// </summary>
            private readonly string value;

            /// <summary>
            /// Beginning of the deserialization.
            /// </summary>
            /// <param name="info"></param>
            /// <param name="context"></param>
            private SerializationHelper(SerializationInfo/*!*/info, StreamingContext context)
            {
                this.value = (string)info.GetValue(InfoValueName, typeof(string));
            }

            [System.Security.SecurityCritical]
            internal static void GetObjectData(PhpString/*!*/instance, SerializationInfo info, StreamingContext context)
            {
                Debug.Assert(instance != null);
                Debug.Assert(info != null);

                info.SetType(typeof(SerializationHelper));
                info.AddValue(InfoValueName, instance.ToString());
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                // should never be called
                throw new InvalidOperationException();
            }

            public object GetRealObject(StreamingContext context)
            {
                return this.value;
            }

            public virtual void OnDeserialization(object sender)
            {
                
            }
        }

        [System.Security.SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            SerializationHelper.GetObjectData(this, info, context);
        }

#endif
        #endregion
    }

	#region Array Proxy

	/// <summary>
	/// Created by ensuring operators (i.e. when a chain is written) if the ensured value is a non-empty string.
	/// Holds a string container (<see cref="PhpString"/> or <see cref="PhpBytes"/>).
	/// The operator immediately following the ensuring operator either raises an error if it is an ensuring operator
	/// or modifies byte/character in the string if it is a <see cref="PhpArray.SetArrayItem"/> operator.
	/// </summary>
	internal sealed class PhpArrayString : PhpArray // TODO: Bytes/String
	{
		internal PhpString String { get { return (PhpString)obj; } }
		internal PhpBytes Bytes { get { return (PhpBytes)obj; } }
		internal object Object { get { return obj; } }

		readonly private object obj/*!*/;

		internal PhpArrayString(object obj)
		{
			Debug.Assert(obj is PhpString || obj is PhpBytes);
			this.obj = obj;
		}
		
		#region Operators

        protected override object GetArrayItemOverride(object key, bool quiet)
		{
			Debug.Fail("N/A: written chains only");
			throw null;
		}

        protected override PhpReference/*!*/ GetArrayItemRefOverride()
		{
			PhpException.VariableMisusedAsArray(obj, true);
			return new PhpReference();
		}

        protected override PhpReference/*!*/ GetArrayItemRefOverride(object key)
		{
			PhpException.VariableMisusedAsArray(obj, true);
			return new PhpReference();
		}

        protected override PhpReference/*!*/ GetArrayItemRefOverride(int key)
		{
			PhpException.VariableMisusedAsArray(obj, true);
			return new PhpReference();
		}

        protected override PhpReference/*!*/ GetArrayItemRefOverride(string key)
		{
			PhpException.VariableMisusedAsArray(obj, true);
			return new PhpReference();
		}

        protected override void SetArrayItemOverride(object value)
        {
            PhpException.VariableMisusedAsArray(obj, false);
        }

        protected override void SetArrayItemOverride(object key, object value)
        {
            int index;
            if (Operators.CheckStringIndexRange(index = Convert.ObjectToInteger(key), Int32.MaxValue, false))
            {
                if (obj.GetType() == typeof(PhpString))
                    Operators.SetStringItem((PhpString)obj, index, value);
                else
                    Operators.SetBytesItem((PhpBytes)obj, index, value);
            }
        }

        protected override void SetArrayItemOverride(int key, object value)
        {
            PhpException.VariableMisusedAsArray(obj, true);
        }

        protected override void SetArrayItemOverride(string key, object value)
        {
            PhpException.VariableMisusedAsArray(obj, true);
        }

        protected override void SetArrayItemRefOverride(object key, PhpReference value)
		{
			PhpException.VariableMisusedAsArray(obj, true);
		}

        protected override PhpArray EnsureItemIsArrayOverride(object key)
		{
			// error (postponed error, which cannot be reported by the previous operator):  
			PhpException.VariableMisusedAsArray(obj, false);
			return null;
		}

        protected override PhpArray EnsureItemIsArrayOverride()
		{
			PhpException.VariableMisusedAsArray(obj, false);
			return null;
		}

        protected override DObject EnsureItemIsObjectOverride(object key, ScriptContext/*!*/ context)
		{
			// error (postponed error, which cannot be reported by the previous operator):  
			PhpException.VariableMisusedAsObject(obj, false);
			return null;
		}

        protected override DObject EnsureItemIsObjectOverride(ScriptContext/*!*/ context)
		{
			PhpException.VariableMisusedAsObject(obj, false);
			return null;
		}
		
		#endregion
	}

	#endregion
}
