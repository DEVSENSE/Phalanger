[expect php]

[file]
<?php
function foo (&$var)
{
    $var++;
}

$a=5;
foo ($a);
echo $a;
?>
