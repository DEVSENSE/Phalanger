[expect exact]
bool(true)

[file]
<?php
$foo = 10;   // $foo is an integer
$bar = (bool) $foo;   // $bar is a bool

var_dump($bar);

?>
