/*

 Copyright (c) 2003-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.SymbolStore;

using PHP.Core.Emit;
using PHP.Core.AST;

namespace PHP.Core
{
	#region LazyLoadSCPlace

	/// <summary>
	/// Contains <see cref="PHP.Core.ScriptContext"/> loading method along with its context.
	/// </summary>
	internal sealed class LazyLoadSCPlace : IPlace
	{
		/// <summary>
		/// Builder of the local variable in which the <see cref="PHP.Core.ScriptContext"/> is cached.
		/// </summary>
		private LocalBuilder localBuilder;

		/// <summary>
		/// Emits code that loads current <see cref="PHP.Core.ScriptContext"/> by calling
		/// <see cref="PHP.Core.ScriptContext.CurrentContext"/> and remembers it in a local.
		/// </summary>
		/// <param name="il"></param>
		public void EmitLoad(ILEmitter il)
		{
			if (localBuilder == null)
			{
				localBuilder = il.DeclareLocal(typeof(ScriptContext));
				il.EmitCall(OpCodes.Call, Methods.ScriptContext.GetCurrentContext, null);
				il.Stloc(localBuilder);
			}
			il.Ldloc(localBuilder);
		}

		/// <summary>
		/// Returns a type of the place.
		/// </summary>
		public Type PlaceType
		{
			get
			{
				return typeof(ScriptContext);
			}
		}

		public void EmitLoadAddress(ILEmitter il)
		{
			throw new InvalidOperationException();
		}

		public bool HasAddress { get { return false; } }

		public void EmitStore(ILEmitter il)
		{
			throw new InvalidOperationException();
		}
	}

	#endregion
}