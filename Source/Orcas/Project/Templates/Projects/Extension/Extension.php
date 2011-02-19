<?

/* The namespace must start with "PHP:::Library" to let the Phalanger know,
 * it contains extension functions/classes/constants.
 * Classes declared within different namespaces will not be accessible in
 * global scope. They can be used for internal purposes of the extension.
 *
 * To use this extension, add this Project into References of your Phalanger
 * application (In the solution explorer, References -> Add Reference -> Projects).
 * or add the reference in your .config file in the <classLibrary> section as follows:
 * <add url="ClassLibrary1.dll" />
 */
namespace PHP:::Library:::MyNamespace
{
    /* The assembly attribute stating it is PHP Library.
     */
	[assembly: PHP:::Core:::PhpExtension("$projectname$")]
	
	/* Any class in the "PHP:::Library" namespace will be visible in
	 * all the Phalanger applications using this managed extension.
	 *
	 * Example:
	 * $x = new MyFunctions();
	 */
	class MyFunctions
	{
	    /* Member functions are accessible through the class instance.
	     *
	     * Example:
	     * $x->MyFunction("Hello World!");
	     */
	    function MyFunction($s)
		{
			echo $s;
		}
		
		/* Define global extension function using following attribute.
		 * The function must be public static and not global. The parameter
		 * of the attribute specifies the name of global function.
		 * The function will be also accessible through its real name
		 * within the type MyFunctions.
		 *
		 * Example:
	     * my_function("Hello World!") or MyFunctions::MyFunc("...");
		 */
		[PHP:::Core:::ImplementsFunction("my_function")]
        static function MyFunc($s)
        {
	        echo $s;
        }
        
        /* Define global case-sensitive constant. The following attribute
         * specifies its name in the global code. The constant must be
         * public and const, with literal value initialized.
         *
         * Example:
	     * MY_CONSTANT or MyFunctions::MyConstant;
         */
        [PHP:::Core:::ImplementsConstant("MY_CONSTANT")]
        const MyConstant = 123;
	}
}

?>