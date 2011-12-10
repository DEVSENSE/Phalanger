using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using PHP.Core.Binders;
using PHP.Core.Emit;

namespace PHP.Core.Reflection
{
    //for now only for interoperability
    public class DMetaObject: DynamicMetaObject
    {
        public new DObject Value
        {
            get
            {
                return (DObject)base.Value;
            }
        }

        public DMetaObject(Expression expression, DObject value)
            : base(expression, BindingRestrictions.Empty, value) {
        }


        public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
        {
            return InteropBinder.InvokeMember.Bind(binder, this, args, binder.FallbackInvokeMember);
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            return InteropBinder.GetMember.Bind(binder, this, binder.FallbackGetMember);
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            return InteropBinder.SetMember.Bind(binder, this, value, binder.FallbackSetMember);
        }

        public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
        {
            return InteropBinder.Invoke.Bind( binder, this, args);
        }



    }
}
