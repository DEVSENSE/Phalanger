using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;

using PHP.Core;
using PHP.Core.Emit;
using PHP.Core.AST;
using PHP.Core.Reflection;

namespace PHP.Core.Compiler.CodeGenerator
{
    /// <summary>
    /// CallSite fields manager and emitter.
    /// </summary>
    internal class CallSitesBuilder
    {
        #region Fields & Properties

        /// <summary>
        /// The module that will contain the call sites container.
        /// </summary>
        private readonly ModuleBuilder/*!*/moduleBuilder;

        /// <summary>
        /// User-friendly call site container name.
        /// </summary>
        private readonly string/*!*/userFriendlyName;

        /// <summary>
        /// The unique id to name the container class.
        /// </summary>
        private static long nextContainerId = 0;

        /// <summary>
        /// Lazily initialized class contained static instances of declared call sites.
        /// </summary>
        private TypeBuilder containerClass;

        /// <summary>
        /// .cctor of the call sites container. Not null if <see cref="containerClass"/> has been initialized.
        /// </summary>
        private ILEmitter staticCtorEmitter;

        /// <summary>
        /// If known and if it can be emitted in static .cctor, defines the place where the class context can be loaded.
        /// Otherwise <c>null</c>, the class context will be determined in run time and passed to binder.
        /// </summary>
        private IPlace classContextPlace { get { return classContextPlaces.Peek(); } }
        private Stack<IPlace> classContextPlaces = new Stack<IPlace>(2);

        /// <summary>
        /// Current type declaration to emit site containers properly.
        /// </summary>
        private PhpType classContext { get { return classContexts.Peek(); } }
        private Stack<PhpType> classContexts = new Stack<PhpType>(2);

        /// <summary>
        /// Amount of emitted call sites. Used to build unique call site field name.
        /// </summary>
        private long callSitesCount = 0;

        private readonly DelegateBuilder/*!*/delegateBuilder;

        #endregion

        #region Constructors

        /// <summary>
        /// Create new call sites builder.
        /// </summary>
        /// <param name="moduleBuilder">Module to contain call sites container.</param>
        /// <param name="userFriendlyName">User friendly name used to identify the call sites container by user.</param>
        /// <param name="classContextPlace">If known and if it can be emitted in static .cctor, defines the place where the class context can be loaded. Otherwise <c>null</c> if the class context will be determined in run time.</param>
        public CallSitesBuilder(ModuleBuilder/*!*/moduleBuilder, string/*!*/userFriendlyName, IPlace classContextPlace)
            : this(moduleBuilder, userFriendlyName, classContextPlace, null)
        {
            
        }

        /// <summary>
        /// Create new call sites builder.
        /// </summary>
        /// <param name="moduleBuilder">Module to contain call sites container.</param>
        /// <param name="userFriendlyName">User friendly name used to identify the call sites container by user.</param>
        /// <param name="classContextPlace">If known and if it can be emitted in static .cctor, defines the place where the class context can be loaded. Otherwise <c>null</c> if the class context will be determined in run time.</param>
        /// <param name="classContext">Current PHP type context.</param>
        public CallSitesBuilder(ModuleBuilder/*!*/moduleBuilder, string/*!*/userFriendlyName, IPlace classContextPlace, PhpType classContext)
        {
            Debug.Assert(moduleBuilder != null && userFriendlyName != null);

            this.userFriendlyName = userFriendlyName;
            this.moduleBuilder = moduleBuilder;
            this.PushClassContext(classContextPlace, classContext);
            this.delegateBuilder = new DelegateBuilder(moduleBuilder);
        }

        #endregion

        #region Changing class context

        /// <summary>
        /// Change current class context. Remember the previous ones.
        /// </summary>
        /// <param name="classContextPlace">New class context place.</param>
        /// <param name="classContext">New class context type.</param>
        internal void PushClassContext(IPlace classContextPlace, PhpType classContext)
        {
            this.classContextPlaces.Push(classContextPlace);
            this.classContexts.Push(classContext);
        }

        /// <summary>
        /// Change current class context to the previous one.
        /// </summary>
        internal void PopClassContext()
        {
            this.classContextPlaces.Pop();
            this.classContexts.Pop();
        }

        #endregion

        #region EnsureContainer, Bake, DefineCallSite

        /// <summary>
        /// Ensure the call sites container is created and return the <see cref="TypeBuilder"/>.
        /// </summary>
        /// <returns></returns>
        private TypeBuilder/*!*/EnsureContainer()
        {
            if (containerClass == null)
            {
                if (this.classContext != null && this.classContext.IsGeneric)
                {
                    // we will emit single call sites in the class context. It is easier than to build generic sites container.
                    Debug.Assert(this.classContext.RealTypeBuilder != null);
                    return this.classContext.RealTypeBuilder;
                }

                Debug.Assert(staticCtorEmitter == null);

                var containerClassName = string.Format("<{0}>o_Sitescontainer'{1}", this.userFriendlyName.Replace('.', '_'), System.Threading.Interlocked.Increment(ref nextContainerId));
                containerClass = moduleBuilder.DefineType(PluginHandler.ConvertCallSiteName(containerClassName), TypeAttributes.Sealed | TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Abstract);

                staticCtorEmitter = new ILEmitter(containerClass.DefineTypeInitializer());
            }

            return containerClass;
        }

        /// <summary>
        /// Bake the container class if any. Close the call sites builder.
        /// </summary>
        public void Bake()
        {
            if (containerClass != null)
            {
                Debug.Assert(staticCtorEmitter != null);

                // finish static ctor:
                staticCtorEmitter.Emit(OpCodes.Ret);

                // bake the call sites container:
                containerClass.CreateType();

                // avoid of using the builders anymore:
                containerClass = null;
                staticCtorEmitter = null;
            }
        }

        /// <summary>
        /// Define new instance of CallSite&lt;<paramref name="delegateType"/>&gt; and initialize it with specified binder.
        /// </summary>
        /// <param name="cg"><see cref="ILEmitter"/> of the body that is using this call site. This method may emit initialization of the call site into this <paramref name="cg"/>.</param>
        /// <param name="userFriendlyName">User friendly name used as name for the CallSite field.</param>
        /// <param name="delegateType">CallSite type argument.</param>
        /// <param name="binderInstanceEmitter">Function used to emit initialization of the binder from within the call sites container .cctor.</param>
        /// <returns>The <see cref="FieldInfo"/> containing the instance of the created CallSite.</returns>
        public FieldInfo/*!*/DefineCallSite(PHP.Core.CodeGenerator/*!*/cg, string/*!*/userFriendlyName, Type/*!*/delegateType, Action<CallSitesBuilder, ILEmitter, object>/*!*/binderInstanceEmitter, object args)
        {
            Debug.Assert(userFriendlyName != null && delegateType != null && binderInstanceEmitter != null);

            userFriendlyName += ("'" + (callSitesCount++));

            // call sites container 
            var type = EnsureContainer();

            // call site type
            var callSiteType = Types.CallSiteGeneric[0].MakeGenericType(delegateType);

            // define the field:
            // public static readonly CallSite<delegateType> <userFriendlyName>
            var attrs = FieldAttributes.Static | FieldAttributes.InitOnly | ((staticCtorEmitter == null) ? FieldAttributes.Private : FieldAttributes.Public);
            var field = type.DefineField(PluginHandler.ConvertCallSiteName(userFriendlyName), callSiteType, attrs);

            if (staticCtorEmitter == null) // => this.classContext != null
            {
                // emit initialization of the call site just in the body of current method (as it is in C#, we need current generic arguments):
                Debug.Assert(this.classContext != null);
                var bodyEmitter = cg.IL;
                
                // check if the call site if not null, otherwise initialize it first:

                // if (<field> == null) <InitializeCallSite>;
                Label ifend = bodyEmitter.DefineLabel();
                bodyEmitter.Emit(OpCodes.Ldsfld, field);
                bodyEmitter.Emit(OpCodes.Brtrue, ifend);

                // init the field:
                InitializeCallSite(bodyEmitter, callSiteType, field, binderInstanceEmitter, args);

                bodyEmitter.MarkLabel(ifend);
            }
            else
            {
                // init the field in .cctor:
                InitializeCallSite(staticCtorEmitter, callSiteType, field, binderInstanceEmitter, args);
            }

            //
            return field;
        }

        /// <summary>
        /// Emit the initialization code for defined call site.
        /// </summary>
        private void InitializeCallSite(ILEmitter/*!*/il, Type/*!*/callSiteType, FieldBuilder/*!*/field, Action<CallSitesBuilder,ILEmitter,object>/*!*/binderInstanceEmitter, object args)
        {
            // <field> = CallSite<...>.Create( <BINDER> )
            binderInstanceEmitter(this,il,args);
            il.Emit(OpCodes.Call, callSiteType.GetMethod("Create", Types.CallSiteBinder));
            il.Emit(OpCodes.Stsfld, field);
        }

        #endregion

        #region EmitMethodCall

        /// <summary>
        /// Helper method, returns additional type arguments for delegate used by <see cref="EmitMethodCall"/>.
        /// </summary>
        private static IEnumerable<Type>/*!!*/MethodCallDelegateAdditionalArguments(bool staticCall, bool methodNameIsKnown, bool classContextIsKnown)
        {
            if (staticCall) yield return Types.DTypeDesc[0];
            if (!classContextIsKnown) yield return Types.DTypeDesc[0];
            if (!methodNameIsKnown) yield return Types.Object[0];
        }

        /// <summary>
        /// Helper method, loads parameters onto evaluation stack.
        /// </summary>
        private static void EmitMethodCallParameters(PHP.Core.CodeGenerator/*!*/cg, CallSignature callSignature)
        {
            foreach (var t in callSignature.GenericParams) t.EmitLoadTypeDesc(cg, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors); // load DTypeDescs on the stack
            foreach (var p in callSignature.Parameters) { cg.EmitBoxing(p.Emit(cg)); }  // load boxed args on the stack            
        }

        internal static Type/*!*/AccessToReturnType(AccessType access)
        {
            Debug.Assert(
                  access == AccessType.None || access == AccessType.Read || access == AccessType.ReadRef || access == AccessType.ReadUnknown,
                  "Unhandled access type.");

            switch (access)
            {
                case AccessType.None: return Types.Void;
                case AccessType.Read: return Types.Object[0];
                case AccessType.ReadRef:
                case AccessType.ReadUnknown: return Types.PhpReference[0];
                default: throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Helper class containing arguments for <see cref="EmitMethodCall"/>. By encapsulation of arguments into a class, we are saving stack.
        /// </summary>
        public class EmitMethodCallArgs
        {
            /// <summary>
            /// Return type of the method call determined by current access of the method call.
            /// </summary>
            public Type returnType;
            /// <summary>
            /// The method call instance expression (the target) if it is an instance method call.
            /// </summary>
            public Expression/*!*/targetExpr;
            /// <summary>
            /// The target type if it is a static method call.
            /// </summary>
            public DType/*!*/targetType;
            /// <summary>
            /// If known at compile time, the method name. Otherwise <c>null</c>.
            /// </summary>
            public string methodFullName;
            /// <summary>
            /// If the <see cref="methodFullName"/> is null, this will be the expression giving the method name in run time.
            /// </summary>
            public Expression methodNameExpr;
            /// <summary>
            /// The call signature of the method call.
            /// </summary>
            public CallSignature callSignature;
        }

        /// <summary>
        /// Emit call of the instance/static method. This defines the call site and call it using given parameters.
        /// </summary>
        /// <param name="cg">Current code <see cref="CodeGenerator"/>.</param>
        /// <param name="args">Parameters to this method call. Packed into a class instance to save stack.</param>
        /// <returns>The resulting value type code. This value will be pushed onto the evaluation stack.</returns>
        public static PhpTypeCode EmitMethodCall(PHP.Core.CodeGenerator/*!*/cg, EmitMethodCallArgs/*!*/args)
        {
            Debug.Assert(args.methodFullName != null ^ args.methodNameExpr != null);          

            //
            //bool staticCall = (targetExpr == null); // we are going to emit static method call
            //bool methodNameIsKnown = (methodFullName != null);
            //bool classContextIsKnown = (this.classContextPlace != null);

            //
            // define the call site:
            //
            var delegateType = /*System.Linq.Expressions.Expression.*/cg.CallSitesBuilder.delegateBuilder.GetDelegateType(
                cg.CallSitesBuilder.MethodCallDelegateTypeArgs(
                    args.callSignature,
                /*staticCall*/(args.targetExpr == null) ? Types.DObject[0] : Types.Object[0],
                    MethodCallDelegateAdditionalArguments(/*staticCall*/(args.targetExpr == null), args.methodFullName != null, cg.CallSitesBuilder.classContextPlace != null),
                    args.returnType),
                cg.CallSitesBuilder.callSitesCount);    // (J) do not create dynamic delegates in dynamic modules, so they can be referenced from non-transient assemblies

            //
            var field = cg.CallSitesBuilder.DefineCallSite(cg, string.Format("call_{0}", args.methodFullName ?? "$"), delegateType, (self, il, _) =>
            {
                var _args = (EmitMethodCallArgs)_;
                // <LOAD> Binder.{MethodCall|StaticMethodCall}( methodFullName, genericParamsCount, paramsCount, classContext, <returnType> )
                if (_args.methodFullName != null) il.Emit(OpCodes.Ldstr, _args.methodFullName); else il.Emit(OpCodes.Ldnull);
                il.LdcI4(_args.callSignature.GenericParams.Count);
                il.LdcI4(_args.callSignature.Parameters.Count);
                if (self.classContextPlace != null) self.classContextPlace.EmitLoad(il); else il.Emit(OpCodes.Ldsfld, Fields.UnknownTypeDesc.Singleton);

                il.Emit(OpCodes.Ldtoken, _args.returnType);
                il.Emit(OpCodes.Call, Methods.GetTypeFromHandle);

                il.Emit(OpCodes.Call, /*staticCall*/(_args.targetExpr == null) ? Methods.Binder.StaticMethodCall : Methods.Binder.MethodCall);
            }, args);

            //
            // call the CallSite:
            //

            // <field>.Target( <field>, <targetExpr|self>, <scriptContext>, <callSignature.EmitLoadOnEvalStack>, <targetType>?, (classContext)?, <methodNameExpr>? ):

            cg.IL.Emit(OpCodes.Ldsfld, field);
            cg.IL.Emit(OpCodes.Ldfld, field.FieldType.GetField("Target"));
            cg.IL.Emit(OpCodes.Ldsfld, field);
            if (/*staticCall*/(args.targetExpr == null))
            {
                cg.EmitLoadSelf();
            }
            else
            {
                //EmitMethodTargetExpr(args.targetExpr, cg);
                // start a new operators chain (as the rest of chain is read)
                cg.ChainBuilder.Create();
                cg.ChainBuilder.Begin();
                cg.ChainBuilder.Lengthen(); // for hop over ->

                // prepare for operator invocation
                EmitBoxingHelper(args.targetExpr.Emit(cg), cg);
                cg.ChainBuilder.End();
            }
            cg.EmitLoadScriptContext();
            EmitMethodCallParameters(cg, args.callSignature);
            if (/*staticCall*/(args.targetExpr == null)) args.targetType.EmitLoadTypeDesc(cg, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
            if (/*!classContextIsKnown*/cg.CallSitesBuilder.classContextPlace == null) cg.EmitLoadClassContext();
            if (/*!methodNameIsKnown*/args.methodFullName == null) cg.EmitName(args.methodFullName/*null*/, args.methodNameExpr, true);
            
            cg.MarkTransientSequencePoint();
            cg.IL.Emit(OpCodes.Callvirt, delegateType.GetMethod("Invoke"));
            
            cg.MarkTransientSequencePoint();
            
            //
            return PhpTypeCodeEnum.FromType(args.returnType);
        }

        #endregion

        #region Helper methods

        ///// <summary>
        ///// Emit the target of instance method invocation.
        ///// </summary>
        ///// <param name="cg"></param>
        ///// <param name="targetExpr"></param>
        //private static void EmitMethodTargetExpr(Expression/*!*/targetExpr, PHP.Core.CodeGenerator/*!*/cg)
        //{
        //    // start a new operators chain (as the rest of chain is read)
        //    cg.ChainBuilder.Create();
        //    cg.ChainBuilder.Begin();
        //    cg.ChainBuilder.Lengthen(); // for hop over ->

        //    // prepare for operator invocation
        //    EmitBoxingHelper(targetExpr.Emit(cg), cg);
        //    cg.ChainBuilder.End();
        //}

        private static void EmitBoxingHelper(PhpTypeCode typeCode, PHP.Core.CodeGenerator/*!*/cg)
        {
            cg.EmitBoxing(typeCode);
        }

        /// <summary>
        /// Make an array containing types for CallSite generic type used for method invocation.
        /// </summary>
        /// <param name="callSignature">The method call signature.</param>
        /// <param name="targetType">The type of value passed as method target (object for instance method, DTypeDesc for static method).</param>
        /// <param name="additionalArgs">Additional arguments added after the target expression.</param>
        /// <param name="returnType">The return value type.</param>
        /// <returns></returns>
        private Type[]/*!*/MethodCallDelegateTypeArgs(CallSignature callSignature, Type/*!*/targetType, IEnumerable<Type> additionalArgs, Type/*!*/returnType)
        {
            List<Type> typeArgs = new List<Type>(callSignature.Parameters.Count + callSignature.GenericParams.Count + 6);

            // Type[]{CallSite, <targetType>, ScriptContext, {argsType}, (DTypeDesc)?, (DTypeDesc)?, (object)?, <returnType>}:

            // CallSite:
            typeArgs.Add(Types.CallSite[0]);

            // object instance / target type:
            typeArgs.Add(targetType);

            // ScriptContext:
            typeArgs.Add(Types.ScriptContext[0]);

            // parameters:
            foreach (var t in callSignature.GenericParams) typeArgs.Add(Types.DTypeDesc[0]);
            foreach (var p in callSignature.Parameters) typeArgs.Add(Types.Object[0]);

            // DTypeDesc: (in case of static method call)
            // class context (if not known at compile time):
            // method name (if now known at compile time):
            if (additionalArgs != null) typeArgs.AddRange(additionalArgs);

            // return type:
            typeArgs.Add(returnType);

            //
            return typeArgs.ToArray();
        }

        private Type[]/*!*/GetPropertyDelegateTypeArgs(Type/*!*/targetType, Type[] additionalArgs, Type/*!*/returnType)
        {
            List<Type> typeArgs = new List<Type>(6);

            // Type[]{CallSite, <targetType|targetTypeDesc>, (DTypeDesc)?, (object)?, (bool)?, <returnType>}:

            // CallSite:
            typeArgs.Add(Types.CallSite[0]);

            // target type (object instance / class type):
            typeArgs.Add(targetType);

            // DTypeDesc: (in case of static method call)
            // class context (if not known at compile time):
            // field name (if now known at compile time):
            if (additionalArgs != null) typeArgs.AddRange(additionalArgs);

            // return type:
            typeArgs.Add(returnType);

            //
            return typeArgs.ToArray();
        }
        
        #endregion       

        #region EmitGetProperty

        /// <summary>
        /// Create and call <see cref="CallSite"/> for getting property.
        /// </summary>
        /// <param name="cg"><see cref="CodeGenerator"/>.</param>
        /// <param name="wantRef">Wheter <see cref="PhpReference"/> is expected as the result.</param>
        /// <param name="targetExpr">The expression representing the target (object).</param>
        /// <param name="targetObjectPlace">The place representing the target (<see cref="DObject"/>) iff <paramref name="targetExpr"/> is not provided.</param>
        /// <param name="targetPlace">The place representing the target (object) iff <paramref name="targetExpr"/> and <paramref name="targetObjectPlace"/> are not provided.</param>
        /// <param name="targetType">Type of target iff we are getting property statically.</param>
        /// <param name="fieldName">The name of the field. Can be null if the name is not known at compile time (indirect).</param>
        /// <param name="fieldNameExpr">The expression used to get field name in run time (iff <paramref name="fieldName"/> is <c>null</c>.</param>
        /// <param name="issetSemantics">Wheter we are only checking if the property exists. If true, no warnings are thrown during run time.</param>
        /// <returns>Type code of the value that is pushed onto the top of the evaluation stack.</returns>
        public PhpTypeCode EmitGetProperty(
            PHP.Core.CodeGenerator/*!*/cg, bool wantRef,
            Expression targetExpr, IPlace targetObjectPlace, IPlace targetPlace, DType targetType,
            string fieldName, Expression fieldNameExpr,
            bool issetSemantics)
        {
            Debug.Assert(fieldName != null ^ fieldNameExpr != null);
            Debug.Assert(targetExpr != null || targetObjectPlace != null || targetPlace != null || targetType != null);
            
            //
            bool staticCall = (targetExpr == null && targetObjectPlace == null && targetPlace == null); // we are going to access static property
            bool fieldNameIsKnown = (fieldName != null);
            bool classContextIsKnown = (this.classContextPlace != null);

            //
            // binder flags:
            //
            Type returnType = wantRef ? Types.PhpReference[0] : Types.Object[0];
            
            //
            // define the call site:
            //

            //
            List<Type> additionalArgs = new List<Type>();
            if (!classContextIsKnown) additionalArgs.Add(Types.DTypeDesc[0]);
            if (!fieldNameIsKnown) additionalArgs.Add(Types.String[0]);

            var delegateTypeArgs = GetPropertyDelegateTypeArgs(
                staticCall ? Types.DTypeDesc[0] : ((targetObjectPlace != null) ? Types.DObject[0] : Types.Object[0]),   // DTypeDesc of static field's declaring type || DObject if field called on DObject known at compile time || otherwise object
                additionalArgs.ToArray(),
                returnType);

            var delegateType = /*System.Linq.Expressions.Expression.*/delegateBuilder.GetDelegateType(delegateTypeArgs, callSitesCount);    // (J) do not create dynamic delegates in dynamic modules, so they can be referenced from non-transient assemblies

            //
            var field = DefineCallSite(cg, string.Format("get{0}_{1}", wantRef ? "ref" : string.Empty, fieldName ?? "$"), delegateType, (self, il, _) =>
            {
                // <LOAD> Binder.{GetProperty|GetStaticProperty}( fieldName, classContext, issetSemantics, <returnType> )
                if (fieldName != null) il.Emit(OpCodes.Ldstr, fieldName); else il.Emit(OpCodes.Ldnull);
                if (self.classContextPlace != null) self.classContextPlace.EmitLoad(il); else il.Emit(OpCodes.Ldsfld, Fields.UnknownTypeDesc.Singleton);
                il.LoadBool(issetSemantics);

                il.Emit(OpCodes.Ldtoken, returnType);
                il.Emit(OpCodes.Call, Methods.GetTypeFromHandle);

                il.Emit(OpCodes.Call, staticCall ? Methods.Binder.StaticGetProperty : Methods.Binder.GetProperty);
            }, null);

            //
            // call the CallSite:
            //

            // <field>.Target( <field>, <targetExpr|targetType>, (classContext)?, <methodNameExpr>? ):

            cg.IL.Emit(OpCodes.Ldsfld, field);
            cg.IL.Emit(OpCodes.Ldfld, field.FieldType.GetField("Target"));
            cg.IL.Emit(OpCodes.Ldsfld, field);
            if (staticCall) targetType.EmitLoadTypeDesc(cg, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
            else if (targetExpr != null)
            {
                cg.ChainBuilder.Lengthen(); // for hop over ->
                cg.EmitBoxing(targetExpr.Emit(cg)); // prepare for operator invocation
            }
            else if (targetObjectPlace != null) targetObjectPlace.EmitLoad(cg.IL);
            else if (targetPlace != null) targetPlace.EmitLoad(cg.IL);
            else Debug.Fail();
            if (!classContextIsKnown) cg.EmitLoadClassContext();
            if (!fieldNameIsKnown) cg.EmitName(fieldName/*null*/, fieldNameExpr, true, PhpTypeCode.String);

            cg.MarkTransientSequencePoint();
            cg.IL.Emit(OpCodes.Callvirt, delegateType.GetMethod("Invoke"));

            cg.MarkTransientSequencePoint();
            
            //
            return PhpTypeCodeEnum.FromType(returnType);
        }


        #endregion
    }
}
