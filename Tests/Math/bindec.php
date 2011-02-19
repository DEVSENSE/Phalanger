[expect php]
[file]


<?php

function dump($n)
{
 if (is_float($n)) echo "double: ";
 else if (is_int($n)) echo "Int: ";
 else echo "???: ";
 echo $n;
}

echo bindec('110011') . "\n";
echo bindec('000110011') . "\n";

echo bindec('111') . "\n";
dump( bindec(decbin(2147483647)) ); 
echo "\n";
dump( bindec(decbin(2147483647+1)) ); 
echo "\n";
dump( bindec(decbin(2147483647*2)) ); 
echo "\n";
dump( bindec(decbin(2147483647*2+1)) ); 
echo "\n";
dump( bindec("11111111111111111111111111111111") ); 
echo "\n";

?> 