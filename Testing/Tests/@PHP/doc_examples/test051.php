[expect php]

[file]
<?php
$a = 1; /* global scope */ 

function Test()
{ 
    $a = 2;
    echo $a; /* reference to local scope variable */ 
} 

Test();

echo $a;
include "a.inc";

?>
