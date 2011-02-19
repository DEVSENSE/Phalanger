[expect php]

[file]
<?php

$x = 10;

function foo()
{
    global $x;

    include 'xyz.inc';

    echo "$x $y $z";
}


foo();                    // A green apple
echo "$x";   // A green

?>
