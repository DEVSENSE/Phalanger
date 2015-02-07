[expect php]
[file]
<?php

function test($value)
{
	echo "\nAssignAdd - $value:\n";
	
	$arr = array();
	
	$arr[] .= $value;
	$arr[] += $value;
	$arr[] -= $value;
	$arr[] *= $value;
	$arr[] /= $value;
	// ...
	
	var_dump($arr);
}

test(123);
test("hello");
test("456");
test(-789);

?>