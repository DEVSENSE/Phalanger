using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core.Reflection;

using System.Linq.Expressions;


namespace PHP.Core.Binders
{
    using DlrAst = Expression;

    class BinderHelpers
    {

        //internal static Expression/*!*/ GenerateThrowMethodVisibilityError(DRoutineDesc method, DTypeDesc caller)
        //{
        //    if (method.IsProtected)
        //    {
        //        return DlrAst.Call(Methods.PhpException.Throw,

        //        PhpException.Throw(PhpError.Error, CoreResources.GetString("protected_method_called",
        //            method.DeclaringType.MakeFullName(),
        //            method.MakeFullName(),
        //            (caller == null ? String.Empty : caller.MakeFullName())));
        //    }
        //    else if (method.IsPrivate)
        //    {
        //        PhpException.Throw(PhpError.Error, CoreResources.GetString("private_method_called",
        //            method.DeclaringType.MakeFullName(),
        //            method.MakeFullName(),
        //            (caller == null ? String.Empty : caller.MakeFullName())));
        //    }
        //}




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
