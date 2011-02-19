/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Collections;
using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Core.Emit
{
	/// <summary>
	/// Emits PHP functions implementations.
	/// </summary>
	internal class FunctionBuilder
	{
		/// <summary>
		/// An index of "this" argument in PHP user instance methods. 
		/// </summary>
		public const int ArgThis = 0;

		/// <summary>
		/// An index of "context" argument in PHP user functions.
		/// </summary>
		public const int ArgContext = 0;

		/// <summary>
		/// An index of "context" argument in PHP user instance methods.
		/// </summary>
		public const int ArgContextInstance = 1;

		/// <summary>
		/// An index of "context" argument in PHP user static methods.
		/// </summary>
		public const int ArgContextStatic = 0;

		/// <summary>
		/// An index of "stack" argument in arg-less static method stubs.
		/// </summary>
		public const int ArgStackStatic = 1;

		/// <summary>
		/// An index of "stack" argument in PHP user instance method stubs.
		/// </summary>
		public const int ArgStackInstance = 1;

		/// <summary>
		/// A stack place (used by arg-less overload emitter).
		/// </summary>
		private IndexedPlace stack = new IndexedPlace(PlaceHolder.Argument, 0);
	}
}
