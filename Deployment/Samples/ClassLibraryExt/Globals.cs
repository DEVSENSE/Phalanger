using System;
using System.Runtime.Serialization;
using PHP.Core;
using PHP.Core.Reflection;

//
// PHP.Library is the root namespace for PHalanger Extension functions, constants and classes.
//
namespace PHP.Library
{
    // The class without [ImplementsType] attribute is just a wrapper
    // holding PHP global functions and constants.
	public sealed class MyClass
	{
        /// <summary>
        /// Define global constant "MY_NEW_CONSTANT".
        /// </summary>
		[ImplementsConstant("MY_NEW_CONSTANT")]
		public const string MyConstant = "Hello World!";
    
        /// <summary>
        /// Define global function "foo".
        /// </summary>
        /// <returns></returns>
		[ImplementsFunction("foo")]
		public static PhpArray Foo(int number)
		{
            return PhpArray.New(1, 2, 3, number);
		}
	}
}  
