using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using PHP.Core.Reflection;
using System.Linq.Expressions;

namespace PHP.Core.Binders
{
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
        public abstract string ActualMethodName
        {
            get;
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

        // CallSite< Func< CallSite, 
        //      object /*target instance*/,
        //      ScriptContext, {args}*/*method call arguments*/,
        //      (DTypeDesc)?/*class context,
        //      iff <classContext>.IsUnknown*/,
        //      (object)?/*method name,
        //      iff <methodName>==null*/, <returnType> > >

        /// <summary>
        /// Php ivoke call site signature is non-standard for DLR. If the object implements IDynamicMetaObjectProvider
        /// a call has to be transleted to arguments that can understand. If it's object comming from Phalanger
        /// for now we just fallback to FallbackInvokeMember method.
        /// </summary>
        public override DynamicMetaObject/*!*/ Bind(DynamicMetaObject/*!*/ target, DynamicMetaObject/*!*/[]/*!*/ args)
        {
            Debug.Assert(args.Length > 0);

            if (target.Value is IDynamicMetaObjectProvider)
            {
                //Translate arguments to DLR standard
                //TODO: Create DlrCompatibilityInvokeBinder because it has to be derived from InvokeMemberBinder
                //return target.BindInvokeMember(this,args);
            }

            return FallbackInvokeMember(target, args);
        }


        protected abstract DynamicMetaObject/*!*/ FallbackInvokeMember(DynamicMetaObject target/*!*/, DynamicMetaObject/*!*/[]/*!*/ args);


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
        public override string ActualMethodName
        {
            get { return this.methodName;  }
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

        internal PhpInvokeMemberBinder(string methodName, int genericParamsCount, int paramsCount, DTypeDesc callerClassContext, Type returnType):
            base(methodName, genericParamsCount, paramsCount, callerClassContext, returnType)
        {

        }

        #endregion

        #region Methods

        protected override DynamicMetaObject/*!*/ FallbackInvokeMember(DynamicMetaObject/*!*/ target, DynamicMetaObject/*!*/[]/*!*/ args)
        {
            BindingRestrictions restrictions;
            Expression invokeMethodExpr;

            DObject obj = target.Value as DObject;// target.Value can be something else which isn't DObject ?
            ScriptContext scriptContext = args[0].Value as ScriptContext;
            Expression[] realMethodArgs = PackToExpressions(args, 0, RealMethodArgumentCount);

            bool invokeCallMethod = false;

            if (obj == null)
            {
                //TODO: I'll solve this later
                //if (x != null && Configuration.Application.Compiler.ClrSemantics)
                //{
                //    // TODO: some normalizing conversions (PhpString, PhpBytes -> string):
                //    obj = ClrObject.WrapRealObject(x);
                //}
                //else
                //{
                //    context.Stack.RemoveFrame();
                //    PhpException.Throw(PhpError.Error, CoreResources.GetString("method_called_on_non_object", methodName));
                //    return new PhpReference();
                //}
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
                    //TODO: Generate Error
                    Expression.Call(Methods.PhpException.UndefinedMethodCalled, Expression.Constant(obj.TypeName), Expression.Constant(ActualMethodName));
                    
                }
                else
                {
                    invokeCallMethod = true;
                }

            }

            // throw an error if the method was found but the caller is not allowed to call it due to its visibility
            if (result == GetMemberResult.BadVisibility)
            {
                //stack.RemoveFrame();
                //TODO: Generate ThrowMethodVisibilityError(method, caller);
                return null;
            }

            if (invokeCallMethod)
            {
                InvokeCallMethod(target, args, obj, method, out restrictions, out invokeMethodExpr);

                return new DynamicMetaObject(invokeMethodExpr, restrictions);

            }
            else
            {
                // we are invoking the method
                
                // PhpObject
                if (typeof(PhpObject).IsAssignableFrom(target.LimitType))
                {
                    InvokePhpMethod(target.Expression, realMethodArgs, (PhpObject)target.Value, method.PhpRoutine, out restrictions, out invokeMethodExpr);
                    return new DynamicMetaObject(invokeMethodExpr, restrictions);
                }
                else
                //ClrObject
                if (target.LimitType == typeof(ClrObject))
                {
                    //overload resolution
                }
                //IClrValue
            }

            throw new NotImplementedException();

        }

        private void InvokeCallMethod(DynamicMetaObject target, DynamicMetaObject/*!*/[] args, DObject/*!*/ obj, DRoutineDesc/*!*/ method, out BindingRestrictions restrictions, out Expression invokeMethodExpr)
        {
            var insideCaller = Expression.Property(
                                        Expression.Convert(target.Expression, Types.DObject[0]),
                                         "insideCaller");

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
            var callerMethodArgs = new Expression[3] { args[0].Expression, Expression.Constant(ActualMethodName), argsArrayVariable };
            InvokePhpMethod(target.Expression, callerMethodArgs, (PhpObject)target.Value, method.PhpRoutine, out restrictions, out invokeMethodExpr);


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
                                        "insideCaller"),
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
        private void InvokePhpMethod(Expression/*!*/ target, Expression/*!*/[]/*!*/ args, PhpObject/*!*/ targetObj, PhpRoutine/*!*/ routine , out BindingRestrictions restrictions, out Expression result)
        {

            // Restriction: typeof(target) == |target.TypeDesc.RealType|
            restrictions = BindingRestrictions.GetTypeRestriction(target, targetObj.TypeDesc.RealType);

            //((PhpObject)target))
            var realObjEx = Expression.Convert(target, targetObj.TypeDesc.RealType);

            //ArgFull( ((PhpObject)target), ScriptContext, args, ... )
            result = Expression.Call(realObjEx,
                                     routine.ArgFullInfo,
                                     args);

            // boxes a copy of the result:
            // return PhpVariable.MakeReference(PhpVariable.Copy(result, CopyReason.ReturnedByCopy));

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
                //TODO: do nothing or not???
            }

        }

        #endregion


        //DynamicMetaObject self = target.Restrict(target.GetLimitType());
        //BindingRestrictions restrictions = self.Restrictions;
        //foreach (DynamicMetaObject arg in args)
        //    restrictions = restrictions.Merge(arg.Restrictions);

        //// return DynamicMetaObject
        //return new DynamicMetaObject(resultExpr, restrictions);


    }



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
        public override string ActualMethodName
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

        protected override DynamicMetaObject/*!*/ FallbackInvokeMember(DynamicMetaObject target, DynamicMetaObject[] args)
        {
            //args[0] ScriptContext
            //args[1..genericParamsCount-1]  Type
            //args[1 + genericParamsCount.. 1 + genericParamsCount + paramsCount -1]  object
            //args[1 + genericParamsCount + paramsCount ] callContext?
            //args[1|2 + genericParamsCount + paramsCount ] methodName


            DynamicMetaObject dmoMethodName;

            if (args.Length == genericParamsCount + paramsCount + 3) // args contains ClassContext 
                dmoMethodName = args[genericParamsCount + paramsCount + 2];
            else
                dmoMethodName = args[genericParamsCount + paramsCount + 1];

            string name = PhpVariable.AsString(dmoMethodName.Value);
            if (name == null)
            {
                //TODO generate PhpException.Throw(PhpError.Error, CoreResources.GetString("invalid_method_name"));
                //return new PhpReference();

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

}
