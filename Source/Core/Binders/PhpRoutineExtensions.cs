using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Linq.Expressions;
using PHP.Core.Emit;
using PHP.Core.Reflection;

namespace PHP.Core.Binders
{

    internal static class PhpRoutineExtensions
    {
        /// <summary>
        /// Prepares arguments for argfull overload.
        /// </summary>
        /// <param name="routine">Routine for which arguments should be prepared</param>
        /// <param name="arguments">Arguments to be prepared for the routine</param>
        /// <param name="genericArguments">Amount of generic arguments provided by call site.</param>
        /// <param name="regularArguments">Amount of value arguments provided by call site.</param>
        /// <param name="restrictions">Type restrictions for the arguments</param>
        /// <returns>Array of prepared arguments to be called with routine</returns>
        /// <remarks>
        /// This is basically substitute for everything important that was done in argless overload (except it doesn't use PhpStack but evaluation stack).
        /// It adopts the arguments according to routine. e.g. dereference reference if value is needed, supplies default argument, etc.
        /// </remarks>
        public static Expression[] PrepareArguments(this PhpRoutine routine, DynamicMetaObject[] arguments, int genericArguments, int regularArguments, out BindingRestrictions restrictions)
        {
            const int scriptContextIndex = 0;
            DynamicMetaObject arg;
            int result_offset = 0;
            int argument_offset = 0;
            Expression[] result = new Expression[1 + routine.Signature.GenericParamCount + routine.Signature.ParamCount];//ScriptContext + all arguments = actual arguments to be passed to argfull overload
            restrictions = BindingRestrictions.Empty;

            result[scriptContextIndex] = arguments[scriptContextIndex].Expression;
            ++result_offset;
            ++argument_offset;

			// peek pseudo-generic arguments:
            for (int i = 0; i < routine.Signature.GenericParamCount; ++i)
            {
                if (i < genericArguments)
                {
                    arg = arguments[argument_offset + i];
                }
                else
                {
                    arg = null;
                }

                result[result_offset + i] = GeneratePeekPseudoGenericArgument(routine, arguments[scriptContextIndex], arg, i);

                // it isn't necessary to add restriction for type argument, it is always DTypeDesc
            }

            result_offset += routine.Signature.GenericParamCount;
            argument_offset += genericArguments;

			// peek regular arguments:
            // skip first one ScriptContext and generic parameters
            for (int i = 0; i < routine.Signature.ParamCount; ++i)
            {
                if (i < regularArguments)
                {
                    arg = arguments[argument_offset + i];

                    if (arg.RuntimeType != null)
                        restrictions = restrictions.Merge(BindingRestrictions.GetTypeRestriction(arguments[argument_offset + i].Expression, arguments[argument_offset + i].LimitType));
                    else
                        restrictions = restrictions.Merge(BindingRestrictions.GetInstanceRestriction(arguments[argument_offset + i].Expression, null));//(MB) is it necessary?
                }
                else
                {
                    arg = null;
                }
                
                result[result_offset + i] = GeneratePeekArgument(routine, arguments[scriptContextIndex], arg, i);

            }

            return result;
        }

        private static Expression/*!*/ GeneratePeekPseudoGenericArgument(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int index)
        {
            bool optional = index >= routine.Signature.MandatoryGenericParamCount;
            int indexTransformed = index + 1; // in PHP indexes of arguments starts from index 1

            if (optional)
                return PeekTypeOptional(routine, scriptContext, arg, indexTransformed);
            else
                return PeekType(routine, scriptContext, arg, indexTransformed);

        }

        /// <summary>
        /// Generates expression for a given argument to fit formal argument of the give routine.
        /// </summary>
        /// <param name="routine">Routine for which argument will be supplied.</param>
        /// <param name="scriptContext">ScriptContext DynamicMetaObject</param>
        /// <param name="arg">Actual argument to be supplied to be supplied to routine.</param>
        /// <param name="argIndex">Index of the argument in a routine(not counting ScriptContext argument).</param>
        /// <returns>The expression of an argument that is prepared to be supplied as an argument to the routine.</returns>
        private static Expression/*!*/ GeneratePeekArgument(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int argIndex)
        {
            bool optional = argIndex >= routine.Signature.MandatoryParamCount;
            int argIndexTransformed = argIndex + 1; // in PHP indexes of arguments starts from index 1

            if (routine.Signature.IsAlias(argIndex))
            {
                if (optional)
                    return PeekReferenceOptional(routine, scriptContext, arg, argIndexTransformed);
                else
                    return PeekReference(routine, scriptContext, arg, argIndexTransformed);
            }
            else
            {
                if (optional)
                    return PeekValueOptional(routine, scriptContext, arg, argIndexTransformed);
                else
                    return PeekValue(routine, scriptContext, arg, argIndexTransformed);
            }
        }


        private static Expression/*!*/ PeekValue(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int argIndex)
        {
			if (arg != null)
			{
				// peeks the value:
                return PeekValueUnchecked(routine, scriptContext, arg, argIndex);
			}
			else
			{
                return Expression.Block(
                        BinderHelper.ThrowMissingArgument(argIndex, routine.FullName),
                        Expression.Constant(null));
                
			}
        }

        private static Expression/*!*/ PeekValueOptional(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int argIndex)
        {
            if (arg != null)
            {
                // peeks the value:
                return PeekValueUnchecked(routine, scriptContext, arg, argIndex);
            }
            else
            {
                // default value:
                return Expression.Constant(Arg.Default);
            }
        }


        private static Expression/*!*/ PeekValueUnchecked(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int argIndex)
		{
			// caller may have pushed a reference even if a formal argument is not reference => dereference it:
            if (Types.PhpReference[0].IsAssignableFrom(arg.LimitType)) // what about SmartReference
            {
                //object result = ((PhpReference)arg_i).Value;
                return Expression.Field(Expression.Convert(arg.Expression, Types.PhpReference[0]), Fields.PhpReference_Value);
            }
            else
			// caller may have pushed a runtime chain => evaluate it:
                if (arg.LimitType == Types.PhpRuntimeChain[0])
            {
                //result = php_chain.GetValue(Context);
                return Expression.Call(
                    Expression.Convert(arg.Expression, Types.PhpRuntimeChain[0]),
                    Methods.PhpRuntimeChain.GetValue,
                    scriptContext.Expression);
            }
            else
            {
                return arg.Expression;
            }

		}



        private static Expression/*!*/ PeekReference(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int argIndex)
        {
            if (arg != null)
            {
                // peeks the reference:
                return PeekReferenceUnchecked(routine, scriptContext, arg, argIndex);
            }
            else
            {
                return Expression.Block(
                        BinderHelper.ThrowMissingArgument(argIndex, routine.FullName),
                        Expression.New(Constructors.PhpReference_Void));
            }

        }


        private static Expression/*!*/ PeekReferenceOptional(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int argIndex)
        {
            if (arg != null)
            {
                // peeks the value:
                return PeekReferenceUnchecked(routine, scriptContext, arg, argIndex);
            }
            else
            {
                // default value:
                return Expression.Constant(Arg.Default,Types.PhpReference[0]);
            }
        }

        private static Expression/*!*/ PeekReferenceUnchecked(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int argIndex)
        {
            // the caller may not pushed a reference although the formal argument is a reference:
            // it doesn't matter if called by callback:
            if (!Types.PhpReference[0].IsAssignableFrom(arg.LimitType))
            {
                // caller may have pushed a runtime chain => evaluate it:
                if (arg.LimitType == Types.PhpRuntimeChain[0])
                {
                    //result = php_chain.GetReference(Context);
                    return Expression.Call(
                        Expression.Convert(arg.Expression, Types.PhpRuntimeChain[0]),
                        Methods.PhpRuntimeChain.GetReference,
                        scriptContext.Expression);
                }
                else
                {
                    // the reason of copy is not exactly known (it may be returning by copy as well as passing by copy):
                    // result = new PhpReference(PhpVariable.Copy(arg_i, CopyReason.Unknown));

                    ParameterExpression resultVariable = Expression.Parameter(Types.PhpReference[0], "result");
                    ParameterExpression[] vars = new ParameterExpression[] { resultVariable };

                    return Expression.Block(vars,
                        Expression.Assign(
                            resultVariable,
                            Expression.New(Constructors.PhpReference_Object, Expression.Call(Methods.PhpVariable.Copy, arg.Expression, Expression.Constant(CopyReason.Unknown)))),
                        BinderHelper.ThrowArgumentNotPassedByRef(argIndex, routine.FullName),
                        resultVariable);

                    //(MB) I'm not sure if it's necessary to execute these two in this order


                    //Original code
                    //
                    //(MB) I don't have to solve this now, PhpCallback is called in a old manner. So I can just throw exception always now.
                    //
                    // Reports an error in the case that we are not called by callback.
                    // Although, this error is fatal one can switch throwing exceptions off.
                    // If this is the case the afterwards behavior will be the same as if callback was called.
                    //if (!Callback)
                    //{
                    //    // warning (can invoke user code => we have to save and restore callstate):
                    //    CallState call_state = SaveCallState();

                    //    PhpException.ArgumentNotPassedByRef(i, CalleeName);
                    //    RestoreCallState(call_state);
                    //}
                }
            }
            else
            {
                return Expression.Convert(arg.Expression, arg.LimitType);
            }

          
        }


        private static Expression/*!*/ PeekType(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int argIndex)
        {
            if (arg != null)
            {
                // peeks the value:
                return Expression.Convert(arg.Expression, arg.LimitType);
            }
            else
            {
                return Expression.Block(
                    BinderHelper.ThrowMissingTypeArgument(argIndex, routine.FullName),
                    Expression.Constant(DTypeDesc.ObjectTypeDesc, Types.DTypeDesc[0]));
            }
        }

        private static Expression/*!*/ PeekTypeOptional(PhpRoutine routine, DynamicMetaObject scriptContext, DynamicMetaObject arg, int argIndex)
        {
            if (arg != null)
            {
                // peeks the value:
                return Expression.Convert(arg.Expression, arg.LimitType);
            }
            else
            {
                // default value:
                return Expression.Constant(Arg.DefaultType, Types.DTypeDesc[0]);
            }

        }



    }
}
