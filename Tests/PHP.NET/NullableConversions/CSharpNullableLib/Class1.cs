using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSharpNullableLib
{
	public class NullableTests
	{
		public int? IntNull { get; set; }
		public double? DoubleNull { get; set; }
		public bool? BoolNull { get; set; }

		int? Wtf()
		{
			return null;
		}

		public void Print()
		{
			Console.WriteLine("Int?    = {0}", IntNull);
			Console.WriteLine("Double? = {0}", DoubleNull);
			Console.WriteLine("Bool?   = {0}", BoolNull);
		}
	}
}
