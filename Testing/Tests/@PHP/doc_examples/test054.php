[expect php]
[file]
<?php
	error_reporting(E_ALL & ~E_NOTICE);
	function test_global()
	{
		// Most predefined variables aren't "super" and require 
		// 'global' to be available to the functions local scope.
		global $HTTP_POST_VARS;
	    
		echo $HTTP_POST_VARS['name'];
	    
		// Superglobals are available in any scope and do 
		// not require 'global'. Superglobals are available 
		// as of PHP 4.1.0
		echo $_POST['name'];
	}

	$HTTP_POST_VARS['name'] = "My name";
	test_global();
?>
