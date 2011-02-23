[expect exact] 6

[file]
<?php
function foo (&$var)
{
    echo ++$var;
}

function bar() // Note the missing &
{
    $a = 5;
    return $a;
}
foo(bar());

?>
