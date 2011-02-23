[expect ct-error] Syntax error

[file]<?php
function foo(){
    static $int = 0;          // correct 
//    static $int = 1+2;        // wrong  (as it is an expression)
    static $int = sqrt(121);  // wrong  (as it is an expression too)

    $int++;
    echo $int;
}

foo();

?>
