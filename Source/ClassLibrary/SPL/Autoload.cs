using System;
using System.Collections.Generic;
using System.Text;
using PHP.Core;
using PHP.Core.Reflection;

namespace PHP.Library
{
    /// <summary>
    /// Functions for date and time manipulation.
    /// </summary>
    /// <threadsafety static="true"/>
    [ImplementsExtension(LibraryDescriptor.ExtSpl)]
    public static class Autoload
    {
        #region Constants

        /// <summary>
        /// The name of spl_autoload default function.
        /// </summary>
        public const string SplAutoloadFunction = "spl_autoload";

        #endregion

        #region spl_autoload_call, spl_autoload_extensions, spl_autoload_functions, spl_autoload_register, spl_autoload_unregister, spl_autoload

        /// <summary>
        /// This function can be used to manually search for a class or interface using the registered __autoload functions.
        /// </summary>
        [ImplementsFunction("spl_autoload_call", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static void Call(NamingContext namingContext, DTypeDesc caller, string className)
        {
            ScriptContext context = ScriptContext.CurrentContext;

            // If class isn't defined autoload functions are called automatically until class is declared
            if (context.IsSplAutoloadEnabled)
                ScriptContext.CurrentContext.ResolveType(className, namingContext, caller, null, ResolveTypeFlags.UseAutoload);
        }

        [ImplementsFunction("spl_autoload_extensions")]
        public static string SetExtensions()
        {
            var context = ScriptContext.CurrentContext;

            StringBuilder sb = null;
            foreach (string extension in context.SplAutoloadExtensions)
            {
                if (sb == null) sb = new StringBuilder();
                else sb.Append(',');

                sb.Append(extension);
            }

            return sb.ToString();
        }

        [ImplementsFunction("spl_autoload_extensions")]
        public static string SetExtensions(string fileExtensions)
        {
            ScriptContext.CurrentContext.SplAutoloadExtensions = Array.ConvertAll(fileExtensions.Split(new char[] { ',' }), (value) => value.Trim());

            return fileExtensions;
        }
                
        [ImplementsFunction("spl_autoload_functions")]
        [return:CastToFalse]
        public static PhpArray GetFunctions()
        {
            var context = ScriptContext.CurrentContext;
            if (context.IsSplAutoloadEnabled)
            {
                PhpArray result = new PhpArray();
                foreach (var func in context.SplAutoloadFunctions)
                    result.Add(func.ToPhpRepresentation());
                
                return result;
            }
            else
            {
                return null;
            }

            
        }

        [ImplementsFunction("spl_autoload_register", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static bool Register(NamingContext namingContext, DTypeDesc caller)
        {
            return Register(namingContext, caller, new PhpCallback(SplAutoloadFunction), true, false);
        }

        [ImplementsFunction("spl_autoload_register", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static bool Register(NamingContext namingContext, DTypeDesc caller, PhpCallback autoloadFunction)
        {
            return Register(namingContext, caller, autoloadFunction, true, false);
        }

        [ImplementsFunction("spl_autoload_register", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static bool Register(NamingContext namingContext, DTypeDesc caller, PhpCallback autoloadFunction, bool throwError)
        {
            return Register(namingContext, caller, autoloadFunction, throwError, false);
        }

        [ImplementsFunction("spl_autoload_register", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static bool Register(NamingContext namingContext, DTypeDesc caller, PhpCallback autoloadFunction, bool throwError, bool prepend)
        {
            if (autoloadFunction == null)
            {
                PhpException.ArgumentNull("autoloadFunction");
                return false;
            }

            if (autoloadFunction.Bind(!throwError, caller, namingContext))
            {
                var context = ScriptContext.CurrentContext;
                if (FindAutoloadFunction(context, autoloadFunction.ToPhpRepresentation()) != null)
                    return false;
                
                if (prepend)
                    context.SplAutoloadFunctions.AddFirst(autoloadFunction);
                else
                    context.SplAutoloadFunctions.AddLast(autoloadFunction);

                return true;
            }
            else
            {
                return false;
            }
        }

        [ImplementsFunction("spl_autoload_unregister")]
        public static bool Unregister(object autoloadFunction)
        {
            var context = ScriptContext.CurrentContext;
            var functionNode = FindAutoloadFunction(context, autoloadFunction);

            if (functionNode != null)
            {
                context.SplAutoloadFunctions.Remove(functionNode);
                return true;
            }
            else
            {
                return false;
            }
        }

        [ImplementsFunction("spl_autoload", FunctionImplOptions.NeedsClassContext | FunctionImplOptions.NeedsNamingContext)]
        public static void DefaultAutoload(NamingContext namingContext, DTypeDesc caller, string className)
        {
            // TODO: skip in pure mode

            var context = ScriptContext.CurrentContext;

            var fileExtensions = context.SplAutoloadExtensions.GetEnumerator();
            bool stateChanged = true;

            while (!stateChanged || ScriptContext.CurrentContext.ResolveType(className, namingContext, caller, null, ResolveTypeFlags.None) == null)
            {
                if (!fileExtensions.MoveNext())
                {
                    PhpException.Throw(PhpError.Error, string.Format(CoreResources.class_could_not_be_loaded, className));
                    return;
                }

                // try to dynamically include the file specified by the class name, if it exists
                string FullFileName = className + fileExtensions.Current;

                if (PhpFile.Exists(FullFileName))
                {
                    context.DynamicInclude(FullFileName, context.WorkingDirectory, null, null, null, InclusionTypes.IncludeOnce);
                    stateChanged = true;
                }
                else
                {
                    stateChanged = false;
                }
            }
        }

        #endregion

        #region helpers

        /// <summary>
        /// Finds the specified autoload function list element.
        /// </summary>
        /// <param name="context">Current script context.</param>
        /// <param name="autoloadFunction">The PHP representation of callback function to find in list of SPL autoload functions.</param>
        /// <returns>List node or null if such a functions does not exist in the list.</returns>
        private static LinkedListNode<PhpCallback> FindAutoloadFunction(ScriptContext/*!*/context, object autoloadFunction)
        {
            Debug.Assert(context != null);

            if (context.IsSplAutoloadEnabled)
                for (var node = context.SplAutoloadFunctions.First; node != null; node = node.Next)
                    if (PhpComparer.CompareEq(node.Value.ToPhpRepresentation(), autoloadFunction))
                        return node;

            return null;
        }

        #endregion

    }
}
