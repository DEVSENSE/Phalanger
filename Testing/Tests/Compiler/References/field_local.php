[expect php]
[file]
<?php

class X {
var $objects = 7;

function __construct() {
$o = & $this->objects;
}
}

$x = new X();
$checkpoint = (array)$x;
$x->objects = 25;

echo "x: ";
var_dump($x);
echo "<br/>checkpoint ";
var_dump($checkpoint);



?>