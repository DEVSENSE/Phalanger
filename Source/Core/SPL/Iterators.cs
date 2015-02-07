/*

 Copyright (c) 2005-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using PHP.Core;
using PHP.Core.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace PHP.Library.SPL
{
	/// <summary>
	/// Built-in marker interface.
	/// </summary>
	[ImplementsType]
	public interface Traversable
	{ }

	/// <summary>
	/// Interface for external iterators or objects that can iterate themselves internally.
	/// </summary>
	/// <remarks>
	/// Note that contrary to the .NET framework enumerating interfaces,
	/// calling <c>rewind</c> positions the iterator on the first element, so <c>next</c>
	/// shall not be called until the first element is retrieved.
	/// </remarks>
	[ImplementsType]
	public interface Iterator : Traversable
	{
		/// <summary>
		/// Rewinds the iterator to the first element.
		/// </summary>
		[ImplementsMethod]
		object rewind(ScriptContext context);

		/// <summary>
		/// Moves forward to next element.
		/// </summary>
		[ImplementsMethod]
		object next(ScriptContext context);

		/// <summary>
		/// Checks if there is a current element after calls to <see cref="rewind"/> or <see cref="next"/>.
		/// </summary>
        /// <returns><c>bool</c>.</returns>
		[ImplementsMethod]
		object valid(ScriptContext context);

		/// <summary>
		/// Returns the key of the current element.
		/// </summary>
		[ImplementsMethod]
		object key(ScriptContext context);

		/// <summary>
		/// Returns the current element (value).
		/// </summary>
		[ImplementsMethod]
		[AllowReturnValueOverride]
		object current(ScriptContext context);
	}

    /// <summary>
    /// The Seekable iterator.
    /// </summary>
    [ImplementsType]
    public interface SeekableIterator : Iterator
    {
        /// <summary>
        /// Seeks to a given position in the iterator.
        /// </summary>
        [ImplementsMethod]
        [AllowReturnValueOverride]
        object seek(ScriptContext context, object position);
    }

	/// <summary>
	/// Interface to create an external iterator.
	/// </summary>
	/// <remarks>
	/// This interface contains only arg-less stubs as signatures should not be restricted.
	/// </remarks>
	[ImplementsType]
	public interface IteratorAggregate : Traversable
	{
		/// <summary>
		/// Returns an <see cref="Iterator"/> or another <see cref="IteratorAggregate"/> for
		/// the implementing object.
		/// </summary>
		[ImplementsMethod]
		object getIterator(ScriptContext context);
	}

    /// <summary>
    /// Classes implementing OuterIterator can be used to iterate over iterators.
    /// </summary>
    [ImplementsType]
	public interface OuterIterator : Iterator
    {
        /// <summary>
        /// Returns the inner iterator for the current iterator entry.
        /// </summary>
        /// <returns>The inner <see cref="Iterator"/> for the current entry.</returns>
        object getInnerIterator(ScriptContext context);
    }

    /// <summary>
    /// Classes implementing RecursiveIterator can be used to iterate over iterators recursively.
    /// </summary>
    [ImplementsType]
    public interface RecursiveIterator : Iterator
    {
        /// <summary>
        /// Returns an iterator for the current iterator entry.
        /// </summary>
        /// <returns>An <see cref="RecursiveIterator"/> for the current entry.</returns>
        object getChildren(ScriptContext context);

        /// <summary>
        /// Returns if an iterator can be created fot the current entry.
        /// </summary>
        /// <returns>Returns TRUE if the current entry can be iterated over, otherwise returns FALSE.</returns>
        object hasChildren(ScriptContext context);
    }

    [ImplementsType]
    public class ArrayIterator : PhpObject, Iterator, Traversable, ArrayAccess, SeekableIterator, Countable, Serializable
    {
        #region Fields & Properties

        private PhpArray array;
        private OrderedHashtable<IntStringKey>.Enumerator arrayEnumerator;
        private bool isArrayIterator { get { return this.array != null; } }

        private DObject dobj;
        private IDictionaryEnumerator dobjEnumerator;
        private bool isObjectIterator { get { return this.dobj != null; } }

        private bool isValid = false;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Constructs an <see cref="ArrayIterator"/> object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="array">The array or object to be iterated on.</param>
        /// <returns></returns>
        [ImplementsMethod]
        public virtual object __construct(ScriptContext/*!*/context, object array)
        {
            if ((this.array = array as PhpArray) != null)
            {
                this.arrayEnumerator = new OrderedHashtable<IntStringKey>.Enumerator(this.array, false);
            }
            else if ((this.dobj = array as DObject) != null)
            {
                this.dobjEnumerator = dobj.GetForeachEnumerator(true, false, null);
            }
            else
            {
                // throw an PHP.Library.SPL.InvalidArgumentException if anything besides an array or an object is given.
                var e = new InvalidArgumentException(context, true);
				e.__construct(context, null, 0);
				throw new PhpUserException(e);
            }

            // move first:
            this.rewind(context);
            return null;
        }

        #endregion

        #region Implementation details

        internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ArrayIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ArrayIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object array = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).__construct(stack.Context, array);
        }

        #endregion

        #region ArrayIterator (uasort, uksort, natsort, natcasesort, ksort, asort)

        public static object uasort(object instance, PhpStack stack)
        {
            var cmp_function = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).uasort(stack.Context, cmp_function);
        }

        public static object uksort(object instance, PhpStack stack)
        {
            var cmp_function = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).uksort(stack.Context, cmp_function);
        }

        public static object natsort(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).natsort(stack.Context);
        }

        public static object natcasesort(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).natcasesort(stack.Context);
        }

        public static object ksort(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).ksort(stack.Context);
        }

        public static object asort(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).asort(stack.Context);
        }

        #endregion

        #region ArrayIterator (getFlags, setFlags, append, getArrayCopy)

        public static object getFlags(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).getFlags(stack.Context);
        }

        public static object setFlags(object instance, PhpStack stack)
        {
            var value = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).setFlags(stack.Context, value);
        }

        public static object getArrayCopy(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).getArrayCopy(stack.Context);
        }

        public static object append(object instance, PhpStack stack)
        {
            var value = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).append(stack.Context, value);
        }

        #endregion

        #region interface Iterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object rewind(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).rewind(stack.Context);
        }

        public static object next(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).next(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object valid(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).valid(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object key(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).key(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object current(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).current(stack.Context);
        }

        #endregion

        #region interface ArrayAccess

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetGet(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).offsetGet(stack.Context, index);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetSet(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            object value = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).offsetSet(stack.Context, index, value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetUnset(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).offsetUnset(stack.Context, index);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetExists(object instance, PhpStack stack)
        {
            object index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).offsetExists(stack.Context, index);
        }

        #endregion

        #region interface SeekableIterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object seek(object instance, PhpStack stack)
        {
            object position = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).seek(stack.Context, position);
        }

        #endregion

        #region interface Countable

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object count(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).count(stack.Context);
        }

        #endregion

        #region interface Serializable

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object serialize(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayIterator)instance).serialize(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object unserialize(object instance, PhpStack stack)
        {
            object data = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayIterator)instance).unserialize(stack.Context, data);
        }

        #endregion

        #endregion

        #region ArrayIterator (uasort, uksort, natsort, natcasesort, ksort, asort)

        public virtual object uasort(ScriptContext/*!*/context, object cmp_function)
        {
            throw new NotImplementedException();
        }

        public virtual object uksort(ScriptContext/*!*/context, object cmp_function)
        {
            throw new NotImplementedException();
        }

        public virtual object natsort(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        public virtual object natcasesort(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        public virtual object ksort(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        public virtual object asort(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ArrayIterator (getFlags, setFlags, append, getArrayCopy)

        public virtual object getFlags(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        public virtual object setFlags(ScriptContext/*!*/context, object flags)
        {
            throw new NotImplementedException();
        }

        public virtual PhpArray getArrayCopy(ScriptContext/*!*/context)
        {
            if (isArrayIterator)
                return new PhpArray(array);

            throw new NotImplementedException();
        }

        public virtual object append(ScriptContext/*!*/context, object value)
        {
            if (isArrayIterator)
            {
                array.Add(value);
            }
            else if (isObjectIterator)
            {
                // php_error_docref(NULL TSRMLS_CC, E_RECOVERABLE_ERROR, "Cannot append properties to objects, use %s::offsetSet() instead", Z_OBJCE_P(object)->name);
            }
            
            return null;
        }

        #endregion

        #region interface Iterator

        [ImplementsMethod]
        public object rewind(ScriptContext context)
        {
            if (isArrayIterator)
            {
                this.isValid = arrayEnumerator.MoveFirst();
            }
            else if (isObjectIterator)
            {
                dobjEnumerator.Reset();
                this.isValid = dobjEnumerator.MoveNext();
            }

            return null;
        }

        [ImplementsMethod]
        public object next(ScriptContext context)
        {
            if (isArrayIterator)
                this.isValid = arrayEnumerator.MoveNext();
            else if (isObjectIterator)
                this.isValid = dobjEnumerator.MoveNext();

            return null;
        }

        [ImplementsMethod]
        public virtual object valid(ScriptContext context)
        {
            return this.isValid;
        }

        [ImplementsMethod]
        public virtual object key(ScriptContext context)
        {
            if (isArrayIterator)
                return arrayEnumerator.Current.Key.Object;
            else if (isObjectIterator)
                return dobjEnumerator.Key;

            return false;
        }

        [ImplementsMethod]
        public virtual object current(ScriptContext context)
        {
            if (isArrayIterator)
                return arrayEnumerator.Current.Value;
            else if (isObjectIterator)
                return dobjEnumerator.Value;

            return false;
        }

        #endregion

        #region interface ArrayAccess

        [ImplementsMethod]
        public virtual object offsetGet(ScriptContext context, object index)
        {
            if (isArrayIterator)
                return array[index];
            else if (isObjectIterator)
                return dobj[index];

            return false;
        }

        [ImplementsMethod]
        public virtual object offsetSet(ScriptContext context, object index, object value)
        {
            if (isArrayIterator)
            {
                if (index != null) array.Add(index, value);
                else array.Add(value);
            }
            else if (isObjectIterator)
            {
                dobj.Add(index, value);
            }

            return null;
        }

        [ImplementsMethod]
        public virtual object offsetUnset(ScriptContext context, object index)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object offsetExists(ScriptContext context, object index)
        {
            if (isArrayIterator)
                return array.ContainsKey(new IntStringKey(index));
            else if (isObjectIterator)
                return dobj.Contains(index);

            return false;
        }

        #endregion

        #region interface SeekableIterator

        [ImplementsMethod]
        public object seek(ScriptContext context, object position)
        {
            int currentPosition = 0;
            int targetPosition = PHP.Core.Convert.ObjectToInteger(position);

            if (targetPosition < 0)
            {
                //
            }

            this.rewind(context);

            while ((bool)this.valid(context) && currentPosition < targetPosition)
            {
                this.next(context);
                currentPosition++;
            }

            return null;
        }

        #endregion

        #region interface Countable

        [ImplementsMethod]
        public virtual object count(ScriptContext context)
        {
            if (isArrayIterator)
                return array.Count;
            else if (isObjectIterator)
                return dobj.Count;

            return false;
        }

        #endregion

        #region interface Serializable

        [ImplementsMethod]
        public virtual object serialize(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object unserialize(ScriptContext context, object data)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

		/// <summary>
		/// Deserializing constructor.
		/// </summary>
        protected ArrayIterator(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

#endif
		#endregion
    }
}
