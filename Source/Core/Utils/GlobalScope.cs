using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using PHP.Core.Reflection;
using PHP.Core.Binders;
using PHP.Core.Emit;

namespace PHP.Core.Utils
{
    public sealed class GlobalScope : DynamicObject
    {
        private ScriptContext context;

        internal GlobalScope(ScriptContext currentContext)
        {
            context = currentContext;
        }

        public override bool TryInvokeMember(
            InvokeMemberBinder binder,
            Object[] args,
            out Object result
        )
        {
            object[] wrappedArgs = new object[args.Length];

            for(int i = 0; i < args.Length; ++i)
            {
                wrappedArgs[i] = ClrObject.WrapDynamic(args[i]);
            }

            result = PhpVariable.Dereference(context.Call(binder.Name, null, null, wrappedArgs));
            return true;
        }

        public override bool TryGetMember(
            GetMemberBinder binder,
            out Object result
        )
        {
            context.GlobalVariables.TryGetValue(binder.Name, out result);
            result = PhpVariable.Dereference(result);

            return true;
        }

        public override bool TrySetMember(
            SetMemberBinder binder,
            Object value
        )
        {
            context.GlobalVariables[binder.Name] = ClrObject.WrapDynamic(value);
            return true;
        }

    }


}
