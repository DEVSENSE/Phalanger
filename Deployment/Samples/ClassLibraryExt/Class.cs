using System;
using System.Runtime.Serialization;
using PHP.Core;
using PHP.Core.Reflection;

//
// PHP.Library is the root namespace for PHalanger Extension functions, constants and classes.
//
namespace PHP.Library
{
    /// <summary>
    /// Define global PHP type "MyPhpClass".
    /// 
    /// Currently, dynamic (argless) stubs must be created manually.
    /// Argless stubs have a signature of "object ????(object instance, PhpStack stack)" and
    /// they must be registered manually within __PopulateTypeDesc method.
    /// </summary>
	[Serializable]
    [ImplementsType]
	public partial class MyPhpClass : PhpObject
    {
        #region class & PHP class constants

        /// <summary>Class constant "MyPhpClass::e"</summary>
		public static readonly object e = 2.71;

        #endregion

        #region properties

        /// <summary>Public instance field.</summary>
		public PhpReference x;

		/// <summary>Public static field.
        /// The field should be marked as [ThreadStatic] to get initialized every time the web request starts.</summary>
        [PhpHasInitValue, ThreadStatic, System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
		public static PhpReference y;

        #endregion

        #region class method

        /// <summary>Public instance method.</summary>
        [ImplementsMethod]
		public object Foo(string value)
		{
			// echo the value:
			ScriptContext.CurrentContext.Echo(value);

			return null;
        }

        #endregion

        #region Constructor

        public MyPhpClass()
			: base(ScriptContext.CurrentContext, true)
		{
            __construct();
		}

        [PhpVisible, PhpFinal, ImplementsMethod]
        public void __construct()
        {
            // class instance initialization
        }

        #endregion

	}  
}  
