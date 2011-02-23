[expect exact] ahoj

[file]
<?
include "b.inc";

class C extends B
{
}

$x = new B;
$x->x = "ahoj";

$x->foo();

?>