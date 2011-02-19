[expect ct-error] Only variables can be passed by reference

[file]
<?php
function foo (&$var)
{
    echo $var++;
}


foo(5); // Constant, not variable
?>
