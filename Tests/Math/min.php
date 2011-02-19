[expect php]
[file]

<?php
echo min(2, 3, 1, 6, 7);  // 1
echo "\n";
echo min(array(2, 4, 5)); // 2
echo "\n";

echo min(0, 'hello');     // 0
echo "\n";
echo min('hello', 0);     // hello
echo "\n";
echo min('hello', -1);    // -1
echo "\n";


function printme($a)
{
foreach ($a as $k => $v) echo "[$k] => $v\n";
}

// With multiple arrays, min compares from left to right
// so in our example: 2 == 2, but 4 < 5
$val = min(array(2, 4, 8), array(2, 5, 1)); // array(2, 4, 8)
printme($val);
echo "\n\n";

// If both an array and non-array are given, the array
// is never returned as it's considered the largest
echo $val = min('string', array(2, 5, 7), 42);   // string
echo "\n\n";
?> 