[expect php]
[file]
<?php

function destruir_foo() 
{
    global $foo;
    unset($foo);
}

$foo = 'bar';
destruir_foo();
echo $foo;

?> 
