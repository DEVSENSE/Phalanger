/*

 Copyright (c) 2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Xml;
using System.Xml.Schema;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using PHP.Core;

namespace PHP.Library.Xml
{
	/// <summary>
	/// DOM configuration (not implemented in PHP 5.1.6).
	/// </summary>
	[ImplementsType]
	public partial class DOMConfiguration
	{
		#region Operations

		[PhpVisible]
		public void setParameter(string name, object value)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		[PhpVisible]
		public void getParameter(string name)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		[PhpVisible]
		public void canSetParameter(string name, object value)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion
	}

	/// <summary>
	/// DOM user data handler (not implemented in PHP 5.1.6).
	/// </summary>
	[ImplementsType]
	public partial class DOMUserDataHandler
	{
		#region Operations

		[PhpVisible]
		public void handle(int operation, string key, object data, DOMNode src, DOMNode dst)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion
	}

	/// <summary>
	/// DOM locator (not implemented in PHP 5.1.6).
	/// </summary>
	[ImplementsType]
	public partial class DOMLocator
	{
		#region Properties

		[PhpVisible]
		public object lineNumber
		{
			get { return null; }
		}

		[PhpVisible]
		public object columnNumber
		{
			get { return null; }
		}

		[PhpVisible]
		public object offset
		{
			get { return null; }
		}

		[PhpVisible]
		public object relatedNode
		{
			get { return null; }
		}

		[PhpVisible]
		public object uri
		{
			get { return null; }
		}

		#endregion
	}

	/// <summary>
	/// The DOM error (not implemented in PHP 5.1.6).
	/// </summary>
	[ImplementsType]
	public partial class DOMDomError
	{
		#region Properties

		[PhpVisible]
		public object severity
		{
			get { return null; }
		}

		[PhpVisible]
		public object message
		{
			get { return null; }
		}

		[PhpVisible]
		public object type
		{
			get { return null; }
		}

		[PhpVisible]
		public object relatedException
		{
			get { return null; }
		}

		[PhpVisible]
		public object related_data
		{
			get { return null; }
		}

		[PhpVisible]
		public object location
		{
			get { return null; }
		}

		#endregion
	}

	/// <summary>
	/// DOM error handler (not implemented in PHP 5.1.6).
	/// </summary>
	[ImplementsType]
	public partial class DOMErrorHandler
	{
		#region Operations

		[PhpVisible]
		public void handleError(DOMDomError error)
		{
			PhpException.Throw(PhpError.Warning, Resources.NotYetImplemented);
		}

		#endregion
	}

	/// <summary>
	/// DOM type info (not implemented in PHP 5.1.6).
	/// </summary>
	[ImplementsType]
	public partial class DOMTypeinfo
	{
		#region Properties

		[PhpVisible]
		public object typeName
		{
			get { return null; }
		}

		[PhpVisible]
		public object typeNamespace
		{
			get { return null; }
		}

		#endregion
	}
}
