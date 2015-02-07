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
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

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

    [ImplementsType]
    public class SplFixedArray : PhpObject, ArrayAccess, Iterator, Countable
    {
        /// <summary>
        /// Internal array storage. <c>null</c> reference if the size is <c>0</c>.
        /// </summary>
        protected object[] array = null;

        /// <summary>
        /// Iterator position in the array.
        /// </summary>
        private long position = 0;

        #region Helper methods

        protected void ReallocArray(long newsize)
        {
            Debug.Assert(newsize >= 0);

            // empty the array
            if (newsize == 0)
            {
                array = null;
                return;
            }

            // resize the array
            var newarray = new object[newsize];

            // TODO: mark new elements as unsetted

            if (array == null)
            {
                array = newarray;
            }
            else
            {
                Array.Copy(array, newarray, Math.Min(array.LongLength, newarray.LongLength));
                array = newarray;
            }
        }

        protected bool IsValidInternal()
        {
            return (position >= 0 && array != null && position < array.LongLength);
        }

        protected object SizeInternal()
        {
            return (array != null) ? ((array.LongLength <= int.MaxValue) ? array.Length : array.LongLength) : 0;
        }

        protected long ToLongHelper(object obj)
        {
            // allow only numeric types

            if (obj is long) return (long)obj;
            if (obj is int) return (int)obj;
            if (obj is bool) return (long)((bool)obj ? 1 : 0);
            if (obj is double) return unchecked((long)(double)obj);

            return -1;
        }

        protected void IndexCheckHelper(ScriptContext/*!*/context, long index)
        {
            if (index < 0 || array == null || index >= array.LongLength)
            {
                var e = new RuntimeException(context, true);
                e.__construct(context, "Index invalid or out of range", 0, null);
                throw new PhpUserException(e);
            }
        }

        #endregion

        #region SplFixedArray

        /// <summary>
        /// Constructs an <see cref="SplFixedArray"/> object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="size">The initial array size.</param>
        /// <returns></returns>
        [ImplementsMethod]
        public virtual object __construct(ScriptContext/*!*/context, [Optional] object size /*= 0*/)
        {
            long nsize = (size == Arg.Default || size == Type.Missing) ? 0 : Core.Convert.ObjectToLongInteger(size);
            if (nsize < 0)
            {
                PhpException.InvalidArgument("size");
                return null;
            }
            
            ReallocArray(nsize);
            
            return null;
        }

        [ImplementsMethod]
        public virtual object fromArray(ScriptContext/*!*/context, object data, [Optional]object save_indexes)
        {
            PhpArray arrdata = data as PhpArray;
            bool bindexes = (save_indexes == Arg.Default || save_indexes == Type.Missing) ? true : Core.Convert.ObjectToBoolean(save_indexes);

            if (arrdata == null || arrdata.Count == 0)
            {
                ReallocArray(0);
            }
            else if (bindexes)
            {
                if (arrdata.StringCount > 0)
                {
                    // TODO: error
                    return null;
                }

                //foreach (var pair in arrdata)
                //    if (pair.Key.IsString || pair.Key.Integer < 0) ; // TODO: error

                ReallocArray(arrdata.MaxIntegerKey + 1);

                foreach (var pair in arrdata)
                    this.array[pair.Key.Integer] = pair.Value;
            }
            else //if (!bindexes)
            {
                ReallocArray(arrdata.Count);

                int i = 0;
                foreach (var pair in arrdata)
                    this.array[i++] = pair.Value;
            }

            return null;
        }

        [ImplementsMethod]
        public virtual object toArray(ScriptContext/*!*/context)
        {
            if (array == null) return PhpArray.NewEmptyArray;

            Debug.Assert(array.LongLength <= int.MaxValue);

            PhpArray result = new PhpArray(array.Length, 0);

            for (int i = 0; i < array.Length; i++)
                result[i] = array[i];

            return result;
        }

        [ImplementsMethod]
        public virtual object getSize(ScriptContext/*!*/context)
        {
            return SizeInternal();
        }

        [ImplementsMethod]
        public virtual object setSize(ScriptContext/*!*/context, object size)
        {
            long newsize = Core.Convert.ObjectToLongInteger(size);
            if (newsize < 0)
            {
                // TODO: error
            }
            else
            {
                ReallocArray(newsize);
            }
            return null;
        }

        #endregion

        #region Implementation details
        
        internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        #region SplFixedArray

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SplFixedArray(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SplFixedArray(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object size = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((SplFixedArray)instance).__construct(stack.Context, size);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object fromArray(object instance, PhpStack stack)
        {
            object data = stack.PeekValue(1);
            object save_indexes = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((SplFixedArray)instance).fromArray(stack.Context, data, save_indexes);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object toArray(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplFixedArray)instance).toArray(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getSize(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplFixedArray)instance).getSize(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setSize(object instance, PhpStack stack)
        {
            object size = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplFixedArray)instance).setSize(stack.Context, size);
        }

        #endregion

        #region interface Iterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object rewind(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).rewind(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object next(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).next(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object valid(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).valid(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object key(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).key(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object current(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).current(stack.Context);
        }

        #endregion

        #region interface ArrayAccess

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetGet(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayAccess)instance).offsetGet(stack.Context, index);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetSet(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            object value = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((ArrayAccess)instance).offsetSet(stack.Context, index, value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetUnset(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayAccess)instance).offsetUnset(stack.Context, index);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetExists(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayAccess)instance).offsetExists(stack.Context, index);
        }

        #endregion

        #region interface Countable

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object count(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Countable)instance).count(stack.Context);
        }

        #endregion

        #endregion

        #region interface Iterator

        [ImplementsMethod]
        public object rewind(ScriptContext context)
        {
            position = 0;
            return null;
        }

        [ImplementsMethod]
        public object next(ScriptContext context)
        {
            position++;
            return null;
        }

        [ImplementsMethod]
        public virtual object valid(ScriptContext context)
        {
            return IsValidInternal();
        }

        [ImplementsMethod]
        public virtual object key(ScriptContext context)
        {
            return ((position <= int.MaxValue) ? (int)position : position);
        }

        [ImplementsMethod]
        public virtual object current(ScriptContext context)
        {
            return IsValidInternal() ? array[position] : null;
        }

        #endregion

        #region interface ArrayAccess

        [ImplementsMethod]
        public virtual object offsetGet(ScriptContext context, object index)
        {
            long i = ToLongHelper(index);
            IndexCheckHelper(context, i);   // throws if the index is out of range

            return array[i];
        }

        [ImplementsMethod]
        public virtual object offsetSet(ScriptContext context, object index, object value)
        {
            long i = ToLongHelper(index);
            IndexCheckHelper(context, i);   // throws if the index is out of range

            array[i] = value;

            return null;
        }

        [ImplementsMethod]
        public virtual object offsetUnset(ScriptContext context, object index)
        {
            return offsetSet(context, index, null); // TODO: mark unsetted element
        }

        [ImplementsMethod]
        public virtual object offsetExists(ScriptContext context, object index)
        {
            long i = ToLongHelper(index);
            return (i < 0 || array == null || i >= array.LongLength) ? false : (array[i] != null);  // TODO: null does not correspond to unsetted element
        }

        #endregion

        #region interface Countable

        [ImplementsMethod]
        public virtual object count(ScriptContext context)
        {
            return SizeInternal();
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected SplFixedArray(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
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
