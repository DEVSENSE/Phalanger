[expect php]
[file]
<?php

function foo($value)
{
	echo "($value) ";
	return $value;
}

function test($value)
{
	echo "\nAssignAdd - $value:\n";
	
	$arr = array();
	
	$arr[] .= $value;
	$arr[] += $value;
	$arr[] -= $value;
	$arr[] *= foo($value);
	$arr[] /= foo($value);
	$arr[] &= foo($value);
	// ...
	
	var_dump($arr);
}

test(123);
test("hello");
test("456");
test(-789);

?>