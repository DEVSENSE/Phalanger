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
            object p = _obj;

            // empty
            if (p == null)
            {
                _obj = new DictionaryNode() { key = key, value = value };
            }
            // DictionaryNode
            else if (p.GetType() == typeof(DictionaryNode))
            {
                // replaces value if key already in collection,
                // counts items
                int count = 0;
                for (var node = (DictionaryNode)p; node != null; node = node.next)
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
                    _obj = new DictionaryNode() { key = key, value = value, next = (DictionaryNode)p };
                }
                else
                {
                    // upgrade to hashtable
                    var hashtable = ToHashtable((DictionaryNode)p);
                    hashtable.Add(key, value);
                    _obj = hashtable;
                }
            }
            // Hashtable
            else if (p.GetType() == typeof(Hashtable))
            {
                ((Hashtable)p)[key] = value;
            }
            else
            {
                throw new InvalidOperationException();
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

            object p = _obj;
            
            // empty container
            if (p != null)
            {
                if (p.GetType() == typeof(DictionaryNode))
                {
                    for (var node = (DictionaryNode)p; node != null; node = node.next)
                        if (node.key == key)
                            return node.value;
                }
                else if (p.GetType() == typeof(Hashtable))
                {
                    return ((Hashtable)p)[key];
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

            var p = _obj;

            if (p != null)
            {
                if (p.GetType() == typeof(DictionaryNode))
                {
                    DictionaryNode prev = null;
                    for (var node = (DictionaryNode)p; node != null; node = node.next)
                    {
                        if (node.key == key)
                        {
                            if (prev == null) _obj = p = node.next;
                            else prev.next = node.next;
                            
                            return true;
                        }
                    }
                }
                else if (p.GetType() == typeof(Hashtable))
                {
                    var hashtable = (Hashtable)p;
                    int count = hashtable.Count;
                    hashtable.Remove(key);
                    if (hashtable.Count != count)
                    {
                        if (hashtable.Count <= MaxListSize)
                            _obj = ToList((Hashtable)p);

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
            _obj = null;
        }

        /// <summary>
        /// Gets amount of properties in the container.
        /// </summary>
        public int Count
        {
            get
            {
                var p = _obj;

                if (p == null) return 0;
                if (p.GetType() == typeof(PropertyCollection.DictionaryNode)) return CountItems((PropertyCollection.DictionaryNode)p);
                if (p.GetType() == typeof(Hashtable)) return ((Hashtable)p).Count;
                throw new InvalidOperationException();
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

        #endregion
    }
}
