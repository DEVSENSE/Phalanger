#if DLR_OVERLOAD_RESOLUTION
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using PHP.Core.Reflection;
using System.Reflection;
using Microsoft.Scripting.Actions;
using Microsoft.Scripting.Actions.Calls;
using Microsoft.Scripting.Runtime;
using Microsoft.Scripting.Utils;


using System.Linq.Expressions;
using PHP.Core.Emit;

namespace PHP.Core.Binders
{
    class PhpBinder : DefaultBinder
    {
        internal static readonly PhpBinder Instance = new PhpBinder();

        public DynamicMetaObject CallClrMethod(ClrMethod method, DynamicMetaObject target, DynamicMetaObject[] args)
        {
            CallSignature signature = new CallSignature(args.Length);
            return Call(signature,null,new DefaultOverloadResolverFactory(this), method, target, args);
        }

        /// <summary>
        /// Provides default binding for performing a call on the specified meta objects.
        /// </summary>
        /// <param name="signature">The signature describing the call</param>
        /// <param name="target">The meta object to be called.</param>
        /// <param name="args">
        /// Additional meta objects are the parameters for the call as specified by the CallSignature in the CallAction.
        /// </param>
        /// <param name="resolverFactory">Overload resolver factory.</param>
        /// <param name="errorSuggestion">The result should the object be uncallable.</param>
        /// <returns>A MetaObject representing the call or the error.</returns>
        public DynamicMetaObject Call(CallSignature signature, DynamicMetaObject errorSuggestion, OverloadResolverFactory resolverFactory,ClrMethod method, DynamicMetaObject target, params DynamicMetaObject[] args)
        {
            ContractUtils.RequiresNotNullItems(args, "args");
            ContractUtils.RequiresNotNull(resolverFactory, "resolverFactory");

            TargetInfo targetInfo = GetTargetInfo(method, target, args);

            if (targetInfo != null)
            {
                // we're calling a well-known MethodBase
                DynamicMetaObject res = MakeMetaMethodCall(signature, resolverFactory, targetInfo);
                if (res.Expression.Type.IsValueType)
                {
                    if (res.Expression.Type == Types.Void)
                        res = new DynamicMetaObject(
                            Expression.Block(Types.Object[0],
                                res.Expression,
                                Expression.Constant(null)),
                            res.Restrictions
                        );
                    else
                        res = new DynamicMetaObject(
                            Expression.Convert(res.Expression, typeof(object)),
                            res.Restrictions
                    );
                }

                return res;
            }
            else
            {
                // we can't call this object
                return errorSuggestion ?? MakeCannotCallRule(target, target.GetLimitType());
            }
        }

        /// <summary>
        /// Gets a TargetInfo object for performing a call on this object.  
        /// </summary>
        private TargetInfo GetTargetInfo(ClrMethod method, DynamicMetaObject target, DynamicMetaObject[] args)
        {
            Debug.NotNull(method);
            Debug.Assert(target.HasValue);
            object objTarget = target.Value;

            List<MethodBase> foundTargets = new List<MethodBase>(method.Overloads.Count);

            foreach (PHP.Core.Reflection.ClrMethod.Overload overload in method.Overloads)
            {
                foundTargets.Add(overload.Method);
            }

            return new TargetInfo(target, args, target.Restrictions, foundTargets.ToArray()); 


        }


    }
}
#endif