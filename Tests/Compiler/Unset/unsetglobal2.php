[expect php]
[file]
<?php
function foo() 
{
    unset($GLOBALS['bar']);
}

$bar = "something";
foo();
echo $bar;
?> 
