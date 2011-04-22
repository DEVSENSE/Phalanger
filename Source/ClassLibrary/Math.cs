/*

 Copyright (c) 2004-2006 Jan Benda and Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Text;
using System.Collections;
using System.ComponentModel;

using PHP.Core;

#if SILVERLIGHT
using PHP.CoreCLR;
using MathEx = PHP.CoreCLR.MathEx;
#else
using MathEx = System.Math;
#endif

namespace PHP.Library
{
	/// <summary>
	/// Implements PHP mathematical functions and constants.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class PhpMath
	{
		#region Per-request Random Number Generators

		/// <summary>
		/// Gets an initialized random number generator associated with the current thread.
		/// </summary>
		internal static Random Generator
		{
			get
			{
				if (_generator == null)
					_generator = new Random(unchecked((int)System.DateTime.Now.ToFileTime()));
				return _generator;
			}
		}
#if !SILVERLIGHT
		[ThreadStatic]
#endif
		private static Random _generator;

		/// <summary>
		/// Gets an initialized Mersenne Twister random number generator associated with the current thread.
		/// </summary>
		internal static MersenneTwister MTGenerator
		{
			get
			{
				if (_mtGenerator == null)
					_mtGenerator = new MersenneTwister(unchecked((uint)System.DateTime.Now.ToFileTime()));
				return _mtGenerator;
			}
		}
#if !SILVERLIGHT
		[ThreadStatic]
#endif
		private static MersenneTwister _mtGenerator;

		/// <summary>
		/// Registers <see cref="ClearGenerators"/> routine to be called on request end.
		/// </summary>
		static PhpMath()
		{
            RequestContext.RequestEnd += new Action(ClearGenerators);
		}

		/// <summary>
		/// Nulls <see cref="_generator"/> and <see cref="_mtGenerator"/> fields on request end.
		/// </summary>
		private static void ClearGenerators()
		{
			_generator = null;

			if (_mtGenerator != null)
				_mtGenerator.Seed(unchecked((uint)System.DateTime.Now.ToFileTime()));
		}

		#endregion

		#region Constants

		[ImplementsConstant("M_PI")]
		public const double Pi = System.Math.PI;
		[ImplementsConstant("M_E")]
		public const double E = System.Math.E;
		[ImplementsConstant("M_LOG2E")]
		public const double Log2e = 1.4426950408889634074;
		[ImplementsConstant("M_LOG10E")]
		public const double Log10e = 0.43429448190325182765;
		[ImplementsConstant("M_LN2")]
		public const double Ln2 = 0.69314718055994530942;
		[ImplementsConstant("M_LN10")]
		public const double Ln10 = 2.30258509299404568402;
		[ImplementsConstant("M_PI_2")]
		public const double PiHalf = 1.57079632679489661923;
		[ImplementsConstant("M_PI_4")]
		public const double PiFourth = 0.78539816339744830962;
		[ImplementsConstant("M_1_PI")]
		public const double Pith = 0.31830988618379067154;
		[ImplementsConstant("M_2_PI")]
		public const double TwoPiths = 0.63661977236758134308;
		[ImplementsConstant("M_SQRTPI")]
		public const double SqrtPi = 1.77245385090551602729;
		[ImplementsConstant("M_2_SQRTPI")]
		public const double TwoSqrtPi = 1.12837916709551257390;
		[ImplementsConstant("M_SQRT3")]
		public const double Sqrt3 = 1.73205080756887729352;
		[ImplementsConstant("M_SQRT1_2")]
		public const double SqrtHalf = 0.70710678118654752440;
		[ImplementsConstant("M_LNPI")]
		public const double LnPi = 1.14472988584940017414;
		[ImplementsConstant("M_EULER")]
		public const double Euler = 0.57721566490153286061;
		[ImplementsConstant("NAN")]
		public const double NaN = Double.NaN;
		[ImplementsConstant("INF")]
		public const double Infinity = Double.PositiveInfinity;

		#endregion

		#region Absolutize Range

		/// <summary>
		/// Absolutizes range specified by an offset and a length relatively to a dimension of an array.
		/// </summary>
		/// <param name="count">The number of items in array. Should be non-negative.</param>
		/// <param name="offset">
		/// The offset of the range relative to the beginning (if non-negative) or the end of the array (if negative).
		/// If the offset underflows or overflows the length is shortened appropriately.
		/// </param>
		/// <param name="length">
		/// The length of the range if non-negative. Otherwise, its absolute value is the number of items
		/// which will not be included in the range from the end of the array. In the latter case 
		/// the range ends with the |<paramref name="length"/>|-th item from the end of the array (counting from zero).
		/// </param>
		/// <remarks>
		/// Ensures that <c>[offset,offset + length]</c> is subrange of <c>[0,count]</c>.
		/// </remarks>
		public static void AbsolutizeRange(ref int offset, ref int length, int count)
		{
			Debug.Assert(count >= 0);

			// prevents overflows:
			if (offset >= count || count == 0)
			{
				offset = count;
				length = 0;
				return;
			}

			// negative offset => offset is relative to the end of the string:
			if (offset < 0)
			{
				offset += count;
				if (offset < 0) offset = 0;
			}

			Debug.Assert(offset >= 0 && offset < count);

			if (length < 0)
			{
				// there is count-offset items from offset to the end of array,
				// the last |length| items is taken away:
				length = count - offset + length;
				if (length < 0) length = 0;
			}
			else if ((long)offset + length > count)
			{
				// interval ends on the end of array:
				length = count - offset;
			}

			Debug.Assert(length >= 0 && offset + length <= count);
		}

		#endregion

		#region rand, srand, getrandmax, uniqid, lcg_value

        /// <summary>
        /// Seed the random number generator. No return value.
        /// </summary>
        [ImplementsFunction("srand")]
        public static void Seed()
        {
            _generator = new Random();
        }

        /// <summary>
        /// Seed the random number generator. No return value.
        /// </summary>
        /// <param name="seed">Optional seed value.</param>
		[ImplementsFunction("srand")]
		public static void Seed(int seed)
		{
			_generator = new Random(seed);
		}

        /// <summary>
        /// Show largest possible random value.
        /// </summary>
        /// <returns>The largest possible random value returned by rand().</returns>
		[ImplementsFunction("getrandmax")]
		public static int GetMaxRandomValue()
		{
			return Int32.MaxValue;
		}

        /// <summary>
        /// Generate a random integer.
        /// </summary>
        /// <returns>A pseudo random value between 0 and getrandmax(), inclusive.</returns>
		[ImplementsFunction("rand")]
		public static int Random()
		{
			return Generator.Next();
		}

        /// <summary>
        /// Generate a random integer.
        /// </summary>
        /// <param name="min">The lowest value to return.</param>
        /// <param name="max">The highest value to return.</param>
        /// <returns>A pseudo random value between min and max, inclusive. </returns>
		[ImplementsFunction("rand")]
		public static int Random(int min, int max)
		{
			return (min < max) ? Generator.Next(min, max) : Generator.Next(max, min);
		}

        /// <summary>
        /// Generate a unique ID.
        /// Gets a prefixed unique identifier based on the current time in microseconds. 
        /// </summary>
        /// <returns>Returns the unique identifier, as a string.</returns>
		[ImplementsFunction("uniqid")]
		public static string UniqueId()
		{
			return UniqueId(null, false);
		}

        /// <summary>
        /// Generate a unique ID.
        /// Gets a prefixed unique identifier based on the current time in microseconds. 
        /// </summary>
        /// <param name="prefix">Can be useful, for instance, if you generate identifiers simultaneously on several hosts that might happen to generate the identifier at the same microsecond.
        /// With an empty prefix , the returned string will be 13 characters long.
        /// </param>
        /// <returns>Returns the unique identifier, as a string.</returns>
		[ImplementsFunction("uniqid")]
		public static string UniqueId(string prefix)
		{
			return UniqueId(prefix, false);
		}

		/// <summary>
		/// Generate a unique ID.
		/// </summary>
		/// <remarks>
        /// With an empty prefix, the returned string will be 13 characters long. If more_entropy is TRUE, it will be 23 characters.
		/// </remarks>
		/// <param name="prefix">Use the specified prefix.</param>
		/// <param name="more_entropy">Use LCG to generate a random postfix.</param>
		/// <returns>A pseudo-random string composed from the given prefix, current time and a random postfix.</returns>
		[ImplementsFunction("uniqid")]
		public static string UniqueId(string prefix, bool more_entropy)
		{
			// Note that Ticks specify time in 100nanoseconds but it is raised each 100144 
			// ticks which is around 10 times a second (the same for Milliseconds).
			string ticks = String.Format("{0:X}", DateTime.Now.Ticks + Generator.Next());

			ticks = ticks.Substring(ticks.Length - 13);
			if (prefix == null) prefix = "";
			if (more_entropy)
			{
				string rnd = LcgValue().ToString();
				rnd = rnd.Substring(2, 8);
				return String.Format("{0}{1}.{2}", prefix, ticks, rnd);
			}
			else return String.Format("{0}{1}", prefix, ticks);
		}

		/// <summary>
		/// Generates a pseudo-random number using linear congruential generator in the range of (0,1).
		/// </summary>
		/// <remarks>
		/// This method uses the Framwork <see cref="Random"/> generator
		/// which may or may not be the same generator as the PHP one (L(CG(2^31 - 85),CG(2^31 - 249))).
		/// </remarks>
		/// <returns></returns>
		[ImplementsFunction("lcg_value")]
		public static double LcgValue()
		{
			return Generator.NextDouble();
		}

		#endregion

		#region mt_getrandmax, mt_rand, mt_srand

		[ImplementsFunction("mt_getrandmax")]
		public static int MtGetMaxRandomValue()
		{
			return Int32.MaxValue;
		}

		[ImplementsFunction("mt_rand")]
		public static int MtRandom()
		{
			return MTGenerator.Next();
		}

		[ImplementsFunction("mt_rand")]
		public static int MtRandom(int min, int max)
		{
			return (min < max) ? MTGenerator.Next(min, max) : MTGenerator.Next(max, min);
		}

        /// <summary>
        /// Seed the better random number generator.
        /// No return value.
        /// </summary>
        [ImplementsFunction("mt_srand")]
        public static void MtSeed()
        {
            MtSeed(Generator.Next());
        }

        /// <summary>
        /// Seed the better random number generator.
        /// No return value.
        /// </summary>
        /// <param name="seed">Optional seed value.</param>
		[ImplementsFunction("mt_srand")]
		public static void MtSeed(int seed)
		{
			MTGenerator.Seed(unchecked((uint)seed));
		}

		#endregion

		#region is_nan,is_finite,is_infinite

		[ImplementsFunction("is_nan")]
		[PureFunction]
        public static bool IsNaN(double x)
		{
			return Double.IsNaN(x);
		}

		[ImplementsFunction("is_finite")]
        [PureFunction]
        public static bool IsFinite(double x)
		{
			return !Double.IsInfinity(x);
		}

		[ImplementsFunction("is_infinite")]
        [PureFunction]
        public static bool IsInfinite(double x)
		{
			return Double.IsInfinity(x);
		}

		#endregion

		#region decbin, bindec, decoct, octdec, dechex, hexdec, base_convert

		/// <summary>
		/// Converts the given number to int (if the number is whole
		/// and fits into the int's range).
		/// </summary>
		/// <param name="number"></param>
		/// <returns><c>int</c> representation of number if possible, otherwise a <c>double</c> representation.</returns>
		private static object ConvertToInt(double number)
		{
			if ((Math.Round(number) == number) && (number <= int.MaxValue) && (number >= int.MinValue))
			{
				return (int)number;
			}
			return number;
		}

		/// <summary>
		/// Converts the lowest 32 bits of the given number to a binary string.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		[ImplementsFunction("decbin")]
		public static PhpBytes DecToBin(double number)
		{
			// Trim the number to the lower 32 binary digits.
			uint temp = unchecked((uint)number);
			return DoubleToBase(temp, 2);
		}

        /// <summary>
        /// Converts the lowest 32 bits of the given number to a binary string.
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        [ImplementsFunction("decbin_unicode")]
        public static string DecToBinUnicode(double number)
        {
            // Trim the number to the lower 32 binary digits.
            uint temp = unchecked((uint)number);
            return DoubleToBaseUnicode(temp, 2);
        }

		[ImplementsFunction("bindec")]
		public static object BinToDec(PhpBytes str)
		{
			if (str == null) return 0;
			return ConvertToInt(BaseToDouble(str, 2));
		}


        [ImplementsFunction("bindec_unicode")]
        public static object BinToDecUnicode(string str)
        {
            if (str == null) return 0;
            return ConvertToInt(BaseToDoubleUnicode(str, 2));
        }

        [ImplementsFunction("decoct")]
        public static PhpBytes DecToOct(int number)
        {
            return new PhpBytes(System.Convert.ToString(number, 8));
        }

		[ImplementsFunction("decoct_unicode")]
		public static string DecToOctUnicode(int number)
		{
			return System.Convert.ToString(number, 8);
		}

        [ImplementsFunction("octdec")]
        public static object OctToDec(PhpBytes str)
        {
            if (str == null) return 0;
            return ConvertToInt(BaseToDouble(str, 8));
        }

		[ImplementsFunction("octdec_unicode")]
		public static object OctToDecUnicode(string str)
		{
			if (str == null) return 0;
			return ConvertToInt(BaseToDoubleUnicode(str, 8));
		}

		[ImplementsFunction("dechex")]
		public static PhpBytes DecToHex(int number)
		{
			return new PhpBytes(System.Convert.ToString(number, 16));
		}

        [ImplementsFunction("dechex_unicode")]
        public static string DecToHexUnicode(int number)
        {
            return System.Convert.ToString(number, 16);
        }

        [ImplementsFunction("hexdec")]
        public static object HexToDec(PhpBytes str)
        {
            if (str == null) return 0;
            return ConvertToInt(BaseToDouble(str, 16));
        }

		[ImplementsFunction("hexdec_unicode")]
		public static object HexToDecUnicode(string str)
		{
			if (str == null) return 0;
			return ConvertToInt(BaseToDoubleUnicode(str, 16));
		}

        public static double BaseToDouble(PhpBytes number, int fromBase)
        {
            if (number == null)
            {
                PhpException.ArgumentNull("number");
                return 0.0;
            }

            if (fromBase < 2 || fromBase > 36)
            {
                PhpException.InvalidArgument("toBase", LibResources.GetString("arg:out_of_bounds"));
                return 0.0;
            }

            double fnum = 0;
            for (int i = 0; i < number.Length; i++)
            {
                int digit = Core.Convert.AlphaNumericToDigit((char)number.ReadonlyData[i]);
                if (digit < fromBase)
                    fnum = fnum * fromBase + digit;
            }

            return fnum;
        }


		public static double BaseToDoubleUnicode(string number, int fromBase)
		{
			if (number == null)
			{
				PhpException.ArgumentNull("number");
				return 0.0;
			}

			if (fromBase < 2 || fromBase > 36)
			{
				PhpException.InvalidArgument("toBase", LibResources.GetString("arg:out_of_bounds"));
				return 0.0;
			}

			double fnum = 0;
			for (int i = 0; i < number.Length; i++)
			{
				int digit = Core.Convert.AlphaNumericToDigit(number[i]);
				if (digit < fromBase)
					fnum = fnum * fromBase + digit;
			}

			return fnum;
		}

        private const string digitsUnicode = "0123456789abcdefghijklmnopqrstuvwxyz";
        private static byte[] digits = new byte[] {(byte)'0',(byte)'1',(byte)'2',(byte)'3',(byte)'4',(byte)'5',(byte)'6',(byte)'7',(byte)'8',(byte)'9',
            (byte)'a',(byte)'b',(byte)'c',(byte)'d',(byte)'e',(byte)'f',(byte)'g',(byte)'h',(byte)'i',(byte)'j',(byte)'k',(byte)'l',(byte)'m',(byte)'n',
            (byte)'o',(byte)'p',(byte)'q',(byte)'r',(byte)'s',(byte)'t',(byte)'u',(byte)'v',(byte)'w',(byte)'x',(byte)'y',(byte)'z' };
     
        public static PhpBytes DoubleToBase(double number, int toBase)
        {
            if (toBase < 2 || toBase > 36)
            {
                PhpException.InvalidArgument("toBase", LibResources.GetString("arg:out_of_bounds"));
                return PhpBytes.Empty;
            }

            // Don't try to convert infinity or NaN:
            if (Double.IsInfinity(number) || Double.IsNaN(number))
            {
                PhpException.InvalidArgument("number", LibResources.GetString("arg:out_of_bounds"));
                return PhpBytes.Empty;
            }

            double fvalue = Math.Floor(number); /* floor it just in case */
            if (Math.Abs(fvalue) < 1) return new PhpBytes(new byte[]{(byte)'0'});

            System.Collections.Generic.List<byte> sb = new System.Collections.Generic.List<byte>();
            while (Math.Abs(fvalue) >= 1)
            {
                double mod = Fmod(fvalue, toBase);
                int i = (int)mod;
                byte b = digits[i];
                //sb.Append(digits[(int) fmod(fvalue, toBase)]);
                sb.Add(b);
                fvalue /= toBase;
            }

            sb.Reverse();

            return new PhpBytes(sb.ToArray());
        }

		public static string DoubleToBaseUnicode(double number, int toBase)
		{
			if (toBase < 2 || toBase > 36)
			{
				PhpException.InvalidArgument("toBase", LibResources.GetString("arg:out_of_bounds"));
				return String.Empty;
			}

			// Don't try to convert infinity or NaN:
			if (Double.IsInfinity(number) || Double.IsNaN(number))
			{
				PhpException.InvalidArgument("number", LibResources.GetString("arg:out_of_bounds"));
				return String.Empty;
			}

			double fvalue = Math.Floor(number); /* floor it just in case */
			if (Math.Abs(fvalue) < 1) return "0";

			StringBuilder sb = new StringBuilder();
			while (Math.Abs(fvalue) >= 1)
			{
				double mod = Fmod(fvalue, toBase);
				int i = (int)mod;
				char c = digitsUnicode[i];
				//sb.Append(digits[(int) fmod(fvalue, toBase)]);
				sb.Append(c);
				fvalue /= toBase;
			}

			return PhpStrings.Reverse(sb.ToString());
		}

		[ImplementsFunction("base_convert")]
		[return: CastToFalse]
		public static string BaseConvert(string number, int fromBase, int toBase)
		{
			double value;
			if (number == null) return "0";
			try
			{
				value = BaseToDoubleUnicode(number, fromBase);
			}
			catch (ArgumentException)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:invalid_value", "fromBase", fromBase));
				return null;
			}
			try
			{
				return DoubleToBaseUnicode(value, toBase);
			}
			catch (ArgumentException)
			{
				PhpException.Throw(PhpError.Warning, LibResources.GetString("arg:invalid_value", "toBase", toBase));
				return null;
			}
		}

		#endregion

		#region deg2rad, pi, cos, sin, tan, acos, asin, atan, atan2

		/// <summary>
		/// Degrees to radians.
		/// </summary>
		/// <param name="degrees"></param>
		/// <returns></returns>
		[ImplementsFunction("deg2rad"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
		public static double DegreesToRadians(double degrees)
		{
			return degrees / 180 * Math.PI;
		}

		/// <summary>
		/// Radians to degrees.
		/// </summary>
		/// <param name="radians"></param>
		/// <returns></returns>
		[ImplementsFunction("rad2deg")]
        [PureFunction]
        public static double RadiansToDegrees(double radians)
		{
			return radians / Math.PI * 180;
		}

		[ImplementsFunction("pi"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double PI()
		{
			return Math.PI;
		}

		[ImplementsFunction("acos"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Acos(double x)
		{
			return Math.Acos(x);
		}

		[ImplementsFunction("asin"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Asin(double x)
		{
			return Math.Asin(x);
		}

		[ImplementsFunction("atan"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Atan(double x)
		{
			return Math.Atan(x);
		}

		[ImplementsFunction("atan2")]
        [PureFunction]
        public static double Atan2(double y, double x)
		{
			double rv = Math.Atan(y / x);
			if (x < 0)
			{
				return ((rv > 0) ? -Math.PI : Math.PI) + rv;
			}
			else return rv;
		}

		[ImplementsFunction("cos"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Cos(double x)
		{
			return Math.Cos(x);
		}

		[ImplementsFunction("sin"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Sin(double x)
		{
			return Math.Sin(x);
		}

		[ImplementsFunction("tan"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Tan(double x)
		{
			return Math.Tan(x);
		}

		#endregion

		#region cosh, sinh, tanh, acosh, asinh, atanh

		[ImplementsFunction("cosh")]
        [PureFunction]
        public static double Cosh(double x)
		{
			return Math.Cosh(x);
		}

		[ImplementsFunction("sinh")]
        [PureFunction]
        public static double Sinh(double x)
		{
			return Math.Sinh(x);
		}

		[ImplementsFunction("tanh")]
        [PureFunction]
        public static double Tanh(double x)
		{
			return Math.Tanh(x);
		}

		[ImplementsFunction("acosh")]
        [PureFunction]
        public static double Acosh(double x)
		{
			return Math.Log(x + Math.Sqrt(x * x - 1));
		}

		[ImplementsFunction("asinh")]
        [PureFunction]
        public static double Asinh(double x)
		{
			return Math.Log(x + Math.Sqrt(x * x + 1));
		}

		[ImplementsFunction("atanh")]
        [PureFunction]
        public static double Atanh(double x)
		{
			return Math.Log((1 + x) / (1 - x)) / 2;
		}

		#endregion

		#region exp, expm1, log, log10, log1p, pow, sqrt, hypot

		[ImplementsFunction("exp"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Exp(double x)
		{
			return Math.Exp(x);
		}

		/// <summary>
        /// expm1() returns the equivalent to 'exp(arg) - 1' computed in a way that is accurate even
        /// if the value of arg is near zero, a case where 'exp (arg) - 1' would be inaccurate due to
        /// subtraction of two numbers that are nearly equal. 
		/// </summary>
        /// <param name="x">The argument to process </param>
		[ImplementsFunction("expm1")]
        [PureFunction]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static double ExpM1(double x)
		{
            return Math.Exp(x) - 1.0;   // TODO: implement exp(x)-1 for x near to zero
		}

		[ImplementsFunction("log10")]
        [PureFunction]
        public static double Log10(double x)
		{
			return Math.Log10(x);
		}

		[ImplementsFunction("log"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Log(double x)
		{
			return Math.Log(x);
		}

		[ImplementsFunction("log"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Log(double x, double logBase)
		{
			return MathEx.Log(x, logBase);
		}

        /// <summary>
        /// log1p() returns log(1 + number) computed in a way that is accurate even when the value
        /// of number is close to zero. log()  might only return log(1) in this case due to lack of precision. 
        /// </summary>
        /// <param name="x">The argument to process </param>
        /// <returns></returns>
		[ImplementsFunction("log1p")]
        [PureFunction]
        [EditorBrowsable(EditorBrowsableState.Never)]
		public static double Log1P(double x)
		{
            return Math.Log(x + 1.0);   // TODO: implement log(x+1) for x near to zero
		}

		[ImplementsFunction("pow")]
        [PureFunction]
        public static object Power(object @base, object exp)
		{
			double dbase, dexp;
			int ibase, iexp;
			long lbase, lexp;
			Core.Convert.NumberInfo info_base, info_exp;

			info_base = Core.Convert.ObjectToNumber(@base, out ibase, out lbase, out dbase);
			info_exp = Core.Convert.ObjectToNumber(exp, out iexp, out lexp, out dexp);

			if (((info_base | info_exp) & PHP.Core.Convert.NumberInfo.Double) == 0 && lexp >= 0)
			{
				// integer base, non-negative integer exp  //

				long lpower;
				double dpower;

				if (!Power(lbase, lexp, out lpower, out dpower))
					return dpower;

				if (lpower >= Int32.MinValue && lpower <= Int32.MaxValue)
					return (Int32)lpower;

				return lpower;
			}

			if (dbase < 0)
			{
				// cannot rount to integer:
				if (Math.Ceiling(dexp) > dexp)
					return Double.NaN;

				double result = Math.Pow(-dbase, dexp);
				return (Math.IEEERemainder(Math.Abs(dexp), 2.0) < 1.0) ? result : -result;
			}

			if (dexp < 0)
				return 1 / Math.Pow(dbase, -dexp);
			else
				return Math.Pow(dbase, dexp);
		}

		private static bool Power(long x, long y, out long longResult, out double doubleResult)
		{
			long l1 = 1, l2 = x;

            if (y == 0) // anything powered by 0 is 1
			{
				doubleResult = longResult = 1;
				return true;
			}

            if (x == 0) // 0^(anything except 0) is 0
            {
                doubleResult = longResult = 0;
                return true;
            }

            try
            {
                while (y >= 1)
                {
                    if ((y & 1) != 0)
                    {
                        l1 *= l2;
                        y--;
                    }
                    else
                    {
                        l2 *= l2;
                        y /= 2;
                    }
                }
            }
            catch(ArithmeticException)
            {
                longResult = 0;//ignored
                doubleResult = (double)l1 * Math.Pow(l2, y);
                return false;
            }
            
            // able to do it with longs
			doubleResult = longResult = l1;
			return true;
		}

		[ImplementsFunction("sqrt"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Sqrt(double x)
		{
			return Math.Sqrt(x);
		}

		[ImplementsFunction("hypot")]
        [PureFunction]
        public static double Hypotenuse(double x, double y)
		{
			return Math.Sqrt(x * x + y * y);
		}

		#endregion

		#region  ceil, floor, round, abs, fmod, max, min

		[ImplementsFunction("ceil"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Ceiling(double x)
		{
			return Math.Ceiling(x);
		}

		[ImplementsFunction("floor"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Floor(double x)
		{
			return Math.Floor(x);
		}

		[ImplementsFunction("round"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Round(double x)
		{
			return Math.Round(x);
		}

		private static double[] pow10 = 
		{
			1.0, 10.0, 100.0, 1000.0, 10000.0, 100000.0,
			1000000.0, 10000000.0, 100000000.0, 1000000000.0
		};

		[ImplementsFunction("round"/*, FunctionImplOptions.Special*/)]
        [PureFunction]
        public static double Round(double x, int precision)
		{
			long p = Math.Abs((long)precision);
			double power;

			if (p < pow10.Length)
			{
				power = pow10[p];
			}
			else
			{
				long _lpower;
				Power(10, p, out _lpower, out power);
			}

			if (precision < 0)
				power = 1 / power;

			x *= power;

			if (x >= 0.0) x = Math.Floor(x + 0.50000000001);
			else x = Math.Ceiling(x - 0.50000000001);

			x /= power;
			return x;
		}

		[ImplementsFunction("abs")]
        [PureFunction]
        public static object Abs(object x)
		{
			double dx;
			int ix;
			long lx;

			switch (Core.Convert.ObjectToNumber(x, out ix, out lx, out dx) & Core.Convert.NumberInfo.TypeMask)
			{
				case Core.Convert.NumberInfo.Double:
					return Math.Abs(dx);

				case Core.Convert.NumberInfo.Integer:
					if (ix == int.MinValue)
						return -lx;
					else
						return Math.Abs(ix);

				case Core.Convert.NumberInfo.LongInteger:
					if (lx == long.MinValue)
						return -dx;
					else
						return Math.Abs(lx);
			}

			return null;
		}

		[ImplementsFunction("fmod")]
        [PureFunction]
        public static double Fmod(double x, double y)
		{
			y = Math.Abs(y);
			double rem = Math.IEEERemainder(Math.Abs(x), y);
			if (rem < 0) rem += y;
			return (x >= 0) ? rem : -rem;
		}

		[ImplementsFunction("max")]
        [PureFunction]
        public static object Max(params object[] numbers)
		{
			return GetExtreme(numbers, true);
		}

		[ImplementsFunction("min")]
        [PureFunction]
        public static object Min(params object[] numbers)
		{
			return GetExtreme(numbers, false);
		}

		internal static object GetExtreme(object[] numbers, bool maximum)
		{
			if ((numbers.Length == 1) && (numbers[0] is PhpArray))
			{
				IEnumerable e = (numbers[0] as PhpArray).Values;
				Debug.Assert(e != null);
				return FindExtreme(e, maximum);
			}
			return FindExtreme(numbers, maximum);
		}

		internal static object FindExtreme(IEnumerable array, bool maximum)
		{
			object ex = null;
			int fact = maximum ? 1 : -1;
			foreach (object o in array)
			{
				if (ex == null) ex = o;
				else
				{
					if ((PhpComparer.Default.Compare(o, ex) * fact) > 0) ex = o;
				}
			}
			return ex;
		}

		#endregion
	}
}
