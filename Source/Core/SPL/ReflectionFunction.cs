/*

 Copyright (c) 2012 DEVSENSE

 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 
 You must not remove this notice from this software.

*/


using System;
using System.Linq;
using PHP.Core;
using PHP.Core.Reflection;
using System.ComponentModel;
using System.Collections;
using System.Runtime.InteropServices;

namespace PHP.Library.SPL
{
    #region ReflectionFunctionAbstract

    /// <summary>
    /// A parent class to <see cref="ReflectionFunction"/>.
    /// </summary>
    [ImplementsType]
    public abstract class ReflectionFunctionAbstract : PhpObject, Reflector
    {
        #region Properties

        [PhpVisible]
        public virtual string name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionFunctionAbstract(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionFunctionAbstract(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        { }

        #endregion

        #region Methods

        [ImplementsMethod]
        private object __clone(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __clone(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).__clone(stack.Context);
        }

        [ImplementsMethod]
        public virtual object getClosureThis(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getClosureThis(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getClosureThis(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*string*/getDocComment(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getDocComment(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getDocComment(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*int*/getEndLine(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getEndLine(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getEndLine(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*ReflectionExtension*/getExtension(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getExtension(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getExtension(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*string*/getExtensionName(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getExtensionName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getExtensionName(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*string*/getFileName(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getFileName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getFileName(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*string*/getName(ScriptContext/*!*/context)
        {
            return this.name;
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getName(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*string*/getNamespaceName(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getNamespaceName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getNamespaceName(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*int*/getNumberOfParameters(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getNumberOfParameters(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getNumberOfParameters(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*int*/getNumberOfRequiredParameters(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getNumberOfRequiredParameters(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getNumberOfRequiredParameters(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*array*/getParameters(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getParameters(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getParameters(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*string*/getShortName(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getShortName(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getShortName(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*int*/getStartLine(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getStartLine(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getStartLine(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*array*/getStaticVariables(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getStaticVariables(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).getStaticVariables(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/inNamespace(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object inNamespace(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).inNamespace(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isClosure(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isClosure(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).isClosure(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isDeprecated(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isDeprecated(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).isDeprecated(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isInternal(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isInternal(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).isInternal(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isUserDefined(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isUserDefined(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).isUserDefined(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/returnsReference(ScriptContext/*!*/context)
        {
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object returnsReference(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).returnsReference(stack.Context);
        }

        [ImplementsMethod]
        public abstract object __toString(ScriptContext/*!*/context);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __toString(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunctionAbstract)instance).__toString(stack.Context);
        }

        #endregion
    }

    #endregion

    #region ReflectionFunction

    /// <summary>
    /// The ReflectionFunction class reports information about a function.
	/// </summary>
    [ImplementsType]
    public class ReflectionFunction : ReflectionFunctionAbstract
    {
        private DRoutineDesc routine;

        #region Constants

        /// <summary>
        /// Indicates deprecated functions.
        /// </summary>
        public const int IS_DEPRECATED = 262144;

        #endregion

        #region Properties

        [PhpVisible]
        public override string name
        {
            get
            {
                return (routine != null) ? routine.MakeFullName() : null;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionFunction(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionFunction(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object argument = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionFunction)instance).__construct(stack.Context, argument);
        }

        /// <summary>
        /// Constructs a ReflectionFunction object.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="arg">The name of the function to reflect or a closure.</param>
        /// <returns></returns>
        [ImplementsMethod]
        public virtual object __construct(ScriptContext context, object arg)
        {
            string name = PhpVariable.AsString(arg);
            if (!string.IsNullOrEmpty(name))
            {
                routine = context.ResolveFunction(name, null, false);
            }
            else
            {
                PhpException.InvalidArgument("arg");
            }

            if (routine == null)
                PhpException.Throw(PhpError.Error, string.Format("Function {0}() does not exist", name));
            
            return null;
        }

        #endregion

        [ImplementsMethod]
        public override object __toString(ScriptContext context)
        {
            return string.Format("Function [ function {0} ] {}", this.name);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static object __toString(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunction)instance).__toString(stack.Context);
        }

        //public static string export ( string $name [, string $return ] )

        [ImplementsMethod]
        public virtual object/*Closure*/getClosure(ScriptContext context)
        {
            return new SPL.Closure(context, routine.ArglessStub, null, null);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getClosure(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunction)instance).getClosure(stack.Context);
        }
        
        [ImplementsMethod, NeedsArgless]
        public virtual object/*mixed*/invoke(ScriptContext context)
        {
            Debug.Fail("ArgLess should be called instead!");
            throw new InvalidOperationException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object invoke(object instance, PhpStack stack)
        {
            ReflectionFunction func = (ReflectionFunction)instance;
            if (func.routine == null)
            {
                stack.RemoveFrame();
                return false;
            }
            
            return func.routine.Invoke(null, stack);
        }

        [ImplementsMethod]
        public virtual object/*mixed*/invokeArgs(ScriptContext context, object args)
        {
            if (routine == null)
                return false;

            ICollection values = null;
            IDictionary dict;
            if ((dict = args as IDictionary) != null) values = dict.Values;
            else values = args as ICollection;

            // invoke the routine
            var stack = context.Stack;
            stack.AddFrame(values ?? ArrayUtils.EmptyObjects);
            return routine.Invoke(null, stack);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object invokeArgs(object instance, PhpStack stack)
        {
            var args = stack.PeekValue(1);
            stack.RemoveFrame();
            
            return ((ReflectionFunction)instance).invokeArgs(stack.Context, args);
        }
        
        [ImplementsMethod]
        public virtual object/*bool*/isDisabled(ScriptContext context)
        {
            return false;   // not supported by Phalanger yet
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isDisabled(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunction)instance).isDisabled(stack.Context);
        }
    }

    #endregion

    #region ReflectionMethod

    [ImplementsType]
    public class ReflectionMethod : ReflectionFunctionAbstract
    {
        internal DTypeDesc dtype;
        internal DRoutineDesc method;
        
        #region Constants

        public const int IS_STATIC = 1;
        public const int IS_ABSTRACT = 2;
        public const int IS_FINAL = 4;
        public const int IS_PUBLIC = 256;
        public const int IS_PROTECTED = 512;
        public const int IS_PRIVATE = 1024;

        #endregion

        #region Properties

        [PhpVisible]
        public override string name
        {
            get
            {
                return (method != null) ? method.MakeFullName() : null;
            }
        }

        [PhpVisible]
        public string @class
        {
            get
            {
                return (dtype != null) ? dtype.MakeFullName() : null;
            }
        }

        #endregion

        #region Constructor

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionMethod(ScriptContext/*!*/context, bool newInstance)
            : base(context, newInstance)
        { }

        /// <summary>
        /// For internal purposes only.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public ReflectionMethod(ScriptContext/*!*/context, DTypeDesc caller)
            : base(context, caller)
        { }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            object @class = stack.PeekValue(1);
            object methodname = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).__construct(stack.Context, @class, methodname);
        }

        /// <summary>
        /// Constructs a ReflectionFunction object.
        /// </summary>
        [ImplementsMethod]
        public virtual object __construct(ScriptContext context, object @class, object methodname)
        {
            string methodnameStr = PhpVariable.AsString(methodname);

            this.dtype = null;
            this.method = null;

            DObject dobj;
            
            if ((dobj = (@class as DObject)) != null)
            {
                this.dtype = dobj.TypeDesc;
            }
            else
            {
                var str = PhpVariable.AsString(@class);
                if (str != null)
                    this.dtype = context.ResolveType(str, null, null, null, ResolveTypeFlags.UseAutoload);

                if (this.dtype == null)
                {
                    PhpException.Throw(PhpError.Error, string.Format("Class {0} does not exist", str));
                    return false;
                }
            }
            
            if (this.dtype.GetMethod(new Name(methodnameStr), dtype, out this.method) == GetMemberResult.NotFound)
            {
                PhpException.Throw(PhpError.Error, string.Format("Method {0}::{1}() does not exist", dtype.MakeFullName(), methodnameStr));  
                return false;
            }

            return null;
        }

        #endregion

        //public static string export ( string $class , string $name [, bool $return = false ] )
        
        [ImplementsMethod]
        public virtual object/*Closure*/getClosure(ScriptContext context, [Optional]object @object)
        {
            if (method == null)
                return false;

            if (@object == Arg.Default)
                @object = null;

            DObject dobj = null;

            // get instance object
            if (method.IsStatic)
            {
                if (@object != null)
                {
                    PhpException.InvalidArgument("object");
                    @object = null;
                }
            }
            else
            {
                dobj = @object as DObject;
                if (dobj == null)
                {
                    PhpException.InvalidArgumentType("object", DObject.PhpTypeName);
                    return false;
                }
            }

            // check whether method can be called on this object
            if (dobj != null)
            {
                if (!this.dtype.IsAssignableFrom(dobj.TypeDesc))
                {
                    PhpException.InvalidArgument("object");
                    return false;
                }
            }

            // create closure that calls the method on specified instance:
            return new SPL.Closure(context, (instance, stack) => method.ArglessStub(dobj, stack), null, null);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getClosure(object instance, PhpStack stack)
        {
            var @object = stack.PeekValueOptional(1);
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).getClosure(stack.Context, @object);
        }
        
        [ImplementsMethod]
        public virtual object/*int*/getModifiers(ScriptContext context)
        {
            if (method == null)
                return false;

            int result = 0;
            
            if (method.IsStatic) result |= IS_STATIC;
            if (method.IsAbstract) result |= IS_ABSTRACT;
            if (method.IsFinal) result |= IS_FINAL;
            if (method.IsPublic) result |= IS_PUBLIC;
            if (method.IsProtected) result |= IS_PROTECTED;
            if (method.IsPrivate) result |= IS_PRIVATE;
            
            return result;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getModifiers(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).getModifiers(stack.Context);
        }
        
        /// <summary>
        /// Gets the method prototype (if there is one).
        /// Prototype is a method of base class.
        /// </summary>
        /// <param name="context"></param>
        /// <returns><see cref="ReflectionMethod"/> or <c>FALSE</c>.</returns>
        [ImplementsMethod]
        public virtual object/*ReflectionMethod*/getPrototype(ScriptContext context)
        {
            if (dtype == null || method == null || dtype.Base == null)
                return false;

            DRoutineDesc prototype;
            if (dtype.Base.GetMethod(method.KnownRoutine.Name, dtype, out prototype) == GetMemberResult.NotFound)
                return false;

            return new ReflectionMethod(context, true)
            {
                dtype = prototype.DeclaringType,
                method = prototype,
            };
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getPrototype(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).getPrototype(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*ReflectionClass*/getDeclaringClass(ScriptContext context)
        {
            if (dtype == null)
                return false;

            return new ReflectionClass(context, true)
            {
                typedesc = dtype
            };
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object getDeclaringClass(object instance, PhpStack stack)
        {
            stack.RemoveFrame();

            return ((ReflectionMethod)instance).getDeclaringClass(stack.Context);
        }

        [ImplementsMethod, NeedsArgless]
        public virtual object/*mixed*/invoke(ScriptContext context, object instance)
        {
            Debug.Fail("ArgLess should be called instead!");
            throw new InvalidOperationException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object invoke(object instance, PhpStack stack)
        {
            var m = (ReflectionMethod)instance;
            if (m.method == null)
            {
                stack.RemoveFrame();
                return false;
            }

            object obj = stack.PeekValue(1);
            PhpArray args = new PhpArray((stack.ArgCount > 1 ? (stack.ArgCount - 1) : (0)));
            for (int i = 2; i <= stack.ArgCount; i++)
                args.Add(stack.PeekValue(i));
            stack.RemoveFrame();
            
            //
            return m.invokeArgs(stack.Context, obj, args);
        }

        [ImplementsMethod]
        public virtual object/*mixed*/invokeArgs(ScriptContext context, object instance, object args)
        {
            if (method == null)
                return false;

            var dobj = instance as DObject;
            if (!method.IsStatic)
            {
                if (dobj == null || !this.dtype.IsAssignableFrom(dobj.TypeDesc))    // non static method needs compatible instance
                {
                    PhpException.InvalidArgument("instance");
                    return false;
                }
            }

            ICollection values = null;
            IDictionary dict;
            if ((dict = args as IDictionary) != null) values = dict.Values;
            else values = args as ICollection;

            // invoke the routine
            var stack = context.Stack;
            stack.AddFrame(values ?? ArrayUtils.EmptyObjects);
            stack.LateStaticBindType = dtype;

            return method.Invoke(dobj, stack);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object invokeArgs(object instance, PhpStack stack)
        {
            var args = stack.PeekValue(1);
            stack.RemoveFrame();
            
            return ((ReflectionFunction)instance).invokeArgs(stack.Context, args);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isAbstract(ScriptContext context)
        {
            return method != null && method.IsAbstract;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isAbstract(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).isAbstract(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isConstructor(ScriptContext context)
        {
            return method != null && method.IsConstructor;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isConstructor(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).isConstructor(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isDestructor(ScriptContext context)
        {
            return method != null && DObject.SpecialMethodNames.Destruct.Value.EqualsOrdinalIgnoreCase(method.MakeFullName());
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isDestructor(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).isDestructor(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/isFinal(ScriptContext context)
        {
            return method != null && method.IsFinal;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isFinal(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).isFinal(stack.Context);
        }
        
        [ImplementsMethod]
        public virtual object/*bool*/isPrivate(ScriptContext context)
        {
            return method != null && method.IsPrivate;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isPrivate(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).isPrivate(stack.Context);
        }
        
        [ImplementsMethod]
        public virtual object/*bool*/isProtected(ScriptContext context)
        {
            return method != null && method.IsProtected;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isProtected(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).isProtected(stack.Context);
        }
        
        [ImplementsMethod]
        public virtual object/*bool*/isPublic(ScriptContext context)
        {
            return method != null && method.IsPublic;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isPublic(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).isPublic(stack.Context);
        }
        
        [ImplementsMethod]
        public virtual object/*bool*/isStatic(ScriptContext context)
        {
            return method != null && method.IsStatic;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isStatic(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).isStatic(stack.Context);
        }

        [ImplementsMethod]
        public virtual object/*bool*/setAccessible(ScriptContext context, object accessible)
        {
            if (this.method == null)
                return false;

            bool baccessible = Core.Convert.ObjectToBoolean(accessible);
            // TODO: remember private method accessibility
            return null;
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object setAccessible(object instance, PhpStack stack)
        {
            var accessible = stack.PeekValue(1);
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).setAccessible(stack.Context, accessible);
        }
        
        [ImplementsMethod]
        public override object/*string*/__toString(ScriptContext context)
        {
            if (method == null)
                return false;

            return string.Format("Method [ {0} method {1} ] {}",
                (method.IsStatic ? "static " : string.Empty) + (method.IsPublic ? "public" : (method.IsProtected ? "protected" : "private")),
                this.name);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new static object __toString(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionMethod)instance).__toString(stack.Context);
        }
    }

    #endregion
}
