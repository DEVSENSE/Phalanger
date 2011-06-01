using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core.Reflection;
using System.Linq.Expressions;
using PHP.Core.Emit;


namespace PHP.Core.Binders
{

    internal static class BinderHelper
    {

        public static Expression/*!*/ ThrowError(string id)
        {
            return Expression.Call(Methods.PhpException.Throw,
                Expression.Constant(PhpError.Error),
                Expression.Constant(CoreResources.GetString(id)));
        }

        public static Expression/*!*/ ThrowError(string id, object arg)
        {
            return Expression.Call(Methods.PhpException.Throw,
                Expression.Constant(PhpError.Error),
                Expression.Constant(CoreResources.GetString(id, arg)));
        }

        public static Expression/*!*/ ThrowError(string id, object arg1, object arg2)
        {
            return Expression.Call(Methods.PhpException.Throw,
                Expression.Constant(PhpError.Error),
                Expression.Constant(CoreResources.GetString(id, arg1, arg2)));
        }

        public static Expression/*!*/ ThrowError(string id, object arg1, object arg2, object arg3)
        {
            return Expression.Call(Methods.PhpException.Throw,
                Expression.Constant(PhpError.Error),
                Expression.Constant(CoreResources.GetString(id, arg1, arg2, arg3)));
        }

        /// <summary>
        /// Generates Expression that throws a 'Protected method called' or 'Private method called' <see cref="PhpException"/>.
        /// </summary>
        /// <param name="method">The <see cref="DRoutineDesc"/>.</param>
        /// <param name="callerContext">The caller that was passed to method lookup or <B>null</B>
        /// if it should be determined by this method (by tracing the stack).</param>
        /// <remarks>
        /// This method is intended to be called after <see cref="DTypeDesc.GetMethod"/> has returned
        /// <see cref="GetMemberResult.BadVisibility"/> while performing a method lookup.
        /// </remarks>
        public static Expression/*!*/ ThrowVisibilityError(DRoutineDesc/*!*/ method, DTypeDesc/*!*/ callerContext)
        {
            if (method.IsProtected)
            {
                return ThrowError("protected_method_called",
                                  method.DeclaringType.MakeFullName(),
                                  method.MakeFullName(),
                                  callerContext == null ? String.Empty : callerContext.MakeFullName());
            }
            else if (method.IsPrivate)
            {
                return ThrowError("private_method_called",
                                  method.DeclaringType.MakeFullName(),
                                  method.MakeFullName(),
                                  callerContext == null ? String.Empty : callerContext.MakeFullName());
            }

            throw new NotImplementedException();
        }

        //internal static Expression/*!*/ TypeErrorForProtectedMember(Type/*!*/ type, string/*!*/ name)
        //{
        //    Debug.Assert(!typeof(IPythonObject).IsAssignableFrom(type));

        //    return Ast.Throw(
        //        Ast.Call(
        //            typeof(PythonOps).GetMethod("TypeErrorForProtectedMember"),
        //            AstUtils.Constant(type),
        //            AstUtils.Constant(name)
        //        ),
        //        typeof(object)
        //    );
        //}
    }
}
