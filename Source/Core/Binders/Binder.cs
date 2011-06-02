using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

using PHP.Core;
using PHP.Core.Emit;
using PHP.Core.Reflection;
using System.Threading;

namespace PHP.Core.Binders
{
    /// <summary>
    /// Manages run time binders.
    /// </summary>
    public static class Binder
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

        #region Fields

        /// <summary>
        /// Binders cache. Alows to share binders for the same operations.
        /// TODO: Slit different binders into different dictionaries.
        /// </summary>
        private static readonly Dictionary<string, PhpInvokeMemberBinder/*!*/> invokeMemberBinders = new Dictionary<string, PhpInvokeMemberBinder>();

        /// <summary>
        /// Get*Property* binders.
        /// TODO: Slit different binders into different dictionaries.
        /// </summary>
        private static readonly Dictionary<string, PhpGetMemberBinder/*!*/>/*!*/getMemberBinders = new Dictionary<string, PhpGetMemberBinder>();

        #endregion

        #region MethodCall, StaticMethodCall (same signature)

        /// <summary>
        /// Get the instance method call binder.
        /// </summary>
        /// <param name="methodName">The method name. It is <c>null</c> iff this is not known at compile time.</param>
        /// <param name="classContext">The class context of the call site. It can be null or an instance of <see cref="DTypeDesc"/>.
        /// If the class context is not constant, its <see cref="DTypeDesc.IsUnknown"/> is <c>true</c>.</param>
        /// <param name="genericParamsCount">Type arguments count.</param>
        /// <param name="paramsCount">Parameters count.</param>
        /// <param name="returnType">CallSite return value type.</param>
        /// <returns>An instance of requested binder.</returns>
        [Emitted]
        public static CallSiteBinder/*!*/MethodCall(string methodName, int genericParamsCount, int paramsCount, DTypeDesc classContext, Type/*!*/returnType)
        {
            // CallSite< Func< CallSite, object /*target instance*/, ScriptContext, {args}*/*method call arguments*/, (DTypeDesc)?/*class context, iff <classContext>.IsUnknown*/, (object)?/*method name, iff <methodName>==null*/, <returnType> > >

            PhpInvokeBinderKey key = new PhpInvokeBinderKey(methodName, genericParamsCount, paramsCount, classContext, returnType);

            lock (invokeMemberBinders)
            {
                PhpInvokeMemberBinder res;
                if (!invokeMemberBinders.TryGetValue(key.ToString(), out res))
                {
                    invokeMemberBinders[key.ToString()] = res = PhpBaseInvokeMemberBinder.Create(methodName, genericParamsCount, paramsCount, classContext, returnType);
                }

                return res;
            }

        }

        /// <summary>
        /// Get the static method call binder.
        /// </summary>
        /// <param name="methodName">The method name. It is <c>null</c> iff this is not known at compile time.</param>
        /// <param name="classContext">The class context of the call site. It can be null or an instance of <see cref="DTypeDesc"/>.
        /// If the class context is not constant, its <see cref="DTypeDesc.IsUnknown"/> is <c>true</c>.</param>
        /// <param name="genericParamsCount">Type arguments count.</param>
        /// <param name="paramsCount">Parameters count.</param>
        /// <param name="returnType">CallSite return value type.</param>
        /// <returns>An instance of requested binder.</returns>
        [Emitted]
        public static CallSiteBinder/*!*/StaticMethodCall(string methodName, int genericParamsCount, int paramsCount, DTypeDesc classContext, Type/*!*/returnType)
        {
            // CallSite< Func< CallSite, DTypeDesc /*target type*/, ScriptContext, {args}*/*method call arguments*/, DObject/*type*/, (DTypeDesc)?/*class context, iff <classContext>.IsUnknown*/, (object)?/*method name, iff <methodName>==null*/, <returnType> > >

            throw new NotImplementedException();
        }

        #endregion

        #region GetProperty, StaticGetProperty

        [Emitted]
        public static CallSiteBinder/*!*/GetProperty(string fieldName, DTypeDesc classContext, bool issetSemantics, Type/*!*/returnType)
        {
            // the binder cache key
            string key = string.Format("{0}'{1}'{2}'{3}",
                fieldName ?? "$",
                (classContext != null) ? (classContext.GetHashCode().ToString()) : string.Empty,
                issetSemantics ? "1" : "0",
                returnType.FullName
                );

            lock (getMemberBinders)
            {
                PhpGetMemberBinder binder;
                if (!getMemberBinders.TryGetValue(key, out binder))
                    getMemberBinders[key] = binder = new PhpGetMemberBinder(fieldName, classContext, issetSemantics, returnType);
                
                return binder;
            }
            
            throw new NotImplementedException();
        }

        [Emitted]
        public static CallSiteBinder/*!*/StaticGetProperty(string fieldName, DTypeDesc classContext, bool issetSemantics, Type/*!*/returnType)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
