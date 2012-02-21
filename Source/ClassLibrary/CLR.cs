/*

 Copyright (c) 2006 Tomas Matousek.  

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.IO;
using System.Threading;
using System.Collections;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
	public static class Clr
	{
		#region Threads

		private class Worker
		{
			private ScriptContext context;
			private object[] args;

			public Worker(ScriptContext/*!*/ context, object[] args)
			{
				this.context = context;
				this.args = args;
			}

			public void Run(object _)
			{
                var callback = _ as PhpCallback;

				callback.SwitchContext(context.Fork());
				callback.Invoke(args);
			}
		}

		[ImplementsFunction("clr_create_thread")]
		public static DObject CreateClrThread(PhpCallback/*!*/ callback, params object[] args)
		{
			if (callback == null)
				PhpException.ArgumentNull("callback");

			if (!callback.Bind())
				return null;

			object[] copies = (args != null) ? new object[args.Length] : ArrayUtils.EmptyObjects;

			for (int i = 0; i < copies.Length; i++)
				copies[i] = PhpVariable.DeepCopy(args[i]);

            return ClrObject.WrapRealObject(ThreadPool.QueueUserWorkItem(new Worker(ScriptContext.CurrentContext, copies).Run, callback));
		}

		#endregion

		#region Types

		[ImplementsFunction("clr_typeof", FunctionImplOptions.NeedsNamingContext | FunctionImplOptions.NeedsClassContext)]
		public static DObject GetTypeOf(NamingContext/*!*/ namingContext, DTypeDesc caller, object typeNameOrObject)
		{
			ScriptContext context = ScriptContext.CurrentContext;
			DTypeDesc type = PhpObjects.ClassNameOrObjectToType(context, namingContext, caller, typeNameOrObject, true);
			if (type == null) return null;

			return ClrObject.Create(type.RealType);
		}

		#endregion
	}

}
