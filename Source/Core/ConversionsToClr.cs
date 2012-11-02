using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using PHP.Library;

namespace PHP.Core
{
	/// <summary>
	/// Implements PHP conversions of CLR types.
	/// </summary>
	[DebuggerNonUserCode]
	public class ConvertToClr
	{
		#region TryObjectToXxx

		/// <summary>
		/// Represents "quality" of conversion
		/// </summary>
		public enum ConversionStrictness : byte
		{
			/// <summary> Type was safely converted to matching type: int to int, long to int (in range) </summary>
			ImplExactMatch = 0,
			/// <summary> Type was converted but percision may be lost: double to float, decimal to double </summary>
			ImplPercisionLost = 1,
			/// <summary> Type was converted but value domain is diferent: string to bool, int to bool, null to (T)null</summary>
			ImplDomainChange = 2,
			/// <summary> Type was covnerted using explicit conversion </summary>
			Explicit = 3,	
			/// <summary> Type was not converted. </summary>
			Failed = 4		
		}
		
		[Emitted]
		public static Boolean TryObjectToBoolean(object obj, out ConversionStrictness strictness)
		{
			string s;
			PhpBytes b;
			PhpString ps;
			
			if (obj is bool) { strictness = ConversionStrictness.ImplExactMatch; return (bool)obj; }
			if (obj is int) { strictness = ConversionStrictness.ImplDomainChange; return (int)obj != 0; }
			if (obj is double) { strictness = ConversionStrictness.ImplDomainChange; return (double)obj != 0.0; }
			if (obj is long) { strictness = ConversionStrictness.ImplDomainChange; return (long)obj != 0; }

			// we have to check PHP string types separately from the rest of IPhpConvertibles here
			// as only these strings are "naturally" convertible to boolean:
			if ((s = obj as string) != null) { strictness = ConversionStrictness.ImplDomainChange; return Convert.StringToBoolean(s); }
			if ((b = obj as PhpBytes) != null) { strictness = ConversionStrictness.ImplDomainChange; return b.ToBoolean(); }
			if ((ps = obj as PhpString) != null) { strictness = ConversionStrictness.ImplDomainChange; return ps.ToBoolean(); }

			// explicit conversion
			// if ((conv = obj as IPhpConvertible) != null) { strictness = ConversionStrictness.Explicit; return conv.ToBoolean(); }
			
			strictness = ConversionStrictness.Failed;
			return false;
		}

		[Emitted]
		public static SByte TryObjectToInt8(object obj, out ConversionStrictness strictness)
		{
			int result = TryObjectToInt32(obj, out strictness);
			if (result < SByte.MinValue || result > SByte.MaxValue) strictness = ConversionStrictness.Failed;
			return unchecked((SByte)result);
		}

		[Emitted]
		public static Int16 TryObjectToInt16(object obj, out ConversionStrictness strictness)
		{
			int result = TryObjectToInt32(obj, out strictness);
			if (result < Int16.MinValue || result > Int16.MaxValue) strictness = ConversionStrictness.Failed;
			return unchecked((Int16)result);
		}

		[Emitted]
		public static Byte TryObjectToUInt8(object obj, out ConversionStrictness strictness)
		{
			int result = TryObjectToInt32(obj, out strictness);
			if (result < Byte.MinValue || result > Byte.MaxValue) strictness = ConversionStrictness.Failed; 
			return unchecked((Byte)result);
		}

		[Emitted]
		public static UInt16 TryObjectToUInt16(object obj, out ConversionStrictness strictness)
		{
			int result = TryObjectToInt32(obj, out strictness);
			if (result < UInt16.MinValue || result > UInt16.MaxValue) strictness = ConversionStrictness.Failed;
			return unchecked((UInt16)result);
		}

		[Emitted]
		public static UInt32 TryObjectToUInt32(object obj, out ConversionStrictness strictness)
		{
			long result = TryObjectToInt64(obj, out strictness);
			if (result < UInt32.MinValue || result > UInt32.MaxValue) strictness = ConversionStrictness.Failed;
			return unchecked((UInt32)result);
		}

		[Emitted]
		public static Int32 TryObjectToInt32(object obj, out ConversionStrictness strictness)
		{
			string s;

			if (obj is int) { strictness = ConversionStrictness.ImplExactMatch; return (int)obj; }
			if (obj is bool) { strictness = ConversionStrictness.ImplDomainChange; return (bool)obj ? 1 : 0; }

			if (obj is long)
			{
				long lval = (long)obj;
				if (lval < Int32.MinValue || lval > Int32.MaxValue)
					strictness = ConversionStrictness.Failed;
				else
					strictness = ConversionStrictness.ImplExactMatch;
				return unchecked((Int32)lval);
			}

			if (obj is double)
			{
				double dval = (double)obj;
				if (dval < Int32.MinValue || dval > Int32.MaxValue)
					strictness = ConversionStrictness.Failed;
				else
					strictness = ConversionStrictness.ImplPercisionLost;
				return unchecked((Int32)dval);
			}

			if ((s = PhpVariable.AsString(obj)) != null)
			{
				int ival;
				double dval;
				long lval;

				// successfull iff the number encoded in the string fits the Int32:
				Convert.NumberInfo info = Convert.StringToNumber(s, out ival, out lval, out dval);
				if ((info & Convert.NumberInfo.Integer) != 0)
				{ strictness = ConversionStrictness.ImplDomainChange; return ival; } // "123 hello world" -> 123 (for example)

				strictness = ConversionStrictness.Failed;
				return unchecked((Int32)lval);
			}

			// explicit conversion
			/*IPhpConvertible conv;
			if ((conv = obj as IPhpConvertible) != null) 
			{
				int ival;
				double dval;
				long lval;

				Convert.NumberInfo info = conv.ToNumber(out ival, out lval, out dval);
				if ((info & (Convert.NumberInfo.Integer | Convert.NumberInfo.IsNumber)) ==
					(Convert.NumberInfo.Integer | Convert.NumberInfo.IsNumber))
				{
					strictness = ConversionStrictness.Explicit; 
					return ival;
				}

				strictness = ConversionStrictness.Failed;
				return unchecked((Int32)lval);
			}*/

			strictness = ConversionStrictness.Failed;
			return 0;
		}

		[Emitted]
		public static Int64 TryObjectToInt64(object obj, out ConversionStrictness strictness)
		{
			string s;

			if (obj is int) { strictness = ConversionStrictness.ImplExactMatch; return (int)obj; }
			if (obj is long) { strictness = ConversionStrictness.ImplExactMatch; return (long)obj; }
			if (obj is bool) { strictness = ConversionStrictness.ImplDomainChange; return (bool)obj ? 1 : 0; }

			if (obj is double)
			{
				double dval = (double)obj;
				if (dval < Int64.MinValue || dval > Int64.MaxValue)
					strictness = ConversionStrictness.Failed;
				else
					strictness = ConversionStrictness.ImplPercisionLost;
				return unchecked((Int32)dval);
			}

			if ((s = PhpVariable.AsString(obj)) != null)
			{
				int ival;
				double dval;
				long lval;

				// successfull iff the number encoded in the string fits Int32 or Int64:
				Convert.NumberInfo info = Convert.StringToNumber(s, out ival, out lval, out dval);
				if ((info & Convert.NumberInfo.Integer) != 0)
				{ strictness = ConversionStrictness.ImplDomainChange; return ival; }
				if ((info & Convert.NumberInfo.LongInteger) != 0)
				{ strictness = ConversionStrictness.ImplDomainChange; return lval; }

				strictness = ConversionStrictness.Failed;
				return unchecked((Int64)dval);
			}

			// explicit conversion
			/*IPhpConvertible conv;
			if ((conv = obj as IPhpConvertible) != null)
			{
				int ival;
				double dval;
				long lval;

				Convert.NumberInfo info = conv.ToNumber(out ival, out lval, out dval);
				if ((info & Convert.NumberInfo.Integer) != 0)
				{ strictness = ConversionStrictness.Explicit; return ival; }
				if ((info & Convert.NumberInfo.LongInteger) != 0)
				{ strictness = ConversionStrictness.Explicit; return lval; }

				strictness = ConversionStrictness.Failed;
				return unchecked((Int64)dval);
			}*/

			strictness = ConversionStrictness.Failed;
			return 0;
		}

		[Emitted]
		public static UInt64 TryObjectToUInt64(object obj, out ConversionStrictness strictness)
		{
			string s;

			if (obj is int)
			{
				int ival = (int)obj;
				strictness = ival >= 0 ? 
					ConversionStrictness.ImplExactMatch : ConversionStrictness.Failed;
				return unchecked((UInt64)ival);
			}

			if (obj is long)
			{
				long lval = (long)obj;
				strictness = lval >= 0 ? 
					ConversionStrictness.ImplExactMatch : ConversionStrictness.Failed;
				return unchecked((UInt64)lval);
			}

			if (obj is bool)
			{
				strictness = ConversionStrictness.ImplDomainChange;
				return (ulong)((bool)obj ? 1 : 0);
			}

			if (obj is double)
			{
				double dval = (double)obj;
				strictness = (dval >= UInt64.MinValue && dval <= UInt64.MaxValue) ?
					ConversionStrictness.ImplPercisionLost : ConversionStrictness.Failed;
				return unchecked((UInt64)dval);
			}

			if ((s = PhpVariable.AsString(obj)) != null)
			{
				int ival;
				double dval;
				long lval;

				// successfull iff the number encoded in the string fits Int32 or Int64:
				Convert.NumberInfo info = Convert.StringToNumber(s, out ival, out lval, out dval);
				if ((info & Convert.NumberInfo.Integer) != 0)
				{ strictness = ConversionStrictness.ImplDomainChange; return unchecked((UInt64)ival); }
				if ((info & Convert.NumberInfo.LongInteger) != 0)
				{ strictness = ConversionStrictness.ImplDomainChange; return unchecked((UInt64)lval); }

				strictness = (dval >= UInt64.MinValue && dval <= UInt64.MaxValue) ?
					ConversionStrictness.ImplPercisionLost : ConversionStrictness.Failed;
				return unchecked((UInt64)dval);
			}

			// explicit conversion
			/*IPhpConvertible conv;
			if ((conv = obj as IPhpConvertible) != null)
			{
				int ival;
				double dval;
				long lval;

				Convert.NumberInfo info = conv.ToNumber(out ival, out lval, out dval);
				if ((info & Convert.NumberInfo.Integer) != 0)
				{ strictness = ConversionStrictness.Explicit; return unchecked((UInt64)ival); }
				if ((info & Convert.NumberInfo.LongInteger) != 0)
				{ strictness = ConversionStrictness.Explicit; return unchecked((UInt64)lval); }

				strictness = (dval >= UInt64.MinValue && dval <= UInt64.MaxValue) ?
					ConversionStrictness.Explicit : ConversionStrictness.Failed;
				return unchecked((UInt64)dval);
			}*/

			strictness = ConversionStrictness.Failed;
			return 0;
		}

		[Emitted]
		public static Single TryObjectToSingle(object obj, out ConversionStrictness strictness)
		{
			double result = TryObjectToDouble(obj, out strictness);
			strictness = (ConversionStrictness)Math.Min((byte)ConversionStrictness.ImplPercisionLost, (byte)strictness);
			if (result < Single.MinValue && result > Single.MaxValue) strictness = ConversionStrictness.Failed;
			return unchecked((Single)result);
		}

		[Emitted]
		public static Double TryObjectToDouble(object obj, out ConversionStrictness strictness)
		{
			string s;

			if (obj is double) { strictness = ConversionStrictness.ImplExactMatch; return (double)obj; }
			if (obj is int) { strictness = ConversionStrictness.ImplExactMatch; return (double)(int)obj; }
			if ((s = PhpVariable.AsString(obj)) != null) { strictness = ConversionStrictness.ImplDomainChange; return Convert.StringToDouble(s); }
			if (obj is bool) { strictness = ConversionStrictness.ImplDomainChange; return (bool)obj ? 1.0 : 0.0; }
			if (obj is long) { strictness = ConversionStrictness.ImplExactMatch; return (double)(long)obj; }

			strictness = ConversionStrictness.Failed;
			return 0.0;
		}

		[Emitted]
		public static Decimal TryObjectToDecimal(object obj, out ConversionStrictness strictness)
		{
			int ival;
			long lval;
			double dval;

			// ignores the higher precision of decimal:
			decimal ret = 0;
			Convert.NumberInfo ni = Convert.ObjectToNumber(obj, out ival, out lval, out dval);
			switch (ni & Convert.NumberInfo.TypeMask)
			{
				case Convert.NumberInfo.Integer: { strictness = ConversionStrictness.ImplExactMatch; ret = ival; break; }
				case Convert.NumberInfo.LongInteger: { strictness = ConversionStrictness.ImplExactMatch; ret = lval; break; }
				case Convert.NumberInfo.Double: { strictness = ConversionStrictness.ImplPercisionLost; ret = unchecked((decimal)dval); break; }
				case Convert.NumberInfo.Unconvertible: { strictness = ConversionStrictness.Failed; ret = 0; break; }
				default: Debug.Fail(); throw null;
			}
			if (obj is string) strictness = ConversionStrictness.ImplDomainChange;
			if (obj is bool) strictness = ConversionStrictness.ImplDomainChange;
			if (obj is IPhpConvertible) strictness = ConversionStrictness.Explicit;
			if (obj == null) strictness = ConversionStrictness.ImplDomainChange;
			return ret;
		}

		[Emitted]
		public static Char TryObjectToChar(object obj, out ConversionStrictness strictness)
		{
			string result = TryObjectToString(obj, out strictness);
            if (result != null && result.Length == 1) { strictness = ConversionStrictness.ImplExactMatch; return result[0]; }

			strictness = ConversionStrictness.Failed;
			return '\0';
		}

		[Emitted]
		public static String TryObjectToString(object obj, out ConversionStrictness strictness)
		{
			string s;
			PhpReference pr;
			IPhpConvertible conv;

			if ((s = PhpVariable.AsString(obj)) != null) { strictness = ConversionStrictness.ImplExactMatch; return s; }
			if ((pr = obj as PhpReference) != null) return TryObjectToString(pr.Value, out strictness);
			if (obj == null) { strictness = ConversionStrictness.ImplDomainChange; return null; }
			if (obj is int) { strictness = ConversionStrictness.ImplDomainChange; return obj.ToString(); }
			if (obj is bool) { strictness = ConversionStrictness.ImplDomainChange; return ((bool)obj) ? "1" : String.Empty; }
			if (obj is double) { strictness = ConversionStrictness.ImplDomainChange; return Convert.DoubleToString((double)obj); }
			if (obj is long) { strictness = ConversionStrictness.ImplDomainChange; return obj.ToString(); }

			// explicit conversion
			if ((conv = obj as IPhpConvertible) != null)
			{
				bool success;
				string ret = conv.ToString(false, out success);
				strictness = success?ConversionStrictness.Failed:ConversionStrictness.Explicit;
				return ret;
			} 
			
			strictness = ConversionStrictness.Failed; 
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
		/// <param name="strictness"></param>
		/// <returns></returns>
		[Emitted]
		public static DateTime TryObjectToDateTime(object obj, out ConversionStrictness strictness)
		{
            // try wrapped DateTime:
            var exactMatch = obj as Reflection.ClrValue<DateTime>;
            if (exactMatch != null)
            {
                strictness = ConversionStrictness.ImplExactMatch;
                return exactMatch.realValue;
            }

            // try obj -> String -> DateTime
			string str = TryObjectToString(obj, out strictness);

			if (strictness != ConversionStrictness.Failed)
			{
				DateTime result;
#if !SILVERLIGHT
				if (DateTime.TryParse(str, out result))
#else
				// TODO: Any way to optimize this?
				result = default(DateTime);
				bool success = true;
				try { result = DateTime.Parse(str); } catch { success = false; }
				if (success)
#endif
                { strictness = ConversionStrictness.ImplDomainChange; return result; }
			}

			strictness = ConversionStrictness.Failed;
			return new DateTime();
		}

		/// <summary>
		/// Converts to <see cref="DBNull"/>. 
		/// The conversion is always successful and results to the <see cref="DBNull.Value"/> singleton.
		/// </summary>
		[Emitted]
		public static DBNull TryObjectToDBNull(object obj, out ConversionStrictness strictness)
		{
			strictness = ConversionStrictness.ImplDomainChange;
			return DBNull.Value;
		}

		[Emitted]
		public static T TryObjectToClass<T>(object obj, out ConversionStrictness strictness)
			where T : class
		{
			if (obj == null) { strictness = ConversionStrictness.ImplDomainChange; return null; }

			T result = null;
			if ((result = PhpVariable.Unwrap(obj) as T) != null && (!(result is IPhpVariable) || result is PhpObject || result is PhpArray))
			{
				strictness = ConversionStrictness.ImplExactMatch; 
				return result;
			}

			strictness = ConversionStrictness.Failed;
			return default(T);
		}

		[Emitted]
		public static T TryObjectToDelegate<T>(object obj, out ConversionStrictness strictness)
			where T : class
		{
			T result = null;
			object bare_obj = PhpVariable.Unwrap(obj);
			if (bare_obj == null || (result = bare_obj as T) != null)
			{
				strictness = ConversionStrictness.ImplExactMatch;
				return result;
			}

			// try to convert the object to PhpCallback
			PhpCallback callback = Convert.ObjectToCallback(obj, true);
			if (callback != null && callback.Bind(true))
			{
				// generate a conversion stub
				result = EventClass<T>.GetStub(
					callback.TargetInstance,
					callback.TargetRoutine,
					callback.IsBoundToCaller ? callback.RoutineName : null);

				if (result != null)
				{
					strictness = ConversionStrictness.ImplExactMatch;
					return result;
				}
			}

			strictness = ConversionStrictness.Failed;
			return default(T);
		}

		[Emitted]
		public static T[] TryObjectToArray<T>(object obj, out ConversionStrictness strictness)
		{
			T[] result = PhpVariable.Unwrap(obj) as T[];
			if (result != null)
			{
				strictness = ConversionStrictness.ImplExactMatch;
				return result;
			}

			// try to convert PhpArray to the desired array
			PhpArray array = obj as PhpArray;
			if (array != null && array.StringCount == 0)
			{
				result = new T[array.MaxIntegerKey + 1];

				strictness = ConversionStrictness.ImplExactMatch;
				for (int i = 0; i < result.Length; i++)
				{
					object item;
					if (array.TryGetValue(i, out item))
					{
						// try to convert the item
						ConversionStrictness tmp;
						result[i] = TryObjectToType<T>(item, out tmp);
						if (tmp > strictness) strictness = tmp;
						if (strictness == ConversionStrictness.Failed) return default(T[]);
					}
				}

				return result;
			}

			strictness = ConversionStrictness.Failed;
			return default(T[]);
		}

		[Emitted]
		public static T TryObjectToStruct<T>(object obj, out ConversionStrictness strictness)
			where T : struct
		{
			obj = PhpVariable.Unwrap(obj);
			if (obj is T)
			{
				strictness = ConversionStrictness.ImplExactMatch;
				return (T)obj;
			}
			else
			{
				strictness = ConversionStrictness.Failed;
				return default(T);
			}
		}

		private delegate T TryObjectToTypeDelegate<T>(object obj, out ConversionStrictness strictness);

		/// <summary>
		/// Used when the type is unknown at compiler-time, e.g. it is a generic parameter.
		/// </summary>
		[Emitted]
		public static T TryObjectToType<T>(object obj, out ConversionStrictness strictness)
		{
            return ((TryObjectToTypeDelegate<T>)GetConversionRoutine(typeof(T)))(obj, out strictness);
		}

        /// <summary>
        /// Used when the type is unknown at compiler-time, and must be converted during the runtime.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="target_type"></param>
        /// <returns></returns>
        public static object ObjectToType(object obj, Type target_type)
        {
            if (obj != null && obj.GetType() == target_type)
                return obj;
            
            Delegate conversion_routine = GetConversionRoutine(target_type);

            object result = conversion_routine.DynamicInvoke(new object[]{obj, null});
            if(obj != null && result == null)
            {
                throw new ArgumentOutOfRangeException("target_type", "Cannot convert to type " + target_type.ToString());
            }

            return result;
        }

		/// <summary>
		/// Stores instances of <see cref="TryObjectToTypeDelegate{T}"/>.
		/// </summary>
		private static Dictionary<Type, Delegate> conversionRoutines = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Get the proper instance of conversion routine delegate for the given type.
        /// </summary>
        /// <param name="target_type">Target type of the conversion.</param>
        /// <returns>Proper instance of conversion routine delegate.</returns>
        private static Delegate GetConversionRoutine(Type target_type)
        {
            Delegate conversion_routine;

            lock (conversionRoutines)
            {
                if (!conversionRoutines.TryGetValue(target_type, out conversion_routine))
                {
                    conversion_routine = CreateConversionDelegate(target_type);
                    conversionRoutines.Add(target_type, conversion_routine);
                }
            }

            return conversion_routine;
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
							generic_method = Emit.Methods.ConvertToClr.TryObjectToStruct;
						}
						else
						{
							if (targetType.IsArray)
							{
								generic_arg = targetType.GetElementType();
								generic_method = Emit.Methods.ConvertToClr.TryObjectToArray;
							}
							else
							{
								generic_arg = targetType;
								if (typeof(Delegate).IsAssignableFrom(targetType))
								{
									generic_method = Emit.Methods.ConvertToClr.TryObjectToDelegate;
								}
								else
								{
									generic_method = Emit.Methods.ConvertToClr.TryObjectToClass;
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

		#region Nullable Conversions

		/// <summary>
		/// This is handled specially in ClrOverloadBuilder...
		/// </summary>
		[Emitted]
		public static object UnwrapNullable(object obj)
		{
			obj = PhpVariable.Unwrap(obj);
			if (obj == null) return null;

			// This could be done more efficiently, but it is not used very often...
			Type ty = obj.GetType();
			if (ty.IsGenericType && ty.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				bool val = (bool)ty.GetProperty("HasValue").GetGetMethod().Invoke(obj, Type.EmptyTypes);
				if (!val) return null;
				return ty.GetProperty("Value").GetGetMethod().Invoke(obj, Type.EmptyTypes);
			}
			else
				return obj;
		}

		#endregion
	}
}
