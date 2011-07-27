[expect php]
[file]
<?php

class foo
{
	function bar($a,$b)
	{
		return  $a + $b;
	}
}


$a = 1;
$b = 2;

//global test
include "classcontext_include.inc";

echo "---------\n";

// function test
function notglobal()
{
	$a = 3;
	$b = 4;
	
	include "classcontext_include.inc";
}

notglobal();

echo "---------\n";

//method test
class method_test
{
	function m()
	{	
	
		$a = 6;
		$b = 7;
	
		include "classcontext_include.inc";
	}
}

$m = new method_test();
$m->m();



?>