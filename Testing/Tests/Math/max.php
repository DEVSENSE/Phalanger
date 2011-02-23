[expect php]
[file]

<?php
echo max(1, 3, 5, 6, 7);  // 7
echo "\n";
echo max(array(2, 4, 5)); // 5
echo "\n";

echo max(0, 'hello');     // 0
echo "\n";
echo max('hello', 0);     // hello
echo "\n";
echo max(-1, 'hello');    // hello
echo "\n";


function printme($a)
{
foreach ($a as $k => $v) echo "[$k] => $v\n";
}

// With multiple arrays, max compares from left to right
// so in our example: 2 == 2, but 4 < 5
$val = max(array(2, 4, 8), array(2, 5, 7)); // array(2, 5, 7)
printme($val);
echo "\n\n";

// If both an array and non-array are given, the array
// is always returned as it's seen as the largest
$val = max('string', array(2, 5, 7), 42);   // array(2, 5, 7)
printme($val);
echo "\n\n";
?> 