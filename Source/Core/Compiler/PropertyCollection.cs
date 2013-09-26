using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Core
{
    public interface IPropertyOwner
    {
        void SetProperty(object key, object value);
        object GetProperty(object key);
        bool RemoveProperty(object key);
        void ClearProperties();
        object this[object key] { get; set; }
    }

    /// <summary>
    /// Manages list of properties, organized by their <see cref="System.Type"/>.
    /// </summary>
    [Serializable]
    public struct PropertyCollection : IPropertyOwner
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
            else if (p == key)
            {
                _obj = value;
            }
            // linked list
            else if (p == TypeList)
            {
                Debug.Assert(_obj is DictionaryNode);

                // replaces value if key already in collection,
                // counts items
                int count = 0;
                for (var node = (DictionaryNode)_obj; node != null; node = node.next)
                {
                    if (node.key == key)
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
            else if (p == TypeHashtable)
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
                if (p == key)
                {
                    return _obj;
                }
                else if (p == TypeList)
                {
                    for (var node = (DictionaryNode)_obj; node != null; node = node.next)
                        if (node.key == key)
                            return node.value;
                }
                else if (p == TypeHashtable)
                {
                    return ((Hashtable)_obj)[key];
                }
            }

            // nothing found
            return null;
        }

        /// <summary>
        /// Removes property from the container.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns><c>True</c> if property was found and removed. otherwise <c>false</c>.</returns>
        public bool Remove(object key)
        {
            CheckKey(key);

            var p = _type;

            if (p != null)
            {
                if (p == key)
                {
                    _type = null;
                    _obj = null;
                    return true;
                }
                else if (p == TypeList)
                {
                    DictionaryNode prev = null;
                    for (var node = (DictionaryNode)_obj; node != null; node = node.next)
                    {
                        if (node.key == key)
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
                    }
                }
                else if (p == TypeHashtable)
                {
                    var hashtable = (Hashtable)_obj;
                    int count = hashtable.Count;
                    hashtable.Remove(key);
                    if (hashtable.Count != count)
                    {
                        if (hashtable.Count <= MaxListSize)
                        {
                            _obj = ToList((Hashtable)p);
                            _type = TypeHashtable;
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        public void Clear()
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
                if (p == TypeList) return CountItems((PropertyCollection.DictionaryNode)_obj);
                if (p == TypeHashtable) return ((Hashtable)_obj).Count;
                return 1;
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

        #region IPropertyOwner

        void IPropertyOwner.SetProperty(object key, object value)
        {
            this.SetProperty(key, value);
        }

        object IPropertyOwner.GetProperty(object key)
        {
            return this.GetProperty(key);
        }

        bool IPropertyOwner.RemoveProperty(object key)
        {
            return this.Remove(key);
        }

        void IPropertyOwner.ClearProperties()
        {
            this.Clear();
        }

        object IPropertyOwner.this[object key]
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
    }
}
