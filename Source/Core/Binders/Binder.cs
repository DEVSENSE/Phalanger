using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

using PHP.Core;
using PHP.Core.Emit;
using PHP.Core.Reflection;

namespace PHP.Core.Binders
{
    /// <summary>
    /// Manages run time binders.
    /// </summary>
    internal static class Binder
    {
        #region BinderFlags

        /// <summary>
        /// Requested binder options.
        /// </summary>
        [Flags]
        public enum BinderFlags : int
        {
            None = 0,

            /// <summary>
            /// The return value is required. Object is expected as the return value type.
            /// </summary>
            ResultWanted = 1,

            /// <summary>
            /// PhpReference of the return value is expected. Return type must be PhpReference and the value must not be null.
            /// </summary>
            ResultAsPhpReferenceWanted = 2 | ResultWanted,

        }

        #endregion

        #region MethodCall, StaticMethodCall (same signature)

        /// <summary>
        /// Get the instance method call binder.
        /// </summary>
        /// <param name="methodName">The method name. It is <c>null</c> iff this is not known at compile time.</param>
        /// <param name="classContext">The class context of the call site. It can be null or an instance of <see cref="DTypeDesc"/>.
        /// If the class context is not constant, its <see cref="DTypeDesc.IsUnknown"/> is <c>true</c>.</param>
        /// <param name="flags">The requested binder flags.</param>
        /// <returns>An instance of requested binder.</returns>
        [Emitted]
        public static CallSiteBinder/*!*/MethodCall(string methodName, DTypeDesc classContext, BinderFlags flags)
        {
            // CallSite< Func< CallSite, object /*target instance*/, ScriptContext, {args}*/*method call arguments*/, (DTypeDesc)?/*class context, iff <classContext>.IsUnknown*/, (object)?/*method name, iff <methodName>==null*/, <returnType> > >

            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the static method call binder.
        /// </summary>
        /// <param name="methodName">The method name. It is <c>null</c> iff this is not known at compile time.</param>
        /// <param name="classContext">The class context of the call site. It can be null or an instance of <see cref="DTypeDesc"/>.
        /// If the class context is not constant, its <see cref="DTypeDesc.IsUnknown"/> is <c>true</c>.</param>
        /// <param name="flags">The requested binder flags.</param>
        /// <returns>An instance of requested binder.</returns>
        [Emitted]
        public static CallSiteBinder/*!*/StaticMethodCall(string methodName, DTypeDesc classContext, BinderFlags flags)
        {
            // CallSite< Func< CallSite, DTypeDesc /*target type*/, ScriptContext, {args}*/*method call arguments*/, (DTypeDesc)?/*class context, iff <classContext>.IsUnknown*/, (object)?/*method name, iff <methodName>==null*/, <returnType> > >

            throw new NotImplementedException();
        }

        #endregion
    }
}
