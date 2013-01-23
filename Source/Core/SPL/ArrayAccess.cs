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
    [Serializable]
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
                Exception.ThrowSplException(
                    _ctx => new RuntimeException(_ctx, true),
                    context,
                    CoreResources.spl_index_invalid, 0, null);
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

                using (var enumerator = arrdata.GetFastEnumerator())
                    while (enumerator.MoveNext())
                        this.array[enumerator.CurrentKey.Integer] = enumerator.CurrentValue;
            }
            else //if (!bindexes)
            {
                ReallocArray(arrdata.Count);

                int i = 0;
                using (var enumerator = arrdata.GetFastEnumerator())
                    while (enumerator.MoveNext())
                        this.array[i++] = enumerator.CurrentValue;
            }

            return null;
        }

        [ImplementsMethod]
        public virtual object toArray(ScriptContext/*!*/context)
        {
            if (array == null) return new PhpArray();

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

    /// <summary>
    /// This class allows objects to work as arrays.
    /// </summary>
    [ImplementsType]
    [Serializable]
    public class ArrayObject : PhpObject, IteratorAggregate, Traversable, ArrayAccess, Serializable, Countable
    {
        #region Constants

        /// <summary>
        /// Properties of the object have their normal functionality when accessed as list (var_dump, foreach, etc.).
        /// </summary>
        public const int STD_PROP_LIST = 1;

        /// <summary>
        /// Entries can be accessed as properties (read and write).
        /// </summary>
        public const int ARRAY_AS_PROPS = 2;

        #endregion

        #region Implementation details

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ArrayObject(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        {
        }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ArrayObject(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        {
        }

        internal static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ArrayObject

        /// <summary>
        /// Construct a new array object.
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <param name="input">Optional. The input parameter accepts an array or an Object.</param>
        /// <param name="flags">Optional. Flags to control the behaviour of the ArrayObject object. See ArrayObject::setFlags().</param>
        /// <param name="iterator_class">Optional. Specify the class that will be used for iteration of the ArrayObject object.</param>
        [ImplementsMethod]
        public virtual object __construct(ScriptContext/*!*/context, [Optional]object input, [Optional]object/*int*/flags/*= 0*/, [Optional]object/*string*/iterator_class/*= "ArrayIterator"*/ )
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            var input = stack.PeekValueOptional(1);
            var flags = stack.PeekValueOptional(2);
            var iterator_class = stack.PeekValueOptional(3);
            stack.RemoveFrame();
            return ((ArrayObject)instance).__construct(stack.Context, input, flags, iterator_class);
        }

        //public void append ( mixed $value )
        //public array exchangeArray ( mixed $input )
        //public array getArrayCopy ( void )
        
        /// <summary>
        /// Appends a new value as the last element.
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <param name="value">The value being appended.</param>
        /// <remarks>This method cannot be called when the ArrayObject was constructed from an object. Use ArrayObject::offsetSet() instead.</remarks>
        [ImplementsMethod]
        public virtual object append(ScriptContext context, object value)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object append(object instance, PhpStack stack)
        {
            var value = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).append(stack.Context, value);
        }

        /// <summary>
        /// Exchange the current array with another array or object.
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <param name="input">The new array or object to exchange with the current array.</param>
        /// <returns>Returns the old array.</returns>
        [ImplementsMethod]
        public virtual object exchangeArray(ScriptContext context, object input)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object exchangeArray(object instance, PhpStack stack)
        {
            var input = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).exchangeArray(stack.Context, input);
        }

        /// <summary>
        /// Exports the ArrayObject to an array.
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <returns>Returns a copy of the array. When the ArrayObject refers to an object an array of the public properties of that object will be returned.</returns>
        [ImplementsMethod]
        public virtual object getArrayCopy(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getArrayCopy(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).getArrayCopy(stack.Context);
        }

        //public int getFlags ( void )
        //public void setFlags ( int $flags )

        /// <summary>
        /// Gets the behavior flags of the ArrayObject. See the ArrayObject::setFlags method for a list of the available flags.
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <returns>Returns the behavior flags of the ArrayObject.</returns>
        [ImplementsMethod]
        public virtual object getFlags(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getFlags(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).getFlags(stack.Context);
        }

        /// <summary>
        /// Set the flags that change the behavior of the ArrayObject.
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <param name="flags">The new ArrayObject behavior. It takes on either a bitmask, or named constants. Using named constants is strongly encouraged to ensure compatibility for future versions.</param>
        [ImplementsMethod]
        public virtual object setFlags(ScriptContext context, object/*int*/flags)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setFlags(object instance, PhpStack stack)
        {
            var flags = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).setFlags(stack.Context, flags);
        }
        
        //public string getIteratorClass ( void )
        //public void setIteratorClass ( string $iterator_class )

        /// <summary>
        /// Gets the class name of the array iterator that is used by ArrayObject::getIterator().
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <returns>Returns the iterator class name that is used to iterate over this object.</returns>
        [ImplementsMethod]
        public virtual object getIteratorClass(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getIteratorClass(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).getIteratorClass(stack.Context);
        }

        /// <summary>
        /// Sets the classname of the array iterator that is used by ArrayObject::getIterator().
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <param name="iterator_class">The classname of the array iterator to use when iterating over this object.</param>
        [ImplementsMethod]
        public virtual object setIteratorClass(ScriptContext context, object/*string*/iterator_class)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setIteratorClass(object instance, PhpStack stack)
        {
            var iterator_class = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).setIteratorClass(stack.Context, iterator_class);
        }
        
        //public void asort ( void )
        //public void ksort ( void )
        //public void natcasesort ( void )
        //public void natsort ( void )
        
        /// <summary>
        /// Sorts the entries such that the keys maintain their correlation with the entries they are associated with. This is used mainly when sorting associative arrays where the actual element order is significant.
        /// </summary>
        [ImplementsMethod]
        public virtual object asort(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object asort(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).asort(stack.Context);
        }

        /// <summary>
        /// Sorts the entries by key, maintaining key to entry correlations. This is useful mainly for associative arrays.
        /// </summary>
        [ImplementsMethod]
        public virtual object ksort(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object ksort(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).ksort(stack.Context);
        }

        /// <summary>
        /// This method is a case insensitive version of ArrayObject::natsort.
        /// This method implements a sort algorithm that orders alphanumeric strings in the way a human being would while maintaining key/value associations. This is described as a "natural ordering".
        /// </summary>
        [ImplementsMethod]
        public virtual object natcasesort(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object natcasesort(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).natcasesort(stack.Context);
        }

        /// <summary>
        /// This method implements a sort algorithm that orders alphanumeric strings in
        /// the way a human being would while maintaining key/value associations. This is
        /// described as a "natural ordering". An example of the difference between this
        /// algorithm and the regular computer string sorting algorithms (used in ArrayObject::asort)
        /// method can be seen in the example below.
        /// </summary>
        [ImplementsMethod]
        public virtual object natsort(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object natsort(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).natsort(stack.Context);
        }

        //public void uasort ( callable $cmp_function )
        //public void uksort ( callable $cmp_function )

        /// <summary>
        /// This function sorts the entries such that keys maintain their correlation with the entry that they are associated with, using a user-defined comparison function.
        /// This is used mainly when sorting associative arrays where the actual element order is significant.
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <param name="cmp_function">Function cmp_function should accept two parameters
        /// which will be filled by pairs of entries. The comparison function must return
        /// an integer less than, equal to, or greater than zero if the first argument is
        /// considered to be respectively less than, equal to, or greater than the second.</param>
        [ImplementsMethod]
        public virtual object uasort(ScriptContext context, object/*callable*/cmp_function)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object uasort(object instance, PhpStack stack)
        {
            var cmp_function = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).uasort(stack.Context, cmp_function);
        }

        /// <summary>
        /// This function sorts the keys of the entries using a user-supplied comparison function. The key to entry correlations will be maintained.
        /// </summary>
        /// <param name="context">Script Context. Cannot be null.</param>
        /// <param name="cmp_function">The callback comparison function.
        /// Function cmp_function should accept two parameters which will be filled by
        /// pairs of entry keys. The comparison function must return an integer less than,
        /// equal to, or greater than zero if the first argument is considered to be
        /// respectively less than, equal to, or greater than the second.</param>
        [ImplementsMethod]
        public virtual object uksort(ScriptContext context, object/*callable*/cmp_function)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object uksort(object instance, PhpStack stack)
        {
            var cmp_function = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).uksort(stack.Context, cmp_function);
        }
        
        #endregion

        #region Serialization (CLR only)
#if !SILVERLIGHT

        /// <summary>
        /// Deserializing constructor.
        /// </summary>
        protected ArrayObject(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

#endif
        #endregion

        #region Serializable Members

        [ImplementsMethod]
        public virtual object serialize(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object serialize(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).serialize(stack.Context);
        }

        [ImplementsMethod]
        public virtual object unserialize(ScriptContext context, object data)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object unserialize(object instance, PhpStack stack)
        {
            var serialized = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).unserialize(stack.Context, serialized);
        }

        #endregion

        #region Countable Members

        [ImplementsMethod]
        public virtual object count(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object count(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).count(stack.Context);
        }

        #endregion

        #region IteratorAggregate Members

        [ImplementsMethod]
        public virtual object getIterator(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getIterator(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ArrayObject)instance).getIterator(stack.Context);
        }

        #endregion

        #region ArrayAccess Members

        [ImplementsMethod]
        public virtual object offsetGet(ScriptContext context, object index)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetGet(object instance, PhpStack stack)
        {
            var index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).offsetGet(stack.Context, index);
        }

        [ImplementsMethod]
        public virtual object offsetSet(ScriptContext context, object index, object value)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetSet(object instance, PhpStack stack)
        {
            var index = stack.PeekValue(1);
            var value = stack.PeekValue(2);
            stack.RemoveFrame();
            return ((ArrayObject)instance).offsetSet(stack.Context, index, value);
        }

        [ImplementsMethod]
        public virtual object offsetUnset(ScriptContext context, object index)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetUnset(object instance, PhpStack stack)
        {
            var index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).offsetUnset(stack.Context, index);
        }

        [ImplementsMethod]
        public virtual object offsetExists(ScriptContext context, object index)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object offsetExists(object instance, PhpStack stack)
        {
            var index = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ArrayObject)instance).offsetExists(stack.Context, index);
        }

        #endregion
    }

	internal class PhpArrayObject : PhpArray
	{
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

        protected override object GetArrayItemOverride(object key, bool quiet)
		{
			PhpStack stack = ScriptContext.CurrentContext.Stack;
			stack.AddFrame(key);
			return PhpVariable.Dereference(arrayAccess.InvokeMethod(offsetGet, null, stack.Context));
		}

        protected override PhpReference GetArrayItemRefOverride()
		{
			return GetUserArrayItemRef(arrayAccess, null, ScriptContext.CurrentContext);
		}

        protected override PhpReference GetArrayItemRefOverride(object key)
		{
			return GetUserArrayItemRef(arrayAccess, key, ScriptContext.CurrentContext);
		}

        protected override PhpReference/*!*/ GetArrayItemRefOverride(int key)
		{
			return GetUserArrayItemRef(arrayAccess, key, ScriptContext.CurrentContext);
		}

        protected override PhpReference/*!*/ GetArrayItemRefOverride(string key)
		{
			return GetUserArrayItemRef(arrayAccess, key, ScriptContext.CurrentContext);
		}	

		protected override void SetArrayItemOverride(object value)
		{
			PhpStack stack = ScriptContext.CurrentContext.Stack;
			stack.AddFrame(null, value);
			arrayAccess.InvokeMethod(offsetSet, null, stack.Context);
		}

		protected override void SetArrayItemOverride(object key, object value)
		{
			PhpStack stack = ScriptContext.CurrentContext.Stack;
			stack.AddFrame(key, value);
			arrayAccess.InvokeMethod(offsetSet, null, stack.Context);
		}

        protected override void SetArrayItemOverride(int key, object value)
		{
            SetArrayItemOverride((object)key, value);
		}

        protected override void SetArrayItemOverride(string key, object value)
		{
            SetArrayItemOverride((object)key, value);
		}

        protected override void SetArrayItemRefOverride(object key, PhpReference value)
		{
			PhpStack stack = ScriptContext.CurrentContext.Stack;
			stack.AddFrame(key, value);
			arrayAccess.InvokeMethod(offsetSet, null, stack.Context);
		}

        protected override PhpArray EnsureItemIsArrayOverride()
		{
			return EnsureIndexerResultIsRefArray(null);
		}
		
		protected override PhpArray EnsureItemIsArrayOverride(object key)
		{
			// an object behaving like an array:
			return EnsureIndexerResultIsRefArray(key);
		}

		protected override DObject EnsureItemIsObjectOverride(ScriptContext/*!*/ context)
		{
			return EnsureIndexerResultIsRefObject(null, context);
		}

        protected override DObject EnsureItemIsObjectOverride(object key, ScriptContext/*!*/ context)
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

            object new_value;
            var wrappedwarray = Operators.EnsureObjectIsArray(ref_result.Value, out new_value);
            if (wrappedwarray != null)
            {
                if (new_value != null) ref_result.Value = new_value;
                return wrappedwarray;
            }

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
