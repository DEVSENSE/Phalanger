/*

 Copyright (c) 2005-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using PHP.Core;

namespace PHP.Library.SPL
{
	/// <summary>
	/// Interface for customized serializing.
	/// </summary>
	/// <remarks>
	/// Classes that implement this interface no longer support <c>__sleep</c> and <c>__wakeup</c>.
	/// The method <c>serialize</c> is called whenever an instance needs to be serialized. This does not invoke
	/// <c>__destruct</c> or has any other side effect unless programmed inside the method. When the data
	/// is unserialized the class is known and the appropriate <c>unserialize</c> method is called as a
	/// constructor instead of calling <c>__construct</c>. If you need to execute the standard constructor
	/// you may do so in the method.
	/// </remarks>
	[ImplementsType]
	public interface Serializable
	{
		/// <summary>
		/// Returns a string representation of the instance or <B>null</B>.
		/// </summary>
		[ImplementsMethod]
		[AllowReturnValueOverride]
		object serialize(ScriptContext context);

		/// <summary>
		/// Reconstructs the instance from a string representation passed as the only parameter.
		/// </summary>
		[ImplementsMethod]
		object unserialize(ScriptContext context, object data);
	}
}
