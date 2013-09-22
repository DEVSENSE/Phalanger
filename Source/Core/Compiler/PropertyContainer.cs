using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Core
{
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

            // empty list or one-item list
            if (p == null || p.GetType() == typeof(T))
            {
                this.obj = value;
                return;
            }

            // 8-items list
            if (p.GetType() == typeof(object[]))
            {
                if (SetProperty<T>((object[])p, value))
                {
                    return;
                }
                else
                {
                    this.obj = ToDictionary((object[])p);
                }
            }
            // multiple items
            if (p.GetType() == typeof(Dictionary<Type, object>))
            {
                ((Dictionary<Type, object>)p)[typeof(T)] = (object)value;
                return;
            }

            // upgrade one-list to array
            this.obj = ToArray(value);
        }

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        /// <typeparam name="T">Type of the property to get.</typeparam>
        /// <returns><c>null</c> or property value.</returns>
        public T GetProperty<T>()
        {
            object p = this.obj;
            
            // empty list
            if (p == null)
                return default(T);

            // one-item list
            if (p.GetType() == typeof(T))
                return (T)p;

            // 8-items list
            if (p.GetType() == typeof(object[]))
                return GetProperty<T>((object[])p);
            
            // multiple items
            Debug.Assert(p is Dictionary<Type, object>);
            return (T)((Dictionary<Type, object>)p)[typeof(T)];
        }

        /// <summary>
        /// Removes property from the container.
        /// </summary>
        /// <typeparam name="T">Type of the property to remove.</typeparam>
        /// <returns><c>True</c> if property was found and removed. otherwise <c>false</c>.</returns>
        public bool Remove<T>()
        {
            var p = this.obj;

            if (p == null)
                return false;

            if (p.GetType() == typeof(T))
            {
                this.obj = null;
                return true;
            }

            if (p.GetType() == typeof(object[]))
            {
                var arr = (object[])p;
                for (int i = 0; i < arr.Length; i++)
                    if (arr[i] != null && arr[i].GetType() == typeof(T))
                    {
                        arr[i] = null;
                        return true;
                    }

                return false;
            }

            if (p.GetType() == typeof(Dictionary<Type, object>))
            {
                return ((Dictionary<Type, object>)p).Remove(typeof(T));
            }

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

            var dict = new Dictionary<Type, object>(values.Length + 1);
            object obj;
            for (int i = 0; i < values.Length; i++)
            {
                if ((obj = values[i]) != null)
                    dict[obj.GetType()] = obj;
            }

            return dict;
        }

        private static object[] ToArray(object value)
        {
            var arr = new object[8];
            arr[0] = value;
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
