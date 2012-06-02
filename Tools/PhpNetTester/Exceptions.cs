using System;

namespace PHP.Testing
{
	public class TestException : Exception
	{
		public TestException(string message)
			: base(message)
		{
		}
	}

	public class InvalidArgumentException : Exception
	{
		public InvalidArgumentException(string message)
			: base(message)
		{
		}
	}

}