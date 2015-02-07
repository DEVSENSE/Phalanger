/*

 Copyright (c) 2005-2006 Ladislav Prosek.

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/

using System;
using System.Collections;
using System.Collections.Generic;
using PHP.Core;
using PHP.Core.Reflection;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.InteropServices;

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
        [ImplementsMethod]
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
        [ImplementsMethod]
        object getChildren(ScriptContext context);

        /// <summary>
        /// Returns if an iterator can be created for the current entry.
        /// </summary>
        /// <returns>Returns TRUE if the current entry can be iterated over, otherwise returns FALSE.</returns>
        [ImplementsMethod]
        object hasChildren(ScriptContext context);
    }

    /// <summary>
    /// This iterator allows to unset and modify values and keys while iterating over Arrays and Objects.
    /// 
    /// When you want to iterate over the same array multiple times you need to instantiate ArrayObject
    /// and let it create ArrayIterator instances that refer to it either by using foreach or by calling
    /// its getIterator() method manually.
    /// </summary>
    [ImplementsType]
    public class ArrayIterator : PhpObject, Iterator, Traversable, ArrayAccess, SeekableIterator, Countable, Serializable
    {
        #region Fields & Properties

        protected PhpArray array;
        protected OrderedDictionary.Enumerator arrayEnumerator;    // lazily instantiated so we can rewind() once when needed
        protected bool isArrayIterator { get { return this.array != null; } }

        protected DObject dobj;
        protected IEnumerator<KeyValuePair<object, object>> dobjEnumerator;    // lazily instantiated so we can rewind() once when needed
        protected bool isObjectIterator { get { return this.dobj != null; } }

        /// <summary>
        /// Instantiate new PHP array's enumerator and advances its position to the first element.
        /// </summary>
        /// <returns><c>True</c> whether there is an first element.</returns>
        protected void InitArrayIteratorHelper()
        {
            Debug.Assert(this.array != null);

            this.arrayEnumerator = new OrderedDictionary.Enumerator(this.array, false);
            this.isValid = this.arrayEnumerator.MoveFirst();
        }

        /// <summary>
        /// Instantiate new object's enumerator and advances its position to the first element.
        /// </summary>
        /// <returns><c>True</c> whether there is an first element.</returns>
        protected void InitObjectIteratorHelper()
        {
            Debug.Assert(this.dobj != null);

            this.dobjEnumerator = dobj.InstancePropertyIterator(null, false);   // we have to create new enumerator (or implement InstancePropertyIterator.Reset)
            this.isValid = this.dobjEnumerator.MoveNext();
        }

        protected bool isValid = false;

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
                InitArrayIteratorHelper();  // instantiate now, avoid repetitous checks during iteration
            }
            else if ((this.dobj = array as DObject) != null)
            {
                //InitObjectIteratorHelper();   // lazily to avoid one additional allocation
            }
            else
            {
                // throw an PHP.Library.SPL.InvalidArgumentException if anything besides an array or an object is given.
                Exception.ThrowSplException(
                    _ctx => new InvalidArgumentException(_ctx, true),
                    context,
                    null, 0, null);
            }

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

        [EditorBrowsable(EditorBrowsableState.Never)]
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

        [ImplementsMethod]
        public virtual object uasort(ScriptContext/*!*/context, object cmp_function)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object uksort(ScriptContext/*!*/context, object cmp_function)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object natsort(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object natcasesort(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object ksort(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object asort(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ArrayIterator (getFlags, setFlags, append, getArrayCopy)

        [ImplementsMethod]
        public virtual object getFlags(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual object setFlags(ScriptContext/*!*/context, object flags)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public virtual PhpArray getArrayCopy(ScriptContext/*!*/context)
        {
            if (isArrayIterator)
                return new PhpArray(array);

            throw new NotImplementedException();
        }

        [ImplementsMethod]
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
        public virtual object rewind(ScriptContext context)
        {
            if (isArrayIterator)
            {
                this.isValid = arrayEnumerator.MoveFirst();
            }
            else if (isObjectIterator)
            {
                // isValid set by InitObjectIteratorHelper()
                InitObjectIteratorHelper(); // DObject enumerator does not support MoveFirst()
            }

            return null;
        }

        private void EnsureEnumeratorsHelper()
        {
            if (isObjectIterator && dobjEnumerator == null)
                InitObjectIteratorHelper();

            // arrayEnumerator initialized in __construct()
        }

        [ImplementsMethod]
        public virtual object next(ScriptContext context)
        {
            if (isArrayIterator)
            {
                this.isValid = arrayEnumerator.MoveNext();
            }
            else if (isObjectIterator)
            {
                EnsureEnumeratorsHelper();
                this.isValid = dobjEnumerator.MoveNext();
            }

            return null;
        }

        [ImplementsMethod]
        public virtual object valid(ScriptContext context)
        {
            EnsureEnumeratorsHelper();
            return this.isValid;
        }

        [ImplementsMethod]
        public virtual object key(ScriptContext context)
        {
            EnsureEnumeratorsHelper();

            if (this.isValid)
            {
                if (isArrayIterator)
                    return arrayEnumerator.Current.Key.Object;
                else if (isObjectIterator)
                    return dobjEnumerator.Current.Key;
                else
                    Debug.Fail();
            }

            return null;
        }

        [ImplementsMethod]
        public virtual object current(ScriptContext context)
        {
            EnsureEnumeratorsHelper();

            if (this.isValid)
            {
                if (isArrayIterator)
                    return arrayEnumerator.Current.Value;
                else if (isObjectIterator)
                    return dobjEnumerator.Current.Value;
                else
                    Debug.Fail();
            }

            return null;
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

    /// <summary>
    /// The EmptyIterator class for an empty iterator.
    /// </summary>
    [ImplementsType]
    public class EmptyIterator : PhpObject, Iterator, Traversable
    {
        public virtual object __construct(ScriptContext/*!*/context)
        {
            return null;
        }

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
        public EmptyIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EmptyIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((EmptyIterator)instance).__construct(stack.Context);
        }

        #endregion

        #region interface Iterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object rewind(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((EmptyIterator)instance).rewind(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object next(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((EmptyIterator)instance).next(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object valid(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((EmptyIterator)instance).valid(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object key(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((EmptyIterator)instance).key(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object current(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((EmptyIterator)instance).current(stack.Context);
        }

        #endregion

        #endregion

        #region interface Iterator

        [ImplementsMethod]
        public object rewind(ScriptContext context)
        {
            return null;
        }

        [ImplementsMethod]
        public object next(ScriptContext context)
        {
            return null;
        }

        [ImplementsMethod]
        public virtual object valid(ScriptContext context)
        {
            return false;
        }

        [ImplementsMethod]
        public virtual object key(ScriptContext context)
        {
            Exception.ThrowSplException(
                _ctx => new BadMethodCallException(_ctx, true),
                context,
                CoreResources.spl_empty_iterator_key_access, 0, null);
            return null;
        }

        [ImplementsMethod]
        public virtual object current(ScriptContext context)
        {
            Exception.ThrowSplException(
                _ctx => new BadMethodCallException(_ctx, true),
                context,
                CoreResources.spl_empty_iterator_value_access, 0, null);
            return null;
        }

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected EmptyIterator(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// This iterator wrapper allows the conversion of anything that is Traversable into an Iterator.
    /// It is important to understand that most classes that do not implement Iterators have reasons
    /// as most likely they do not allow the full Iterator feature set. If so, techniques should be provided
    /// to prevent misuse, otherwise expect exceptions or fatal errors.
    /// </summary>
    [ImplementsType]
    public class IteratorIterator : PhpObject, OuterIterator, Iterator, Traversable
    {
        /// <summary>
        /// Object to iterate on.
        /// </summary>
        private DObject/*!*/iterator;

        /// <summary>
        /// Enumerator over the <see cref="iterator"/>.
        /// </summary>
        protected IDictionaryEnumerator/*!*/enumerator;

        /// <summary>
        /// Wheter the <see cref="enumerator"/> is in valid state (initialized and not at the end).
        /// </summary>
        protected bool isValid = false;

        [ImplementsMethod]
        public virtual object __construct(ScriptContext/*!*/context, object/*Traversable*/ iterator, [Optional]object classname)
        {
            this.iterator = iterator as DObject;    // iterator.RealObject is Traversable ?
            if (this.iterator != null)
            {
                if (classname != null && classname != Arg.Default &&
                    !(this.iterator.RealObject is SPL.Iterator)    // downcast only if it is not an Iterator instance
                    )
                {
                    var downcast = context.ResolveType(PhpVariable.AsString(classname), null, this.iterator.TypeDesc, null, ResolveTypeFlags.ThrowErrors);

                    if (downcast == null || // not found
                        !downcast.IsAssignableFrom(this.iterator.TypeDesc) ||    // not base
                        !DTypeDesc.Create(typeof(Traversable)).IsAssignableFrom(downcast))   // not Traversable
                    {
                        // TODO: error
                        // zend_throw_exception(spl_ce_LogicException, "Class to downcast to not found or not base class or does not implement Traversable", 0 TSRMLS_CC);

                        this.iterator = null;
                    }
                    else
                    {
                        //if (DTypeDesc.Create(typeof(IteratorAggregate)).IsAssignableFrom(downcast))
                        //{
                        //    // {downcast} is IteratorAggregate
                        //    context.Stack.AddFrame();
                        //    var result = this.iterator.InvokeMethod("getIterator", null, context);

                        //    if (result == null || !(result is DObject) || !(((DObject)result).RealObject is Traversable))
                        //    {
                        //        //zend_throw_exception_ex(spl_ce_LogicException, 0 TSRMLS_CC, "%s::getIterator() must return an object that implements Traversable", ce->name);
                        //        this.iterator = null;
                        //    }
                        //    else
                        //    {
                        //        this.iterator = (DObject)result;
                        //    }
                        //}
                        throw new NotImplementedException();
                    }
                }
            }
            else
            {
                // TODO: error
            }

            //rewind(context);  // not in PHP, performance reasons (foreach causes rewind() itself)

            return null;
        }

        #region __call

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __call(object instance, PhpStack stack)
        {
            var name = stack.PeekValue(1);
            var args = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((IteratorIterator)instance).__call(stack.Context, name, args);            
        }

        [ImplementsMethod, NeedsArgless]    // TODO: hide this method to not be visible by PHP code, make this behaviour internal
        public virtual object __call(ScriptContext context, object name, object args)
        {
            var methodname = PhpVariable.AsString(name);
            var argsarr = args as PhpArray;
            
            if (this.iterator == null || argsarr == null)
            {
                PhpException.UndefinedMethodCalled(this.TypeName, methodname);
                return null;
            }

            // call the method on internal iterator, as in PHP,
            // only PHP leaves $this to self (which is potentionally dangerous and not correctly typed)
            context.Stack.AddFrame((ICollection)argsarr.Values);
            return this.iterator.InvokeMethod(methodname, null, context);
        }

        #endregion

        #region OuterIterator

        [ImplementsMethod]
        public virtual object getInnerIterator(ScriptContext context)
        {
            return this.iterator;
        }

        #endregion

        #region Iterator

        [ImplementsMethod]
        public virtual object rewind(ScriptContext context)
        {
            if (iterator != null)
            {
                // we can make use of standard foreach enumerator
                enumerator = iterator.GetForeachEnumerator(true, false, null);

                isValid = enumerator.MoveNext();
            }

            return null;
        }

        [ImplementsMethod]
        public virtual object next(ScriptContext context)
        {
            if (enumerator == null)
                rewind(context);    // init iterator first (this skips the first element as on PHP)

            if (enumerator != null) // enumerator can be still null, if iterator is null
                isValid = enumerator.MoveNext();

            return null;
        }

        [ImplementsMethod]
        public virtual object valid(ScriptContext context)
        {
            return isValid;
        }

        [ImplementsMethod]
        public virtual object key(ScriptContext context)
        {
            return (enumerator != null && isValid) ? enumerator.Key : null;
        }

        [ImplementsMethod]
        public virtual object current(ScriptContext context)
        {
            return (enumerator != null && isValid) ? enumerator.Value : null;
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
        public IteratorIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public IteratorIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object iterator = stack.PeekValue(1);
            object classname = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((IteratorIterator)instance).__construct(stack.Context, iterator, classname);
        }

        #endregion

        #region interface OuterIterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getInnerIterator(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((OuterIterator)instance).getInnerIterator(stack.Context);
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

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected IteratorIterator(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// This abstract iterator filters out unwanted values. This class should be extended to implement
    /// custom iterator filters. The FilterIterator::accept() must be implemented in the subclass.
    /// </summary>
    [ImplementsType]
    public abstract class FilterIterator : IteratorIterator, OuterIterator, Iterator, Traversable
    {
        [ImplementsMethod]
        public abstract object accept(ScriptContext/*!*/context);

        private void SkipNotAccepted(ScriptContext/*!*/context)
        {
            if (this.enumerator != null)
                while (this.isValid && !Core.Convert.ObjectToBoolean(this.accept(context)))
                    this.isValid = enumerator.MoveNext();   // skip not accepted elements
        }

        [ImplementsMethod]
        public override object rewind(ScriptContext context)
        {
            base.rewind(context);
            SkipNotAccepted(context);

            return null;
        }

        [ImplementsMethod]
        public override object next(ScriptContext context)
        {
            base.next(context);
            SkipNotAccepted(context);

            return null;
        }

        #region Implementation details

        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object accept(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((FilterIterator)instance).accept(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object rewind(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((FilterIterator)instance).rewind(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object next(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((FilterIterator)instance).next(stack.Context);
        }

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FilterIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public FilterIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion        

        #endregion
    }

    /// <summary>
    /// This iterator allows to unset and modify values and keys while iterating over Arrays
    /// and Objects in the same way as the ArrayIterator. Additionally it is possible to iterate
    /// over the current iterator entry.
    /// </summary>
    [ImplementsType]
    public class RecursiveArrayIterator : ArrayIterator, RecursiveIterator
    {
        #region RecursiveIterator

        [ImplementsMethod]
        public virtual object getChildren(ScriptContext context)
        {
            object current;
            if (!this.isValid || (current = this.current(context)) == null)
                return null;

            if (current is RecursiveArrayIterator)
                return current;
            else
            {
                var childIterator = new RecursiveArrayIterator(context, true);
                childIterator.__construct(context, current);
                return childIterator;
            }
        }

        [ImplementsMethod]
        public virtual object hasChildren(ScriptContext context)
        {
            object current;
            return this.isValid && (current = this.current(context)) is IPhpEnumerable && (current is PhpArray || current is SPL.Traversable);
        }

        #endregion

        #region Implementation details

        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RecursiveArrayIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RecursiveArrayIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region Constants

        public const int CHILD_ARRAYS_ONLY = 0x4;

        #endregion

        #region RecursiveIterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getChildren(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIterator)instance).getChildren(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object hasChildren(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIterator)instance).hasChildren(stack.Context);
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Helper class containing <see cref="IPhpEnumerable"/> object and its enumerator and current key and value.
    /// </summary>
    internal class EnumerableIteratorEntry
    {
        public readonly IPhpEnumerable obj;

        private IDictionaryEnumerator enumerator;

        public bool isValid { get; private set; }
        public object currentValue { get; private set; }
        public object currentKey { get; private set; }

        public EnumerableIteratorEntry(IPhpEnumerable/*!*/obj)
        {
            Debug.Assert(obj != null);

            this.isValid = false;
            this.enumerator = null;
            this.currentKey = this.currentValue = null;
            
            this.obj = obj;
        }

        public void rewind()
        {
            if (enumerator is PhpObject.PhpIteratorEnumerator)
                ((PhpObject.PhpIteratorEnumerator)enumerator).Reset();    // we can rewind() existing PhpIteratorEnumerator
            else
                enumerator = obj.GetForeachEnumerator(true, false, null); // we have to reinitialize (null or not PhpIteratorEnumerator)

            isValid = false;// enumerator.MoveNext();
        }

        public void next()
        {
            if (isValid = (enumerator != null && enumerator.MoveNext()))
            {
                this.currentValue = enumerator.Value;
                this.currentKey = enumerator.Key;
            }
            else
            {
                this.currentValue = this.currentKey = null;
            }
        }
    }

    /// <summary>
    /// Can be used to iterate through recursive iterators.
    /// </summary>
    [ImplementsType]
    public class RecursiveIteratorIterator : PhpObject, OuterIterator, Iterator, Traversable
    {
        private int maxDepth = -1;
        private int level { get { return (iterators.Count > 0) ? (iterators.Count - 1) : (0); } }

        /// <summary>
        /// The root iterator object.
        /// </summary>
        private DObject iterator;

        /// <summary>
        /// "Stack" of active iterators and their enumerators.
        /// </summary>
        private List<EnumerableIteratorEntry>/*!*/iterators = new List<EnumerableIteratorEntry>(3);
        private IEnumerator<KeyValuePair<object, object>> enumerator = null;
        private bool isValid = false;

        private Modes mode;
        private bool catchGetChild;

        private enum Modes
        {
            LeavesOnly = LEAVES_ONLY,
            SelfFirst = SELF_FIRST,
            ChildFirst = CHILD_FIRST,
        }

        [Flags]
        private enum Flags
        {
            CatchGetChilds = CATCH_GET_CHILD,
        }

        private IEnumerator<KeyValuePair<object, object>>/*!*/GetEnumerator(ScriptContext/*!*/context, List<EnumerableIteratorEntry>/*!*/iterators)
        {
            // reset the top level iterator
            if (iterators.Count == 0)
                iterators.Add(new EnumerableIteratorEntry(this.iterator));

            // rewind
            iterators[0].rewind();
            this.beginIteration(context);

            // yield return elements
            var l = 0;

            for (; ; )
            {
                iterators[l].next();

                if (iterators[l].isValid)
                {
                    var currentValue = iterators[l].currentValue;
                    var currentKey = iterators[l].currentKey;
                    
                    // iterators[l].current is another iterator?
                    if (Core.Convert.ObjectToBoolean(this.callHasChildren(context)) &&
                        (this.maxDepth == -1 || this.maxDepth > l))
                    {
                        
                        if (mode == Modes.SelfFirst)
                        {
                            this.nextElement(context);
                            yield return new KeyValuePair<object, object>(currentValue, currentKey);
                        }

                        var child = this.callGetChildren(context) as DObject;
                        if (child != null && child.RealObject is IPhpEnumerable)
                        {
                            iterators.Add(new EnumerableIteratorEntry(child.RealObject as IPhpEnumerable));

                            iterators[++l].rewind();
                            this.beginChildren(context);
                        }
                    }
                    else
                    {
                        this.nextElement(context);
                        yield return new KeyValuePair<object, object>(currentValue, currentKey);                     
                    }
                }
                else
                {
                    // iterator[l] end
                    if (l == 0)
                    {
                        break;
                    }
                    else
                    {
                        this.endChildren(context);
                        iterators.RemoveAt(l--);

                        if (mode == Modes.ChildFirst)
                        {
                            this.nextElement(context);
                            yield return new KeyValuePair<object, object>(iterators[l].currentValue, iterators[l].currentKey);
                        }
                    }
                }
            }
        }

        #region RecursiveIteratorIterator

        /// <summary>
        /// Begin children.
        /// </summary>
        [ImplementsMethod]
        public virtual object beginChildren(ScriptContext/*!*/context) { return null; }

        /// <summary>
        /// Begin Iteration.
        /// </summary>
        [ImplementsMethod]
        public virtual object beginIteration(ScriptContext/*!*/context) { return null; }

        /// <summary>
        /// Get children.
        /// </summary>
        [ImplementsMethod]
        public virtual object callGetChildren(ScriptContext/*!*/context)
        {
            var obj = ((iterators.Count > 0) ? iterators[iterators.Count - 1].obj : iterator) as RecursiveIterator;

            if (obj != null)
                return obj.getChildren(context);
            else
                return false;
        }

        /// <summary>
        /// Has children.
        /// </summary>
        [ImplementsMethod]
        public virtual object callHasChildren(ScriptContext/*!*/context)
        {
            var obj = ((iterators.Count > 0) ? iterators[iterators.Count - 1].obj : iterator) as RecursiveIterator;

            if (obj != null)
                return obj.hasChildren(context);
            else
                return false;
        }

        /// <summary>
        /// Construct a RecursiveIteratorIterator.
        /// </summary>
        [ImplementsMethod]
        public virtual object __construct(ScriptContext/*!*/context, object/*Traversable*/iterator, [Optional]object/*int*/mode /*= LEAVES_ONLY*/ , [Optional]object/*int*/flags /*= 0*/)
        {
            // ensure iterator is DObject
            var it = iterator as DObject;
            if (it.RealObject is IteratorAggregate)
                it = ((IteratorAggregate)it.RealObject).getIterator(context) as DObject;

            //
            this.mode = (mode == Arg.Default || mode == Type.Missing) ?
                Modes.LeavesOnly :
                (Modes)Core.Convert.ObjectToInteger(mode);

            this.catchGetChild = (flags == Arg.Default || mode == Type.Missing) ?
                false :
                ((Core.Convert.ObjectToInteger(mode) & (int)Flags.CatchGetChilds)) != 0;

            if (this.catchGetChild)
                PhpException.ArgumentValueNotSupported("flags", (int)Flags.CatchGetChilds);

            // prepare stack of iterators
            this.iterator = it;

            if (this.iterators != null)
            {
                // TODO: rewind
            }
            else
            {
                // TODO: error
            }

            //
            return null;
        }

        /// <summary>
        /// End children.
        /// </summary>
        [ImplementsMethod]
        public virtual object endChildren(ScriptContext/*!*/context) { return null; }

        /// <summary>
        /// End Iteration.
        /// </summary>
        [ImplementsMethod]
        public virtual object endIteration(ScriptContext/*!*/context) { return null; }

        /// <summary>
        /// Get the current depth of the recursive iteration.
        /// </summary>
        [ImplementsMethod]
        public virtual object getDepth(ScriptContext/*!*/context)
        {
            return level;
        }

        /// <summary>
        /// Get max depth.
        /// </summary>
        [ImplementsMethod]
        public virtual object getMaxDepth(ScriptContext/*!*/context)
        {
            return (maxDepth == -1) ? (object)false : maxDepth;
        }

        /// <summary>
        /// The current active sub iterator.
        /// </summary>
        [ImplementsMethod]
        public virtual object getSubIterator(ScriptContext/*!*/context, [Optional]object level)
        {
            int l = (level == Arg.Default) ? this.level : Core.Convert.ObjectToInteger(level);

            if (l == 0) return iterator;
            else if (l > 0 && l < iterators.Count) return iterators[l].obj;
            else return null;
        }

        /// <summary>
        /// Next element.
        /// </summary>
        [ImplementsMethod]
        public virtual object nextElement(ScriptContext/*!*/context) { return null; }

        /// <summary>
        /// Set max depth.
        /// </summary>
        [ImplementsMethod]
        public virtual object setMaxDepth(ScriptContext/*!*/context, [Optional]object/*int*/max_depth /*= -1*/ )
        {
            int i = (max_depth == Arg.Default || max_depth == Type.Missing) ? -1 : Core.Convert.ObjectToInteger(max_depth);

            if (i < -1)
            {
                // TODO: zend_throw_exception(spl_ce_OutOfRangeException, "Parameter max_depth must be >= -1", 0 TSRMLS_CC);
            }
            else
            {
                this.maxDepth = i;
            }

            return null;        
        }

        #endregion

        #region __call

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __call(object instance, PhpStack stack)
        {
            var name = stack.PeekValue(1);
            var args = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).__call(stack.Context, name, args);
        }

        [ImplementsMethod, NeedsArgless]    // TODO: hide this method to not be visible by PHP code, make this behaviour internal
        public virtual object __call(ScriptContext context, object name, object args)
        {
            var methodname = PhpVariable.AsString(name);
            var argsarr = args as PhpArray;

            if (this.iterator == null || argsarr == null)
            {
                PhpException.UndefinedMethodCalled(this.TypeName, methodname);
                return null;
            }

            // call the method on internal iterator, as in PHP,
            // only PHP leaves $this to self (which is potentionally dangerous and not correctly typed)
            context.Stack.AddFrame((ICollection)argsarr.Values);
            return this.iterator.InvokeMethod(methodname, null, context);
        }

        #endregion

        #region OuterIterator

        [ImplementsMethod]
        public object getInnerIterator(ScriptContext context)
        {
            var l = level;
            if (l == 0) return this.iterator;
            else if (l < iterators.Count) return iterators[level].obj;
            else return null;
        }

        #endregion

        #region Iterator

        [ImplementsMethod]
        public virtual object rewind(ScriptContext context)
        {
            // up to the first level
            while (iterators.Count > 1)
            {
                iterators.RemoveAt(iterators.Count - 1);
                endChildren(context);
            }

            // start enumeration
            this.enumerator = this.GetEnumerator(context, iterators);
            this.isValid = this.enumerator.MoveNext();

            return null;
        }

        [ImplementsMethod]
        public virtual object next(ScriptContext context)
        {
            if (enumerator == null)
                rewind(context);
            else
                this.isValid = enumerator.MoveNext();

            return null;
        }

        [ImplementsMethod]
        public virtual object valid(ScriptContext context)
        {
            if (!this.isValid && this.enumerator != null)
            {
                this.endIteration(context);
                this.enumerator = null;
            }

            return this.isValid;
        }

        [ImplementsMethod]
        public virtual object key(ScriptContext context)
        {
            if (iterators.Count > 0)
                return iterators[iterators.Count - 1].currentKey;
            else
                return null;
        }

        [ImplementsMethod]
        public virtual object current(ScriptContext context)
        {
            if (iterators.Count > 0)
                return iterators[iterators.Count - 1].currentValue;
            else
                return null;
        }

        #endregion

        #region Implementation details

        internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        #region Constants

        public const int LEAVES_ONLY = 0;
        public const int SELF_FIRST = 1;
        public const int CHILD_FIRST = 2;

        public const int CATCH_GET_CHILD = 16;

        #endregion

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RecursiveIteratorIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RecursiveIteratorIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region RecursiveIteratorIterator

        public static object beginChildren(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).beginChildren(stack.Context);
        }

        public static object beginIteration(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).beginIteration(stack.Context);
        }

        public static object callGetChildren(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).callGetChildren(stack.Context);
        }

        public static object callHasChildren(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).callHasChildren(stack.Context);
        }

        public static object __construct(object instance, PhpStack stack)
        {
            object iterator = stack.PeekValue(1);
            object mode = stack.PeekValueOptional(2);
            object flags = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).__construct(stack.Context, iterator, mode, flags);
        }

        public static object endChildren(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).endChildren(stack.Context);
        }

        public static object endIteration(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).endIteration(stack.Context);
        }

        public static object getDepth(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).getDepth(stack.Context);
        }

        public static object getMaxDepth(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).getMaxDepth(stack.Context);
        }

        public static object getSubIterator(object instance, PhpStack stack)
        {
            var level = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).getSubIterator(stack.Context, level);
        }

        public static object nextElement(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).nextElement(stack.Context);
        }

        public static object setMaxDepth(object instance, PhpStack stack)
        {
            object max_depth = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((RecursiveIteratorIterator)instance).setMaxDepth(stack.Context, max_depth);
        }

        #endregion

        #region interface OuterIterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getInnerIterator(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((OuterIterator)instance).getInnerIterator(stack.Context);
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

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected RecursiveIteratorIterator(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion
    }

    /// <summary>
    /// An Iterator that iterates over several iterators one after the other.
    /// </summary>
    [ImplementsType]
    public class AppendIterator : IteratorIterator, OuterIterator, Traversable, Iterator
    {
        /// <summary>
        /// Contained iterators.
        /// </summary>
        private List<EnumerableIteratorEntry>/*!*/iterators = new List<EnumerableIteratorEntry>(3);
        private int iterators_index = 0;

        private void NextInternal(ScriptContext/*!*/context)
        {
            if (iterators_index < iterators.Count)
            {
                var it = iterators[iterators_index];

                it.next();
                this.isValid = it.isValid;

                if (!this.isValid)
                {
                    // proceed to the next iterator, if available
                    this.iterators_index++;
                    if (iterators_index < iterators.Count)
                    {
                        iterators[iterators_index].rewind();
                        NextInternal(context);
                    }
                }
            }
            else
                this.isValid = false;   // no more iterators
        }

        #region AppendIterator

        [ImplementsMethod]
        public object __construct(ScriptContext/*!*/context)
        {
            return null;
        }

        [ImplementsMethod]
        public virtual object append(ScriptContext/*!*/context, object/*Iterator*/iterator)
        {
            var dobj = iterator as IPhpEnumerable;
            if (dobj != null)
            {
                var newit = new EnumerableIteratorEntry(dobj);
                this.iterators.Add(newit);

                if (iterators_index + 1 == iterators.Count)
                {
                    // PHP calls valid() on the previous iterator again, but we know it is at the end
                    // ...

                    // continue with new iterator
                    newit.rewind();
                    NextInternal(context);
                }
            }

            return null;
        }

        [ImplementsMethod]
        public virtual object getArrayIterator(ScriptContext/*!*/context)
        {
            throw new NotImplementedException(); // we dont use ArrayIterator internally
        }

        [ImplementsMethod]
        public virtual object getIteratorIndex(ScriptContext/*!*/context)
        {
            return this.iterators_index;
        }

        #endregion

        #region OuterIterator

        [ImplementsMethod]
        public override object getInnerIterator(ScriptContext/*!*/context)
        {
            return (isValid && iterators_index < iterators.Count) ? iterators[iterators_index].obj : null;
        }

        #endregion

        #region Iterator

        [ImplementsMethod]
        public override object rewind(ScriptContext context)
        {
            iterators_index = 0;

            if (iterators.Count > 0)
                iterators[0].rewind();

            NextInternal(context);

            return null;
        }

        [ImplementsMethod]
        public override object next(ScriptContext context)
        {
            NextInternal(context);
            return null;
        }

        [ImplementsMethod]
        public override object valid(ScriptContext context)
        {
            return base.valid(context);
        }

        [ImplementsMethod]
        public override object key(ScriptContext context)
        {
            return isValid ? iterators[iterators_index].currentKey : null;
        }

        [ImplementsMethod]
        public override object current(ScriptContext context)
        {
            return isValid ? iterators[iterators_index].currentValue : null;
        }

        #endregion

        #region Implementation details

        internal static new void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public AppendIterator(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public AppendIterator(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #endregion

        #region class AppendIterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object __construct(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((AppendIterator)instance).__construct(stack.Context);
        }

        [ImplementsMethod]
        public static object append(object instance, PhpStack stack)
        {
            object it = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((AppendIterator)instance).append(stack.Context, it);
        }

        [ImplementsMethod]
        public static object getArrayIterator(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((AppendIterator)instance).getArrayIterator(stack.Context);
        }

        [ImplementsMethod]
        public static object getIteratorIndex(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((AppendIterator)instance).getIteratorIndex(stack.Context);
        }

        #endregion

        #region interface OuterIterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object getInnerIterator(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((OuterIterator)instance).getInnerIterator(stack.Context);
        }

        #endregion

        #region interface Iterator

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object rewind(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).rewind(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object next(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).next(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object valid(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).valid(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object key(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).key(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static new object current(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Iterator)instance).current(stack.Context);
        }

        #endregion

        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected AppendIterator(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion        
    }
}
