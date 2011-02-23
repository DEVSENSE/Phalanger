using System;
using System.Collections.Generic;
using System.Text;

namespace CS
{
	public interface I1
	{
		void f1();
	}

	public interface I2 : I1
	{
		void f2();
	}

	public interface I3 : I1, I2
	{
		void f3();
	}
	
	public struct C
	{
		public C(int x)
		{

		}
	}

	public struct D
	{
	}
	
}
