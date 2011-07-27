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


class Utils
{
	function test($x,$a,$b)
	{
		return $x->bar($a,$b);
	}
}

$utils = new utils;


$x = new foo();
$arr = array(12,$x);
$ref = &$x;

$a = 7;
$b = 8;

//PhpRuntimeChain target
echo $utils->test($arr[1],$a,$b);

//PhpReference target
echo $utils->test($ref,$a,$b);


?>