using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.CoreCLR
{
	public class MathEx
	{
		/// <summary>
		/// Returns the logarithm of a specified number in a specified base.
		/// </summary>
		public static double Log(double a, double newBase)
		{
			if ((newBase != 1) && ((a == 1) || ((newBase != 0) && !double.IsPositiveInfinity(newBase))))
			{
				return (System.Math.Log(a) / System.Math.Log(newBase));
			}
			return double.NaN;
		}


		/// <summary>
		/// Calculates the quotient of two 64-bit signed integers and also returns the remainder in an output parameter.
		/// </summary>
		public static long DivRem(long a, long b, out long result)
		{
			result = a % b;
		  return (a / b);
		}


		/// <summary>
		/// Calculates the quotient of two 64-bit signed integers and also returns the remainder in an output parameter.
		/// </summary>
		public static int DivRem(int a, int b, out int result)
		{
			result = a % b;
			return (a / b);
		}
	}
}
