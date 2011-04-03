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

namespace PHP.CoreCLR
{
    public static class ArrayEx
    {
        public static bool TrueForAll<T>(T[] arr, Predicate<T> cond)
        {
            foreach (var t in arr)
                if (!cond(t)) return false;
            return true;
        }
   
        public static bool Exists<T>(T[] array,Predicate<T> match)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            return (ArrayEx.FindIndex<T>(array, 0, array.Length, match) != -1);
        }

        private static int FindIndex<T>(T[] array, int startIndex, int count, Predicate<T> match)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if ((startIndex < 0) || (startIndex > array.Length))
            {
                throw new ArgumentOutOfRangeException("startIndex");
            }
            if ((count < 0) || (startIndex > (array.Length - count)))
            {
                throw new ArgumentOutOfRangeException("count");
            }
            if (match == null)
            {
                throw new ArgumentNullException("match");
            }
            int num = startIndex + count;
            for (int i = startIndex; i < num; i++)
            {
                if (match(array[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public static TOutput[] ConvertAll<TInput, TOutput>(TInput[] array, Converter<TInput, TOutput> converter)
        {
            if (array == null)
            {
                throw new ArgumentNullException("array");
            }
            if (converter == null)
            {
                throw new ArgumentNullException("converter");
            }
            TOutput[] localArray = new TOutput[array.Length];
            for (int i = 0; i < array.Length; i++)
            {
                localArray[i] = converter(array[i]);
            }
            return localArray;
        }

        
        internal static int IndexOf(Type[] expected,Type iface)
        {
 	        throw new NotImplementedException();
        }
    
    }
}
