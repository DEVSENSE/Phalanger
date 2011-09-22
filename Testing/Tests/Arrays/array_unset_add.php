[expect php]
[file]
<?php

// test1
echo "test1 (PassedByCopy):\n";

function test1($x)
{
	$x[] = 4;	
	var_dump($x);	
	return $x;
}

$x = array();
$x[0] = 1;
$x[1] = 2;
$x[2] = 3;
unset( $x[2] );
$x = test1($x);
$x[] = 5;
var_dump($x);

// test2
echo "\n\ntest2 (ReturnedByCopy):\n";

function test2()
{
	$x = array(1,2,3);
	unset($x[2]);
	var_dump($x);
	return $x;
}

$x = test2($x);
$x[] = 4;
var_dump($x);

// test3
echo "\n\ntest3 (Assigned):\n";

$x = array();
$x[0] = 1;
$x[1] = 2;
$x[2] = 3;
unset( $x[2] );
$y = $x;
$y[] = 5;
var_dump($y);

?>