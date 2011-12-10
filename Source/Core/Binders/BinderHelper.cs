using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core.Reflection;
using System.Linq.Expressions;
using PHP.Core.Emit;
using System.Dynamic;

using System.Reflection;
using System.Reflection.Emit;


namespace PHP.Core.Binders
{

    internal static class BinderHelper
    {
        #region PhpException.Throw

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

        public static Expression/*!*/ ThrowWarning(string id)
        {
            return Expression.Call(Methods.PhpException.Throw,
                Expression.Constant(PhpError.Warning),
                Expression.Constant(CoreResources.GetString(id)));
        }
        public static Expression/*!*/ ThrowWarning(string id, object arg)
        {
            return Expression.Call(Methods.PhpException.Throw,
                Expression.Constant(PhpError.Warning),
                Expression.Constant(CoreResources.GetString(id, arg)));
        }
        public static Expression/*!*/ ThrowWarning(string id, object arg1, object arg2)
        {
            return Expression.Call(Methods.PhpException.Throw,
                Expression.Constant(PhpError.Warning),
                Expression.Constant(CoreResources.GetString(id, arg1, arg2)));
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


        public static Expression/*!*/ ThrowMissingArgument(int argIndex, string calleeName)
        {
            if (calleeName != null)
                return ThrowWarning("missing_argument_for", argIndex, calleeName);
            else
                return ThrowWarning("missing_argument", argIndex);
        }

        public static Expression/*!*/ ThrowMissingTypeArgument(int argIndex, string calleeName)
        {
            if (calleeName != null)
                return ThrowWarning("missing_type_argument_for", argIndex, calleeName);
            else
                return ThrowWarning("missing_type_argument", argIndex);
        }


        public static Expression/*!*/ ThrowArgumentNotPassedByRef(int argIndex, string calleeName)
        {
            if (calleeName != null)
                return ThrowWarning("argument_not_passed_byref_to", argIndex, calleeName);
            else
                return ThrowWarning("argument_not_passed_byref", argIndex);
        }

    
        #endregion

        #region (ClrObject, IClrValue)

        /// <summary>
        /// Builds <see cref="Expression"/> that properly wraps given expression to return valid PHP type.
        /// It does not perform any conversion for PHP primitive types. Byte array is wrapped into <see cref="PhpBytes"/> and
        /// anything else is wrapped using <see cref="ClrObject.Create"/> method.
        /// </summary>
        /// <param name="expression">Expression returning an object/value.</param>
        /// <returns><see cref="Expression"/> returning valid PHP object.</returns>
        public static Expression/*!*/ClrObjectWrapDynamic(Expression/*!*/expression)
        {
            Debug.Assert(expression != null);

            var type = expression.Type;

            if (type.IsGenericParameter)
                type = Types.Object[0];

            // PHP types as they are:
            if (PhpVariable.IsPrimitiveType(type) || /*Types.DObject[0].IsAssignableFrom(type) || */typeof(IPhpVariable).IsAssignableFrom(type))
                return expression;

            // (byte[])<expression> -> PhpBytes( <expression> )
            // (byte[])null -> null
            if (type == typeof(byte[]))
            {
                var value = Expression.Variable(typeof(byte[]));
                return
                    Expression.Block(Types.PhpBytes[0],
                        new[] { value },
                        Expression.Condition(
                            Expression.Equal(Expression.Assign(value, expression),Expression.Constant(null)),
                            Expression.Constant(null, Types.PhpBytes[0]),
                            Expression.New(Constructors.PhpBytes_ByteArray, value)
                    ));
            }
            
            // from Emit/ClrOverloadBuilder.cs, ClrOverloadBuilder.EmitConvertToPhp:
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return Expression.Convert(expression, Types.Bool[0]);
                // coercion:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.Int32:
                    return Expression.Convert(expression, Types.Int[0]);

                case TypeCode.Int64:
                case TypeCode.UInt32:
                    return Expression.Convert(expression, Types.LongInt[0]);

                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                    return Expression.Convert(expression, Types.Double[0]);

                case TypeCode.Char:
                    return Expression.Call(Expression.Convert(expression, Types.Object[0]), Methods.Object_ToString);

                case TypeCode.DBNull:
                    return Expression.Constant(null, Types.Object[0]);
            }

            // value type -> ClrValue<T>
            // ref type -> ClrObject
            return Expression.Call(null,
                type == Types.Object[0] ?
                    Methods.ClrObject_WrapDynamic :     // expression can represent anything, check type in run time and wrap dynamically
                    Methods.ClrObject_WrapRealObject,   // expression is surely not PHP primitive type, DObject nor byte[], wrap into ClrObject or IClrValue
                Expression.Convert(expression, Types.Object[0]));
        }

        /// <summary>
        /// Unwraps <see cref="DObject.RealObject"/> or <see cref="ClrValue&lt;T&gt;.realValue"/> from <see cref="ClrObject"/> or <see cref="ClrValue&lt;T&gt;"/>.
        /// </summary>
        /// <param name="target">Original <b>target</b> of binding operation.</param>
        /// <param name="realType">Expected <see cref="Type"/> of the operation.</param>
        /// <returns><see cref="Expression"/> getting the real object wrapped into given target.</returns>
        public static Expression/*!*/ClrRealObject(DynamicMetaObject/*!*/target, Type/*!*/realType)
        {
            Debug.Assert(target != null);
            Debug.Assert(realType != null);

            var obj = target.Value as DObject;

            Debug.Assert(obj != null, "Not DObject!");
            Debug.Assert(realType.IsAssignableFrom(obj.RealType), "Not compatible types!");

            if (obj is ClrObject)
            {
                // (<realType>)((DObject)target).RealObject: // TODO: access ClrObject.realObject directly
                return Expression.Convert(
                    Expression.Property(Expression.Convert(target.Expression, Types.DObject[0]), Properties.DObject_RealObject),
                    realType);
            }
            else if (obj is IClrValue) // => obj is ClrValue<T>
            {
                var ClrValue_Type = obj.GetType(); // ClrValue'1
                var ClrValue_ValueField = ClrValue_Type.GetField("realValue"); // ClrValue'1.realValue

                // (T)((ClrValue<T>)target).realValue:
                return Expression.Field(Expression.Convert(target.Expression, ClrValue_Type), ClrValue_ValueField);
            }
            else
                throw new NotImplementedException();
        }

        #endregion

        #region PhpReference

        /// <summary>
        /// Ensures the expression returns <see cref="PhpReference"/>. If not the expression is wrapped to a new instance of <see cref="PhpReference"/>.
        /// </summary>
        /// <param name="expression">The <see cref="Expression"/> to be wrapped.</param>
        /// <returns>Expression representing PhpReference.</returns>
        public static Expression/*!*/MakePhpReference(Expression/*!*/expression)
        {
            // PhpReference already:
            if (Types.PhpReference[0].IsAssignableFrom(expression.Type))
                return expression;

            // void -> new PhpReference():
            if (expression.Type == Types.Void)
                return Expression.New(Constructors.PhpReference_Void);

            // object -> PhpReference(object):
            return Expression.New(Constructors.PhpReference_Object, Expression.Convert(expression, Types.Object[0]));
        }

        #endregion

        public static Expression[]/*!*/ CombineArguments(Expression/*!*/ arg, Expression/*!*/[]/*!*/ args)
        {
            Expression[] arguments = new Expression[1 + args.Length];
            arguments[0] = arg;
            for (int i = 0; i < args.Length; ++i)
                arguments[1 + i] = args[i];

            return arguments;
        }
        
        public static DynamicMetaObject[]/*!*/ CombineArguments(DynamicMetaObject/*!*/ arg, DynamicMetaObject/*!*/[]/*!*/ args)
        {
            DynamicMetaObject[] arguments = new DynamicMetaObject[1 + args.Length];
            arguments[0] = arg;
            for (int i = 0; i < args.Length; ++i)
                arguments[1 + i] = args[i];

            return arguments;
        }

        public static BindingRestrictions GetSimpleInvokeRestrictions(DynamicMetaObject/*!*/ target, DynamicMetaObject[]/*!*/ args)
        {
            BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType);

            foreach (var arg in args)
            {
                if (arg.RuntimeType != null)
                    restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(arg.Expression, arg.LimitType));
                else
                    restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(arg.Expression, null));//(MB) is it
            }

            return restrictions;
        }

        public static Expression/*!*/ AssertNotPhpReference(Expression objEx)
        {
#if DEBUG
            Func<object,object> isNotPhpReference = (obj) =>
            {
               Debug.Assert( !(obj is PhpReference) );
               return obj;
            };

            return Expression.Call(null, isNotPhpReference.Method, objEx);
#else
            return objEx;
#endif
        }


        public static BindingRestrictions ValueTypeRestriction( this DynamicMetaObject target)
        {
            return (target.HasValue && target.Value == null) ?
                        BindingRestrictions.GetInstanceRestriction(target.Expression, null) :
                        BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType);
        }

        /// <summary>
        /// Create static <see cref="DynamicMethod"/> that wraps call of given <paramref name="mi"/>. The call is performed statically, method's overrides are not called.
        /// </summary>
        /// <param name="mi"><see cref="MethodInfo"/> to be called statically.</param>
        /// <returns>New <see cref="MethodInfo"/> representing static method stub.</returns>
        public static MethodInfo/*!*/WrapInstanceMethodCall(MethodInfo/*!*/mi)
        {
            Debug.Assert(mi != null);
            Debug.Assert(!mi.IsStatic, "'mi' must not be static!");

            var parameters = mi.GetParameters();

            // array of parameters type
            // Type[]{ <DeclaringType>, <arg1.Type>, ..., <argn.Type> }
            var paramTypes = new Type[parameters.Length + 1]; // = new Type[]{ mi.DeclaringType }.Concat(parameters.Select<ParameterInfo, Type>(p => p.ParameterType)).ToArray();
            paramTypes[0] = mi.DeclaringType;
            for (int i = 0; i < parameters.Length; i++)
                paramTypes[i + 1] = parameters[i].ParameterType;

            // create static dynamic method that calls given MethodInfo statically
            DynamicMethod stub = new DynamicMethod(mi.Name + "_", mi.ReturnType, paramTypes, mi.DeclaringType);
            ILEmitter il = new ILEmitter(stub);

            // return <mi>( instance, arg_1, arg_2, ..., arg_n ):
            for (int i = 0; i <= parameters.Length; i++)
                il.Ldarg(i);

            il.Emit(OpCodes.Call, mi);
            il.Emit(OpCodes.Ret);
            
            //
            return stub;
        }

        /// <summary>
        /// Converts first #length elements from a given array of DynamicMetaObject to array of Expression
        /// </summary>
        /// <param name="args">Array of DynamicMetaObject to be converted to Expression[]</param>
        /// <param name="startIndex">Index of first argument that's going to be converted</param>
        /// <param name="length">Count of arguments that are going to be converted</param>
        /// <returns>Expression[] of values of DynamicMetaObject array</returns>
        public static Expression[]/*!*/ PackToExpressions(DynamicMetaObject/*!*/[]/*!*/ args, int startIndex, int length)
        {
            int top = startIndex + length;
            Expression[] arguments = new Expression[length];
            for (int i = 0; i < length; ++i)
                arguments[i] = args[i + startIndex].Expression;

            return arguments;
        }

        public static Expression[]/*!*/ PackToExpressions(DynamicMetaObject/*!*/[]/*!*/ args)
        {
            return PackToExpressions(args, 0, args.Length);
        }
    }
}
