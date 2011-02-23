[expect php]

[file]
<?php
class c
{
var $x;
function c($y)
{
	$x = $y;
}

function foo()
{
	echo $this->x;
}
}

$a = new c(2);
$a->foo();

function &returns_reference(&$x)
{
    $x->foo();
    $x->x = 5;
    return $x;
}

$newref =& returns_reference($a);
$newref->foo();

?>
