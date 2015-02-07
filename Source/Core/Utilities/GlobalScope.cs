using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using PHP.Core.Reflection;
using PHP.Core.Binders;
using PHP.Core.Emit;

namespace PHP.Core.Utilities
{

    /// <summary>
    /// Convenience class for accessing global functions and global variables
    /// </summary>
    public sealed class GlobalScope : DynamicObject
    {
        private ScriptContext context;

        /// <summary>
        /// Initialize GlobalScope object
        /// </summary>
        /// <param name="currentContext"></param>
        public GlobalScope(ScriptContext currentContext)
        {
            context = currentContext;
        }

        #region DynamicObject

        /// <summary>
        /// Specifies dynamic behavior for invoke operation for global function
        /// </summary>
        public override bool TryInvokeMember(
            InvokeMemberBinder binder,
            Object[] args,
            out Object result
        )
        {
            object[] wrappedArgs = new object[args.Length];

            for(int i = 0; i < args.Length; ++i)
            {
                Debug.Assert(!(args[i] is PhpReference));
                wrappedArgs[i] = ClrObject.WrapDynamic(args[i]);
            }

            result = PhpVariable.Dereference(context.Call(binder.Name, null, null, wrappedArgs));
            return true;
        }

        /// <summary>
        /// Specifies dynamic behavior for get operation for global variable
        /// </summary>
        public override bool TryGetMember(
            GetMemberBinder binder,
            out Object result
        )
        {
            context.GlobalVariables.TryGetValue(binder.Name, out result);
            result = PhpVariable.Dereference(result);

            return true;
        }

        /// <summary>
        /// Specifies dynamic behavior for set operation for global function
        /// </summary>
        public override bool TrySetMember(
            SetMemberBinder binder,
            Object value
        )
        {
            Debug.Assert(!(value is PhpReference));
            
            context.GlobalVariables[binder.Name] = ClrObject.WrapDynamic(value);
            return true;
        }

        #endregion

    }


}
