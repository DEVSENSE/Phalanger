using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace PHP.CoreCLR
{
    public static class CollectionFunctions
    {
        public static bool TrueForAll<T>(this List<T> col, Predicate<T> cond)
        {
            foreach (var t in col)
                if (!cond(t)) return false;
            return true;
        }
    }

    public class SortedList<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private List<KeyValuePair<TKey, TValue>> list;
        IComparer<TKey> comparer;
        bool sorted = false;

        public SortedList(int capacity, IComparer<TKey> comparer)
        {
            this.comparer = comparer;
            list = new List<KeyValuePair<TKey, TValue>>(capacity);
        }

        public void Add(TKey key, TValue value)
        {
            list.Add(new KeyValuePair<TKey, TValue>(key, value));
            sorted = false;
        }

        public object GetKey(int i)
        {
            EnsureSorted();
            return list[i].Key;
        }

        private void EnsureSorted()
        {
            if (!sorted)
            {
                list.Sort(delegate(KeyValuePair<TKey, TValue> a, KeyValuePair<TKey, TValue> b)
                { 
                    return comparer.Compare(a.Key, b.Key); 
                });
                sorted = true;
            }
        }

        public object GetByIndex(int i)
        {
            EnsureSorted();
            return list[i].Value;
        }

        public int Count
        {
            get { return list.Count; }
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

    }
}
