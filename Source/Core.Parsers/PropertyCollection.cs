using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PHP.Core
{
    /// <summary>
    /// Provides set of keyed properties.
    /// </summary>
    public interface IPropertyCollection
    {
        /// <summary>
        /// Sets property into collection.
        /// </summary>
        /// <param name="key">Key to the property, cannot be <c>null</c>.</param>
        /// <param name="value">Value.</param>
        void SetProperty(object key, object value);

        /// <summary>
        /// Sets property into collection under the key <c>typeof(T)</c>.
        /// </summary>
        /// <typeparam name="T">Type of the value and property key.</typeparam>
        /// <param name="value">Value.</param>
        void SetProperty<T>(T value);

        /// <summary>
        /// Gets property from the collection.
        /// </summary>
        /// <param name="key">Key to the property, cannot be <c>null</c>.</param>
        /// <returns>Property value or <c>null</c> if property does not exist.</returns>
        object GetProperty(object key);

        /// <summary>
        /// Gets property of type <typeparamref name="T"/> from the collection.
        /// </summary>
        /// <typeparam name="T">Type and key of the property.</typeparam>
        /// <returns>Property value.</returns>
        T GetProperty<T>();

        /// <summary>
        /// Removes property from the collection.
        /// </summary>
        /// <param name="key">Key to the property.</param>
        /// <returns><c>True</c> if property was found and removed, otherwise <c>false</c>.</returns>
        bool RemoveProperty(object key);

        /// <summary>
        /// Removes property from the collection.
        /// </summary>
        /// <typeparam name="T">Key to the property.</typeparam>
        /// <returns><c>True</c> if property was found and removed, otherwise <c>false</c>.</returns>
        bool RemoveProperty<T>();

        /// <summary>
        /// Clear the collection of properties.
        /// </summary>
        void ClearProperties();

        /// <summary>
        /// Gets or sets property.
        /// </summary>
        /// <param name="key">Property key, cannot be <c>null</c>.</param>
        /// <returns>Property value or <c>null</c> if property does not exist.</returns>
        object this[object key] { get; set; }
    }

    /// <summary>
    /// Manages list of properties, organized by a key.
    /// </summary>
    [Serializable]
    public struct PropertyCollection : IPropertyCollection
    {
        #region Fields & Properties

        /// <summary>
        /// Reference to actual collection of properties.
        /// </summary>
        /// <remarks>
        /// This mechanism saves memory for small properties sets.
        /// type of this object depends on amount of properties in the set.
        /// </remarks>
        private object _obj;

        /// <summary>
        /// Type of the hybrid table.
        /// </summary>
        private object _type;

        private static readonly object TypeHashtable = new object();
        private static readonly object TypeList = new object();
        
        /// <summary>
        /// If amount of properties exceeds this number, hashtable will be used instead of an array.
        /// </summary>
        private const int MaxListSize = 8;

        #endregion

        #region Nested class: DictionaryNode

        [Serializable]
        private sealed class DictionaryNode
        {
            public object key;
            public object value;
            public PropertyCollection.DictionaryNode next;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Sets property into the container.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public void SetProperty(object key, object value)
        {
            CheckKey(key);

            //
            object p = _type;

            // empty list
            if (p == null)
            {
                _type = key;
                _obj = value;
            }
            // one item list, with the same key
            else if (object.Equals(p, key))
            {
                _obj = value;
            }
            // linked list
            else if (object.ReferenceEquals(p, TypeList))
            {
                Debug.Assert(_obj is DictionaryNode);

                // replaces value if key already in collection,
                // counts items
                int count = 0;
                for (var node = (DictionaryNode)_obj; node != null; node = node.next)
                {
                    if (object.Equals(node.key, key))
                    {
                        node.value = value;
                        return;
                    }
                    count++;
                }

                // add new item
                if (count < MaxListSize)
                {
                    _obj = new DictionaryNode() { key = key, value = value, next = (DictionaryNode)_obj };
                }
                else
                {
                    // upgrade to hashtable
                    var hashtable = ToHashtable((DictionaryNode)_obj);
                    hashtable.Add(key, value);

                    _obj = hashtable;
                    _type = TypeHashtable;
                }
            }
            // hashtable
            else if (object.ReferenceEquals(p, TypeHashtable))
            {
                Debug.Assert(_obj is Hashtable);
                ((Hashtable)_obj)[key] = value;
            }
            // one item list,
            // upgrade to linked list
            else
            {
                _obj = new DictionaryNode()
                {
                    key = _type,
                    value = _obj,
                    next = new DictionaryNode()
                    {
                        key = key,
                        value = value,
                        next = null,
                    }
                };
                _type = TypeList;
            }
        }

        /// <summary>
        /// Sets property into the container.
        /// </summary>
        public void SetProperty<T>(T value)
        {
            SetProperty(typeof(T), (object)value);
        }

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><c>null</c> or property value.</returns>
        public object GetProperty(object key)
        {
            CheckKey(key);

            object p = _type;
            
            // empty container
            if (p != null)
            {
                if (object.Equals(p, key))
                {
                    return _obj;
                }
                else if (object.ReferenceEquals(p, TypeList))
                {
                    Debug.Assert(_obj is DictionaryNode);
                    for (var node = (DictionaryNode)_obj; node != null; node = node.next)
                        if (object.Equals(node.key, key))
                            return node.value;
                }
                else if (object.ReferenceEquals(p, TypeHashtable))
                {
                    Debug.Assert(_obj is Hashtable);
                    return ((Hashtable)_obj)[key];
                }
            }

            // nothing found
            return null;
        }

        /// <summary>
        /// Tries to get property from the container.
        /// </summary>
        public T GetProperty<T>()
        {
            return (T)GetProperty(typeof(T));
        }

        /// <summary>
        /// Removes property from the container.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><c>True</c> if property was found and removed. otherwise <c>false</c>.</returns>
        public bool RemoveProperty(object key)
        {
            CheckKey(key);

            var p = _type;

            if (p != null)
            {
                if (object.Equals(p, key))
                {
                    _type = null;
                    _obj = null;
                    return true;
                }
                else if (object.ReferenceEquals(p, TypeList))
                {
                    Debug.Assert(_obj is DictionaryNode);
                    DictionaryNode prev = null;
                    for (var node = (DictionaryNode)_obj; node != null; node = node.next)
                    {
                        if (object.Equals(node.key, key))
                        {
                            if (prev == null)
                            {
                                if ((_obj = node.next) == null)
                                {
                                    _type = null;   // empty list
                                }
                            }
                            else
                            {
                                prev.next = node.next;
                            }
                            
                            return true;
                        }

                        //
                        prev = node;
                    }
                }
                else if (object.ReferenceEquals(p, TypeHashtable))
                {
                    Debug.Assert(_obj is Hashtable);
                    var hashtable = (Hashtable)_obj;
                    int count = hashtable.Count;
                    hashtable.Remove(key);
                    if (hashtable.Count != count)
                    {
                        if (hashtable.Count <= MaxListSize)
                        {
                            _obj = ToList(hashtable);
                            _type = TypeList;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes property from the container.
        /// </summary>
        public bool RemoveProperty<T>()
        {
            return RemoveProperty(typeof(T));
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        public void ClearProperties()
        {
            _obj = _type = null;
        }

        /// <summary>
        /// Gets amount of properties in the container.
        /// </summary>
        public int Count
        {
            get
            {
                var p = _type;

                if (p == null) return 0;
                if (object.ReferenceEquals(p, TypeList)) return CountItems((PropertyCollection.DictionaryNode)_obj);
                if (object.ReferenceEquals(p, TypeHashtable)) return ((Hashtable)_obj).Count;
                return 1;
            }
        }

        /// <summary>
        /// Gets or sets named property.
        /// </summary>
        /// <param name="key">Property key.</param>
        /// <returns>Property value or <c>null</c>.</returns>
        public object this[object key]
        {
            get
            {
                return this.GetProperty(key);
            }
            set
            {
                this.SetProperty(key, value);
            }
        }
        
        #endregion

        #region Helper functions

        private static void CheckKey(object key)
        {
            if (key == null)
                throw new ArgumentNullException("key");
        }

        /// <summary>
        /// Counts items in the linked list.
        /// </summary>
        private static int CountItems(DictionaryNode head)
        {
            int count = 0;
            for (var p = head; p != null; p = p.next)
                count++;
            return count;
        }

        private static Hashtable/*!*/ToHashtable(DictionaryNode/*!*/node)
        {
            var hashtable = new Hashtable(13);

            for (var p = node; p != null; p = p.next)
                hashtable.Add(p.key, p.value);

            return hashtable;
        }
        private static DictionaryNode ToList(Hashtable/*!*/hashtable)
        {
            DictionaryNode list = null;
            foreach (DictionaryEntry p in hashtable)
            {
                list = new DictionaryNode() { key = p.Key, value = p.Value, next = list };
            }
            return list;
        }

        #endregion
    }
}
