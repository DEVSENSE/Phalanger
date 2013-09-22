using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Core
{
    public interface IPropertyOwner
    {
        void SetProperty<T>(T value);
        T GetProperty<T>();
        bool RemoveProperty<T>();
        void ClearProperties();
    }

    /// <summary>
    /// Manages list of properties, organized by their <see cref="System.Type"/>.
    /// </summary>
    public struct PropertyContainer
    {
        #region Fields

        /// <summary>
        /// Reference to either the one property,
        /// or a dictionary of properties.
        /// </summary>
        /// <remarks>
        /// This mechanism saves memory for small properties sets.
        /// type of this object depends on amount of properties in the set.
        /// </remarks>
        private object obj;

        /// <summary>
        /// If amount of properties exceeds this number, Dictionary should be used instead of array or list.
        /// </summary>
        private const int MinDictSize = 9;

        #endregion

        #region Public methods

        /// <summary>
        /// Sets property into the container.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="value">Value.</param>
        public void SetProperty<T>(T value)
        {
            if (value == null)
                throw new ArgumentNullException("value");

            if (value.GetType() != typeof(T))
                throw new ArgumentException();

            if (typeof(T) == typeof(object[]))
                throw new ArgumentException();

            if (typeof(T) == typeof(Dictionary<Type, object>))
                throw new ArgumentException();
            
            //
            object p = this.obj;

            // empty container or one-item container
            if (p == null || p.GetType() == typeof(T))
            {
                this.obj = value;
                return;
            }

            // few items container
            if (p.GetType() == typeof(object[]))
            {
                if (SetProperty<T>((object[])p, value))
                {
                    return;
                }
                else
                {
                    this.obj = p = ToDictionary((object[])p);
                    // continue to add {value} to dictionary
                }
            }
            // many items container
            if (p.GetType() == typeof(Dictionary<Type, object>))
            {
                ((Dictionary<Type, object>)p)[typeof(T)] = (object)value;
                return;
            }

            // upgrade one-item container to array
            this.obj = ToArray((object)value, this.obj);
        }

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        /// <typeparam name="T">Type of the property to get.</typeparam>
        /// <returns><c>null</c> or property value.</returns>
        public T GetProperty<T>()
        {
            object p = this.obj;
            
            // empty container
            if (p != null)
            {
                // one-item container
                if (p.GetType() == typeof(T))
                    return (T)p;

                // few items container
                if (p.GetType() == typeof(object[]))
                    return GetProperty<T>((object[])p);

                // many items container
                if (p.GetType() == typeof(Dictionary<Type, object>))
                {
                    object obj;
                    if (((Dictionary<Type, object>)p).TryGetValue(typeof(T), out obj))
                        return (T)obj;
                }
            }

            // nothing found
            return default(T);
        }

        /// <summary>
        /// Removes property from the container.
        /// </summary>
        /// <typeparam name="T">Type of the property to remove.</typeparam>
        /// <returns><c>True</c> if property was found and removed. otherwise <c>false</c>.</returns>
        public bool Remove<T>()
        {
            var p = this.obj;

            // empty container
            if (p == null)
                return false;

            // single item container
            if (p.GetType() == typeof(T))
            {
                this.obj = null;
                return true;
            }

            // container of few items
            if (p.GetType() == typeof(object[]))
            {
                // count items,
                // find item of type T

                var arr = (object[])p;  // array of items
                int count = 0;          // amount of items left in array
                object lastp = null;    // reference to last/some item left in array
                int index = -1;         // index of item of type T (if any)
                for (int i = 0; i < arr.Length; i++)
                {
                    var arrp = arr[i];
                    if (arrp != null)
                    {
                        if (arrp.GetType() == typeof(T))
                        {
                            index = i;
                        }
                        else
                        {
                            lastp = arrp;
                            count++;
                        }
                    }
                }

                if (index >= 0)
                {
                    // item found
                    if (count <= 1)
                    {
                        // only one or none items left
                        // downgrade to single-item container
                        this.obj = lastp;
                    }
                    else
                    {
                        // remove the item from the array
                        arr[index] = null;
                    }
                    
                    return true;
                }
                else
                {
                    return false;
                }
            }

            // container of many items
            if (p.GetType() == typeof(Dictionary<Type, object>))
            {
                var dict = (Dictionary<Type, object>)p;
                if (dict.Remove(typeof(T)))
                {
                    if (dict.Count < MinDictSize)
                    {
                        // dowgrade to object[]
                        this.obj = dict.Values.ToArray();
                    }

                    return true;
                }
                else
                {
                    return false;
                }
            }

            // nothing found
            return false;
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        public void Clear()
        {
            this.obj = null;
        }

        /// <summary>
        /// Gets amount of properties in the container.
        /// </summary>
        public int Count
        {
            get
            {
                var p = this.obj;

                if (p == null) return 0;
                if (p.GetType() == typeof(object[])) return ((object[])p).Count(x => x != null);
                if (p.GetType() == typeof(Dictionary<Type, object>)) return ((Dictionary<Type, object>)p).Count;
                return 1;
            }
        }

        #endregion

        #region Helper functions

        private static Dictionary<Type, object> ToDictionary(object[]/*!*/values)
        {
            Debug.Assert(values != null);

            var dict = new Dictionary<Type, object>(MinDictSize);
            object obj;
            for (int i = 0; i < values.Length; i++)
            {
                if ((obj = values[i]) != null)
                    dict[obj.GetType()] = obj;
            }

            return dict;
        }

        private static object[] ToArray(object value1, object value2)
        {
            Debug.Assert(MinDictSize >= 2);

            var arr = new object[MinDictSize - 1];
            arr[0] = value1;
            arr[1] = value2;
            return arr;
        }

        private static bool SetProperty<T>(object[]/*!*/array, T value)
        {
            Debug.Assert(array != null);

            object obj;
            for (int i = 0; i < array.Length; i++)
            {
                if ((obj = array[i]) == null || obj.GetType() == typeof(T))
                {
                    array[i] = (object)value;
                    return true;
                }
            }

            return false;
        }

        private static T GetProperty<T>(object[]/*!*/array)
        {
            Debug.Assert(array != null);

            object obj;
            for (int i = 0; i < array.Length; i++)
            {
                if ((obj = array[i]) != null && obj.GetType() == typeof(T))
                    return (T)obj;
            }

            return default(T);
        }

        #endregion
    }
}
