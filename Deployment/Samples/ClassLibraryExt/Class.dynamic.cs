using System;
using System.Runtime.Serialization;
using System.ComponentModel;
using PHP.Core;
using PHP.Core.Reflection;

//
// PHP.Library is the root namespace for PHalanger Extension functions, constants and classes.
//
namespace PHP.Library
{
    /// <summary>
    /// Currently, dynamic (argless) stubs must be created manually.
    /// Argless stubs have a signature of "object ????(object instance, PhpStack stack)" and
    /// they must be registered manually within __PopulateTypeDesc method. (and others)
    /// 
    /// In the future, dynamic stubs won't be needed as Phalanger will reflect PHP classes automatically.
    /// 
    /// </summary>
    public partial class MyPhpClass : PhpObject
    {
        #region properties getter/setter

        /// <summary>
        /// Dynamic (argless) stub for the <c>x</c> property. This is called in runtime to get the value of <c>x</c>.
        /// </summary>
        /// <param name="instance">The instance of <c>MyPhpClass</c> provided by PHalanger</param>
        /// <returns>The value of <c>x</c> property.</returns>
        private static object __get_x(object instance) { return ((MyPhpClass)instance).x; }

        /// <summary>
        /// Dynamic (argless) stub for the <c>x</c> property. This is called in runtime to set the value of <c>x</c>.
        /// </summary>
        /// <param name="instance">The instance of <c>MyPhpClass</c> provided by Phalanger.</param>
        /// <param name="value">New value of <c>x</c> property.</param>
        private static void __set_x(object instance, object value) { ((MyPhpClass)instance).x = (PhpReference)value; }

        /// <summary>
        /// Dynamic (argless) stub for the <c>y</c> static property. This is called in runtime to get the value of <c>y</c>.
        /// </summary>
        /// <param name="instance">The instance of <c>MyPhpClass</c> provided by Phalanger.</param>
        /// <returns>The value of <c>y</c> static property.</returns>
        private static object __get_y(object instance) { return y; }

        /// <summary>
        /// Dynamic (argless) stub for the <c>y</c> static property. This is called in runtime to set the value of <c>y</c>.
        /// </summary>
        /// <param name="instance">null</param>
        /// <param name="value">New value of <c>y</c> static property.</param>
        private static void __set_y(object instance, object value) { y = (PhpReference)value; }

        #endregion

        #region class method dynamic stub

        /// <summary>
        /// Stub for instance method.
        /// </summary>
        /// <param name="__context">Current ScriptContext.</param>
        /// <param name="value">1st typed argument of the method.</param>
        /// <returns>result of Foo(value)</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object Foo(ScriptContext __context, object value)
        {
            return Foo(PHP.Core.Convert.ObjectToString(value));
        }

        /// <summary>
        /// Dynamic (argless) stub for the <c>Foo</c> method call. This method is called in runtime.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="stack"></param>
        /// <returns></returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object Foo(object instance, PhpStack stack)
        {
            // setup the stack, get arguments from the stack
            stack.CalleeName = "Foo";
            object arg1 = stack.PeekValue(1);
            stack.RemoveFrame();

            // call the actual Foo with arguments obtained from the stack
            return ((MyPhpClass)instance).Foo(stack.Context, arg1);
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Stub for instance method.
        /// </summary>
        /// <param name="__context">Current ScriptContext.</param>
        /// <returns>result of Foo(value)</returns>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public object __construct(ScriptContext __context)
        {
            __construct();
            return null;
        }

        /// <summary></summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static object __construct(object instance, PhpStack stack)
        {
            // setup the stack, get arguments from the stack
            stack.CalleeName = "__construct";
            stack.RemoveFrame();

            // call the actual constructor
            return ((MyPhpClass)instance).__construct(stack.Context);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public MyPhpClass(ScriptContext context, bool newInstance)
            : base(context, newInstance)
        {
            //InitializeInstanceFields(context);
            __InitializeStaticFields(context);
        }
        protected MyPhpClass(SerializationInfo info1, StreamingContext context1)
            : base(info1, context1)
        {
            __InitializeStaticFields(ScriptContext.CurrentContext);
        }
        [EditorBrowsable(EditorBrowsableState.Never)]
        public MyPhpClass(ScriptContext context, DTypeDesc caller)
            : this(context, true)
        {
            this.InvokeConstructor(context, caller);
        }

        #endregion

        #region Static field initialization

        [ThreadStatic]
        private static ScriptContext __lastContext;

        /// <summary>
        /// The static fields should be reinitialized. It is not needed
        /// if you do not mark the static field as [ThreadStatic]/
        /// </summary>
        /// <param name="context">Currently new ScriptContext.</param>
        public static void __InitializeStaticFields(ScriptContext context)
        {
            if (context != __lastContext)
            {
                y = new PhpSmartReference("static one");
                __lastContext = context;
            }
        }

        #endregion

        #region PHP class members

        /// <summary>
        /// Populates the provided <see cref="DTypeDesc"/> with this class's methods and properties.
        /// This is called to fill the type description table with dynamic stubs. They are called then in runtime.
        /// </summary>
        /// <param name="typeDesc">The type desc to populate.</param>
        private static void __PopulateTypeDesc(PhpTypeDesc typeDesc)
        {
            typeDesc.SetStaticInit(MyPhpClass.__InitializeStaticFields);

            typeDesc.AddMethod("Foo", PhpMemberAttributes.Public, Foo);
            typeDesc.AddMethod("__construct", PhpMemberAttributes.Public, __construct);

            typeDesc.AddProperty("x", PhpMemberAttributes.Public, __get_x, __set_x);
            typeDesc.AddProperty("y", PhpMemberAttributes.Public | PhpMemberAttributes.Static, __get_y, __set_y);
        }

        #endregion

    }
}
