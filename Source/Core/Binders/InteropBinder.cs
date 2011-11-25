using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using PHP.Core.Reflection;
using PHP.Core.Emit;
using System.Linq.Expressions;

namespace PHP.Core.Binders
{
    internal static class InteropBinder
    {

        public static Expression WrapDynamic(Expression arg)
        {
            return Expression.Call(Methods.ClrObject_WrapDynamic, 
                            Expression.Convert( arg, Types.Object[0]));

        }


        
        internal sealed class InvokeMember : DynamicMetaObjectBinder
        {

            #region Php -> DLR

            public override DynamicMetaObject  Bind(DynamicMetaObject target, DynamicMetaObject[] args)
            {
 	            throw new NotImplementedException();
            }

            #endregion

            #region DLR -> Php

            public static DynamicMetaObject/*!*/ Bind(InvokeMemberBinder/*!*/ binder, DynamicMetaObject/*!*/ target,
                DynamicMetaObject/*!*/[]/*!*/ args, Func<DynamicMetaObject, DynamicMetaObject[], DynamicMetaObject>/*!*/ fallback) {
                return Bind(binder.Name, binder.CallInfo, binder, target, args, fallback);
            }

            public static DynamicMetaObject/*!*/ Bind(string/*!*/ methodName, CallInfo/*!*/ callInfo,
                DynamicMetaObjectBinder/*!*/ binder, DynamicMetaObject/*!*/ target, DynamicMetaObject/*!*/[]/*!*/ args,
                Func<DynamicMetaObject, DynamicMetaObject[], DynamicMetaObject>/*!*/ fallback)
            {
                Debug.Assert(fallback != null);

                //create DMO
                var phpInvokeBinder = Binder.MethodCall(methodName, 0,callInfo.ArgumentCount, null, Types.Object[0]) as PhpBaseInvokeMemberBinder;

                if (phpInvokeBinder != null)
                {

                    //Add ScriptContext.CurrentContext
                    var context = new DynamicMetaObject(Expression.Call(Methods.ScriptContext.GetCurrentContext), BindingRestrictions.Empty);

                    var restrictions = BinderHelper.GetSimpleInvokeRestrictions(target, args);

                    //Value type arguments have to be boxed
                    DynamicMetaObject[] arguments = new DynamicMetaObject[1 + args.Length];
                    arguments[0] = context;
                    for (int i = 0; i < args.Length; ++i)
                        arguments[1 + i] = new DynamicMetaObject(WrapDynamic(args[i].Expression),
                                                                 args[i].Restrictions);
                    //delegate preparation

                    var result = phpInvokeBinder.Bind(target, arguments);

                    return new DynamicMetaObject(result.Expression, restrictions);
                }
                else
                    return fallback(target, args);//this will never happen
            }

            #endregion

        }

        internal sealed class GetMember : GetMemberBinder
        {
            internal GetMember(string/*!*/ name)
                : base(name, false)
            {
            }

            #region DLR -> Php

            public static DynamicMetaObject/*!*/ Bind(GetMemberBinder/*!*/ binder, DynamicMetaObject/*!*/ target,
                Func<DynamicMetaObject, DynamicMetaObject>/*!*/ fallback)
            {
                Debug.Assert(fallback != null);

                //create DMO
                var phpGetMemberBinder = Binder.GetProperty(binder.Name, null, false, Types.Object[0]) as PhpGetMemberBinder;

                if (phpGetMemberBinder != null)
                {
                    // Get ClassContext of actual object
                    var args = new DynamicMetaObject[]{
                    new DynamicMetaObject(Expression.Field(Expression.Convert( target.Expression, Types.DObject[0]), Fields.DObject_TypeDesc), BindingRestrictions.Empty)
                    };

                    return phpGetMemberBinder.Bind(target, args);
                }
                else
                    return fallback(target);//this will never happen

            }

            #endregion

            #region Php -> DLR

            public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

        internal sealed class SetMember : DynamicMetaObjectBinder
        {

            #region DLR -> Php

            public static DynamicMetaObject/*!*/ Bind(SetMemberBinder/*!*/ binder, DynamicMetaObject/*!*/ target,
                DynamicMetaObject/*!*/ value, Func<DynamicMetaObject, DynamicMetaObject, DynamicMetaObject>/*!*/ fallback)
            {
                Debug.Assert(target.HasValue && target.LimitType != Types.PhpReference[0], "Target should not be PhpReference!");
                Debug.Assert(fallback != null);

                BindingRestrictions restrictions = BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType); //target.Restrictions;

                //wrap reference

                return new DynamicMetaObject(
                    Expression.Block(
                        Expression.Call(Expression.Convert( target.Expression, Types.DObject[0]), Methods.DObject_SetProperty,
                            Expression.Constant(binder.Name),
                            WrapDynamic( value.Expression),
                            Expression.Constant(null, Types.DTypeDesc[0])),
                        Expression.Constant(null,Types.Object[0])),
                    restrictions
                        );
            
            }

            #endregion

            #region Php -> DLR

            public override DynamicMetaObject Bind(DynamicMetaObject target, DynamicMetaObject[] args)
            {
                throw new NotImplementedException();
            }

            #endregion
        }

    }
}
