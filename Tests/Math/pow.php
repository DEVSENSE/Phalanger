[expect php]
[file]

<?php

function dump($n)
{
 if (is_float($n)) echo "double: ";
 else if (is_int($n)) echo "Int: ";
 else echo "???: ";
 echo round($n,5) . "\n";
}

dump(pow(2, 8)); // int(256)
//dump(pow(10, 12)); // double(1000000000000)
dump(pow(15, 7));

dump(pow(-1, 20)); // 1
dump(pow(0, 0)); // 1

echo is_infinite(pow(0, -5.5)) ? "INF":"NUMBER"; // error
echo is_infinite(pow(0, 5.5)) ? "INF":"NUMBER"; // error
echo is_nan(pow(-1, 5.5)) ? "NaN":"NUMBER"; // error

dump(pow(2, -5.5)); // 0.0221
dump(pow(2, -5)); // 0.3125


?> 