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
    // - implement IDynamicMetaObjectProvider for DObject, PhpObject, ClrObject, ClrValue
    // - if library function is called with wrong number of arguments throw PhpException.InvalidArgumentCount
    using PHP.Core.Emit;

    /// <summary>
    /// 
    /// </summary>
    public abstract class PhpBaseInvokeMemberBinder : DynamicMetaObjectBinder
    {
        #region Fields

        protected readonly string _methodName;
        protected readonly int _genericParamsCount;
        protected readonly int _paramsCount;
        protected readonly DTypeDesc _classContext;
        protected readonly Type _returnType;

        #endregion

        #region Properties

        protected bool ClassContextIsKnown { get { return _classContext == null || !_classContext.IsUnknown; } }

        /// <summary>
        /// The result type of the invoke operation.
        /// </summary>
        public override Type ReturnType
        {
            get
            {
                return _returnType;
            }
        }


        /// <summary>
        /// This binder binds indirect method calls
        /// </summary>
        public bool IsIndirect
        {
            get { return _methodName == null; }
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
                return 1 + _genericParamsCount + _paramsCount;
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
                return new PhpIndirectInvokeMemberBinder(genericParamsCount, paramsCount, callerClassContext, returnType);
            }
            else
            {
                return new PhpInvokeMemberBinder(methodName, genericParamsCount, paramsCount, callerClassContext, returnType);
            }

        }

        protected PhpBaseInvokeMemberBinder(string methodName, int genericParamsCount, int paramsCount, DTypeDesc callerClassContext, Type returnType)
        {
            this._methodName = methodName;
            this._genericParamsCount = genericParamsCount;
            this._paramsCount = paramsCount;
            this._classContext = callerClassContext;
            this._returnType = returnType;
        }


        #endregion

        #region Methods

        /// <summary>
        /// Php invoke call site signature is non-standard for DLR. If the object implements IDynamicMetaObjectProvider
        /// a call has to be transleted to arguments that can understand. If it's object comming from Phalanger
        /// for now we just fallback to FallbackInvokeMember method.
        /// </summary>
        /// <remarks>
        /// 
        /// CallSite&lt; Func&lt; CallSite, 
        ///      object /*target instance*/,
        ///      ScriptContext,
        ///      {args}*/*method call arguments*/,
        ///      (DTypeDesc)?/*class context,
        ///      iff {classContext}.IsUnknown*/,
        ///      (object)?/*method name,
        ///      iff {methodName}==null*/, {returnType} &gt; &gt;
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


        protected DynamicMetaObject GetRuntimeClassContext(DynamicMetaObject[] args)
        {
            if (args.Length > this._genericParamsCount + this._paramsCount + 1)
                return args[this._genericParamsCount + this._paramsCount + 1];

            return null;
        }

        /// <summary>
        /// Returns ClassContext that was supplied during creation of binder or if it wasn't available that time, it selects it from supplied arguments
        /// </summary>
        /// <param name="args">Arguments supplied during run time bind process</param>
        /// <returns></returns>
        protected DTypeDesc GetActualClassContext(DynamicMetaObject[] args)
        {
            if (this._classContext == null)
                return null;

            if (!this._classContext.IsUnknown)
                return this._classContext;

            var dmoClassContext = GetRuntimeClassContext(args);
            return dmoClassContext.Value != null ? (DTypeDesc)dmoClassContext.Value : null;
        }


        /// <summary>
        /// Handles the return argument of the method
        /// </summary>
        /// <remarks>
        /// Caller needs as returned type
        /// 1.) void
        ///    => result
        /// 2.) PhpReference
        ///    a.) if method returns PhpReference => result
        ///    b.) otherwise => PhpVariable.MakeReference(PhpVariable.Copy(result, CopyReason.ReturnedByCopy));
        /// 3.) otherwise
        ///    a.) if method returns PhpReference = > result.Value
        ///    b.) otherwise => PhpVariable.Dereference(PhpVariable.Copy(result, CopyReason.ReturnedByCopy));
        /// </remarks>
        /// <param name="result">Result to be handled</param>
        /// <param name="methodReturnType">Type of the return argument the method</param>
        /// <param name="dereference">Dereference will be generated.</param>
        protected Expression/*!*/ HandleResult(Expression/*!*/ result, Type/*!*/ methodReturnType, bool dereference = true)
        {

            if (_returnType == Types.Void)
            {
                // do nothing
                return result;
            }
            else if (_returnType == Types.PhpReference[0])
            {
                if (methodReturnType == Types.PhpReference[0])
                    return result;
                else
                {
                    result = CopyByReturn(result);

                    return Expression.Call(Methods.PhpVariable.MakeReference, result);
                }

            }
            else /*if (_returnType != Types.PhpReference[0])*/
            {
                result = CopyByReturn(result);

                if (methodReturnType == Types.PhpReference[0])
                    return Expression.Field(Expression.Convert(result, Types.PhpReference[0]), Fields.PhpReference_Value);
                else if (dereference)
                {
                    return Expression.Call(Methods.PhpVariable.Dereference, result);
                }
                else
                {
                    // We don't need to dereference at all in this point(for argfull overload only!)
                    // To make sure
                    result = BinderHelper.AssertNotPhpReference(result);
                    return result;
                }
            }

        }

        private static Expression CopyByReturn(Expression result)
        {
            result = Expression.Call(Methods.PhpVariable.Copy,
                result,
                Expression.Constant(CopyReason.ReturnedByCopy));
            return result;
        }



        protected DynamicMetaObject DoAndReturnDefault(Expression rule, BindingRestrictions restrictions)
        {
            return new DynamicMetaObject(
                    Expression.Block(
                        rule,
                        _returnType == Types.PhpReference[0] ? (Expression)Expression.New(Constructors.PhpReference_Void) : Expression.Constant(null)),
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
            get { return this._methodName; }
        }

        #endregion

        #region Construction

        internal PhpInvokeMemberBinder(string methodName, int genericParamsCount, int paramsCount, DTypeDesc callerClassContext, Type returnType) :
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

            DTypeDesc classContext = this._classContext;

            if (!ClassContextIsKnown)//ClassContext wasn't supplied during creation of binder => put it into restriction
            {
                Debug.Assert(args.Length > RealMethodArgumentCount, "Not enough arguments!");

                DynamicMetaObject dmoRuntimeClassContext = GetRuntimeClassContext(args);
                Debug.Assert(dmoRuntimeClassContext.Value == null || Types.DTypeDesc[0].IsAssignableFrom(dmoRuntimeClassContext.LimitType), "Wrong class context type!");

                classContext = (DTypeDesc)dmoRuntimeClassContext.Value;
                Debug.Assert(classContext == null || !classContext.IsUnknown, "Class context should be known at run time!");

                classContextRestrictions = BindingRestrictions.GetInstanceRestriction(dmoRuntimeClassContext.Expression, classContext);
                defaultRestrictions = defaultRestrictions.Merge(classContextRestrictions);
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
                    //defaultRestrictions = defaultRestrictions.Merge(BindingRestrictions.GetTypeRestriction
                    if (target.Value == null)
                        defaultRestrictions = BindingRestrictions.GetInstanceRestriction(target.Expression, null);

                    return DoAndReturnDefault(
                                    BinderHelper.ThrowError("method_called_on_non_object", ActualMethodName),
                                    defaultRestrictions);
                }
            }



            // obtain the appropriate method table
            DTypeDesc type_desc = obj.TypeDesc;

            // perform method lookup
            DRoutineDesc method;
            GetMemberResult result = type_desc.GetMethod(new Name(ActualMethodName), classContext, out method);

            //PhpStack stack = context.Stack;

            if (result == GetMemberResult.NotFound)
            {

                if ((result = type_desc.GetMethod(DObject.SpecialMethodNames.Call, classContext, out method)) == GetMemberResult.NotFound)
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
                    BinderHelper.ThrowVisibilityError(method, classContext),
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

                // PhpRoutine (function or method)
                if (method.Member is PhpRoutine)
                {
                    InvokePhpMethod(target, args, method.PhpRoutine, out restrictions, out invokeMethodExpr);
                    return new DynamicMetaObject(invokeMethodExpr, restrictions.Merge(classContextRestrictions));
                }
                // ClrMethod
                else if (method.Member is ClrMethod)
                {
                    var targetwrapper = (target.LimitType == typeof(ClrObject)) ?
                        (DynamicMetaObject)new ClrDynamicMetaObject(target) :       // ((ClrObject)target).RealType restriction
                        (DynamicMetaObject)new ClrValueDynamicMetaObject(target);   // simple type restriction, IClrValue<T> or any .NET class inheriting PhpObject

                    InvokeClrMethod(targetwrapper, args, method, out restrictions, out invokeMethodExpr);

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


#if DLR_OVERLOAD_RESOLUTION

            // Convert arguments
            DynamicMetaObject[] realArgsConverted = Array.ConvertAll<DynamicMetaObject, DynamicMetaObject>(realArgs, (x) =>
            {
                return x.ToPhpDynamicMetaObject();

            });

            //DLR overload resolution
            DynamicMetaObject res = PhpBinder.Instance.CallClrMethod(method.ClrMethod, target, realArgsConverted);
            restriction = res.Restriction;
            invokeMethodExpr = res.Rule;

#else
            // Old overload resolution
            // TODO: in case of zero-parameters, we can call via ArgFull
            InvokeArgLess(target, scriptContext, method, realArgs, out restrictions, out invokeMethodExpr);
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
                argsExpr[0] = Expression.NewArrayInit(Types.Object[0], BinderHelper.PackToExpressions(args, 0, argsWithoutScriptContext));
            }
            else
            {
                //call overload with < N arguments
                //argsExpr = new Expression[argsWithoutScriptContext];
                argsExpr = BinderHelper.PackToExpressions(args, 0, argsWithoutScriptContext);
            }

            var stack = Expression.Field(scriptContext.Expression,
                                            Fields.ScriptContext_Stack);

            // scriptContext.PhpStack
            // PhpStack.Add( args )
            // call argless stub
            invokeMethodExpr = Expression.Block(_returnType,
                                    Expression.Call(
                                        stack,
                                        miAddFrame, argsExpr),
                                    Expression.Assign(
                                        Expression.Field(stack, Fields.PhpStack_AllowProtectedCall),
                                        Expression.Constant(true, Types.Bool[0])),
                                    HandleResult(
                                        Expression.Call(method.ArglessStubMethod,
                                            target.Expression,
                                            stack),
                                        method.ArglessStubMethod.ReturnType));

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
            var justParams = BinderHelper.PackToExpressions(args, 1 + _genericParamsCount, _paramsCount);

            // Expression which calls ((PhpArray)argArray).add method on each real method argument.
            var initArgsArray = Array.ConvertAll<Expression, Expression>(justParams, (x) => Expression.Call(argsArrayVariable, Methods.PhpHashtable_Add, x));

            // Argfull __call signature: (ScriptContext, object, object)->object
            var callerMethodArgs = new DynamicMetaObject[3] {   args[0], 
                                                                new DynamicMetaObject(Expression.Constant(ActualMethodName),BindingRestrictions.Empty),
                                                                new DynamicMetaObject(argsArrayVariable, BindingRestrictions.Empty) };
            // what if method.PhpRoutine is null 
            InvokePhpMethod(target, callerMethodArgs, /*(PhpObject)target.Value, */method.PhpRoutine, out restrictions, out invokeMethodExpr);


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
                                            Expression.Constant(_paramsCount),
                                            Expression.Constant(0))),

                                ((initArgsArray.Length == 0) ? (Expression)Expression.Empty() : Expression.Block(initArgsArray)),

                                Expression.Assign(insideCaller,
                                                Expression.Constant(true)),
                                Expression.TryFinally(
                //__call(caller,args)
                                    Expression.Assign(retValVariable, invokeMethodExpr),
                //Finally part:
                                    Expression.Assign(insideCaller,
                                                        Expression.Constant(false))
                                    ),
                                 HandleResult(retValVariable, method.PhpRoutine.ArgFullInfo.ReturnType, false));
        }


        /// <summary>
        /// This method binds rules for PhpMethod
        /// </summary>
        private void InvokePhpMethod(DynamicMetaObject/*!*/ target, DynamicMetaObject[]/*!!*/ args, /*object targetObj,*/ PhpRoutine/*!*/ routine, out BindingRestrictions restrictions, out Expression invokeMethodExpr)
        {
            Debug.Assert(target != null && target.Value != null);
            Debug.Assert(!(target.Value is IClrValue), "PhpRoutine should not be declared on CLR value type!");

            /*if (target.Value is PhpObject)
            {
                // Restriction: typeof(target) == |target.TypeDesc.RealType|
                var targetPhpObj = (PhpObject)target.Value;
                Debug.Assert(targetPhpObj.TypeDesc.RealType == target.LimitType);
                Debug.Assert(target.Value.GetType() == target.LimitType);
                restrictions = BindingRestrictions.GetTypeRestriction(target.Expression, targetPhpObj.TypeDesc.RealType);
            }
            else*/
            Debug.Assert(typeof(ClrObject).IsSealed);   // just to ensure following condition is correct
            if (target.Value.GetType() == typeof(ClrObject))
            {
                target = new ClrDynamicMetaObject(target);  // unwrap the real object, get restrictions
                restrictions = target.Restrictions;
            }
            else
            {
                Debug.Assert(target.Value.GetType() == target.LimitType);   // just for sure
                Debug.Assert(!(target.Value is PhpObject) || ((PhpObject)target.Value).TypeDesc.RealType == target.LimitType);

                restrictions = BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType);
            }

            BindingRestrictions argumentsRestrictions;
            Expression[] arguments;

            if (routine.Name != PHP.Core.Reflection.DObject.SpecialMethodNames.Call)
            {
                args = GetArgumentsRange(args, 0, RealMethodArgumentCount);// This can't be done when _call method is invoked

                //Check if method has ArgAware attribute
                if ((routine.Properties & RoutineProperties.IsArgsAware) != 0 ||
                    routine.IsStatic)// this is because of hack in PHP.Library.XML library static methods that can be also called like instance methods
                {
                    DynamicMetaObject scriptContext = args[0];

                    //Select arguments without scriptContext
                    DynamicMetaObject[] realArgs = GetArgumentsRange(args, 1, RealMethodArgumentCount - 1);

                    InvokeArgLess(target, scriptContext, routine.RoutineDesc, realArgs, out argumentsRestrictions, out invokeMethodExpr);
                    restrictions = restrictions.Merge(argumentsRestrictions);
                    return;
                }

                arguments = routine.PrepareArguments(args, _genericParamsCount, _paramsCount, out argumentsRestrictions);
                restrictions = restrictions.Merge(argumentsRestrictions);
            }
            else
            {
                arguments = BinderHelper.PackToExpressions(args);
            }

            //((PhpObject)target))
            var realObjEx = Expression.Convert(target.Expression, routine.ArgFullInfo.DeclaringType);//targetObj.TypeDesc.RealType);

            //ArgFull( ((PhpObject)target), ScriptContext, args, ... )
            invokeMethodExpr = Expression.Call(BinderHelper.WrapInstanceMethodCall(routine.ArgFullInfo),
                                     BinderHelper.CombineArguments(realObjEx, arguments));

            invokeMethodExpr = ReturnArgumentHelpers.ReturnValueConversion(routine.ArgFullInfo, invokeMethodExpr);

            invokeMethodExpr = HandleResult(invokeMethodExpr, routine.ArgFullInfo.ReturnType, false);

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

        ///// <summary>
        ///// Returns methodName from Args
        ///// </summary>
        ///// <param name="args"></param>
        ///// <returns></returns>
        //protected DynamicMetaObject GetRuntimeMethodName(DynamicMetaObject[] args)
        //{
        //    //if (args.Length == this.genericParamsCount + this.paramsCount + 3) // args contains ClassContext 
        //    //    return args[this.genericParamsCount + this.paramsCount + 2];
        //    //else if (args.Length == this.genericParamsCount + this.paramsCount + 2)
        //    //    return args[this.genericParamsCount + this.paramsCount + 1];

        //    //throw new InvalidOperationException();

        //    return args[args.Length - 1];

        //}


        protected override DynamicMetaObject/*!*/ FallbackInvokeMember(
            DynamicMetaObject target,
            DynamicMetaObject[] args)
        {
            Debug.Assert(Types.String[0].IsAssignableFrom(args[args.Length - 1].LimitType), "Wrong field name type!");

            DynamicMetaObject dmoMethodName = args[args.Length - 1];

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

                //transform arguments that it doesn't contains methodName
                Array.Resize<DynamicMetaObject>(ref args, args.Length - 1);

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
