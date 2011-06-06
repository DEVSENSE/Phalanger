using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Microsoft.Scripting.Generation;

namespace Microsoft.Scripting.Runtime
{
    class ScriptingRuntimeHelpers
    {
        private const int MIN_CACHE = -100;
        private const int MAX_CACHE = 1000;

        /// <summary>
        /// A singleton boxed boolean true.
        /// </summary>
        public static readonly object True = true;

        /// <summary>
        ///A singleton boxed boolean false.
        /// </summary>
        public static readonly object False = false;

        internal static readonly MethodInfo BooleanToObjectMethod = typeof(ScriptingRuntimeHelpers).GetMethod("BooleanToObject");
        internal static readonly MethodInfo Int32ToObjectMethod = typeof(ScriptingRuntimeHelpers).GetMethod("Int32ToObject");

        /// <summary>
        /// Gets a singleton boxed value for the given integer if possible, otherwise boxes the integer.
        /// </summary>
        /// <param name="value">The value to box.</param>
        /// <returns>The boxed value.</returns>
        public static object Int32ToObject(Int32 value)
        {
            // caches improves pystone by ~5-10% on MS .Net 1.1, this is a very integer intense app
            // TODO: investigate if this still helps perf. There's evidence that it's harmful on
            // .NET 3.5 and 4.0
            //if (value < MAX_CACHE && value >= MIN_CACHE)
            //{
            //    return cache[value - MIN_CACHE];
            //}
            return (object)value;//just return value
        }

        private static readonly string[] chars = MakeSingleCharStrings();

        private static string[] MakeSingleCharStrings()
        {
            string[] result = new string[255];

            for (char ch = (char)0; ch < result.Length; ch++)
            {
                result[ch] = new string(ch, 1);
            }

            return result;
        }

        public static object BooleanToObject(bool value)
        {
            return value ? True : False;
        }

        /// <summary>
        /// Helper method to create an instance.  Work around for Silverlight where Activator.CreateInstance
        /// is SecuritySafeCritical.
        /// 
        /// TODO: Why can't we just emit the right thing for default(T)?
        /// It's always null for reference types and it's well defined for value types
        /// </summary>
        public static T CreateInstance<T>()
        {
            return default(T);
        }

        public static Exception MakeIncorrectBoxTypeError(Type type, object received)
        {
            return Error.UnexpectedType("StrongBox<" + type.Name + ">", CompilerHelpers.GetType(received).Name);
        }


        public static ArgumentTypeException SimpleTypeError(string message)
        {
            return new ArgumentTypeException(message);
        }
    }
}
