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

$eval_code = '$x = new foo;echo $x->bar($a,$b);';

$a = 1;
$b = 2;

//global test
eval($eval_code);

echo "---------\n";

// function test
function notglobal()
{
	$a = 3;
	$b = 4;
	
	eval($eval_code);
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
	
		eval($eval_code);
	}
}

$m = new method_test();
$m->m();



?>