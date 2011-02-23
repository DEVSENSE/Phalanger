[expect]
double(11.5)
double(-1299)
integer(1)
integer(1)
integer(11)
double(14.2)
double(11)
double(11)

[file]
<?php
$foo = 1 + "10.5";                // $foo is float (11.5)
var_dump($foo);

$foo = 1 + "-1.3e3";              // $foo is float (-1299)
var_dump($foo);

$foo = 1 + "bob-1.3e3";           // $foo is integer (1)
var_dump($foo);

$foo = 1 + "bob3";                // $foo is integer (1)
var_dump($foo);

$foo = 1 + "10 Small Pigs";       // $foo is integer (11)
var_dump($foo);

$foo = 4 + "10.2 Little Piggies"; // $foo is float (14.2)
var_dump($foo);

$foo = "10.0 pigs " + 1;          // $foo is float (11)
var_dump($foo);

$foo = "10.0 pigs " + 1.0;        // $foo is float (11)     
var_dump($foo);

?>
