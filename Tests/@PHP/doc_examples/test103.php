[expect php]

[file]
<?php

$foo = include 'return.inc';

echo $foo; // prints 'PHP'

$bar = include 'noreturn.inc';

echo $bar; // prints 1

?>