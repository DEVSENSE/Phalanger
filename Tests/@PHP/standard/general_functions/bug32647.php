[expect php]
[file]
<?php

function foo()
{
  echo "foo!\n";
}

class bar
{
	function barfoo ()
	{ echo "bar!\n"; }
}

error_reporting(0);

unset($obj);
register_shutdown_function(array($obj,""));            // Invalid
register_shutdown_function(array($obj,"some string")); // Invalid
register_shutdown_function(array(0,""));               // Invalid
//register_shutdown_function(array('bar','foo'));        // Invalid
register_shutdown_function(array(0,"some string"));    // Invalid
//register_shutdown_function('bar');                     // Invalid
register_shutdown_function('foo');                     // Valid
register_shutdown_function(array('bar','barfoo'));     // Valid

$obj = new bar;

//register_shutdown_function(array($obj,'foobar'));      // Invalid
register_shutdown_function(array($obj,'barfoo'));      // Valid
?>