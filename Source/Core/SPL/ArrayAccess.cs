/*

 Copyright (c) 2006 Tomas Matousek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library.SPL
{
	[ImplementsType]
	public interface ArrayAccess
	{
		[ImplementsMethod]
		object offsetGet(ScriptContext context, object index);

		[ImplementsMethod]
		object offsetSet(ScriptContext context, object index, object value);

		[ImplementsMethod]
		object offsetUnset(ScriptContext context, object index);

		[ImplementsMethod]
		object offsetExists(ScriptContext context, object index);
	}

	internal class PhpArrayObject : PhpArray
	{
		public override bool IsProxy { get { return true; } }
		
		internal DObject ArrayAccess { get { return arrayAccess; } }
		readonly private DObject arrayAccess/*!*/;

		internal const string offsetGet = "offsetGet";
		internal const string offsetSet = "offsetSet";
		internal const string offsetUnset = "offsetUnset";
		internal const string offsetExists = "offsetExists";

		/// <summary>
		/// Do not call base class since we don't need to initialize <see cref="PhpArray"/>.
		/// </summary>
		internal PhpArrayObject(DObject/*!*/ arrayAccess)
		{
			Core.Debug.Assert(arrayAccess != null && arrayAccess.RealObject is ArrayAccess);
			this.arrayAccess = arrayAccess;
		}
		
		#region Operators

		public override object GetArrayItem(object key, bool quiet)
		{
			PhpStack stack = ScriptContext.CurrentContext.Stack;
			stack.AddFrame(key);
			return PhpVariable.Dereference(arrayAccess.InvokeMethod(offsetGet, null, stack.Context));
		}
		
		public override PhpReference GetArrayItemRef()
		{
			return GetUserArrayItemRef(arrayAccess, null, ScriptContext.CurrentContext);
		}

		public override PhpReference GetArrayItemRef(object key)
		{
			return GetUserArrayItemRef(arrayAccess, key, ScriptContext.CurrentContext);
		}

		public override PhpReference/*!*/ GetArrayItemRef(int key)
		{
			return GetUserArrayItemRef(arrayAccess, key, ScriptContext.CurrentContext);
		}

		public override PhpReference/*!*/ GetArrayItemRef(string key)
		{
			return GetUserArrayItemRef(arrayAccess, key, ScriptContext.CurrentContext);
		}	

		public override void SetArrayItem(object value)
		{
			PhpStack stack = ScriptContext.CurrentContext.Stack;
			stack.AddFrame(null, value);
			arrayAccess.InvokeMethod(offsetSet, null, stack.Context);
		}

		public override void SetArrayItem(object key, object value)
		{
			PhpStack stack = ScriptContext.CurrentContext.Stack;
			stack.AddFrame(key, value);
			arrayAccess.InvokeMethod(offsetSet, null, stack.Context);
		}

		public override void SetArrayItem(int key, object value)
		{
			SetArrayItem((object)key, value);
		}

		public override void SetArrayItem(string key, object value)
		{
			SetArrayItem((object)key, value);
		}

		public override void SetArrayItemExact(string key, object value, int hashcode)
		{
			SetArrayItem((object)key, value);
		}

		public override void SetArrayItemRef(object key, PhpReference value)
		{
			PhpStack stack = ScriptContext.CurrentContext.Stack;
			stack.AddFrame(key, value);
			arrayAccess.InvokeMethod(offsetSet, null, stack.Context);
		}

		public override void SetArrayItemRef(int key, PhpReference value)
		{
			SetArrayItemRef((object)key, value);
		}

		public override void SetArrayItemRef(string/*!*/ key, PhpReference value)
		{
			SetArrayItemRef((object)key, value);			
		}

		public override PhpArray EnsureItemIsArray()
		{
			return EnsureIndexerResultIsRefArray(null);
		}
		
		public override PhpArray EnsureItemIsArray(object key)
		{
			// an object behaving like an array:
			return EnsureIndexerResultIsRefArray(key);
		}

		public override DObject EnsureItemIsObject(ScriptContext/*!*/ context)
		{
			return EnsureIndexerResultIsRefObject(null, context);
		}

		public override DObject EnsureItemIsObject(object key, ScriptContext/*!*/ context)
		{
			return EnsureIndexerResultIsRefObject(key, context);
		}

		/// <summary>
		/// Calls the indexer (offsetGet) and ensures that its result is an array or can be converted to an array.
		/// </summary>
		/// <param name="key">A key passed to the indexer.</param>
		/// <returns>The array (either previously existing or a created one) or a <B>null</B> reference on error.</returns>
		/// <exception cref="PhpException">The indexer doesn't return a reference (Error).</exception>
		/// <exception cref="PhpException">The return value cannot be converted to an array (Warning).</exception>
		private PhpArray EnsureIndexerResultIsRefArray(object key)
		{
			PhpReference ref_result = GetUserArrayItemRef(arrayAccess, key, ScriptContext.CurrentContext);

			// is the result an array:
			PhpArray result = ref_result.Value as PhpArray;
			if (result != null) return result;

			// checks an object behaving like an array:
			DObject dobj = ref_result.Value as DObject;
			if (dobj != null && dobj.RealObject is Library.SPL.ArrayAccess) return new Library.SPL.PhpArrayObject(dobj);

			// is result empty => creates a new array and writes it back:
			if (Operators.IsEmptyForEnsure(ref_result.Value))
			{
				ref_result.Value = result = new PhpArray();
				return result;
			}

			// non-empty immutable string:
			string str_value = ref_result.Value as string;
			if (str_value != null)
			{
				ref_result.Value = new PhpString(str_value);
				return new PhpArrayString(ref_result.Value);
			}

			// non-empty string:
			if (ref_result.Value is PhpString || ref_result.Value is PhpBytes)
				return new PhpArrayString(ref_result.Value);

			// the result is neither array nor object behaving like array:
			PhpException.VariableMisusedAsArray(ref_result.Value, false);
			return null;
		}

		/// <summary>
		/// Calls the indexer (offsetGet) and ensures that its result is an <see cref="DObject"/> or can be
		/// converted to <see cref="DObject"/>.
		/// </summary>
		/// <param name="key">A key passed to the indexer.</param>
		/// <param name="context">A script context.</param>
		/// <returns>The <see cref="DObject"/> (either previously existing or a created one) or a <B>null</B> reference on error.</returns>
		/// <exception cref="PhpException">The indexer doesn't return a reference (Error).</exception>
		/// <exception cref="PhpException">The return value cannot be converted to a DObject (Warning).</exception>
		private DObject EnsureIndexerResultIsRefObject(object key, ScriptContext/*!*/ context)
		{
			PhpReference ref_result = GetUserArrayItemRef(arrayAccess, key, context);

			// is the result an array:
			DObject result = ref_result.Value as DObject;
			if (result != null) return result;

			// is result empty => creates a new array and writes it back:
			if (Operators.IsEmptyForEnsure(ref_result.Value))
			{
				ref_result.Value = result = stdClass.CreateDefaultObject(context);
				return result;
			}

			// the result is neither array nor object behaving like array not empty value:
			PhpException.VariableMisusedAsObject(ref_result.Value, false);
			return null;
		}

		internal static object GetUserArrayItem(DObject/*!*/ arrayAccess, object index, Operators.GetItemKinds kind)
		{
			PhpStack stack = ScriptContext.CurrentContext.Stack;

			switch (kind)
			{
				case Operators.GetItemKinds.Isset:
					// pass isset() ""/null to say true/false depending on the value returned from "offsetExists": 
					stack.AddFrame(index);
					return Core.Convert.ObjectToBoolean(arrayAccess.InvokeMethod(offsetExists, null, stack.Context)) ? "" : null;

				case Operators.GetItemKinds.Empty:
					// if "offsetExists" returns false, the empty()/isset() returns false (pass null to say true/false): 
					// otherwise, "offsetGet" is called to retrieve the value, which is passed to isset():
					stack.AddFrame(index);
					if (!Core.Convert.ObjectToBoolean(arrayAccess.InvokeMethod(offsetExists, null, stack.Context)))
						return null;
					else
						goto default;
				
				default:
					// regular getter:
					stack.AddFrame(index);
					return PhpVariable.Dereference(arrayAccess.InvokeMethod(offsetGet, null, stack.Context));
			}
			
		}

		/// <summary>
		/// Gets an item of a user array by invoking <see cref="Library.SPL.ArrayAccess.offsetGet"/>.
		/// </summary>
		/// <param name="arrayAccess">User array object.</param>
		/// <param name="index">An index.</param>
		/// <param name="context">The current script context.</param>
		/// <returns>A reference on item returned by the user getter.</returns>
		internal static PhpReference GetUserArrayItemRef(DObject/*!*/ arrayAccess, object index, ScriptContext/*!*/ context)
		{
			Debug.Assert(arrayAccess.RealObject is Library.SPL.ArrayAccess);
			Debug.Assert(!(index is PhpReference));

			context.Stack.AddFrame(index);
			object result = arrayAccess.InvokeMethod(Library.SPL.PhpArrayObject.offsetGet, null, context);
			PhpReference ref_result = result as PhpReference;
			if (ref_result == null)
			{
				// obsolete (?): PhpException.Throw(PhpError.Error,CoreResources.GetString("offsetGet_must_return_byref"));
				ref_result = new PhpReference(result);
			}
			return ref_result;
		}

		#endregion

	}
}
