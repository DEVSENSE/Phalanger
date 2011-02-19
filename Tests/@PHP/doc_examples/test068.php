[expect php]

[file]
<?php

$a = 3;
$a += 5; // sets $a to 8, as if we had said: $a = $a + 5;
echo $a;

$b = "Hello ";
$b .= "There!"; // sets $b to "Hello There!", just like $b = $b . "There!";
echo $b;

?>
