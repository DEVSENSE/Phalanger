[expect php]

[file]
<?php
function foo (&$var)
{
    echo $var++;
}

function &bar()
{
    $a = 5;
    return $a;
}
foo(bar());

?>
