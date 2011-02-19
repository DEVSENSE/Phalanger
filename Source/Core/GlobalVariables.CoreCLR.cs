/*

 Copyright (c) 2004-2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using PHP.Core.Emit;
using System.Reflection;

namespace PHP.Core
{
	/// <summary>
	/// Declares auto-global variables stored in the script context.
	/// </summary>
	public sealed class AutoGlobals
	{
		internal const int EgpcsCount = 5;
		internal const int MaxCount = 9;
		internal const int EstimatedUserGlobalVariableCount = 15;

		#region Fields & Initialization

        /// <summary>
        /// Addr variable ($_ADDR). 
        /// </summary>
        public PhpReference/*!*/ Addr = new PhpReference();
        public const string AddrName = "_ADDR";

		/// <summary>
		/// Global variables ($GLOBALS). 
		/// </summary>
		public PhpReference/*!*/ Globals = new PhpReference();
		public const string GlobalsName = "GLOBALS";

		/// <summary>
		/// Canvas variable ($_CANVAS). Initialized on start.
		/// </summary>
		public PhpReference/*!*/ Canvas = new PhpReference();
		public const string CanvasName = "_CANVAS";

		/// <summary>
		/// Initializes all auto-global variables.
		/// </summary>
		internal void Initialize()
		{
			Globals.Value = new PhpArray(0, 0);
		}

		#endregion

		#region IsAutoGlobal

		/// <summary>
		/// Checks whether a specified name is the name of an auto-global variable.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <returns>Whether <paramref name="name"/> is auto-global.</returns>
		public static bool IsAutoGlobal(string name)
		{
			return name == "GLOBALS" || name == "_CANVAS" || name == "_ADDR";
		}

		#endregion

		#region Variable Addition

		/// <summary>
		/// Adds variables from one auto-global array to another.
		/// </summary>
		/// <param name="dst">The target array.</param>
		/// <param name="src">The source array.</param>
		/// <remarks>Variable values are deeply copied.</remarks>
		/// <exception cref="ArgumentNullException"><paramref name="dst"/> is a <B>null</B> reference.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="src"/> is a <B>null</B> reference.</exception>
		private static void AddVariables(PhpArray/*!*/ dst, PhpArray/*!*/ src)
		{
			Debug.Assert(dst != null && src != null);

			foreach (KeyValuePair<IntStringKey, object> entry in src)
				dst[entry.Key] = PhpVariable.DeepCopy(entry.Value);
		}

		/// <summary>
		/// Adds variables from one auto-global array to another.
		/// </summary>
		/// <param name="dst">A PHP reference to the target array.</param>
		/// <param name="src">A PHP reference to the source array.</param>
		/// <remarks>
		/// Variable values are deeply copied. 
		/// If either reference is a <B>null</B> reference or doesn't contain an array, no copying takes place.
		/// </remarks>
		internal static void AddVariables(PhpReference/*!*/ dst, PhpReference/*!*/ src)
		{
			if (dst != null && src != null)
			{
				PhpArray adst = dst.Value as PhpArray;
				PhpArray asrc = src.Value as PhpArray;
				if (adst != null && asrc != null)
					AddVariables(adst, asrc);
			}
		}

		#endregion

		#region Emit Support

		/// <summary>
		/// Returns 'FieldInfo' representing field in AutoGlobals for given global variable name.
		/// </summary>
		internal static FieldInfo GetFieldForVariable(VariableName name)
		{
			switch (name.ToString())
			{
				case AutoGlobals.GlobalsName:
					return Fields.AutoGlobals.Globals;
				case AutoGlobals.CanvasName:
					return Fields.AutoGlobals.Canvas;
                case AutoGlobals.AddrName:
                    return Fields.AutoGlobals.Addr;
				default:
					return null;
			}
		}

		#endregion
	}
}
