using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using PHP.Core.Emit;
using System.Linq.Expressions;
using PHP.Core.Reflection;

namespace PHP.Core.Binders
{

    //TODO:(MB) Implement IDynamicMetaObject provider to DObject

    internal class PhpReferenceDynamicMetaObject : DynamicMetaObject
    {
        //Restriction: (obj isinst PhpReference) && ( ref.Value.GetType() == |typeof(obj.Value)|)

        public PhpReferenceDynamicMetaObject(DynamicMetaObject dynamic):
            base(Expression.Field(Expression.Convert(dynamic.Expression, Types.PhpReference[0]), Fields.PhpReference_Value),
            

            BindingRestrictions.GetTypeRestriction(dynamic.Expression,Types.PhpReference[0]) // TODO: PhpSmartReference
                /*.Merge(              
                    BindingRestrictions.GetTypeRestriction(
                        Expression.Field(Expression.Convert(dynamic.Expression, Types.PhpReference[0]), Fields.PhpReference_Value), ((PhpReference)dynamic.Value).Value.GetType()))*/,
            ((PhpReference)dynamic.Value).Value
            )
        {
            Debug.Assert(Types.PhpReference[0] == dynamic.LimitType);
        }

    }

    internal class PhpSmartReferenceDynamicMetaObject : DynamicMetaObject
    {
        //Restriction: (obj isinst PhpReference) && ( ref.Value.GetType() == |typeof(obj.Value)|)

        public PhpSmartReferenceDynamicMetaObject(DynamicMetaObject dynamic) :
            base(Expression.Field(Expression.Convert(dynamic.Expression, Types.PhpSmartReference[0]), Fields.PhpReference_Value),


             BindingRestrictions.GetTypeRestriction(dynamic.Expression, Types.PhpSmartReference[0]) // TODO: PhpSmartReference
                /*.Merge(              
                    BindingRestrictions.GetTypeRestriction(
                        Expression.Field(Expression.Convert(dynamic.Expression, Types.PhpReference[0]), Fields.PhpReference_Value), ((PhpReference)dynamic.Value).Value.GetType()))*/,
             ((PhpReference)dynamic.Value).Value
             )
        {
            Debug.Assert(Types.PhpSmartReference[0] == dynamic.LimitType);
        }

    }

    internal class ClrDynamicMetaObject : DynamicMetaObject
    {
        //Restriction: (obj is ClrObject) && (obj.RealType == |typeof(obj.RealType)|))

        public ClrDynamicMetaObject(DynamicMetaObject dynamic) :
            base(Expression.Property(Expression.Convert(dynamic.Expression, typeof(ClrObject)), Properties.DObject_RealObject),


             BindingRestrictions.GetTypeRestriction(dynamic.Expression, typeof(ClrObject)) 
                 .Merge(// TODO: this has to be turned off for DLR overload resolution, because instance is argument for method and DLR creates its restriction
                     BindingRestrictions.GetExpressionRestriction(
                         Expression.Equal(
                            Expression.Property(Expression.Convert(dynamic.Expression, typeof(ClrObject)), Properties.DObject_RealType),
                            Expression.Constant(((ClrObject)dynamic.Value).RealType)))),
            ((ClrObject)dynamic.Value).RealObject
             )
        {
            Debug.Assert(typeof(ClrObject) == dynamic.LimitType);
        }

    }


    internal class ClrValueDynamicMetaObject : DynamicMetaObject
    {
        public ClrValueDynamicMetaObject(DynamicMetaObject dynamic) :
            base(dynamic.Expression,

             BindingRestrictions.GetTypeRestriction(dynamic.Expression, dynamic.LimitType),
             dynamic.Value
             )
        {   
        }

    }

    internal class WrappedClrDynamicMetaObject : DynamicMetaObject
    {
        public WrappedClrDynamicMetaObject(DynamicMetaObject dynamic) :
            base(dynamic.Expression,

             BindingRestrictions.GetTypeRestriction(dynamic.Expression, dynamic.LimitType),

             ClrObject.WrapRealObject(dynamic.Value)
             )
        {
            Debug.Assert(!Types.DObject[0].IsAssignableFrom(dynamic.LimitType) && 
                        dynamic.Value != null &&
                        Configuration.Application.Compiler.ClrSemantics);
        }

        public Expression WrapIt()
        {
            return Expression.Assign(Expression, Expression.Call(
                                Methods.ClrObject_WrapRealObject, Expression));
        }

    }


    internal class PhpDynamicMetaObject : DynamicMetaObject
    {
        public PhpDynamicMetaObject(DynamicMetaObject dynamic) :
            base(dynamic.Expression,

             // Restriction: typeof(target) == |target.TypeDesc.RealType|
             BindingRestrictions.GetTypeRestriction(dynamic.Expression, ((PhpObject)dynamic.Value).TypeDesc.RealType), //TODO: it's sufficient to use typeof(targetObj), but this is faster                                                                                                                                                                
             dynamic.Value
             )
        {
            Debug.Assert(Types.PhpObject[0].IsAssignableFrom(dynamic.LimitType));
        }

    }






}
