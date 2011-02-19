[expect php]
[file]
<?php
include('Phalanger.inc');

array_shift($GLOBALS);

$a = array("foo", "bar", "fubar");
$b = array("3" => "foo", "4" => "bar", "5" => "fubar");
$c = array("a" => "foo", "b" => "bar", "c" => "fubar");

/* simple array */
echo array_shift($a), "\n";
__var_dump($a);

/* numerical assoc indices */
echo array_shift($b), "\n";
__var_dump($b);

/* assoc indices */
echo array_shift($c), "\n";
__var_dump($c);

?>