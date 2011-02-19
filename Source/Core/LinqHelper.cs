/*

 Copyright (c) 2006 Tomas Matousek.
  
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Core
{
	public abstract class LinqContext
	{
		[Emitted]
		protected Dictionary<string, object>/*!*/ variables;
		[Emitted]
		protected ScriptContext/*!*/ context;
		[Emitted]
		protected DTypeDesc typeHandle;
		[Emitted]
		protected DObject outerType;
		
		// variables may be null when the code is global

		protected LinqContext(DObject outerType, Dictionary<string, object> variables, ScriptContext/*!*/ context, DTypeDesc typeHandle)
		{
			if (context == null)
				throw new ArgumentNullException("context");
			
			this.variables = variables;
			this.outerType = outerType;
			this.context = context;
			this.typeHandle = typeHandle;
		}
	}
}
