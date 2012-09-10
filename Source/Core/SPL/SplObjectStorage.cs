/*

 Copyright (c) 2011 Jakub Misek

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
    /// The SplObjectStorage class provides a map from objects to data or, by ignoring data, an object set. This dual purpose can be useful in many cases involving the need to uniquely identify objects.
	/// </summary>
    [ImplementsType]
    public class SplObjectStorage : PhpObject, Countable, Iterator, Traversable, Serializable, ArrayAccess
    {
        /// <summary>
        /// Internal storage.
        /// </summary>
        private OrderedHashtable<object>/*!*/storage = new OrderedHashtable<object>();

        /// <summary>
        /// Internal index while enumerating.
        /// </summary>
        private int index = 0;

        #region SplObjectStorage

        /// <summary>
        /// Tries to cast <paramref name="storage"/> to <see cref="SplObjectStorage"/>.
        /// </summary>
        /// <param name="storage">The object.</param>
        /// <returns><see cref="SplObjectStorage"/> instance or <c>null</c>.</returns>
        private static SplObjectStorage asObjectStorage(object storage)
        {
            var dobj = storage as DObject;
            if (dobj != null)
                return dobj.RealObject as SplObjectStorage;

            return null;
        }

        /// <summary>
        /// Adds all objects from another storage.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        [ImplementsMethod]
        public object addAll(ScriptContext/*!*/context, object storage)
        {
            var data = asObjectStorage(storage);
            if (data != null)
            {
                foreach (var x in data.storage)
                {
                    this.attach(context, x.Key, x.Value);
                }
            }
            else
            {
                // ...
            }
            return null;
        }
        
        /// <summary>
        /// Adds an object in the storage.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="obj"></param>
        /// /// <param name="data"></param>
        /// <returns></returns>
        [ImplementsMethod]
        public object attach(ScriptContext/*!*/context, object obj, [Optional]object data)
        {
            if (obj == null) return null;
            if (data == Arg.Default) data = null;

            this.storage[obj] = data;

            return null;
        }

        /// <summary>
        /// Checks if the storage contains a specific object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        [ImplementsMethod]
        public object contains(ScriptContext/*!*/context, object obj)
        {
            return this.storage.ContainsKey(obj);
        }

        /// <summary>
        /// Removes an object from the storage.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        [ImplementsMethod]
        public object detach(ScriptContext/*!*/context, object obj)
        {
            return this.storage.Remove(obj);
        }

        /// <summary>
        /// Calculate a unique identifier for the contained objects.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        [ImplementsMethod]
        public object getHash(ScriptContext/*!*/context, object obj)
        {
            return obj.GetHashCode().ToString("x32");   // see spl_object_hash()
        }

        /// <summary>
        /// Removes objects contained in another storage from the current storage.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        [ImplementsMethod]
        public object removeAll(ScriptContext/*!*/context, object storage)
        {
            var data = asObjectStorage(storage);
            if (data != null)
            {
                foreach (var x in data.storage)
                {
                    this.detach(context, x.Key);
                }
            }
            else
            {
                // ...
            }
            return null;
        }

        /// <summary>
        /// Removes all objects except for those contained in another storage from the current storage.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="storage"></param>
        /// <returns></returns>
        [ImplementsMethod]
        public object removeAllExcept(ScriptContext/*!*/context, object storage)
        {
            var data = asObjectStorage(storage);
            if (data != null)
            {
                if (data.storage.Count == 0)
                    return null;    // nothing to remove

                // remove all except these in {data.storage}
                foreach (var x in this.storage)
                    if (!data.storage.ContainsKey(x.Key))
                        this.storage.Remove(x.Key);
            }
            else
            {
                // ...
            }
            return null;
        }

        /// <summary>
        /// Returns the data associated with the current iterator entry.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        [ImplementsMethod]
        public object getInfo(ScriptContext/*!*/context)
        {
            return (this.enumerator != null && !this.enumerator.AtEnd) ? this.enumerator.Current.Value : null;
        }

        /// <summary>
        /// Sets the data associated with the current iterator entry.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        [ImplementsMethod]
        public object setInfo(ScriptContext/*!*/context, object data)
        {
            if (this.enumerator != null && !this.enumerator.AtEnd)
                this.enumerator.current.Value = data;

            return null;
        }

        #endregion

        #region Countable

        [ImplementsMethod]
        public object count(ScriptContext/*!*/context)
        {
            return this.storage.Count;
        }

        #endregion

        #region Iterator

        private OrderedHashtable<object>.Enumerator enumerator;

        [ImplementsMethod]
        public object rewind(ScriptContext context)
        {
            this.enumerator = this.storage.GetEnumerator();
            this.enumerator.MoveFirst();
            this.index = 0;

            return null;
        }

        [ImplementsMethod]
        public object next(ScriptContext context)
        {
            if (this.enumerator == null)
                rewind(context);

            this.enumerator.MoveNext();
            this.index++;

            return null;
        }

        [ImplementsMethod]
        public object valid(ScriptContext context)
        {
            return this.enumerator != null && !this.enumerator.AtEnd;
        }

        [ImplementsMethod]
        public object key(ScriptContext context)
        {
            return this.index;
        }

        [ImplementsMethod]
        public object current(ScriptContext context)
        {
            return (this.enumerator != null) ? this.enumerator.Current.Key : null;
        }

        #endregion

        #region Serializable (NS)

        [ImplementsMethod]
        public object serialize(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [ImplementsMethod]
        public object unserialize(ScriptContext context, object data)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ArrayAccess

        [ImplementsMethod]
        public object offsetGet(ScriptContext context, object index)
        {
            return (index != null) ? this.storage[index] : null;
        }

        [ImplementsMethod]
        public object offsetSet(ScriptContext context, object index, object value)
        {
            if (index != null)
                this.storage[index] = value;
            return null;
        }

        [ImplementsMethod]
        public object offsetUnset(ScriptContext context, object index)
        {
            if (index != null)
                this.storage.Remove(index);
            return null;
        }

        [ImplementsMethod]
        public object offsetExists(ScriptContext context, object index)
        {
            return this.storage.ContainsKey(index);
        }

        #endregion

        #region Implementation details

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SplObjectStorage(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public SplObjectStorage(ScriptContext context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        #region class SplObjectStorage

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object addAll(object instance, PhpStack stack)
        {
            var storage = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplObjectStorage)instance).addAll(stack.Context, storage);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object attach(object instance, PhpStack stack)
        {
            var storage = stack.PeekValue(1);
            var data = stack.PeekValueOptional(2);
            stack.RemoveFrame();
            return ((SplObjectStorage)instance).attach(stack.Context, storage, data);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object contains(object instance, PhpStack stack)
        {
            var obj = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplObjectStorage)instance).contains(stack.Context, obj);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object detach(object instance, PhpStack stack)
        {
            var obj = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplObjectStorage)instance).detach(stack.Context, obj);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getHash(object instance, PhpStack stack)
        {
            var obj = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplObjectStorage)instance).getHash(stack.Context, obj);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object removeAll(object instance, PhpStack stack)
        {
            var storage = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplObjectStorage)instance).removeAll(stack.Context, storage);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object removeAllExcept(object instance, PhpStack stack)
        {
            var storage = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplObjectStorage)instance).removeAllExcept(stack.Context, storage);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getInfo(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((SplObjectStorage)instance).getInfo(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setInfo(object instance, PhpStack stack)
        {
            var data = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((SplObjectStorage)instance).setInfo(stack.Context, data);
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

        #region interface Serializable

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object serialize(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((Serializable)instance).serialize(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object unserialize(object instance, PhpStack stack)
        {
            object data = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((Serializable)instance).unserialize(stack.Context, data);
        }

        #endregion

        #endregion
    }
}
