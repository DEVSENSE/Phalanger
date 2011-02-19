using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.CoreCLR
{
	public class ConfigurationErrorsException : Exception
	{
		public ConfigurationErrorsException() { }
		public ConfigurationErrorsException(string message) : base(message) { }
		public ConfigurationErrorsException(string message, Exception inner) : base(message, inner) { }
	}
}
