[expect php]

[file]
<?php
// Let's show all errors
error_reporting(E_ERROR);

class CSquare { public $width; }
$square = new CSquare();
$arr = array( 4 => array(3 => "ahoj"), "foo" => array(3 => "nazdar"));
$great = 'fantastic';
$name = "great";

class C { public $values; }
$obj = new C();
$obj->values = array(3 => $square);

// Won't work, outputs: This is { fantastic}
echo "This is { $great}";

// Works, outputs: This is fantastic
echo "This is {$great}";
echo "This is ${great}";

// Works
echo "This square is {$square->width}00 centimeters broad."; 

// Works
echo "This works: {$arr[4][3]}";

// This is wrong for the same reason as $foo[bar] is wrong 
// outside a string.  In other words, it will still work but
// because PHP first looks for a constant named foo, it will
// throw an error of level E_NOTICE (undefined constant).
// echo "This is wrong: {$arr[foo][3]}"; 

// Works.  When using multi-dimensional arrays, always use
// braces around arrays when inside of strings
echo "This works: {$arr['foo'][3]}";

// Works.
echo "This works: " . $arr['foo'][3];

echo "You can even write {$obj->values[3]->width}";

echo "This is the value of the var named $name: {${$name}}";
?>
