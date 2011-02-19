[expect] true true true true
[expect] Notice
[expect] Notice
[expect] true

[file]
<?php
// This array is the same as ...
$x = array(5 => 43, 32, 56, "b" => 12);

// ...this array
$y = array(5 => 43, 6 => 32, 7 => 56, "b" => 12);

echo $x[5] == $y[5] ? "true " : "false ";
echo $x[6] == $y[6] ? "true " : "false ";
echo $x[7] == $y[7] ? "true " : "false ";
echo $x["b"] == $y["b"] ? "true " : "false ";
echo $x["non existing key"] == $y["non existing key"] ? "true " : "false ";


?>
