[expect php]
[file]
<?php

$myobjects = array(1,array(2,3));

function foo() {
global $myobjects;
$o = & $myobjects[1];
}

foo();

$checkpoint = $myobjects;

$myobjects[1][0] = 7;

echo "x: ";
var_dump($myobjects);
echo '<br/><br/>checkpoint ';
var_dump($checkpoint);

?>