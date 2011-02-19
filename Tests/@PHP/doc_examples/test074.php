[expect php]

[file]
<?php
$a = "Hello ";
$b = $a . "World!"; // now $b contains "Hello World!"
echo $b;

$a = "Hello ";
$a .= "World!";     // now $a contains "Hello World!"
echo $a;

?>
