using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PHP.Core
{
    public static class ScriptContextExtension
    {
        public static T Call<T>(this ScriptContext context, string/*!*/ functionName, NamingContext namingContext,
            Dictionary<string, object> callerLocalVariables, params object[] arguments)
            where T : class
        {
            PhpReference rf = context.Call(functionName, namingContext, callerLocalVariables, arguments);
            if (rf.Value == null)
                return null;
            else
                return DuckTyping.Instance.ImplementDuckType<T>(rf.Value);
        }

        public static T Call<T>(string/*!*/ functionName, params object[] arguments)
            where T : class
        {
            return Call<T>(functionName, null, null, arguments);
        }

        /// <summary>
        /// To be used with DuckType(GlobalFunctions=true) only!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T NewObject<T>(this ScriptContext context)
        {
            return DuckTyping.Instance.ImplementGlobalDuckType<T>();
        }

        /// <summary>
        /// Creates new object with given class name and arguments and then wraps it into
        /// a duck type specified in generic type arguments.
        /// </summary>
        /// <typeparam name="T">Duck type interface to be used for wrapping.</typeparam>
        /// <param name="className">Class name which will be used for new object creation.</param>
        /// <param name="ctorArguments">Constructor arguments to be used.</param>
        /// <returns>Dynamic object wrapped into static wrapper.</returns>
        public static T NewObject<T>(this ScriptContext context, string/*!*/ className, params object[] ctorArguments)
        {
            return context.NewObject<T>(className, null, ctorArguments);
        }

        /// <summary>
        /// Creates new object with given class name, naming context and arguments and then wraps it into
        /// a duck type specified in generic type arguments.
        /// </summary>
        /// <typeparam name="T">Duck type interface to be used for wrapping.</typeparam>
        /// <param name="className">Class name which will be used for new object creation.</param>
        /// <param name="namingContext">Naming context.</param>
        /// <param name="ctorArguments">Constructor arguments to be used.</param>
        /// <returns>Dynamic object wrapped into static wrapper.</returns>
        public static T NewObject<T>(this ScriptContext context, string/*!*/ className, NamingContext namingContext, params object[] ctorArguments)
        {
            //create new argument array and dig wrapped values out of it
            object[] newCtorArgs = new object[ctorArguments.Length];

            for (int i = 0; i < newCtorArgs.Length; i++)
            {
                IDuckType duck = ctorArguments[i] as IDuckType;
                if (duck != null)
                    newCtorArgs[i] = duck.OriginalObject;
                else
                    newCtorArgs[i] = ctorArguments[i];
            }

            object o = context.NewObject(className, namingContext, newCtorArgs);
            return DuckTyping.Instance.ImplementDuckType<T>(o);
        }

        public static T WrapObject<T>(this ScriptContext context, object o)
        {
            return DuckTyping.Instance.ImplementDuckType<T>(o);
        }
    }
}
