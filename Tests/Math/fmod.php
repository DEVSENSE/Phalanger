[expect php]
[file]

<?php

echo fmod(91.25, 2);
echo "~";
echo fmod(91.25, 2.04);
echo "~";
echo fmod(97.25, 10);
echo "~";
echo fmod(-97.25, 10);
echo "~";
echo fmod(97.25, -10);
echo "~";
echo is_nan(fmod(97.25, 0));	// Note: PHP has different syntax of NaN
echo "~";
echo is_nan(fmod(-97.25, 0));

$x = 5.7;
$y = 1.3;
$r = fmod($x, $y);
// $r equals 0.5, because 4 * 1.3 + 0.5 = 5.7
echo "$ mod $y = $r\n";

$x = -5.7;
$y = 1.3;
$r = fmod($x, $y);
echo "$ mod $y = $r\n";

$x = -5.7;
$y = -1.3;
$r = fmod($x, $y);
echo "$ mod $y = $r\n";

$x = 5.7;
$y = -1.3;
$r = fmod($x, $y);
echo "$ mod $y = $r\n";

$x = 0;
$y = 1.3;
$r = fmod($x, $y);
echo "$ mod $y = $r\n";

?> 