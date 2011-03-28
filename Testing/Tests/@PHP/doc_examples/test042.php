[expect php]
[file]
<?php
$foo = "0";  // $foo is string (ASCII 48)
var_dump($foo);

$foo += 2;   // $foo is now an integer (2)
var_dump($foo);

$foo = $foo + 1.3;  // $foo is now a float (3.3)
var_dump($foo);

$foo = 5 + "10 Little Piggies"; // $foo is integer (15)
var_dump($foo);

$foo = 5 + "10 Small Pigs";     // $foo is integer (15)
var_dump($foo);

?>
