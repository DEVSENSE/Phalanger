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

    public static class ArrayFunctions
    {
        public static bool TrueForAll<T>(T[] arr, Predicate<T> cond)
        {
            foreach (var t in arr)
                if (!cond(t)) return false;
            return true;
        }
    }

    public class SortedList 
    {
        private List<KeyValuePair<object, object>> list;
        System.Collections.IComparer comparer;
        bool sorted = false;

        public SortedList(System.Collections.IComparer comparer, int count)
        {
            this.comparer = comparer;
            list = new List<KeyValuePair<object, object>>(count);
        }

        public void Add(object key, object value)
        {
            list.Add(new KeyValuePair<object, object>(key, value));
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
                list.Sort(delegate(KeyValuePair<object, object> a, KeyValuePair<object, object>  b) { 
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
    }
}
