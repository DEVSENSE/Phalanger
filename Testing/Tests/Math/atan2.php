[expect php]
[file]

<?php

// Note: PHP.NET has a different double precision

echo round(atan2(-10, 20)*180/3.14)." deg\n";  //1
echo round(atan2(-20, 10)*180/3.14)." deg\n";
echo round(atan2(-20, -10)*180/3.14)." deg\n";
echo round(atan2(-10, -20)*180/3.14)." deg\n";
echo round(atan2(10, -20)*180/3.14)." deg\n";
echo round(atan2(20, -10)*180/3.14)." deg\n";
echo round(atan2(20, 10)*180/3.14)." deg\n";
echo round(atan2(10, 20)*180/3.14)." deg\n";  // 8

echo "<hr>\n";

echo round(atan(-10 / 20)*180/3.14)." deg\n";  //1
echo round(atan(-20 / 10)*180/3.14)." deg\n";
echo round(atan(-20 / -10)*180/3.14)." deg\n"; // t
echo round(atan(-10 / -20)*180/3.14)." deg\n"; // t
echo round(atan(10 / -20)*180/3.14)." deg\n";  // t
echo round(atan(20 / -10)*180/3.14)." deg\n";  // t
echo round(atan(20 / 10)*180/3.14)." deg\n";
echo round(atan(10 / 20)*180/3.14)." deg\n";  // 8
 
 
?> 