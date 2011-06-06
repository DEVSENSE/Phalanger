using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using PHP.Core.Reflection;
using System.Linq.Expressions;

namespace PHP.Core.Binders
{
    //TODO:
    // - PhpStack.PeekReferenceUnchecked do the same, because as an argument I can receive PhpRefence and PhpRuntimeChain
    // - implement IDynamicMetaObjectProvider for DObject, PhpObject, ClrObject, ClrValue

    using PHP.Core.Emit;

    /// <summary>
    /// 
    /// </summary>
    public abstract class PhpBaseInvokeMemberBinder : DynamicMetaObjectBinder
    {
        #region Fields

        protected readonly string methodName;
        protected readonly int genericParamsCount;
        protected readonly int paramsCount;
        protected readonly DTypeDesc callerClassContext;
        protected readonly Type returnType;

        #endregion

        #region Properties


        /// <summary>
        /// The result type of the invoke operation.
        /// </summary>
        public override Type ReturnType {
            get
            {
                return returnType;
            }
        }


        /// <summary>
        /// This binder binds indirect method calls
        /// </summary>
        public bool IsIndirect
        {
            get { return methodName == null; }
        }

        /// <summary>
        /// Name of the method
        /// </summary>
        protected abstract string ActualMethodName
        {
            get;
        }

        public int RealMethodArgumentCount
        {
            get
            {
                //ScriptContext + generic parameters count + parameters count
                return 1 + genericParamsCount + paramsCount;
            }
        }

        #endregion


        #region Construction

        /// <summary>
        /// Creates appropriate binder according to paramaters specified
        /// </summary>
        /// <param name="methodName">Name of the method known during binder creation.</param>
        /// <param name="genericParamsCount">Number of generic type arguments of the method</param>
        /// <param name="paramsCount">Number of arguments of the method</param>
        /// <param name="callerClassContext">TypeDesc of the class that is calling this method</param>
        /// <param name="returnType">Type which is expected from the call site to return</param>
        /// <returns>Return appropriate binder derived from PhpInvokeMemberBinder</returns>
        public static PhpInvokeMemberBinder Create(string methodName, int genericParamsCount, int paramsCount, DTypeDesc callerClassContext, Type returnType)
        {
            if (methodName == null)
            {
                return new PhpIndirectInvokeMemberBinder( genericParamsCount, paramsCount, callerClassContext, returnType );
            }
            else
            {
                return new PhpInvokeMemberBinder(methodName, genericParamsCount, paramsCount, callerClassContext, returnType);
            }

        }

        protected PhpBaseInvokeMemberBinder(string methodName, int genericParamsCount, int paramsCount, DTypeDesc callerClassContext, Type returnType)
        {
            this.methodName = methodName;
            this.genericParamsCount = genericParamsCount;
            this.paramsCount = paramsCount;
            this.callerClassContext = callerClassContext;
            this.returnType = returnType;
        }


        #endregion

        #region Methods

        /// <summary>
        /// Php ivoke call site signature is non-standard for DLR. If the object implements IDynamicMetaObjectProvider
        /// a call has to be transleted to arguments that can understand. If it's object comming from Phalanger
        /// for now we just fallback to FallbackInvokeMember method.
        /// </summary>
        /// <remarks>
        /// 
        /// CallSite< Func< CallSite, 
        ///      object /*target instance*/,
        ///      ScriptContext, {args}*/*method call arguments*/,
        ///      (DTypeDesc)?/*class context,
        ///      iff <classContext>.IsUnknown*/,
        ///      (object)?/*method name,
        ///      iff <methodName>==null*/, <returnType> > >
        /// 
        /// </remarks>
        public override DynamicMetaObject/*!*/ Bind(DynamicMetaObject/*!*/ target, DynamicMetaObject/*!*/[]/*!*/ args)
        {
            Debug.Assert(args.Length > 0);

            if (target.Value is IDynamicMetaObjectProvider)
            {
                //Translate arguments to DLR standard
                //TODO: Create DlrCompatibilityInvokeBinder because it has to be derived from InvokeMemberBinder
                //return target.BindInvokeMember(this,args);
            }

            //target = target.ToPhpDynamicMetaObject();

            return FallbackInvokeMember(target, args);
        }


        protected abstract DynamicMetaObject/*!*/ FallbackInvokeMember(DynamicMetaObject target/*!*/, DynamicMetaObject/*!*/[]/*!*/ args);


        protected static DynamicMetaObject[]/*!*/ GetArgumentsRange(DynamicMetaObject/*!*/[]/*!*/ args, int startIndex, int length)
        {
            int top = startIndex + length;
            DynamicMetaObject[] arguments = new DynamicMetaObject[length];
            for (int i = 0; i < length; ++i)
                arguments[i] = args[i + startIndex];

            return arguments;
        }


        /// <summary>
        /// Converts first #length elements from a given array of DynamicMetaObject to array of Expression
        /// </summary>
        /// <param name="args">Array of DynamicMetaObject to be converted to Expression[]</param>
        /// <param name="startIndex">Index of first argument that's going to be converted</param>
        /// <param name="length">Count of arguments that are going to be converted</param>
        /// <returns>Expression[] of values of DynamicMetaObject array</returns>
        protected static Expression[]/*!*/ PackToExpressions(DynamicMetaObject/*!*/[]/*!*/ args, int startIndex, int length)
        {
            int top = startIndex + length;
            Expression[] arguments = new Expression[length];
            for (int i = 0; i < length; ++i)
                arguments[i] = args[i + startIndex].Expression;

            return arguments;
        }

        protected static Expression[]/*!*/ PackToExpressions(DynamicMetaObject/*!*/[]/*!*/ args)
        {
            return PackToExpressions(args, 0, args.Length);
        }



        protected DynamicMetaObject GetRuntimeClassContext(DynamicMetaObject[] args)
        {
            if ( args.Length > this.genericParamsCount + this.paramsCount + 1)
                return args[this.genericParamsCount + this.paramsCount + 1];

            return null;
        }

        /// <summary>
        /// Returns ClassContext that was supplied during creation of binder or if it wasn't available that time, it selects it from supplied arguments
        /// </summary>
        /// <param name="args">Arguments supplied during run time bind process</param>
        /// <returns></returns>
        protected DTypeDesc GetActualClassContext(DynamicMetaObject[] args)
        {
            if (this.callerClassContext == null)
                return null;

            if (!this.callerClassContext.IsUnknown)
                return this.callerClassContext;

            var dmoClassContext = GetRuntimeClassContext(args);
            return dmoClassContext != null ? (DTypeDesc)dmoClassContext.Value : null;
        }


        /// <summary>
        /// Boxes a copy of the result.
        /// => return PhpVariable.MakeReference(PhpVariable.Copy(result, CopyReason.ReturnedByCopy));
        /// </summary>
        /// <param name="result">Result to be boxed</param>
        protected Expression HandleResult(Expression result)
        {
            if (returnType != Types.Void)
            {
                result = Expression.Call(Methods.PhpVariable.Copy,
                                        result,
                                        Expression.Constant(CopyReason.ReturnedByCopy));

                if (returnType == Types.PhpReference[0])
                    result = Expression.Call(Methods.PhpVariable.MakeReference, result);
            }
            else
            {
                //TODO: do nothing or not for a void???
            }
            return result;
        }



        protected DynamicMetaObject DoAndReturnDefault(Expression rule, BindingRestrictions restrictions)
        {
            return new DynamicMetaObject(
                    Expression.Block(
                        rule,
                        returnType == Types.PhpReference[0] ? (Expression)Expression.New(Constructors.PhpReference_Void) : Expression.Constant(null)),
                    restrictions);
        }


        #endregion

    }


    public class PhpInvokeMemberBinder : PhpBaseInvokeMemberBinder
    {
        #region Fields

        private ParameterExpression argsArrayVariable;
        private ParameterExpression retValVariable;

        #endregion

        #region Properties

        /// <summary>
        /// Name of the method
        /// </summary>
        protected override string ActualMethodName
        {
            get { return this.methodName;  }
        }

        #endregion

        #region Construction

        internal PhpInvokeMemberBinder(string methodName, int genericParamsCount, int paramsCount, DTypeDesc callerClassContext, Type returnType):
            base(methodName, genericParamsCount, paramsCount, callerClassContext, returnType)
        {

        }


        #endregion

        #region Methods

        protected override DynamicMetaObject/*!*/ FallbackInvokeMember(DynamicMetaObject target/*!*/, DynamicMetaObject/*!*/[]/*!*/ args)
        {
            Expression invokeMethodExpr;

            DObject obj = target.Value as DObject;// target.Value can be something else which isn't DObject ?

            WrappedClrDynamicMetaObject wrappedTarget = null;

            bool invokeCallMethod = false;

            // Restrictions
            BindingRestrictions restrictions;
            BindingRestrictions classContextRestrictions = BindingRestrictions.Empty;
            BindingRestrictions defaultRestrictions = BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType);

            DTypeDesc callerClassContext = GetActualClassContext(args);
            DynamicMetaObject dmoRuntimeClassContext = GetRuntimeClassContext(args);

            if (dmoRuntimeClassContext != null)//ClassContext wasn't supplied during creation of binder => put it into restriction
            {
                classContextRestrictions = BindingRestrictions.GetInstanceRestriction(dmoRuntimeClassContext.Expression, dmoRuntimeClassContext.Value);
                defaultRestrictions.Merge(classContextRestrictions);
            }

            if (obj == null)
            {

                if (target.Value != null && Configuration.Application.Compiler.ClrSemantics)
                {
                    // TODO: some normalizing conversions (PhpString, PhpBytes -> string):
                    target = new WrappedClrDynamicMetaObject(target);
                    obj = target.Value as DObject;

                    wrappedTarget = target as WrappedClrDynamicMetaObject;

                    Debug.Assert(obj != null);

                }
                else
                {
                    return DoAndReturnDefault(
                                    BinderHelper.ThrowError("method_called_on_non_object", ActualMethodName),
                                    defaultRestrictions);
                }
            }



            // obtain the appropriate method table
            DTypeDesc type_desc = obj.TypeDesc;

            // perform method lookup
            DRoutineDesc method;
            GetMemberResult result = type_desc.GetMethod(new Name(ActualMethodName), callerClassContext, out method);


            //PhpStack stack = context.Stack;

            if (result == GetMemberResult.NotFound)
            {

                if ((result = type_desc.GetMethod(DObject.SpecialMethodNames.Call, callerClassContext, out method)) == GetMemberResult.NotFound)
                {
                    return DoAndReturnDefault(
                                    Expression.Call(Methods.PhpException.UndefinedMethodCalled, Expression.Constant(obj.TypeName), Expression.Constant(ActualMethodName)),
                                    defaultRestrictions
                                    );// TODO: alter restrictions
                }
                else
                {
                    invokeCallMethod = true;
                }

            }

            // throw an error if the method was found but the caller is not allowed to call it due to its visibility
            if (result == GetMemberResult.BadVisibility)
            {
                return DoAndReturnDefault(
                    BinderHelper.ThrowVisibilityError(method, callerClassContext),
                    defaultRestrictions);
            }

            if (invokeCallMethod)
            {
                InvokeCallMethod(target, args, obj, method, out restrictions, out invokeMethodExpr);

                return new DynamicMetaObject(invokeMethodExpr, restrictions.Merge(classContextRestrictions));

            }
            else
            {
                // we are invoking the method

                // PhpObject
                if (Types.PhpObject[0].IsAssignableFrom(target.LimitType))
                {
                    InvokePhpMethod(target, args, (PhpObject)target.Value, method.PhpRoutine, out restrictions, out invokeMethodExpr);
                    return new DynamicMetaObject(invokeMethodExpr, restrictions.Merge(classContextRestrictions));
                }
                else
                //ClrObject
                if (target.LimitType == typeof(ClrObject))
                {
                    InvokeClrMethod(new ClrDynamicMetaObject(target), args, method, out restrictions, out invokeMethodExpr);

                    if (wrappedTarget != null)
                    {
                        return new DynamicMetaObject(Expression.Block(wrappedTarget.WrapIt(),
                                                     invokeMethodExpr), wrappedTarget.Restrictions.Merge(classContextRestrictions));
                    }

                    return new DynamicMetaObject(invokeMethodExpr, restrictions.Merge(classContextRestrictions));
                }else
                //IClrValue
                if (typeof(IClrValue).IsAssignableFrom(target.LimitType))
                {
                    InvokeClrMethod(new ClrValueDynamicMetaObject(target), args, method, out restrictions, out invokeMethodExpr);

                    if (wrappedTarget != null)
                    {
                        return new DynamicMetaObject(Expression.Block(wrappedTarget.WrapIt(),
                                                     invokeMethodExpr), wrappedTarget.Restrictions.Merge(classContextRestrictions));
                    }

                    return new DynamicMetaObject(invokeMethodExpr, restrictions.Merge(classContextRestrictions));
                }
            }

            throw new NotImplementedException();

        }

        private void InvokeClrMethod(DynamicMetaObject target, DynamicMetaObject/*!*/[] args, DRoutineDesc method, out BindingRestrictions restrictions, out Expression invokeMethodExpr)
        {
            DynamicMetaObject scriptContext = args[0];

            //Select arguments without scriptContext
            DynamicMetaObject[] realArgs = GetArgumentsRange(args, 1, RealMethodArgumentCount - 1);

            // Convert arguments
            DynamicMetaObject[] realArgsConverted = Array.ConvertAll<DynamicMetaObject, DynamicMetaObject>(realArgs, (x) =>
            {
                return x.ToPhpDynamicMetaObject();

            });

#if DLR_OVERLOAD_RESOLUTION

            //DLR overload resolution
            DynamicMetaObject res = PhpBinder.Instance.CallClrMethod(method.ClrMethod, target, realArgsConverted);
            restriction = res.Restriction;
            invokeMethodExpr = res.Rule;

#else
            // Old overload resolution
            InvokeArgLess(target, scriptContext, method, realArgsConverted, out restrictions, out invokeMethodExpr);
#endif
        }


        private void InvokeArgLess(DynamicMetaObject target, DynamicMetaObject scriptContext, DRoutineDesc method, DynamicMetaObject[] args, out BindingRestrictions restrictions, out Expression invokeMethodExpr)
        {
            int argsWithoutScriptContext = RealMethodArgumentCount - 1;

            System.Reflection.MethodInfo miAddFrame = Methods.PhpStack.AddFrame.Overload(argsWithoutScriptContext);

            Expression[] argsExpr = null;
            if (miAddFrame == Methods.PhpStack.AddFrame.N)
            {
                //Create array of arguments
                argsExpr = new Expression[1];
                argsExpr[0] = Expression.NewArrayInit(Types.Object[0], PackToExpressions(args, 0, argsWithoutScriptContext));
            }
            else
            {
                //call overload with < N arguments
                //argsExpr = new Expression[argsWithoutScriptContext];
                argsExpr = PackToExpressions(args, 0, argsWithoutScriptContext);
            }

            var stack = Expression.Field(scriptContext.Expression,
                                            Fields.ScriptContext_Stack);

            // scriptContext.PhpStack
            // PhpStack.Add( args )
            // call argless stub
            invokeMethodExpr = Expression.Block(returnType,
                                    Expression.Call(
                                        stack,
                                        miAddFrame, argsExpr),
                                    HandleResult(Expression.Call(Expression.Constant(method.ArglessStub, typeof(RoutineDelegate)), "Invoke", Type.EmptyTypes,
                                        target.Expression,
                                        stack)));

            restrictions = target.Restrictions;
        }

        private void InvokeCallMethod(DynamicMetaObject target,
            DynamicMetaObject/*!*/[] args,
            DObject/*!*/ obj,
            DRoutineDesc/*!*/ method,
            out BindingRestrictions restrictions,
            out Expression invokeMethodExpr)
        {
            var insideCaller = Expression.Property(
                                        Expression.Convert(target.Expression, Types.DObject[0]),
                                         Properties.DObject_InsideCaller);

            if (argsArrayVariable == null)
                argsArrayVariable = Expression.Parameter(Types.PhpArray[0], "args");

            if (retValVariable == null)
                retValVariable = Expression.Parameter(Types.Object[0], "retVal");

            ParameterExpression[] vars = new ParameterExpression[] { argsArrayVariable, retValVariable };

            // Just select real method arguments without ScriptContext and generic type arguments
            var justParams = PackToExpressions(args, 1 + genericParamsCount, paramsCount);

            // Expression which calls ((PhpArray)argArray).add method on each real method argument.
            var initArgsArray = Array.ConvertAll<Expression, Expression>(justParams, (x) => Expression.Call(argsArrayVariable, Methods.PhpHashtable_Add, x));

            // Argfull __call signature: (ScriptContext, object, object)->object
            var callerMethodArgs = new DynamicMetaObject[3] {   args[0], 
                                                                new DynamicMetaObject(Expression.Constant(ActualMethodName),BindingRestrictions.Empty),
                                                                new DynamicMetaObject(argsArrayVariable, BindingRestrictions.Empty) };
            // what if method.PhpRoutine is null 
            InvokePhpMethod(target, callerMethodArgs, (PhpObject)target.Value, method.PhpRoutine, out restrictions, out invokeMethodExpr);


            //Expression:
            // if (target.insideCaller)
            //      throw new UndefinedMethodException();
            //
            // args = new PhpArray(paramsCount, 0);
            //
            // args.Add(arg0);
            // .
            // .
            // args.Add(paramsCount);
            //
            // target.insideCaller = true;
            // try
            // {
            //     ret_val = target.__call( scriptContext, methodName, args);
            // }
            // finally
            // {
            //     target.insideCaller = false;
            // }
            // return ret_val;
            //
            invokeMethodExpr = Expression.Block(
                vars,//local variables
                Expression.IfThen(Expression.Property(
                                        Expression.Convert(target.Expression, Types.DObject[0]),
                                        Properties.DObject_InsideCaller),
                                  Expression.Call(Methods.PhpException.UndefinedMethodCalled, Expression.Constant(obj.TypeName), Expression.Constant(ActualMethodName))),

                Expression.Assign(
                                argsArrayVariable,
                                Expression.New(Constructors.PhpArray.Int32_Int32,
                                            Expression.Constant(paramsCount),
                                            Expression.Constant(0))),
                    
                                ((initArgsArray.Length == 0) ?  (Expression)Expression.Empty() : Expression.Block(initArgsArray)),

                                Expression.Assign(insideCaller,
                                                Expression.Constant(true)),
                                Expression.TryFinally(
                                //__call(caller,args)
                                    Expression.Assign(retValVariable, invokeMethodExpr),
                                //Finally part:
                                    Expression.Assign(insideCaller,
                                                        Expression.Constant(false))
                                    ),
                                retValVariable);
        }


        /// <summary>
        /// This method binds rules for PhpMethod
        /// </summary>
        /// <param name="target"></param>
        /// <param name="args"></param>
        /// <param name="targetObj"></param>
        /// <param name="routine"></param>
        /// <returns></returns>
        private void InvokePhpMethod(DynamicMetaObject/*!*/ target, DynamicMetaObject/*!*/[]/*!*/ args, PhpObject/*!*/ targetObj, PhpRoutine/*!*/ routine, out BindingRestrictions restrictions, out Expression invokeMethodExpr)
        {
            // Restriction: typeof(target) == |target.TypeDesc.RealType|
            restrictions = BindingRestrictions.GetTypeRestriction(target.Expression, targetObj.TypeDesc.RealType); //TODO: it's sufficient to use typeof(targetObj), but this is faster                                                                                                                                                                

            if (routine.Name != PHP.Core.Reflection.DObject.SpecialMethodNames.Call)
            {
                args = GetArgumentsRange(args, 0, RealMethodArgumentCount);// This can't be done when _call method is invoked

                //Check if method signature match calling signature or if method has ArgAware attribute
                if (((1 + RealMethodArgumentCount) != routine.Signature.ParamCount ||
                    (routine.Properties & RoutineProperties.IsArgsAware) != 0))
                {
                    DynamicMetaObject scriptContext = args[0];

                    //Select arguments without scriptContext
                    DynamicMetaObject[] realArgs = GetArgumentsRange(args, 1, RealMethodArgumentCount - 1);

                    InvokeArgLess(target, scriptContext, routine.RoutineDesc, realArgs, out restrictions, out invokeMethodExpr);
                    return;
                }
            }

            //((PhpObject)target))
            var realObjEx = Expression.Convert(target.Expression, targetObj.TypeDesc.RealType);

            //ArgFull( ((PhpObject)target), ScriptContext, args, ... )
            invokeMethodExpr = Expression.Call(realObjEx,
                                     routine.ArgFullInfo,
                                     PackToExpressions(args));

            invokeMethodExpr = HandleResult(invokeMethodExpr);

        }

        #endregion

    }

    #region PhpIndirectInvokeMemberBinder

    public sealed class PhpIndirectInvokeMemberBinder : PhpInvokeMemberBinder
    {

        #region Fields

        private string actualMethodName;

        #endregion

        #region Properties

        /// <summary>
        /// Name of the actual method.
        /// </summary>
        /// <remarks>
        /// Can change in the begining of the each binding
        /// </remarks>
        protected override string ActualMethodName
        {
            get { return this.actualMethodName; }
        }

        #endregion

        #region Construction

        internal PhpIndirectInvokeMemberBinder(int genericParamsCount, int paramsCount, DTypeDesc callerClassContext, Type returnType) :
            base(null, genericParamsCount, paramsCount, callerClassContext, returnType)
        {

        }

        #endregion

        #region Methods

        /// <summary>
        /// Returns methodName from Args
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        protected DynamicMetaObject GetRuntimeMethodName(DynamicMetaObject[] args)
        {
            if (args.Length == this.genericParamsCount + this.paramsCount + 3) // args contains ClassContext 
                return args[this.genericParamsCount + this.paramsCount + 2];
            else if (args.Length == this.genericParamsCount + this.paramsCount + 2)
                return args[this.genericParamsCount + this.paramsCount + 1];

            throw new InvalidOperationException();
        }


        protected override DynamicMetaObject/*!*/ FallbackInvokeMember(
            DynamicMetaObject target, 
            DynamicMetaObject[] args)
        {
            DynamicMetaObject dmoMethodName = GetRuntimeMethodName(args);

            string name = PhpVariable.AsString(dmoMethodName.Value);
            if (name == null)
            {
                //PhpException.Throw(PhpError.Error, CoreResources.GetString("invalid_method_name"));
                //return new PhpReference() | null;

                return DoAndReturnDefault(
                                BinderHelper.ThrowError("invalid_method_name"),
                                target.Restrictions);

                throw new NotImplementedException();
            }
            else
            {
                // Restriction: PhpVariable.AsString(methodName) == |methodName|
                BindingRestrictions restrictions = BindingRestrictions.GetExpressionRestriction(
                                    Expression.Equal(
                                        Expression.Call(Methods.PhpVariable.AsString, dmoMethodName.Expression),
                                        Expression.Constant(dmoMethodName.Value, Types.String[0])));

                actualMethodName = name;

                DynamicMetaObject result = base.FallbackInvokeMember(target, args);

                return new DynamicMetaObject(
                    result.Expression,
                    result.Restrictions.Merge(restrictions));//TODO: Creation of this can be saved
            }

        }

        #endregion
    }

    #endregion

}
