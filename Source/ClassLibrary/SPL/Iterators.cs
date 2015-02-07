/*

 Copyright (c) 2004-2006 Ladislav Prosek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/
using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
	/// <summary>
	/// Contains iterators-related class library functions.
	/// </summary>
	/// <threadsafety static="true"/>
	public static class SplIterators
	{
        ///// <summary>
        ///// Applies a user function or method on each element of a specified iterator.
        ///// </summary>
        ///// <returns><B>true</B>.</returns>
        ///// <remarks>See <see cref="Walk(PHP.Core.Reflection.DTypeDesc,PhpHashtable,PhpCallback,object)"/> for details.</remarks>
        ///// <exception cref="PhpException"><paramref name="function"/> or <paramref name="array"/> are <B>null</B> references.</exception>
        //[ImplementsFunction("iterator_apply", FunctionImplOptions.NeedsClassContext)]
        //public static bool Apply(PHP.Core.Reflection.DTypeDesc caller, [PhpRw] PhpHashtable array, PhpCallback function, params object[] args)
        //{
            
        //}
	}
}
