using System;
using System.Collections.Generic;
using System.Text;

namespace PHP.CoreCLR
{
	/// <summary>
	/// The exception that is thrown when a data stream is in an invalid format.
	/// </summary>
	public sealed class InvalidDataException : SystemException
	{
		/// <summary>
		/// Initializes a new instance of the InvalidDataException class.
		/// </summary>
		public InvalidDataException() : base() { }

		/// <summary>
		/// Initializes a new instance of the InvalidDataException class with
		/// a specified error message.
		/// </summary>
		public InvalidDataException(string message) : base(message) { }
		
		/// <summary>
		/// Initializes a new instance of the InvalidDataException class with
		/// a reference to the inner exception that is the cause of this exception.
		/// </summary>
		public InvalidDataException(string message, Exception innerException) : base(message, innerException) { }
	}
}
