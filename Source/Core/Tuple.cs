using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.Core
{
	/// <summary>
	/// Used by LINQ classes (<see cref="PHP.Core.AST.Linq.LinqTuple"/> and other) for representing tuples.
	/// </summary>
	/// <typeparam name="A">Type of first value</typeparam>
	/// <typeparam name="B">Type of second value</typeparam>
	public class Tuple<A, B> 
	{
		private B second;
		public B Second { get { return second; } }

		private A first;
		public A First { get { return first; } }

		public Tuple(A first, B second)
		{
			this.first = first;
			this.second = second;
		}
	}
}
