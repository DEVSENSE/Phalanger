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
            string name = Core.Convert.ObjectToString(arg);
            if (!string.IsNullOrEmpty(name))
            {
                routine = context.ResolveFunction(name, null, false);
            }
            else
            {
                PhpException.InvalidArgument("arg");
            }

            return null;
        }

        #endregion

        public override object __toString(ScriptContext context)
        {
            throw new NotImplementedException();
        }

        //public static string export ( string $name [, string $return ] )
        //public Closure getClosure ( void )
        
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

            if (values == null)
                return false;

            // invoke the routine
            var stack = context.Stack;
            stack.AddFrame(values);
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
            throw new NotImplementedException();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object isDisabled(object instance, PhpStack stack)
        {
            stack.RemoveFrame();
            return ((ReflectionFunction)instance).isDisabled(stack.Context);
        }
    }

    #endregion
}
