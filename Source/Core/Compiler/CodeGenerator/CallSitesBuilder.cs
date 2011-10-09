﻿using System;
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
        private readonly IPlace classContextPlace;

        /// <summary>
        /// Current type declaration to emit site containers properly.
        /// </summary>
        private readonly PhpType classContext = null;

        /// <summary>
        /// Amount of emitted call sites. Used to build unique call site field name.
        /// </summary>
        private long callSitesCount = 0;

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
        public CallSitesBuilder(ModuleBuilder/*!*/moduleBuilder, string/*!*/userFriendlyName, IPlace classContextPlace, PhpType/*!*/classContext)
        {
            Debug.Assert(moduleBuilder != null && userFriendlyName != null);

            this.userFriendlyName = userFriendlyName;
            this.moduleBuilder = moduleBuilder;
            this.classContextPlace = classContextPlace;
            this.classContext = classContext;
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
        /// <param name="bodyEmitter"><see cref="ILEmitter"/> of the body that is using this call site. This method may emit initialization of the call site into this <paramref name="bodyEmitter"/>.</param>
        /// <param name="userFriendlyName">User friendly name used as name for the CallSite field.</param>
        /// <param name="delegateType">CallSite type argument.</param>
        /// <param name="binderInstanceEmitter">Function used to emit initialization of the binder from within the call sites container .cctor.</param>
        /// <returns>The <see cref="FieldInfo"/> containing the instance of the created CallSite.</returns>
        public FieldInfo/*!*/DefineCallSite(ILEmitter/*!*/bodyEmitter,string/*!*/userFriendlyName, Type/*!*/delegateType, Action<ILEmitter>/*!*/binderInstanceEmitter)
        {
            Debug.Assert(userFriendlyName != null && delegateType != null && binderInstanceEmitter != null);

            userFriendlyName += ("'" + (callSitesCount++));

            // call sites container 
            var type = EnsureContainer();

            // call site type
            var callSiteType = typeof(CallSite<>).MakeGenericType(delegateType);

            // define the field:
            // public static readonly CallSite<delegateType> <userFriendlyName>
            var attrs = FieldAttributes.Static | FieldAttributes.InitOnly | ((staticCtorEmitter == null) ? FieldAttributes.Private : FieldAttributes.Public);
            var field = type.DefineField(PluginHandler.ConvertCallSiteName(userFriendlyName), callSiteType, attrs);

            if (staticCtorEmitter == null) // => this.classContext != null
            {
                // emit initialization of the call site just in the body of current method (as it is in C#, we need current generic arguments):
                Debug.Assert(this.classContext != null);
                
                // check if the call site if not null, otherwise initialize it first:

                // if (<field> == null) <InitializeCallSite>;
                Label ifend = bodyEmitter.DefineLabel();
                bodyEmitter.Emit(OpCodes.Ldsfld, field);
                bodyEmitter.Emit(OpCodes.Brtrue, ifend);

                // init the field:
                InitializeCallSite(bodyEmitter, callSiteType, field, binderInstanceEmitter);

                bodyEmitter.MarkLabel(ifend);
            }
            else
            {
                // init the field in .cctor:
                InitializeCallSite(staticCtorEmitter, callSiteType, field, binderInstanceEmitter);
            }

            //
            return field;
        }

        /// <summary>
        /// Emit the initialization code for defined call site.
        /// </summary>
        /// <param name="il"></param>
        /// <param name="callSiteType"></param>
        /// <param name="field"></param>
        /// <param name="binderInstanceEmitter"></param>
        private static void InitializeCallSite(ILEmitter/*!*/il, Type/*!*/callSiteType, FieldBuilder/*!*/field, Action<ILEmitter>/*!*/binderInstanceEmitter)
        {
            // <field> = CallSite<...>.Create( <BINDER> )
            binderInstanceEmitter(il);
            il.Emit(OpCodes.Call, callSiteType.GetMethod("Create", Types.CallSiteBinder));
            il.Emit(OpCodes.Stsfld, field);
        }

        #endregion

        #region GetDelegateType

        private static readonly Type[] DelegateCtorSignature = new Type[] { typeof(object), typeof(IntPtr) };

        /// <summary>
        /// Gets a System.Linq.Expressions.Expression.Type object that represents a generic
        /// System.Func or System.Action delegate type that has specific type arguments.
        /// 
        /// For <paramref name="types"/> longer than 17 items, current module's <see cref="TypeBuilder"/> is used instead of Transient one.
        /// This avoids of "Unable to make a reference to a transient module from a non-transient module." exception.
        /// 
        /// For less or equal than 17 items, <see cref="System.Linq.Expressions.Expression.GetDelegateType"/> is used.
        /// </summary>
        /// <param name="types">The type arguments of the delegate.</param>
        /// <returns>The delegate type.</returns>
        private Type/*!*/GetDelegateType(Type[]/*!*/types)
        {
            Debug.Assert(types != null);

            if (types.Length <= 17 && !types.Any((Type t) => t.IsByRef))    // less or equal 17 items and none of them is by reference
                return System.Linq.Expressions.Expression.GetDelegateType(types);

            // else, Action or Func cannot be used, make the delegate:
            TypeBuilder typeBuilder = moduleBuilder.DefineType("Delegate" + types.Length, TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.AutoClass, typeof(MulticastDelegate));

            Type returnType = types[types.Length - 1];
            Type[] parameterTypes = ArrayUtils.RemoveLast<Type>(types);

            typeBuilder.DefineConstructor(MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.HideBySig | MethodAttributes.RTSpecialName, CallingConventions.Standard, DelegateCtorSignature).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            typeBuilder.DefineMethod("Invoke", MethodAttributes.FamANDAssem | MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask, returnType, parameterTypes).SetImplementationFlags(MethodImplAttributes.CodeTypeMask);
            return typeBuilder.CreateType();
        }

        #endregion

        #region EmitMethodCall

        /// <summary>
        /// Emit call of the instance/static method. This defines the call site and call it using given parameters.
        /// </summary>
        /// <param name="cg">Current code <see cref="CodeGenerator"/>.</param>
        /// <param name="access">Current access of the method call.</param>
        /// <param name="targetExpr">The method call instance expression (the target) if it is an instance method call.</param>
        /// <param name="targetType">The target type if it is a static method call.</param>
        /// <param name="methodFullName">If known at compile time, the method name. Otherwise <c>null</c>.</param>
        /// <param name="methodNameExpr">If the <paramref name="methodFullName"/> is null, this will be the expression giving the method name in run time.</param>
        /// <param name="callSignature">The call signature of the method call.</param>
        /// <returns>The resulting value type code. This value will be pushed onto the evaluation stack.</returns>
        public PhpTypeCode EmitMethodCall(
            PHP.Core.CodeGenerator/*!*/cg, AccessType access,
            Expression/*!*/targetExpr, DType/*!*/targetType,
            string methodFullName, Expression methodNameExpr, CallSignature callSignature)
        {
            Debug.Assert(methodFullName != null ^ methodNameExpr != null);

            Debug.Assert(
                access == AccessType.None || access == AccessType.Read || access == AccessType.ReadRef || access == AccessType.ReadUnknown,
                "Unhandled access type.");

            //
            bool staticCall = (targetExpr == null); // we are going to emit static method call
            bool methodNameIsKnown = (methodFullName != null);
            bool classContextIsKnown = (this.classContextPlace != null);

            //
            // binder flags:
            //
            Type returnType = Types.Void;
            switch (access)
            {
                case AccessType.Read: returnType = Types.Object[0]; break;
                case AccessType.ReadRef:
                case AccessType.ReadUnknown: returnType = Types.PhpReference[0]; break;
            }

            //
            // define the call site:
            //

            //
            List<Type> additionalArgs = new List<Type>();
            if (staticCall) additionalArgs.Add(Types.DTypeDesc[0]);
            if (!classContextIsKnown) additionalArgs.Add(Types.DTypeDesc[0]);
            if (!methodNameIsKnown) additionalArgs.Add(Types.Object[0]);

            var delegateTypeArgs = MethodCallDelegateTypeArgs(
                callSignature,
                staticCall ? Types.DObject[0] : Types.Object[0],
                additionalArgs.ToArray(),
                returnType);

            var delegateType = /*System.Linq.Expressions.Expression.*/GetDelegateType(delegateTypeArgs);    // (J) do not create dynamic delegates in dynamic modules, so they can be referenced from non-transient assemblies

            //
            var field = DefineCallSite(cg.IL, string.Format("call_{0}", methodFullName ?? "$"), delegateType, (il) =>
            {
                // <LOAD> Binder.{MethodCall|StaticMethodCall}( methodFullName, genericParamsCount, paramsCount, classContext, <returnType> )
                if (methodFullName != null) il.Emit(OpCodes.Ldstr, methodFullName); else il.Emit(OpCodes.Ldnull);
                il.LdcI4(callSignature.GenericParams.Count);
                il.LdcI4(callSignature.Parameters.Count);
                if (this.classContextPlace != null) this.classContextPlace.EmitLoad(il); else il.Emit(OpCodes.Ldsfld, Fields.UnknownTypeDesc.Singleton);

                il.Emit(OpCodes.Ldtoken, returnType);
                il.Emit(OpCodes.Call, Methods.GetTypeFromHandle);
                
                il.Emit(OpCodes.Call, staticCall ? Methods.Binder.StaticMethodCall : Methods.Binder.MethodCall);
            });

            //
            // call the CallSite:
            //

            // <field>.Target( <field>, <targetExpr|self>, <scriptContext>, <callSignature.EmitLoadOnEvalStack>, <targetType>?, (classContext)?, <methodNameExpr>? ):

            cg.IL.Emit(OpCodes.Ldsfld, field);
            cg.IL.Emit(OpCodes.Ldfld, field.FieldType.GetField("Target"));
            cg.IL.Emit(OpCodes.Ldsfld, field);
            if (staticCall) cg.EmitLoadSelf(); else EmitMethodTargetExpr(cg, targetExpr);
            cg.EmitLoadScriptContext();
            foreach (var t in callSignature.GenericParams) t.EmitLoadTypeDesc(cg, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors); // load DTypeDescs on the stack
            foreach (var p in callSignature.Parameters) { cg.EmitBoxing(p.Emit(cg)); }  // load boxed args on the stack
            if (staticCall) targetType.EmitLoadTypeDesc(cg, ResolveTypeFlags.UseAutoload | ResolveTypeFlags.ThrowErrors);
            if (!classContextIsKnown) cg.EmitLoadClassContext();
            if (!methodNameIsKnown) cg.EmitName(methodFullName/*null*/, methodNameExpr, true);
            
            cg.MarkTransientSequencePoint();
            cg.IL.Emit(OpCodes.Callvirt, delegateType.GetMethod("Invoke"));
            
            cg.MarkTransientSequencePoint();
            
            //
            return PhpTypeCodeEnum.FromType(returnType);
        }

        #endregion

        #region Helper methods

        /// <summary>
        /// Emit the target of instance method invocation.
        /// </summary>
        /// <param name="cg"></param>
        /// <param name="targetExpr"></param>
        private static void EmitMethodTargetExpr(PHP.Core.CodeGenerator/*!*/cg, Expression/*!*/targetExpr)
        {
            // start a new operators chain (as the rest of chain is read)
            cg.ChainBuilder.Create();
            cg.ChainBuilder.Begin();
            cg.ChainBuilder.Lengthen(); // for hop over ->

            // prepare for operator invocation
            cg.EmitBoxing(targetExpr.Emit(cg));
            cg.ChainBuilder.End();
        }

        /// <summary>
        /// Make an array containing types for CallSite generic type used for method invocation.
        /// </summary>
        /// <param name="callSignature">The method call signature.</param>
        /// <param name="targetType">The type of value passed as method target (object for instance method, DTypeDesc for static method).</param>
        /// <param name="additionalArgs">Additional arguments added after the target expression.</param>
        /// <param name="returnType">The return value type.</param>
        /// <returns></returns>
        private Type[]/*!*/MethodCallDelegateTypeArgs(CallSignature callSignature, Type/*!*/targetType, Type[] additionalArgs, Type/*!*/returnType)
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

            var delegateType = /*System.Linq.Expressions.Expression.*/GetDelegateType(delegateTypeArgs);    // (J) do not create dynamic delegates in dynamic modules, so they can be referenced from non-transient assemblies

            //
            var field = DefineCallSite(cg.IL, string.Format("get{0}_{1}", wantRef ? "ref" : string.Empty, fieldName ?? "$"), delegateType, (il) =>
            {
                // <LOAD> Binder.{GetProperty|GetStaticProperty}( fieldName, classContext, issetSemantics, <returnType> )
                if (fieldName != null) il.Emit(OpCodes.Ldstr, fieldName); else il.Emit(OpCodes.Ldnull);
                if (this.classContextPlace != null) this.classContextPlace.EmitLoad(il); else il.Emit(OpCodes.Ldsfld, Fields.UnknownTypeDesc.Singleton);
                il.LoadBool(issetSemantics);

                il.Emit(OpCodes.Ldtoken, returnType);
                il.Emit(OpCodes.Call, Methods.GetTypeFromHandle);

                il.Emit(OpCodes.Call, staticCall ? Methods.Binder.StaticGetProperty : Methods.Binder.GetProperty);
            });

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
