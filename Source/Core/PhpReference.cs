/*

 Copyright (c) 2004-2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;

#if SILVERLIGHT
using PHP.CoreCLR;
#endif

namespace PHP.Core
{
    /// <summary>
	/// Represents a PHP reference.
	/// </summary>
	[Serializable]
    [DebuggerDisplay("&{this.Value}", Type = "&{PHP.Core.PhpVariable.GetTypeName(this.Value),nq}")]
    [DebuggerNonUserCode]
	public class PhpReference : IPhpVariable, ICloneable, IPhpObjectGraphNode
	{
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public const string PhpTypeName = "reference";

		#region Fields

		/// <summary>
		/// Referenced object.
		/// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public object Value
		{
			[Emitted]
			get { return value; }
			[Emitted]
			set { Debug.Assert(!(value is PhpReference)); this.value = value; }
		}

		/// <summary>
		/// Referenced object. For internal use only. Use property <see cref="Value"/> to access the value.
		/// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
		[Emitted]
		public object value; // an address is required to be taken

		#endregion

		#region Construction

		[Emitted]
		public PhpReference()
		{
			this.value = null;
		}

		[Emitted]
		public PhpReference(object value)
		{
			Debug.Assert(!(value is PhpReference));
			this.value = value;
		}

		internal PhpReference(object value, bool supressDoubleRefCheck)
		{
			Debug.Assert(supressDoubleRefCheck || !(value is PhpReference));
			this.value = value;
		}

		#endregion

		#region IsAliased, IsSet

		/// <summary>
		/// Returns <B>true</B>. Overriden in <see cref="PhpSmartReference"/>.
		/// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public virtual bool IsAliased
		{
			get { return true; }
			[Emitted]
			set { Debug.Assert(value); }
		}

		/// <summary>
		/// Returns <B>true</B>. Overriden in <see cref="PhpSmartReference"/>.
		/// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public virtual bool IsSet
		{
			[Emitted]
			get { return true; }
			[Emitted]
			set { Debug.Assert(value); }
		}

		#endregion

		#region IPhpConvertible Members

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="GetTypeCode"]/*' />
		public PHP.Core.PhpTypeCode GetTypeCode()
		{
			return PhpTypeCode.PhpReference;
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToDouble"]/*' />
		public double ToDouble()
		{
			return Convert.ObjectToDouble(value);
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToInteger"]/*' />
		public int ToInteger()
		{
			return Convert.ObjectToInteger(value);
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToInteger"]/*' />
		public long ToLongInteger()
		{
			return Convert.ObjectToLongInteger(value);
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToBoolean"]/*' />
		public bool ToBoolean()
		{
			return Convert.ObjectToBoolean(value);
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToPhpBytes"]/*' />
		public PhpBytes ToPhpBytes()
		{
			return Convert.ObjectToPhpBytes(value);
		}

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToNumber"]/*' />
		public Convert.NumberInfo ToNumber(out int intValue, out long longValue, out double doubleValue)
		{
			return Convert.ObjectToNumber(value, out intValue, out longValue, out doubleValue);
		}

		string IPhpConvertible.ToString()
		{
            return Convert.ObjectToString(value);
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

		public override string ToString()
		{
			return "&" + ((value != null) ? value.ToString() : PhpVariable.TypeNameNull);
		}

		#endregion

		#region IPhpPrintable Members

		/*
		  NOTES:
		  - We don't print an ampersand nor other text before a referenced value because 
			PHP doesn't do so and, most importantly, a compiler uses variables of type PhpReferences 
			e.g. for global variables which may never be used as a reference by a user 
			(via =& operator). If references were not transparent user may be confused by 
			marking them by an ampersand.
		  - Hence references are marked by ampersand by PhpArrays and PhpObjects on their own.
		*/

		/// <summary>
		/// Prints a value which is referenced by this instance.
		/// </summary>
		/// <param name="output">The output where the value is printed.</param>
		public void Print(System.IO.TextWriter output)
		{
         	PhpVariable.Print(output, value);
		}

		/// <summary>
		/// Dumps a value which is referenced by this instance.
		/// </summary>
		/// <param name="output">The output where the value is printed.</param>
		public void Dump(System.IO.TextWriter output)
		{
            output.Write("&");
			PhpVariable.Dump(output, value);
		}

		/// <summary>
		/// Exports a value which is referenced by this instance.
		/// </summary>
		/// <param name="output">The output where the value is printed.</param>
		public void Export(System.IO.TextWriter output)
		{
			PhpVariable.Export(output, value);
		}

		#endregion

		#region IPhpCloneable Members

		/// <summary>
		/// Retrieves a copy of this instance. 
		/// </summary>
		/// <returns>This instance.</returns>
		/// <remarks>
		/// Actually, references are not copied and this instance is returned instead.
		/// That is because deep-copying stops on references in PHP.
		/// </remarks>
		public virtual object DeepCopy()
		{
			return this;
		}

		/// <summary>
		/// Retrieves a copy of this instance. 
		/// </summary>
		/// <returns>This instance.</returns>
		/// <remarks>
		/// Actually references are not copied and this instance is returned instead.
		/// </remarks>
		public virtual object Copy(CopyReason reason)
		{
			return this;
		}

		#endregion

		#region ICloneable Members

		public object Clone()
		{
			ICloneable cloneable = value as ICloneable;
			return new PhpReference((cloneable != null) ? cloneable.Clone() : value);
		}

		#endregion

		#region IPhpComparable Members

		/// <summary>
		/// Compares a referenced object with an object of arbitrary PHP.NET type.
		/// </summary>
		/// <remarks>
		/// Compares a referenced object with <paramref name="obj"/> and returns the result.
		/// </remarks>
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj,comparer)"]/*'/>
		public int CompareTo(object obj, System.Collections.IComparer comparer)
		{
            return comparer.Compare(value, obj);
		}

		#endregion

		#region IComparable Members

		/// <summary>
		/// Compares this instance with an object of arbitrary PHP.NET type.
		/// </summary>
		/// <remarks>
		/// Compares a referenced object with <paramref name="obj"/> and returns the result.
		/// </remarks>
		/// <include file='Doc/Common.xml' path='docs/method[@name="CompareTo(obj)"]/*'/>
		public int CompareTo(object obj)
		{
			return CompareTo(obj, PhpComparer.Default);
		}

		#endregion

		#region IPhpVariable Members

		/// <summary>
		/// Whether this instance is empty.
		/// </summary>
		/// <returns>Whether a referenced object is empty.</returns>
		public bool IsEmpty()
		{
			return PhpVariable.IsEmpty(value);
		}

		/// <summary>
		/// Defines whether <see cref="PhpReference"/> is a scalar.
		/// </summary>
		/// <returns>Whether a referenced object is a scalar.</returns>
		public bool IsScalar()
		{
			return PhpVariable.IsScalar(value);
		}

		/// <summary>
		/// Returns a name of declaring type.
		/// </summary>
		/// <returns>The name.</returns>
		public string GetTypeName()
		{
			return PhpVariable.GetTypeName(value);
		}

		#endregion

		#region IPhpObjectGraphNode Members

		/// <summary>
		/// Walks the object graph rooted in this node.
		/// </summary>
		/// <param name="callback">The callback method.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>.</param>
		public void Walk(PhpWalkCallback callback, ScriptContext context)
		{
			IPhpObjectGraphNode node = value as IPhpObjectGraphNode;
			if (node != null)
			{
				value = callback(node, context);
				node = value as IPhpObjectGraphNode;

				if (node != null) node.Walk(callback, context);
			}
		}

		#endregion

		#region Helpers

        public static T AsType<T>(PhpReference variable) where T : class
        {
            return (variable != null) ? variable.Value as T : null;
        }

        public static PhpArray AsPhpArray(PhpReference variable)
        {
            return AsType<PhpArray>(variable);
        }

		public static void SetValue(ref PhpReference variable, object value)
		{
			if (variable == null)
				variable = new PhpReference(value);
			else
				variable.Value = value;
		}

		#endregion
	}

	/// <summary>
	/// Represents a PHP reference that remembers whether it is pointed to by more than one location
	/// (i.e. whether it really is a reference).
	/// </summary>
	[Serializable, DebuggerNonUserCodeAttribute]
	public sealed class PhpSmartReference : PhpReference
	{
		#region Fields

		[Flags]
		private enum ReferenceFlags : byte
		{
			/// <summary>
			/// <B>true</B> if this <see cref="PhpSmartReference"/> is referenced from more than one location,
			/// <B>false</B> otherwise.
			/// </summary>
			IsAliased = 1,

			/// <summary>
			/// <B>true</B> if the <see cref="PhpReference.value"/> of this <see cref="PhpSmartReference"/> is set,
			/// <B>false</B> otherwise.
			/// </summary>
			IsSet = 2
		}

		/// <summary>
		/// Contains the <see cref="ReferenceFlags.IsAliased"/> and <see cref="ReferenceFlags.IsSet"/> flags.
		/// </summary>
		private ReferenceFlags flags;

		#endregion

		#region Construction

		public PhpSmartReference()
			: base()
		{ flags = ReferenceFlags.IsSet; }

		public PhpSmartReference(object value)
			: base(value)
		{ flags = ReferenceFlags.IsSet; }

		#endregion

		#region IsAliased, IsSet

		/// <summary>
		/// <B>true</B> if this <see cref="PhpSmartReference"/> is referenced from more than one location,
		/// <B>false</B> otherwise.
		/// </summary>
		public override bool IsAliased
		{
			get { return ((flags & ReferenceFlags.IsAliased) != 0); }
			set { if (value) flags |= ReferenceFlags.IsAliased; else flags &= ~ReferenceFlags.IsAliased; }
		}

		/// <summary>
		/// <B>true</B> if the <see cref="PhpReference.value"/> of this <see cref="PhpSmartReference"/> is set,
		/// <B>false</B> otherwise.
		/// </summary>
		public override bool IsSet
		{
			get { return ((flags & ReferenceFlags.IsSet) != 0); }
			set { if (value) flags |= ReferenceFlags.IsSet; else flags &= ~ReferenceFlags.IsSet; }
		}

		#endregion

		#region IPhpCloneable Overriden Members

		/// <summary>
		/// Retrieves a deep copy of this instance. 
		/// </summary>
		/// <returns>The copy.</returns>
		/// <remarks>
		/// If this <see cref="PhpSmartReference"/> <see cref="IsAliased"/>, this instance is returned without copying.
		/// That is because deep copying stops on references in PHP. If this instance's <see cref="IsAliased"/> is
		/// <B>false</B>, a new <see cref="PhpSmartReference"/> referencing a deep copy of the current value is returned.
		/// </remarks>
		public override object DeepCopy()
		{
            return IsAliased ? this : new PhpSmartReference(PhpVariable.DeepCopy(value));
		}

		/// <summary>
		/// Retrieves a copy of this instance. 
		/// </summary>
		/// <returns>The copy.</returns>
		/// <remarks>
		/// If this <see cref="PhpSmartReference"/> <see cref="IsAliased"/>, this instance is returned without copying.
		/// That is because (deep) copying stops on references in PHP. If this instance is not <see cref="IsAliased"/>,
		/// a new <see cref="PhpSmartReference"/> referencing a copy of the current value is returned.
		/// </remarks>
		public override object Copy(CopyReason reason)
		{
            return IsAliased ? this : new PhpSmartReference(PhpVariable.Copy(value, reason));
		}

		#endregion
	}
}
