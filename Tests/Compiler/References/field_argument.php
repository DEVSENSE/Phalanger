[expect php]
[file]
<?php

class X {
 var $objects = 8;
}

function foo(&$neco) { 
}

$x = new X();
foo($x->objects);
//$o = & $x->objects;

$checkpoint = (array)$x;

$x->objects = 7;

echo "x: ";
var_dump($x->objects);
echo '<br/><br/>checkpoint ';
var_dump($checkpoint);

?>