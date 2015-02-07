/*

 Copyright (c) 2004-2006 Tomas Matousek and Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

using PHP.Library;
using PHP.Core;
using PHP.Core.Reflection;

#if SILVERLIGHT
using MathEx = PHP.CoreCLR.MathEx;
#else
using MathEx = System.Math;
#endif

namespace PHP.Core
{
	#region Interfaces

	/// <summary>
	/// Interface provides methods for conversion between PHP.NET types.
	/// </summary>
	public interface IPhpConvertible
	{
		/// <include file='Doc/Conversions.xml' path='docs/method[@name="GetTypeCode"]/*' />
		PhpTypeCode GetTypeCode();

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToDouble"]/*' />
		double ToDouble();

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToInteger"]/*' />
		int ToInteger();

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToLongInteger"]/*' />
		long ToLongInteger();

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToBoolean"]/*' />
		bool ToBoolean();

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToPhpBytes"]/*' />
		PhpBytes ToPhpBytes();

		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ToNumber"]/*' />
		Convert.NumberInfo ToNumber(out int intValue, out long longValue, out double doubleValue);

		/// <summary>
		/// Converts instance to its string representation according to PHP conversion algorithm.
		/// </summary>
		/// <returns>The converted value.</returns>
		string ToString();

        /// <summary>
		/// Converts instance to its string representation according to PHP conversion algorithm.
		/// </summary>
		/// <param name="success">Indicates whether conversion was successful.</param>
		/// <param name="throwOnError">Throw out 'Notice' when conversion wasn't successful?</param>
		/// <returns>The converted value.</returns>
		string ToString(bool throwOnError, out bool success);
	}

	#endregion

	/// <summary>
	/// Implements PHP conversions of arbitrary PHP.NET type to Framework types used as PHP.NET types.
	/// These are int, double, boolean and string.
	/// </summary>
	[DebuggerNonUserCode]
	public static class Convert
	{
		#region ClrLiteralToPhpLiteral

        /// <summary>
        /// Converts the CLR literal object into PHP compatible representation.
        /// </summary>
        /// <param name="value">The literal to be converted.</param>
        /// <returns>PHP (Phalanger) representation of the CLR literal.</returns>
		internal static object ClrLiteralToPhpLiteral(object value)
		{
			if (value is int || value is string || value == null || value is bool || value is double || value is long)
				return value;

            if (value is Enum) return ClrEnumToPhpLiteral(value);
			if (value is sbyte) return (int)(sbyte)value;
			if (value is byte) return (int)(byte)value;
			if (value is short) return (int)(short)value;
			if (value is ushort) return (int)(ushort)value;
			if (value is char) return ((char)value).ToString();
			if (value is float) return (double)(float)value;

			if (value is uint)
			{
				uint uint_value = (uint)value;

				if (uint_value <= Int32.MaxValue)
					return (int)uint_value;
				else
					return (long)uint_value;
			}

			if (value is ulong)
			{
				ulong ulong_value = (ulong)value;

				if (ulong_value <= Int32.MaxValue)
					return (int)ulong_value;
				else
					return unchecked((long)ulong_value);
			}

			if (value is decimal)
			{
				decimal decimal_value = (decimal)value;
				if (decimal_value >= Int32.MinValue && decimal_value <= Int32.MaxValue)
					return Decimal.ToInt32(decimal_value);
				else if (decimal_value >= Int64.MinValue && decimal_value <= Int64.MaxValue)
					return Decimal.ToInt64(decimal_value);
				else
					return decimal_value.ToString();
			}

			Debug.Fail("Invalid literal type");
			throw null;
		}

        /// <summary>
        /// Converts System.Enum to proper PHP (Phalanger) literal object.
        /// </summary>
        /// <param name="value">Enum to be converted. Cannot be null, must be Enum.</param>
        /// <returns>Int32 or Int64 representation of the value.</returns>
        /// <remarks>The Enum is passed as object for easier manipulation inside the method.</remarks>
        internal static object ClrEnumToPhpLiteral(object/*!*/value)
        {
            Debug.Assert(value is Enum, "value is expected to be Enum and not null!");

            Type underlyingType = Enum.GetUnderlyingType(value.GetType());

            if (underlyingType == typeof(int))
                return (int)value;
            
            if (underlyingType == typeof(sbyte))
                return (int)(sbyte)value;
            
            if (underlyingType == typeof(short))
                return (int)(short)value;
            
            if (underlyingType == typeof(long))
                return (long)value;
            
            if (underlyingType == typeof(uint))
            {
                uint uint_value = (uint)value;

                if (uint_value <= Int32.MaxValue)
                    return (int)uint_value;
                else
                    return (long)uint_value;
            }
            
            if (underlyingType == typeof(byte))
                return (int)(byte)value;
            
            if (underlyingType == typeof(ushort))
                return (int)(ushort)value;
            
            if (underlyingType == typeof(ulong))
            {
                ulong ulong_value = (ulong)value;

                if (ulong_value <= Int32.MaxValue)
                    return (int)ulong_value;
                else
                    return unchecked((long)ulong_value);
            }

            Debug.Fail("Invalid literal type");
            throw null;
        }

		#endregion

		#region ObjectToXxxx

		/// <summary>
		/// Converts value of an arbitrary PHP/CLR type into Unicode character.
		/// </summary>
		/// <param name="obj">The value to convert.</param>
		/// <returns>The converted value.</returns>
		/// <exception cref="PhpException"><paramref name="obj"/> doesn't consist of a single character. (Warning)</exception>
		[Emitted]
		public static char ObjectToChar(object obj)
		{
			// we can simply convert the value to the string since we can expect
			// that the value is one character long, other lengths are errors anyway
			// so no optimization is necessary
			return StringToChar(ObjectToString(obj));
		}

		/// <summary>
		/// Converts value of an arbitrary PHP/CLR type into string value using the same 
		/// conversion algorithms as PHP.
		/// </summary>
		/// <param name="obj">The value to convert.</param>
		/// <returns>The converted value.</returns>
		/// <remarks>If <paramref name="obj"/> is null then the <see cref="String.Empty"/> is returned. This method cannot return null.</remarks>
		[Emitted]
		public static string ObjectToString(object obj)
		{
			if (ReferenceEquals(obj, null)) return String.Empty;

			bool success;
			string result = TryObjectToString(obj, out success);

			if (!success)
			{
				// PhpReference, PhpArray, PhpResource, DObject:
				IPhpConvertible conv = obj as IPhpConvertible;
				if (conv != null) return conv.ToString();
			}
			return result;
		}

		/// <summary>
		/// Converts value of an arbitrary PHP/CLR type into <see cref="PhpBytes"/> value using the same 
		/// conversion algorithms as PHP when converting to string.
		/// </summary>
		/// <param name="obj">The value to convert.</param>
		/// <returns>The converted value.</returns>
		/// <remarks>If <paramref name="obj"/> is a <B>null</B> then an empty bytes are returned.</remarks>
		[Emitted]
		public static PhpBytes ObjectToPhpBytes(object obj)
		{
            if (ReferenceEquals(obj, null)) return PhpBytes.Empty;

			string s;

            if (obj is PhpBytes)
            {
                return (PhpBytes)obj;
            }
			else if ((s = obj as string) != null)
			{
				return new PhpBytes(s);
			}
			else if (obj is int)
			{
				return new PhpBytes(((int)obj).ToString());
			}
			else if (obj is long)
			{
				return new PhpBytes(((long)obj).ToString());
			}
			else if (obj is double)
			{
				// this is not exactly the same behavior as in PHP, but it's very close: 
				return new PhpBytes(DoubleToString((double)obj));
			}
			else if (obj is bool)
			{
				return (bool)obj ? new PhpBytes(1) : PhpBytes.Empty;
			}

			// PhpReference, PhpArray, PhpResource, PhpObject, PhpBytes, null:
			IPhpConvertible php_conv = obj as IPhpConvertible;
			return (php_conv == null) ? PhpBytes.Empty : php_conv.ToPhpBytes();
		}

		/// <summary>
		/// Converts value of an arbitrary PHP/CLR type into boolean value using the same 
		/// conversion algorithms as PHP.
		/// </summary>
		/// <param name="obj">The value to convert.</param>
		/// <returns>The converted value.</returns>
		[Emitted]
		public static bool ObjectToBoolean(object obj)
		{
			string s;

			if (obj is bool) return (bool)obj;
			if (obj is int) return (int)obj != 0;
			if (obj is double) return (double)obj != 0.0;
			if (obj is long) return (long)obj != 0;
			if ((s = obj as string) != null) return StringToBoolean(s);

			// PhpReference, PhpArray, PhpResource, DObject, PhpBytes:
			IPhpConvertible conv = obj as IPhpConvertible;
			if (conv != null) return conv.ToBoolean();

			return false;
		}

		/// <summary>
		/// Converts value of an arbitrary PHP/CLR type into integer value using the same 
		/// conversion algorithms as PHP.
		/// </summary>
		/// <param name="obj">The value to convert.</param>
		/// <returns>The converted value.</returns>
		[Emitted]
		public static int ObjectToInteger(object obj)
		{
			string s;

			if (obj is int) return (int)obj;
			if (obj is bool) return (bool)obj ? 1 : 0;
			if (obj is long) return unchecked((int)(long)obj);
			if (obj is double) return unchecked((int)(double)obj);
			if ((s = obj as string) != null) return StringToInteger(s);

			// PhpString, PhpReference, PhpArray, PhpResource, DObject, PhpBytes:
			IPhpConvertible conv = obj as IPhpConvertible;
			if (conv != null) return conv.ToInteger();

			return 0;
		}

		/// <summary>
		/// Converts value of an arbitrary PHP/CLR type into long integer value using the same 
		/// conversion algorithms as PHP.
		/// </summary>
		/// <param name="obj">The value to convert.</param>
		/// <returns>The converted value.</returns>
		[Emitted]
		public static long ObjectToLongInteger(object obj)
		{
			string s;

			if (obj is long) return (long)obj;
			if (obj is int) return (int)obj;
			if (obj is bool) return (long)((bool)obj ? 1 : 0);
			if (obj is double) return unchecked((long)(double)obj);
			if ((s = obj as string) != null) return StringToLongInteger(s);

			// PhpString, PhpReference, PhpArray, PhpResource, DObject, PhpBytes:
			IPhpConvertible conv = obj as IPhpConvertible;
			if (conv != null) return conv.ToLongInteger();

			return 0;
		}

		/// <summary>
		/// Converts value of an arbitrary PHP.NET type into double precision floating-point
		/// value using the same conversion algorithms as PHP.
		/// </summary>
		/// <param name="obj">The value to convert.</param>
		/// <returns>The converted value.</returns>
		[Emitted]
		public static double ObjectToDouble(object obj)
		{
			string s;

			if (obj is double) return (double)obj;
			if (obj is int) return (double)(int)obj;
			if ((s = obj as string) != null) return StringToDouble(s);
			if (obj is bool) return (bool)obj ? 1.0 : 0.0;
			if (obj is long) return (double)(long)obj;

			// PhpString, PhpReference, PhpArray, PhpResource, DObject, PhpBytes:
			IPhpConvertible conv = obj as IPhpConvertible;
			if (conv != null) return conv.ToDouble();

			return 0.0;
		}

		/// <summary>
		/// Converts value of an arbitrary PHP.NET type into <see cref="PhpArray"/> using the same conversion
		/// algorithms as PHP.
		/// </summary>
		/// <param name="var">The value to convert.</param>
		/// <returns>The converted value. Doesn't do a deep copy.</returns>
		/// <remarks>Variables are not implicitly converted to arrays.</remarks>
		[Emitted]
		public static PhpArray ObjectToPhpArray(object var)
		{
            PhpArray array;
			DObject obj;

			if ((obj = var as DObject) != null)
				return obj.ToPhpArray();

			if ((array = var as PhpArray) != null)
				return array;

			if (var == null) return new PhpArray(0);

			// Integer, Double, Boolean, String, PhpBytes, PhpResource
			PhpArray result = new PhpArray(1);
			result.Add(var);
			return result;
		}

		/// <summary>
		/// Converts value of an arbitrary PHP.NET type into <see cref="DObject"/> using the same conversion
		/// algorithms as PHP.
		/// </summary>
		/// <param name="var">The value to convert.</param>
		/// <param name="context">Current <see cref="ScriptContext"/>. Doesn't do a deep copy.</param>
		/// <returns>The converted value.</returns>
		[Emitted]
		public static DObject/*!*/ObjectToDObject(object var, ScriptContext/*!*/ context)
		{
			PhpArray array;
			DObject obj;

            if ((array = var as PhpArray) != null)
                return PhpArrayToDObject(array, context);
			
			if (var == null) return new stdClass(context);

			if ((obj = var as DObject) != null) return obj;

			// Integer, Double, Boolean, String, PhpBytes, PhpResource:
			obj = new stdClass(context);
			obj.RuntimeFields = new OrderedHashtable<string>(null, 1);
			obj.RuntimeFields.Add("scalar", var);
			return obj;
		}

        /// <summary>
		/// Converts value of an arbitrary PHP.NET type into <see cref="PhpCallback"/>. 
		/// </summary>
		/// <param name="var">The value to convert.</param>
		/// <returns>
		/// The converted value or <B>null</B> if <paramref name="var"/> is empty (<see cref="PhpVariable.IsEmpty"/>)
		/// or could not be converted.
		/// </returns>
		/// <exception cref="PhpException">The variable is non-empty but does not have a valid structure to be used
		/// as a callback (Warning).</exception>
		[Emitted]
		public static PhpCallback ObjectToCallback(object var)
		{
			return ObjectToCallback(var, false);
		}

		/// <summary>
		/// Converts value of an arbitrary PHP.NET type into <see cref="PhpCallback"/>. 
		/// </summary>
		/// <param name="var">The value to convert (real object must be wrapped).</param>
		/// <param name="quiet">If <B>true</B>, no warning should be thrown if <paramref name="var"/> does not have
		/// a valid structure.</param>
		/// <returns>
		/// Either a valid callback, an invalid callback singleton <see cref="PhpCallback.Invalid"/>,
		/// or <B>null</B> if <paramref name="var"/> is empty (<see cref="PhpVariable.IsEmpty"/>).
		/// </returns>
		/// <exception cref="PhpException"><paramref name="quiet"/> is <B>false</B> and the variable is non-empty but
		/// does not have a valid structure to be used as a callback (Warning).</exception>
		public static PhpCallback ObjectToCallback(object var, bool quiet)
		{
            // empty variable
            if (PhpVariable.IsEmpty(var)) return null;
            
            // function name given as string-like type
			string name = PhpVariable.AsString(var);
			if (name != null) return new PhpCallback(name);

			// (instance/class name, method name) pair given as PhpArray
			PhpArray array = var as PhpArray;
			if (array != null && array.Count == 2)
			{
				object item1 = PhpVariable.Dereference(array[1]);

				// method name given as string-like type
				name = PhpVariable.AsString(item1);
				if (name != null)
				{
					object item0 = PhpVariable.Dereference(array[0]);

					// instance
					DObject instance = item0 as DObject;
					if (instance != null) return new PhpCallback(instance, name);

					// class name given as string-like type
					string cls_name = PhpVariable.AsString(item0);
					if (cls_name != null) return new PhpCallback(cls_name, name);
				}
			}

            // DObject::__invoke
            DObject obj;
            DRoutineDesc method;
            if ((obj = var as DObject) != null &&
                obj.TypeDesc.GetMethod(DObject.SpecialMethodNames.Invoke, null, out method) != GetMemberResult.NotFound)
            {
                // __invoke() does not respect visibilities
                return new PhpCallback(obj, method);
            }
            
            // invalid callback
            if (!quiet) PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_callback"));
			return PhpCallback.Invalid;
		}

		/// <summary>
		/// Converts value of an arbitrary PHP.NET type to a <see cref="DTypeDesc"/>.
		/// </summary>
		/// <param name="var">The value to convert.</param>
		/// <param name="resolveFlags">
		/// Flags. Only <see cref="ResolveTypeFlags.UseAutoload"/>, 
		/// <see cref="ResolveTypeFlags.SkipGenericNameParsing"/> and
		/// <see cref="ResolveTypeFlags.ThrowErrors"/> are valid.
		/// </param>
		/// <param name="caller">Current class context.</param>
		/// <param name="context">Current script context.</param>
		/// <param name="nameContext">Current naming context.</param>
        /// <param name="genericArgs">Array of function type params. Stored in pairs in a form of [(string)name1,(DTypeDescs)type1, .., ..]. Can be null.</param>
        /// <returns>The type desc or <B>null</B> on error.</returns>
		/// <exception cref="PhpException">The <paramref name="var"/> is not a string or empty string or not <see cref="DObject"/>. (Error)</exception>
		/// <exception cref="PhpException">The class with the given <paramref name="var"/> was not found
        /// (only if <paramref name="resolveFlags"/> has <see cref="ResolveTypeFlags.ThrowErrors"/>). (Error)</exception>
		[Emitted]
		public static DTypeDesc ObjectToTypeDesc(object var, ResolveTypeFlags resolveFlags, DTypeDesc caller, ScriptContext/*!*/ context, NamingContext nameContext, object[] genericArgs)
		{
			Debug.Assert((resolveFlags & ~(ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors |
				ResolveTypeFlags.SkipGenericNameParsing)) == 0, "Invalid flags in ObjectToTypeDesc.");

			DObject obj;
			if ((obj = var as DObject) != null)
			{
				// var is an instance
				return obj.TypeDesc;
			}
			else
			{
				string name = PhpVariable.AsString(var);
				if (String.IsNullOrEmpty(name))
				{
					PhpException.Throw(PhpError.Error, CoreResources.GetString("invalid_class_name"));
					return null;
				}

				resolveFlags |= ResolveTypeFlags.PreserveFrame;
				//if (autoload) flags |= (ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);

				DTypeDesc type = context.ResolveType(name, nameContext, caller, genericArgs, resolveFlags);

				// fill default type arguments or report an error:
				if (type != null && type.IsGenericDefinition && (resolveFlags & ResolveTypeFlags.SkipGenericNameParsing) == 0)
					type = Operators.MakeGenericTypeInstantiation(type, DTypeDesc.EmptyArray, 0);

				return type;
			}
		}

		#endregion

		#region TryObjectToXxx

		// assumes obj is of a PHP type

		private delegate T TryObjectToTypeDelegate<T>(object obj, out bool success);

		/// <summary>
		/// Stores instances of <see cref="TryObjectToTypeDelegate{T}"/>.
		/// </summary>
		private static Dictionary<Type, Delegate> conversionRoutines = new Dictionary<Type, Delegate>();

		[Emitted]
		public static Boolean TryObjectToBoolean(object obj, out bool success)
		{
			string s;
			PhpBytes b;
			PhpString ps;
			success = true;

			if (obj is bool) return (bool)obj;
			if (obj is int) return (int)obj != 0;
			if (obj is double) return (double)obj != 0.0;
			if (obj is long) return (long)obj != 0;

			// we have to check PHP string types separately from the rest of IPhpConvertibles here
			// as only these strings are "naturally" convertible to boolean:
			if ((s = obj as string) != null) return StringToBoolean(s);
			if ((b = obj as PhpBytes) != null) return b.ToBoolean();
			if ((ps = obj as PhpString) != null) return ps.ToBoolean();

			success = false;
			return false;
		}

		[Emitted]
		public static SByte TryObjectToInt8(object obj, out bool success)
		{
			int result = TryObjectToInt32(obj, out success);

			success &= result >= SByte.MinValue && result <= SByte.MaxValue;
			return unchecked((SByte)result);
		}

		[Emitted]
		public static Int16 TryObjectToInt16(object obj, out bool success)
		{
			int result = TryObjectToInt32(obj, out success);

			success &= result >= Int16.MinValue && result <= Int16.MaxValue;
			return unchecked((Int16)result);
		}

		[Emitted]
		public static Byte TryObjectToUInt8(object obj, out bool success)
		{
			int result = TryObjectToInt32(obj, out success);

			success &= result >= Byte.MinValue && result <= Byte.MaxValue;
			return unchecked((Byte)result);
		}

		[Emitted]
		public static UInt16 TryObjectToUInt16(object obj, out bool success)
		{
			int result = TryObjectToInt32(obj, out success);

			success &= result >= UInt16.MinValue && result <= UInt16.MaxValue;
			return unchecked((UInt16)result);
		}

		[Emitted]
		public static UInt32 TryObjectToUInt32(object obj, out bool success)
		{
			long result = TryObjectToInt64(obj, out success);

			success &= result >= UInt32.MinValue && result <= UInt32.MaxValue;
			return unchecked((UInt32)result);
		}

		[Emitted]
		public static Int32 TryObjectToInt32(object obj, out bool success)
		{
			string s;
			success = true;

			if (obj is int) return (int)obj;
			if (obj is bool) return (bool)obj ? 1 : 0;

			if (obj is long)
			{
				long lval = (long)obj;
				success = lval >= Int32.MinValue && lval <= Int32.MaxValue;
				return unchecked((Int32)lval);
			}

			if (obj is double)
			{
				double dval = (double)obj;
				success = dval >= Int32.MinValue && dval <= Int32.MaxValue;
				return unchecked((Int32)dval);
			}

			if ((s = PhpVariable.AsString(obj)) != null)
			{
				int ival;
				double dval;
				long lval;

				// successfull iff the number encoded in the string fits the Int32:
				NumberInfo info = StringToNumber(s, out ival, out lval, out dval);
				if ((info & (NumberInfo.Integer | NumberInfo.IsNumber)) == (NumberInfo.Integer | NumberInfo.IsNumber))
					return ival;

				success = false;
				return unchecked((Int32)lval);
			}

			success = false;
			return 0;
		}

		[Emitted]
		public static Int64 TryObjectToInt64(object obj, out bool success)
		{
			string s;
			success = true;

			if (obj is int) return (int)obj;
			if (obj is long) return (long)obj;
			if (obj is bool) return (bool)obj ? 1 : 0;

			if (obj is double)
			{
				double dval = (double)obj;
				success = dval >= Int64.MinValue && dval <= Int64.MaxValue;
				return unchecked((Int32)dval);
			}

			if ((s = PhpVariable.AsString(obj)) != null)
			{
				int ival;
				double dval;
				long lval;

				// successfull iff the number encoded in the string fits Int32 or Int64:
				NumberInfo info = StringToNumber(s, out ival, out lval, out dval);
				if ((info & NumberInfo.Integer) != 0)
					return ival;
				if ((info & NumberInfo.LongInteger) != 0)
					return lval;

				success = false;
				return unchecked((Int64)dval);
			}

			success = false;
			return 0;
		}

		[Emitted]
		public static UInt64 TryObjectToUInt64(object obj, out bool success)
		{
			string s;
			success = true;

			if (obj is int)
			{
				int ival = (int)obj;
				success = ival >= 0;
				return unchecked((UInt64)ival);
			}

			if (obj is long)
			{
				long lval = (long)obj;
				success = lval >= 0;
				return unchecked((UInt64)lval);
			}

			if (obj is bool) return (ulong)((bool)obj ? 1 : 0);

			if (obj is double)
			{
				double dval = (double)obj;
				success = dval >= UInt64.MinValue && dval <= UInt64.MaxValue;
				return unchecked((UInt64)dval);
			}

			if ((s = PhpVariable.AsString(obj)) != null)
			{
				int ival;
				double dval;
				long lval;

				// successfull iff the number encoded in the string fits Int32 or Int64:
				NumberInfo info = StringToNumber(s, out ival, out lval, out dval);
				if ((info & NumberInfo.Integer) != 0)
					return unchecked((UInt64)ival);
				if ((info & NumberInfo.LongInteger) != 0)
					return unchecked((UInt64)lval);

				success = dval >= UInt64.MinValue && dval <= UInt64.MaxValue;
				return unchecked((UInt64)dval);
			}

			success = false;
			return 0;
		}

		[Emitted]
		public static Single TryObjectToSingle(object obj, out bool success)
		{
			double result = TryObjectToDouble(obj, out success);

			success &= result >= Single.MinValue && result <= Single.MaxValue;
			return unchecked((Single)result);
		}

		[Emitted]
		public static Double TryObjectToDouble(object obj, out bool success)
		{
			string s;
			success = true;

			if (obj is double) return (double)obj;
			if (obj is int) return (double)(int)obj;
			if ((s = PhpVariable.AsString(obj)) != null) return StringToDouble(s);
			if (obj is bool) return (bool)obj ? 1.0 : 0.0;
			if (obj is long) return (double)(long)obj;

			success = false;
			return 0.0;
		}

		[Emitted]
		public static Decimal TryObjectToDecimal(object obj, out bool success)
		{
			int ival;
			long lval;
			double dval;

			// ignores the higher precision of decimal:
			switch (Convert.ObjectToNumber(obj, out ival, out lval, out dval) & NumberInfo.TypeMask)
			{
				case NumberInfo.Integer: success = true; return ival;
				case NumberInfo.LongInteger: success = true; return lval;
				case NumberInfo.Double: success = true; return unchecked((decimal)dval);
				case NumberInfo.Unconvertible: success = false; return 0;
				default: Debug.Fail(); throw null;
			}
		}

		[Emitted]
		public static Char TryObjectToChar(object obj, out bool success)
		{
			string result = TryObjectToString(obj, out success);

			if (result.Length == 1)
				return result[0];

			success = false;
			return '\0';
		}

		[Emitted]
		public static String TryObjectToString(object obj, out bool success)
		{
			string s;
			success = true;

            if ((s = PhpVariable.AsString(obj)) != null) return s;
            if (obj == null) return String.Empty;
            if (obj is int) return obj.ToString();
			if (obj is bool) return ((bool)obj) ? "1" : String.Empty;
			if (obj is double) return DoubleToString((double)obj);
			if (obj is long) return obj.ToString();

			// others:
			success = false;
			return String.Empty;
		}

		/// <summary>
		/// Tries to convert to <see cref="DateTime"/>.
		/// Conversion is successful if the object is 
		/// <list type="bullet">
		///		<term>a number that fits the long integer; the value is treated as number of ticks</term> 
		///   <term>a stringified form of such number (e.g. "1023"); the value is treated as number of ticks</term>
		///   <term>a string encoding a valid date-time format; the value is parsed by <see cref="DateTime.TryParse(string, out DateTime)"/></term>
		/// </list>
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="success"></param>
		/// <returns></returns>
		[Emitted]
		public static DateTime TryObjectToDateTime(object obj, out bool success)
		{
			string str = TryObjectToString(obj, out success);

			if (success)
			{
				DateTime result;
#if !SILVERLIGHT
				success = DateTime.TryParse(str, out result);
#else
				// TODO: Any way to optimize this?
				success = true;
				result = default(DateTime);
				try { result = DateTime.Parse(str); } catch { success = false; }
#endif
				return result;
			}

			return new DateTime();
		}

		/// <summary>
		/// Converts to <see cref="DBNull"/>. 
		/// The conversion is always successful and results to the <see cref="DBNull.Value"/> singleton.
		/// </summary>
		[Emitted]
		public static DBNull TryObjectToDBNull(object obj, out bool success)
		{
			success = true;
			return DBNull.Value;
		}

		[Emitted]
		public static T TryObjectToClass<T>(object obj, out bool success)
			where T : class
		{
			success = true;
			if (obj == null) return null;

			T result = null;
			if ((result = PhpVariable.Unwrap(obj) as T) != null && (!(result is IPhpVariable) || result is PhpObject))
			{
				// do not leak out instances implementing IPhpVariable (e.g. if T is object)
				// TODO: Add IPhpVariable.Unwrap
				return result;
			}

			success = false;
			return default(T);
		}

		[Emitted]
		public static T TryObjectToDelegate<T>(object obj, out bool success)
			where T : class
		{
			T result = null;
			object bare_obj = PhpVariable.Unwrap(obj);
			if (bare_obj == null || (result = bare_obj as T) != null)
			{
				success = true;
				return result;
			}

			// try to convert the object to PhpCallback
			PhpCallback callback = ObjectToCallback(obj, true);
			if (callback != null && callback.Bind(true))
			{
				// generate a conversion stub
				result = EventClass<T>.GetStub(
					callback.TargetInstance,
					callback.TargetRoutine,
					callback.IsBoundToCaller ? callback.RoutineName : null);

				if (result != null)
				{
					success = true;
					return result;
				}
			}

			success = false;
			return default(T);
		}

		[Emitted]
		public static T[] TryObjectToArray<T>(object obj, out bool success)
		{
			T[] result = PhpVariable.Unwrap(obj) as T[];
			if (result != null)
			{
				success = true;
				return result;
			}

			// try to convert PhpArray to the desired array
			PhpArray array = obj as PhpArray;
			if (array != null && array.StringCount == 0)
			{
				result = new T[array.MaxIntegerKey + 1];

				for (int i = 0; i < result.Length; i++)
				{
					object item;
					if (array.TryGetValue(i, out item))
					{
						// try to convert the item
						result[i] = TryObjectToType<T>(item, out success);
						if (!success) return default(T[]);
					}
				}

				success = true;
				return result;
			}

			success = false;
			return default(T[]);
		}

		[Emitted]
		public static T TryObjectToStruct<T>(object obj, out bool success)
			where T : struct
		{
			obj = PhpVariable.Unwrap(obj);

			success = obj is T;
			return success ? (T)obj : default(T);
		}

		/// <summary>
		/// Used when the type is unknown at compiler-time, e.g. it is a generic parameter.
		/// </summary>
		[Emitted]
		public static T TryObjectToType<T>(object obj, out bool success)
		{
			Type target_type = typeof(T);
			Delegate conversion_routine;

			lock (conversionRoutines)
			{
				if (!conversionRoutines.TryGetValue(target_type, out conversion_routine))
				{
					conversion_routine = CreateConversionDelegate(target_type);
					conversionRoutines.Add(target_type, conversion_routine);
				}
			}

			return ((TryObjectToTypeDelegate<T>)conversion_routine)(obj, out success);
		}

		private static Delegate CreateConversionDelegate(Type targetType)
		{
			switch (Type.GetTypeCode(targetType))
			{
				case TypeCode.Boolean: return new TryObjectToTypeDelegate<bool>(TryObjectToBoolean);
				case TypeCode.SByte: return new TryObjectToTypeDelegate<sbyte>(TryObjectToInt8);
				case TypeCode.Int16: return new TryObjectToTypeDelegate<short>(TryObjectToInt16);
				case TypeCode.Int32: return new TryObjectToTypeDelegate<int>(TryObjectToInt32);
				case TypeCode.Int64: return new TryObjectToTypeDelegate<long>(TryObjectToInt64);
				case TypeCode.Byte: return new TryObjectToTypeDelegate<byte>(TryObjectToUInt8);
				case TypeCode.UInt16: return new TryObjectToTypeDelegate<ushort>(TryObjectToUInt16);
				case TypeCode.UInt32: return new TryObjectToTypeDelegate<uint>(TryObjectToUInt32);
				case TypeCode.UInt64: return new TryObjectToTypeDelegate<ulong>(TryObjectToUInt64);
				case TypeCode.Single: return new TryObjectToTypeDelegate<float>(TryObjectToSingle);
				case TypeCode.Double: return new TryObjectToTypeDelegate<double>(TryObjectToDouble);
				case TypeCode.Decimal: return new TryObjectToTypeDelegate<decimal>(TryObjectToDecimal);
				case TypeCode.Char: return new TryObjectToTypeDelegate<char>(TryObjectToChar);
				case TypeCode.String: return new TryObjectToTypeDelegate<string>(TryObjectToString);
				case TypeCode.DateTime: return new TryObjectToTypeDelegate<DateTime>(TryObjectToDateTime);
				case TypeCode.DBNull: return new TryObjectToTypeDelegate<DBNull>(TryObjectToDBNull);

				case TypeCode.Object:
					{
						Type generic_arg;
						MethodInfo generic_method;

						if (targetType.IsValueType)
						{
							generic_arg = targetType;
							generic_method = Emit.Methods.Convert.TryObjectToStruct;
						}
						else
						{
							if (targetType.IsArray)
							{
								generic_arg = targetType.GetElementType();
								generic_method = Emit.Methods.Convert.TryObjectToArray;
							}
							else
							{
								generic_arg = targetType;
								if (typeof(Delegate).IsAssignableFrom(targetType))
								{
									generic_method = Emit.Methods.Convert.TryObjectToDelegate;
								}
								else
								{
									generic_method = Emit.Methods.Convert.TryObjectToClass;
								}
							}
						}

						// create a new delegate type instantiation and a new delegate instance
						Type delegate_type = typeof(TryObjectToTypeDelegate<>).MakeGenericType(targetType);
						return Delegate.CreateDelegate(delegate_type, generic_method.MakeGenericMethod(generic_arg));
					}

				default:
					{
						Debug.Fail();
						return null;
					}
			}
		}

		#endregion

		#region LINQ

		/// <summary>
		/// Converts a specified object to enumerable LINQ source.
		/// If the source is a dictionary, the values are enumerated.
		/// If the source is a PHP enumerable (array, object, query resource, ...) the enumeration is performed
		/// using the PHP foreach enumerator (who may deep copy the elements).
		/// </summary>
		[Emitted]
		public static IEnumerable<object> ObjectToLinqSource(object var, DTypeDesc caller)
		{
			// try PHP enumerable (CLR object wrapper implements it, so we can enumerate CLR objects):
			IPhpEnumerable php_enumerable = var as IPhpEnumerable;
			if (php_enumerable != null)
			{
				return EnumerateValues(php_enumerable.GetForeachEnumerator(false, false, caller));
			}

			PhpException.Throw(PhpError.Warning, CoreResources.GetString("invalid_query_source"));
			return null;
		}

		private static IEnumerable<object> EnumerateValues(IDictionaryEnumerator/*!*/ enumerator)
		{
			if (enumerator == null)
			{
				PhpException.Throw(PhpError.Warning, "Invalid query source.");
				yield break;
			}

			while (enumerator.MoveNext())
			{
				yield return enumerator.Value;
			}
		}

		#endregion

		#region Narrowing & Coercion

		public static int NarrowToInt32(long value)
		{
			if (value < Int32.MinValue)
				return Int32.MinValue;

			if (value > Int32.MaxValue)
				return Int32.MaxValue;

			return (int)value;
		}

		public static uint NarrowToUInt32(long value)
		{
			if (value > UInt32.MaxValue)
				return UInt32.MaxValue;

			if (value < UInt32.MinValue)
				return UInt32.MinValue;

			return (uint)value;
		}

		#endregion

		#region String to Number Conversions

		[Flags]
		public enum NumberInfo
		{
			Integer = 1,
			LongInteger = 2,
			Double = 4,
			Unconvertible = 16,

			TypeMask = Integer | LongInteger | Double | Unconvertible,

			IsNumber = 64,
			IsHexadecimal = 128
		}

		/// <summary>
		/// Converts a string to integer value and double value and decides whether it represents a number as a whole.
		/// </summary>
		/// <param name="s">The string to convert.</param>
		/// <param name="length">A maximal length of the substring to be parsed.</param>
		/// <param name="p">
		/// A position where to start parsing. Returns a position where the parsing ended
		/// (the first character not visited).
		/// </param>
		/// <param name="i">
		/// Returns a position where integer-parsing ended 
		/// (the first character not included in the resulting double value).
		/// </param>
		/// <param name="l">
		/// Returns a position where long-integer-parsing ended 
		/// (the first character not included in the resulting double value).
		/// </param>
		/// <param name="d">
		/// Returns a position where double-parsing ended
		/// (the first character not included in the resulting double value).
		/// </param>
		/// <param name="intValue">Result of the conversion to integer.</param>
		/// <param name="longValue">Result of the conversion to long integer.</param>
		/// <param name="doubleValue">Result of the conversion to double.</param>
		/// <returns>
		/// Information about parsed number including its type, which is the narrowest one that fits.
		/// E.g. 
		/// IsNumber("10 xyz", ...) includes NumberInfo.Integer,
		/// IsNumber("10000000000000 xyz", ...) includes NumberInfo.LongInteger,
		/// IsNumber("10.9 xyz", ...) includes NumberInfo.Double,
		/// IsNumber("10.9", ...) includes NumberInfo.IsNumber and NumberInfo.Double.
		/// 
		/// The return value always includes one of NumberInfo.Integer, NumberInfo.LongInteger, NumberInfo.Double
		/// and never NumberInfo.Unconvertible (as each string is convertible to a number).
		/// </returns>
		private static NumberInfo IsNumber(string s, int length, ref int p, out int i, out int l, out int d,
			out int intValue, out long longValue, out double doubleValue)
		{
			// invariant after return: 0 <= i <= l <= d <= p <= old(p) + length - 1.
			NumberInfo result = 0;

			if (s == null) s = "";
			if (p < 0) p = 0;
			if (length < 0 || length > s.Length - p) length = s.Length - p;
			int limit = p + length;

			// int:
			intValue = 0;                       // integer value of already read part of the string
			i = -1;                             // last position of an integer part of the string

			// long:
			longValue = 0;                      // long integer value of already read part of the string
			l = -1;                             // last position of an long integer part of the string

			// double:
			int exponent = 0;                   // the value of exponent
			double expBase = 10;                // sign of the exponent (equivalent to bases 10 and 0.1)
			double div = 10;                    // decimal factor
			int e = -1;                         // position where the exponent has started by 'e', 'E', 'd', or 'D'
			doubleValue = 0.0;                  // double value of already read part of the string
			d = -1;

			// common:
			bool contains_digit = false;        // whether a digit is contained in the string
			int sign = +1;                      // a sign of whole number
			int state = 0;                      // automaton state

			// patterns and states:
			// [:white:]*[+-]?0?[0-9]*[.]?[0-9]*([dDeE][+-]?[0-9]+)?
			//  0000000   11  2  222   2   333    4444  55   666     
			// [:white:]*[+-]?0(x|X)[0-9A-Fa-f]*    // TODO: PHP does not resolve [+-] at the beginning, however Phalanger does
			//  0000000   11  2 777  888888888  

			while (p < limit)
			{
				char c = s[p];

				switch (state)
				{
					case 0: // expecting whitespaces to be skipped
						{
							if (!Char.IsWhiteSpace(c))
							{
								state = 1;
								goto case 1;
							}
							break;
						}

					case 1: // expecting result + or - or .
						{
							if (c >= '0' && c <= '9')
							{
								state = 2;
								goto case 2;
							}

							if (c == '-')
							{
								sign = -1;
								state = 2;
								break;
							}

							if (c == '+')
							{
								state = 2;
								break;
							}

							// ends reading (long) integer:
							i = l = p;

							// switch to decimals in next turn:
							if (c == '.')
							{
								state = 3;
								break;
							}

							// unexpected character:
							goto Done;
						}

					case 2: // expecting result
						{
							// a single leading zero:
							if (c == '0' && !contains_digit)
							{
								contains_digit = true;
								state = 7;
								break;
							}

							if (c >= '0' && c <= '9')
							{
								int num = (int)(c - '0');
								contains_digit = true;

								doubleValue = doubleValue * 10 + num;

								// if still reading a long integer (we may read a double only since integer has already overflown):
								if (l == -1)
								{
									if (longValue < Int64.MaxValue / 10 || (longValue == Int64.MaxValue / 10 && num <= Int64.MaxValue % 10))
									{
										longValue = longValue * 10 + num;

										// if still reading an integer:
										if (i == -1)
										{
											if (longValue <= Int32.MaxValue)
											{
												intValue = (int)longValue;
											}
											else if (sign == -1)
											{
												// last integer position:
												i = (-longValue == Int32.MinValue) ? p + 1 : p;
												intValue = Int32.MinValue;
											}
											else
											{
												// last integer position:
												i = p;
												intValue = Int32.MaxValue;
											}
										}
									}
									else if (sign == -1)
									{
										// last long integer position:
										l = p;
										longValue = Int64.MinValue;
									}
									else
									{
										// last long integer position:
										l = p;
										longValue = Int64.MaxValue;
									}
								}
								break;
							}

							// ends reading (long) integer:
							i = l = p;

							// switch to decimals in next turn:
							if (c == '.')
							{
								state = 3;
								break;
							}

							// switch to exponent in next turn:
							if (c == 'd' || c == 'D' || c == 'e' || c == 'E')
							{
								e = p;
								state = 4;
								break;
							}

							// unexpected character:
							goto Done;
						}

					case 3: // expecting decimals
						{
							Debug.Assert(i >= 0 && l >= 0, "Reading double.");

							// reading decimals:
							if (c >= '0' && c <= '9')
							{
								int num = (int)(c - '0');
								doubleValue += num / div;
								div *= 10;
								break;
							}

							// switch to exponent in next turn:
							if (c == 'd' || c == 'D' || c == 'e' || c == 'E')
							{
								e = p;
								state = 4;
								break;
							}

							// unexpected character:
							goto Done;
						}

					case 4: // expecting exponent + or -
						{
							Debug.Assert(i >= 0 && l >= 0, "Reading double.");

							// switch to exponent immediately:
							if (c >= '0' && c <= '9')
							{
								state = 6;
								goto case 6;
							}

							// switch to exponent in next turn:
							if (c == '-')
							{
								expBase = 0.1;
								state = 5;
								break;
							}

							// switch to exponent in next turn:
							if (c == '+')
							{
								state = 5;
								break;
							}

							// unexpected characters:
							goto Done;
						}

					case 5: // expecting exponent after the sign
						{
							state = 6;
							goto case 6;
						}

					case 6: // expecting exponent without the sign
						{
							if (c >= '0' && c <= '9')
							{
								int num = (int)(c - '0');

								// if exponent exceeds max{log(MaxValue),|log(Epsilon)|} < 400 then
								// the result is either infinity or zero, the first is excluded by the condition below;
								// if the result is zero, we can read arbitrarily long exponent:
								if (exponent > 400)
									break;

								exponent = exponent * 10 + num;

								// continues reading exponent if the total value is not infinite:
								if (doubleValue * Math.Pow(expBase, exponent) != Double.PositiveInfinity)
									break;
							}

							// unexpected character:
							goto Done;
						}

					case 7: // a single leading zero read:
						{
							// check for hexa integer:
							if (c == 'x' || c == 'X')
							{
								// end of double reading:
								d = p;

								state = 8;
								break;
							}

							// other cases -> back to integer reading:
							state = 2;
							goto case 2;
						}

					case 8: // hexa integer
						{
							result |= NumberInfo.IsHexadecimal;

							int num = AlphaNumericToDigit(c);

							// unexpected character:
							if (num <= 15)
							{
								if (longValue < Int64.MaxValue / 16 || (longValue == Int64.MaxValue / 16 && num <= Int64.MaxValue % 16))
								{
									longValue = longValue * 16 + num;

									if (longValue <= Int32.MaxValue)
									{
										intValue = (int)longValue;
									}
									else if (sign == -1)
									{
										// last hexa integer position:
										i = (-longValue == Int32.MinValue) ? p + 1 : p;
										intValue = Int32.MinValue;
									}
									else
									{
										// last hexa integer position:
										i = p;
										intValue = Int32.MaxValue;
									}
								}
								else if (sign == -1)
								{
									// last hexa long integer position:
									l = p;
									longValue = Int64.MinValue;
								}
								else
								{
									// last hexa long integer position:
									l = p;
									longValue = Int64.MaxValue;
								}
								break;
							}

							goto Done;
						}
				}
				p++;
			}

		Done:

			// an exponent ends with 'e', 'd', 'E', 'D', '-', or '+':
			if (state == 4 || state == 5)
			{
				Debug.Assert(i >= 0 && l >= 0 && e >= 0, "Reading exponent of double.");

				// shift back:
				p = e;
			}

			// if double index hasn't stopped neither the exponent nor sign have been applied yet:
			if (d == -1) { doubleValue *= Math.Pow(expBase, exponent) * sign; d = p; }

			// if long/integer index hasn't stopped the sign hasn't been applied yet:
			if (l == -1) { longValue *= sign; l = p; }
			if (i == -1) { intValue *= sign; i = p; }
			
			// determine the type comparing strictly d, l, i:
			if (d > l) result |= NumberInfo.Double;
			else if (l > i) result |= NumberInfo.LongInteger;
			else result |= NumberInfo.Integer;
			
			// the string is a number if it was entirely parsed and contains a digit:
			if (contains_digit && p == limit)
				result |= NumberInfo.IsNumber;

			if ((result & NumberInfo.IsHexadecimal) != 0)
				doubleValue = unchecked((double)longValue);

			return result;
		}

		/// <summary>
		/// Converts value of an arbitrary PHP/CLR type into integer value, long integer value or double value using 
		/// conversion algorithms in a manner of PHP. All returned values are valid.
		/// </summary>
		/// <param name="obj">The value to convert.</param>
		/// <param name="doubleValue">The double value.</param>
		/// <param name="longValue">The long integer value.</param>
		/// <param name="intValue">The integer value.param></param>
		/// <returns>Conversion info.</returns>
		public static NumberInfo ObjectToNumber(object obj, out int intValue, out long longValue, out double doubleValue)
		{
			string s;
			IPhpConvertible php_conv;

			if (obj is int)
			{
				intValue = (int)obj;
				longValue = intValue;
				doubleValue = intValue;
				return NumberInfo.Integer | NumberInfo.IsNumber;
			}
			else if (obj is double)
			{
				doubleValue = (double)obj;
				intValue = unchecked((int)doubleValue);
				longValue = unchecked((long)doubleValue);
				return NumberInfo.Double | NumberInfo.IsNumber;
			}
			else if (obj is long)
			{
				longValue = (long)obj;
				intValue = NarrowToInt32(longValue);
				doubleValue = (double)longValue;
				return NumberInfo.LongInteger | NumberInfo.IsNumber;
			}
			else if ((s = obj as string) != null)
			{
				return StringToNumber(s, out intValue, out longValue, out doubleValue);
			}
			else if (obj is bool)
			{
				intValue = (bool)obj ? 1 : 0;
				doubleValue = intValue;
				longValue = intValue;
				return NumberInfo.Integer;
			}
			else if ((php_conv = obj as IPhpConvertible) != null)
			{
				return php_conv.ToNumber(out intValue, out longValue, out doubleValue);
			}
			else if (obj == null)
			{
				intValue = 0;
				longValue = 0;
				doubleValue = 0.0;
				return NumberInfo.Integer;
			}

			intValue = 0;
			longValue = 0;
			doubleValue = 0.0;
			return NumberInfo.Unconvertible;
		}

		/// <summary>
		/// Converts string into integer, long integer and double value using conversion algorithm in a manner of PHP. 
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <param name="intValue">The result of conversion to integer.</param>
		/// <param name="longValue">The result of conversion to long integer.</param>
		/// <param name="doubleValue">The result of conversion to double.</param>
		/// <returns><see cref="NumberInfo"/>.</returns>
		/// <exception cref="ArgumentNullException"><paramref name="str"/> is a <B>null</B> reference.</exception>
		public static NumberInfo StringToNumber(string str, out int intValue, out long longValue, out double doubleValue)
		{
			int i, l, d, p = 0;
			return IsNumber(str, -1, ref p, out i, out l, out d, out intValue, out longValue, out doubleValue);
		}

		/// <summary>
		/// Converts a string to integer using conversion algorithm in a manner of PHP.
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <returns>The result of conversion.</returns>
		public static int StringToInteger(string str)
		{
			int ival, i, l, d, p = 0;
			double dval;
			long lval;
			IsNumber(str, -1, ref p, out i, out l, out d, out ival, out lval, out dval);

			return ival;
		}

		/// <summary>
		/// Converts a string to long integer using conversion algorithm in a manner of PHP.
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <returns>The result of conversion.</returns>
		public static long StringToLongInteger(string str)
		{
			int ival, i, l, d, p = 0;
			double dval;
			long lval;
			IsNumber(str, -1, ref p, out i, out l, out d, out ival, out lval, out dval);

			return lval;
		}

		/// <summary>
		/// Converts a string to double using conversion algorithm in a manner of PHP.
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <returns>The result of conversion.</returns>
		public static double StringToDouble(string str)
		{
			int ival, i, l, d, p = 0;
			double dval;
			long lval;
			IsNumber(str, -1, ref p, out i, out l, out d, out ival, out lval, out dval);

			return dval;
		}

		/// <summary>
		/// Converts a part of a string starting on a specified position to an integer.
		/// </summary>
		/// <param name="str">The string to be parsed.</param>
		/// <param name="length">Maximal length of the substring to parse.</param>
		/// <param name="position">
		/// The position where to start. Points to the first character after the substring storing the integer
		/// when returned.
		/// </param>
		/// <returns>The integer stored in the <paramref name="str"/>.</returns>
		public static int SubstringToInteger(string str, int length, ref int position)
		{
			int d, l, p = position;
			int ival;
			long lval;
			double dval;
			IsNumber(str, length, ref p, out position, out l, out d, out ival, out lval, out dval);

			return ival;
		}

		/// <summary>
		/// Converts a part of a string starting on a specified position to a long integer.
		/// </summary>
		/// <param name="str">The string to be parsed.</param>
		/// <param name="length">Maximal length of the substring to parse.</param>
		/// <param name="position">
		/// The position where to start. Points to the first character after the substring storing the integer
		/// when returned.
		/// </param>
		/// <returns>The integer stored in the <paramref name="str"/>.</returns>
		public static long SubstringToLongInteger(string str, int length, ref int position)
		{
			int i, d, p = position;
			int ival;
			long lval;
			double dval;
			IsNumber(str, length, ref p, out i, out position, out d, out ival, out lval, out dval);

			return lval;
		}

		/// <summary>
		/// Converts a part of a string starting on a specified position to a double.
		/// </summary>
		/// <param name="str">The string to be parsed.</param>
		/// <param name="length">Maximal length of the substring to parse.</param>
		/// <param name="position">
		/// The position where to start. Points to the first character after the substring storing the double
		/// when returned.
		/// </param>
		/// <returns>The double stored in the <paramref name="str"/>.</returns>
		public static double SubstringToDouble(string str, int length, ref int position)
		{
			int i, l, p = position;
			int ival;
			long lval;
			double dval;
			IsNumber(str, length, ref p, out i, out l, out position, out ival, out lval, out dval);

			return dval;
		}

		/// <summary>
		/// Converts a substring to almost long integer in a specified base.
		/// Stops parsing if result overflows unsigned integer.
		/// </summary>
		public static long SubstringToLongStrict(string str, int length, int @base, long maxValue, ref int position)
		{
			if (maxValue <= 0)
				throw new ArgumentOutOfRangeException("maxValue");

			if (@base < 2 || @base > 'Z' - 'A' + 1)
				throw new ArgumentException(CoreResources.GetString("invalid_base"), "base");

			if (str == null) str = "";
			if (position < 0) position = 0;
			if (length < 0 || length > str.Length - position) length = str.Length - position;
			if (length == 0) return 0;

			long result = 0;
			int sign = +1;

			// reads a sign:
			if (str[position] == '+')
			{
				position++;
				length--;
			}
			else if (str[position] == '-')
			{
				position++;
				length--;
				sign = -1;
			}

			long max_div, max_rem;
			max_div = MathEx.DivRem(maxValue, @base, out max_rem);

			while (length-- > 0)
			{
				int digit = AlphaNumericToDigit(str[position]);
				if (digit >= @base) break;

				if (!(result < max_div || (result == max_div && digit <= max_rem)))
				{
					// reads remaining digits:
					while (length-- > 0 && AlphaNumericToDigit(str[position]) < @base) position++;

					return (sign == -1) ? Int64.MinValue : Int64.MaxValue;
				}

				result = result * @base + digit;
				position++;
			}

			return result * sign;
		}

		/// <summary>
		/// Converts a character to a digit.
		/// </summary>
		/// <param name="c">The character [0-9A-Za-z].</param>
		/// <returns>The digit represented by the character or <see cref="Int32.MaxValue"/> 
		/// on non-alpha-numeric characters.</returns>
		public static int AlphaNumericToDigit(char c)
		{
			if (c >= '0' && c <= '9')
				return (int)(c - '0');

			if (c >= 'a' && c <= 'z')
				return (int)(c - 'a') + 10;

			if (c >= 'A' && c <= 'Z')
				return (int)(c - 'A') + 10;

			return Int32.MaxValue;
		}

		/// <summary>
		/// Converts a character to a digit.
		/// </summary>
		/// <param name="c">The character [0-9].</param>
		/// <returns>The digit represented by the character or <see cref="Int32.MaxValue"/> 
		/// on non-numeric characters.</returns>
		public static int NumericToDigit(char c)
		{
			if (c >= '0' && c <= '9')
				return (int)(c - '0');

			return Int32.MaxValue;
		}

		#endregion

		#region Specialized Conversions

		/// <summary>
		/// Converts double value to a string.
		/// </summary>
		/// <param name="value">The value to be converted.</param>
		/// <returns>A string representation of the <paramref name="value"/>.</returns>
		public static string DoubleToString(double value)
		{
			return value.ToString("G", NumberFormatInfo.InvariantInfo);
		}

		/// <summary>
		/// Converts string to a single character.
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <returns>The first character of the string.</returns>
		/// <exception cref="PhpException"><paramref name="str"/> doesn't consist of a single character. (Warning)</exception>
		public static char StringToChar(string str)
		{
			if (str == null || str.Length != 1)
			{
				PhpException.Throw(PhpError.Warning, CoreResources.GetString("string_should_be_single_character"));
				if (String.IsNullOrEmpty(str)) return '\0';
			}

			return str[0];
		}

		/// <summary>
		/// Converts string to boolean.
		/// </summary>
		/// <param name="str">The string to convert.</param>
		/// <returns>Whether <paramref name="str"/> is empty or equal to "0".</returns>
		/// <remarks>Asserts that <paramref name="str"/> is not null.</remarks>
		public static bool StringToBoolean(string str)
		{
			if (str == null) return false;

			return !(str.Length == 0 || (str.Length == 1 && str[0] == '0'));
		}

		/// <summary>
		/// Converts an object of arbitrary PHP.NET type to string or integer array key.
		/// </summary>
		/// <param name="obj">The object ot be converted.</param>
		/// <param name="key">The result. Its validity depends on the value returned.</param>
		/// <returns>Whether <c>obj</c> is a valid key.</returns>
		/// <include file='Doc/Conversions.xml' path='docs/method[@name="ObjectToArrayKey"]/*' />
		public static bool ObjectToArrayKey(object obj, out IntStringKey key)
		{
			if (obj is int)
			{
				key = new IntStringKey((int)obj);
				return true;
			}

			string str;
			if ((str = PhpVariable.AsString(obj)) != null)
			{
				key = StringToArrayKey(str);
				return true;	
			}
			
			if (obj is bool)
			{
				key = new IntStringKey((bool)obj ? 1 : 0);
				return true;
			}

			if (obj is double)
			{
				key = new IntStringKey(unchecked((int)(double)obj));
				return true;
			}

			if (obj is long)
			{
				key = new IntStringKey(unchecked((int)(long)obj));
				return true;
			}

			if (obj == null)
			{
				key = new IntStringKey(String.Empty);
				return true;
			}

			PhpResource resource = obj as PhpResource;
			if (resource != null)
			{
				key = new IntStringKey(resource.ToInteger());
				return true;
			}

            PhpArray array = obj as PhpArray;
            if (array != null)
            {
                key = new IntStringKey("Array");
                return true;
            }

			// invalid index:
			key = new IntStringKey();
			return false;
		}

		/// <summary>
		/// Converts a string to an appropriate integer.
		/// </summary>
		/// <param name="str">The string in "{0 | -?[1-9][0-9]*}" format.</param>
		/// <returns>The array key (integer or string).</returns>
		public static IntStringKey StringToArrayKey(string str)
		{
			int digits, i, sgn, d;

			// null or empty string is not converted:
			if (String.IsNullOrEmpty(str)) return new IntStringKey(str);

			// if the string starts with '-' character:
			if (str[0] == '-')
			{
				sgn = -1;
				i = 1;
				digits = str.Length - 1;

				// minus sign only:
				if (digits == 0) return new IntStringKey(str);
			}
			else
			{
				sgn = +1;
				i = 0;
				digits = str.Length;
			}

			// upper limit (MaxInt/MinInt):
			if (digits > 10) digits = 10;

			// first digit after optional minus sign:
			d = (int)(str[i] - '0');

			// single '0' is converted to zero, number starting by '0' is not converted: 
			if (d == 0) return (digits == 1) ? new IntStringKey(0) : new IntStringKey(str);

			// not a digit:
			if (d < 0 || d > 9) return new IntStringKey(str);

			long result = d;
			while (--digits > 0)
			{
				// next digit:
				d = (int)(str[++i] - '0');

				// not a digit:
				if (d < 0 || d > 9) return new IntStringKey(str);

				result = result * 10 + d;
			}

			// sign:
			result *= sgn;

			// to big/small number:
			if (result < int.MinValue || result > int.MaxValue) return new IntStringKey(str);

			return new IntStringKey((int)result);
		}

        /// <summary>
		/// Converts a size specified as a string to integer. 
		/// </summary>
		/// <param name="str">The size.</param>
		/// <returns>The number of bytes.</returns>
		/// <remarks>
		/// Size may contain either a number of bytes or number of kilo/mega/giga bytes with suffix "K"/"M"/"G".
		/// The first non-white-space character from the end of the string is taken as the suffix.
		/// All numbers may be "PHP numbers", i.e. only a prefix containing an integer is taken.
		/// Suffixes are case insensitive.
		/// If integer overflows or underflows the maximal or minimal integer value is returned, respectively.
		/// </remarks>
		public static int StringByteSizeToInteger(string str)
		{
			if (str == null || str.Length == 0) return 0;
			str = str.Trim();
			if (str.Length == 0) return 0;

			long result = StringToInteger(str);

			switch (str[str.Length - 1])
			{
				case 'K':
				case 'k': result <<= 10; break;
				case 'M':
				case 'm': result <<= 20; break;
				case 'G':
				case 'g': result <<= 30; break;
			}

			if (result >= Int32.MaxValue) return Int32.MaxValue;
			if (result <= Int32.MinValue) return Int32.MinValue;

			return (int)result;
		}

        /// <summary>
        /// Convert elements of given <see cref="PhpArray"/> into a new instance of <see cref="stdClass"/>.
        /// </summary>
        /// <param name="array"><see cref="PhpArray"/> to be read.</param>
        /// <param name="context">Current <see cref="ScriptContext"/>.</param>
        /// <returns>New instance of <see cref="DObject"/> containing fields from <paramref name="array"/>.</returns>
        public static DObject PhpArrayToDObject(PhpArray/*!*/array, ScriptContext/*!*/ context)
        {
            Debug.Assert(array != null, "Argument 'array' cannot be null!");

            var runtimeFields = new OrderedHashtable<string>(null, array.Count);
            //foreach (KeyValuePair<IntStringKey, object> pair in array)
            for (var p = array.table.head.Next; p != array.table.head; p = p.Next)
            {
                // add elements directly into the hashtable (no duplicity check, since array is already valid)
                runtimeFields.Add(/*pair.Key*/p.Key.Object.ToString(), PhpVariable.Copy(/*pair*/p.Value, CopyReason.Assigned));
            }

            // create a new stdClass with runtime fields:
            return new stdClass(context)
            {
                RuntimeFields = runtimeFields
            };
        }

		#endregion

		#region String Quoting (Only for backward compatibility)

		/// <summary>
		/// Quotes a string according to the current configuration.
		/// </summary>
		/// <param name="str">String to quote.</param>
		/// <param name="context">Script context.</param>
		/// <returns>Quoted or unchanged string.</returns>
		public static string Quote(string str, ScriptContext/*!*/ context)
		{
			if (str == null || !context.Config.Variables.QuoteRuntimeVariables)
				return str;

			if (context.Config.Variables.QuoteInDbManner)
				return StringUtils.AddDbSlashes(str);
			else
				return StringUtils.AddCSlashes(str, true, true);
		}

		/// <summary>
		/// Quotes arbitrary data according to the current configuration converting them to a string.
		/// </summary>
		/// <param name="data">Data to quote.</param>
		/// <param name="context">Script context.</param>
		/// <returns>Quoted string or unchanged data.</returns>
		public static object Quote(object data, ScriptContext/*!*/ context)
		{
			if (data == null || !context.Config.Variables.QuoteRuntimeVariables)
				return data;

			string str = ObjectToString(data);
			if (context.Config.Variables.QuoteInDbManner)
				return StringUtils.AddDbSlashes(str);
			else
				return StringUtils.AddCSlashes(str, true, true);
		}

		/// <summary>
		/// Unquotes a string according to the current configuration.
		/// </summary>
		/// <param name="str">String to quote.</param>
		/// <param name="context">Script context.</param>
		/// <returns>Unquoted or unchanged string.</returns>
		public static string Unquote(string str, ScriptContext/*!*/ context)
		{
			if (str == null || !context.Config.Variables.QuoteRuntimeVariables)
				return str;

			if (context.Config.Variables.QuoteInDbManner)
				return StringUtils.StripDbSlashes(str);
			else
				return StringUtils.StripCSlashes(str);
		}

		/// <summary>
		/// Unquotes arbitrary data according to the current configuration converting them to a string.
		/// </summary>
		/// <param name="data">String to quote.</param>
		/// <param name="context">Script context.</param>
		/// <returns>Unquoted string or unchanged data.</returns>
		public static object Unquote(object data, ScriptContext/*!*/ context)
		{
			if (data == null || !context.Config.Variables.QuoteRuntimeVariables)
				return data;

			string str = ObjectToString(data);
			if (context.Config.Variables.QuoteInDbManner)
				return StringUtils.StripDbSlashes(str);
			else
				return StringUtils.StripCSlashes(str);
		}

		#endregion

		#region Unit Testing
#if DEBUG

		class UnitTest
		{
			struct TestCase
			{
				public string s;
				public bool isnum;
				public int p, i, l, d;
				public int iv;
				public long lv;
				public double dv;

				public TestCase(string s, bool isnum, int p, int i, int l, int d, int iv, long lv, double dv)
				{
					this.s = s;
					this.isnum = isnum;
					this.p = p;
					this.i = i;
					this.l = l;
					this.d = d;
					this.iv = iv;
					this.lv = lv;
					this.dv = dv;
				}
			}

			static int MaxInt = Int32.MaxValue;
			static int MinInt = Int32.MinValue;
			static long MaxLong = Int64.MaxValue;
			static string LongOvf = "1250456465465412504564654654";
			static string IntOvf = "12504564654654";
			static long IntOvfL = long.Parse(IntOvf);
			static double IntOvfD = double.Parse(IntOvf);
			static string LongHOvf = "0x09213921739830924323423";

			static TestCase[] cases = new TestCase[]
			{
				//           string                 number?    p   i   l   d       iv       lv  dv
				new TestCase("0",                     true,    1,  1,  1,  1,       0,       0,  0.0),
				new TestCase("0x",                    true,    2,  2,  2,  1,       0,       0,  0.0),
				new TestCase("0X",                    true,    2,  2,  2,  1,       0,       0,  0.0),
				new TestCase("00x1",                 false,    2,  2,  2,  2,       0,       0,  0.0),
				new TestCase("0x10",                  true,    4,  4,  4,  1,      16,      16,  16.0),  // dv changed in v2
				new TestCase("-0xf",                  true,    4,  4,  4,  2,     -15,     -15,  -15.0), // dv changed in v2
				new TestCase("00000000013",           true,   11, 11, 11, 11,      13,      13,  13.0),
				new TestCase("00000000",              true,    8,  8,  8,  8,       0,       0,  0.0),
				new TestCase("1",                     true,    1,  1,  1,  1,       1,       1,  1.0),
				new TestCase("0",                     true,    1,  1,  1,  1,       0,       0,  0.0),
				new TestCase("00008",                 true,    5,  5,  5,  5,       8,       8,  8.0),
				new TestCase(IntOvf,                  true,   14, 10, 14, 14,  MaxInt, IntOvfL,  IntOvfD),
				new TestCase(LongOvf,                 true,   LongOvf.Length,  10, 19, LongOvf.Length, MaxInt, MaxLong,  Double.NaN),
				new TestCase(LongHOvf,                true,   LongHOvf.Length, 17, 24, 1, MaxInt, MaxLong, Double.NaN),
				new TestCase(MaxInt.ToString(),       true,   10, 10, 10, 10,  MaxInt,  MaxInt,  MaxInt),
				new TestCase(MinInt.ToString(),       true,   11, 11, 11, 11,  MinInt,  MinInt,  MinInt),
				new TestCase("0.587e5",               true,    7,  1,  1,  7,       0,       0,  58700.0),
				new TestCase("10dfd",                false,    2,  2,  2,  2,      10,      10,  10.0),
				new TestCase("10efd",                false,    2,  2,  2,  2,      10,      10,  10.0),
				new TestCase("10d",                  false,    2,  2,  2,  2,      10,      10,  10.0),
				new TestCase("10e",                  false,    2,  2,  2,  2,      10,      10,  10.0),
				new TestCase("-.14",                 false,    4,  1,  1,  4,       0,       0, -0.14),
				new TestCase(".14",                  false,    3,  0,  0,  3,       0,       0,  0.14),
				new TestCase("+.e2",                 false,    4,  1,  1,  4,       0,       0,  0.0),
				new TestCase("1e10xy",               false,    4,  1,  1,  4,       1,       1,  10000000000.0),
				new TestCase("   ",                  false,    3,  3,  3,  3,       0,       0,  0.0),
				new TestCase("     -",               false,    6,  6,  6,  6,       0,       0,  0.0),
				new TestCase("       d",             false,    7,  7,  7,  7,       0,       0,  0.0),
				new TestCase("  0  ",                false,    3,  3,  3,  3,       0,       0,  0.0),
				new TestCase(" 2545as fsdf",         false,    5,  5,  5,  5,    2545,    2545,  2545.0),
				new TestCase(" 54.dadasdasd",        false,    4,  3,  3,  4,      54,      54,  54.0),
				new TestCase("54. ",                 false,    3,  2,  2,  3,      54,      54,  54.0),
				new TestCase("2.",                    true,    2,  1,  1,  2,       2,       2,  2.0),
				new TestCase("2.e",                  false,    2,  1,  1,  2,       2,       2,  2.0),
				new TestCase("2.e+",                 false,    2,  1,  1,  2,       2,       2,  2.0),
				new TestCase(".",                    false,    1,  0,  0,  1,       0,       0,  0.0),
				new TestCase("+.",                   false,    2,  1,  1,  2,       0,       0,  0.0),
				new TestCase("-.",                   false,    2,  1,  1,  2,       0,       0,  0.0),
				new TestCase("-",                    false,    1,  1,  1,  1,       0,       0,  0.0),
				new TestCase("+",                    false,    1,  1,  1,  1,       0,       0,  0.0),
				new TestCase("",                     false,    0,  0,  0,  0,       0,       0,  0.0),
				new TestCase(null,                   false,    0,  0,  0,  0,       0,       0,  0.0),
				new TestCase("10e1111111111111111",  false,    6,  2,  2,  6,      10,      10,  Double.PositiveInfinity),
				new TestCase("10e-1111111111111111",  true,   20,  2,  2, 20,      10,      10,  0.0),
				new TestCase("0e-1111111111111111",   true,   19,  1,  1, 19,       0,       0,  0.0),    
			};

			[Test]
			static void TestIsNumber()
			{
				foreach (TestCase c in cases)
				{
					int d, i, l, iv, p = 0;
					double dv;
					long lv;
					Convert.NumberInfo info = Core.Convert.IsNumber(c.s, -1, ref p, out i, out l, out d, out iv, out lv, out dv);

					Debug.Assert(c.isnum == ((info & Convert.NumberInfo.IsNumber) != 0));
					Debug.Assert(c.p == p);
					Debug.Assert(c.i == i);
					Debug.Assert(c.l == l);
					Debug.Assert(c.d == d);
					Debug.Assert(c.iv == iv);
					Debug.Assert(Double.IsNaN(c.dv) || c.dv == dv);
					Debug.Assert(c.lv == lv);
				}
			}
		}

#endif
		#endregion

	}

}
