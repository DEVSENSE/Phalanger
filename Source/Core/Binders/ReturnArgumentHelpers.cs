using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using PHP.Core.Emit;

namespace PHP.Core.Binders
{
    internal static class ReturnArgumentHelpers
    {
        #region Return Value Conversion

        public static Expression/*!*/ ReturnValueConversion(MethodInfo method, Expression/*!*/ returnExpr)
        {
            Type returnType = method.ReturnType;

            if (returnType == null || returnType == Types.Void)
                return returnExpr; // nothing to be converted

            // whether to emit cast to false:
            if (method.ReturnTypeCustomAttributes.IsDefined(typeof(CastToFalseAttribute), false))
            {
                var tmp = Expression.Variable(Types.Object[0]);
                ParameterExpression[] vars = new ParameterExpression[] { tmp };

                if (returnType == typeof(int))
                {
                    return Expression.Block(Types.Object[0], vars,
                        Expression.Assign(tmp, returnExpr),
                        Expression.Condition(Expression.Equal(
                            Expression.Convert(tmp,Types.Int[0]), Expression.Constant(-1)),
                            Expression.Convert(Expression.Constant(false), Types.Object[0]),
                            tmp));
                }
                else
                {
                    return Expression.Block(Types.Object[0], vars,
                        Expression.Assign(tmp, returnExpr),
                        Expression.Condition(Expression.Equal(
                            tmp, Expression.Constant(null)),
                            Expression.Convert(Expression.Constant(false),Types.Object[0]),
                            tmp));
                }
            }
            //TODO: else // deep copy:
            //    if (method.ReturnTypeCustomAttributes.IsDefined(typeof(PhpDeepCopyAttribute), false) && !returnType.IsValueType)
            //    {
            //        // returnValue = (<returnType>)PhpVariable.Copy(returnValue,CopyReason.ReturnedByCopy);
            //    }

            return returnExpr;

        }

        #endregion
    }
}
