using System;
using PHP.Core;

namespace PHP.Library.SPL
{
    /// <summary>
    /// Classes implementing Countable can be used with the count() function.
    /// </summary>
	[ImplementsType]
	public interface Countable
	{
        /// <summary>
        /// Count elements of an object.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
		[ImplementsMethod]
		[AllowReturnValueOverride]
		object count(ScriptContext context);
	}
}
