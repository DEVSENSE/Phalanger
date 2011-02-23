[expect php]
[file]
<?php

$myobjects = array(1,array(2,3));

function foo() {
	global $myobjects;
	$id = 2;
	$localarray[$id] = & $myobjects[1];
}

foo();

$checkpoint = $myobjects;

$myobjects[1][0] = 7;

echo "x: ";
var_dump($myobjects);
echo '<br/><br/>checkpoint ';
var_dump($checkpoint);

?>