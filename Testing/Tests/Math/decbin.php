[expect php]
[file]

<pre>
<?php

function dump($n)
{
 $rv = "";
 if (is_float($n))  $rv .= "double: ";
 else if (is_int($n))  $rv .= "Int: ";
 else  $rv .= "???: ";
 $rv .= $n;
 return $rv;
}


echo "[". dump(2294967295) ."]\n";
echo decbin(2294967295) . "\n";

echo "[". dump(4294967295+1) ."]\n";
echo decbin(4294967295+1) . "\n";

echo "[". dump(4294967295*2) ."]\n";
echo decbin(4294967295*2) . "\n";

echo "[". dump(4294967295*2) ."]\n";
echo decbin(4294967295*2) . "\n";

echo "[". dump(4294967295*2+1) ."]\n";
echo decbin(4294967295*2+1) . "\n";

echo "[". dump(1000) .".000.000.000]\n";
echo decbin(1000000000000) . "\n";

echo "<hr>(-1, -5435, 2.14, 0, 26)\n";

echo decbin(-1) . "\n";
echo decbin(-5435) . "\n";
echo decbin(2.14) . "\n";
echo decbin(0) . "\n";
echo decbin(26);

?> 
</pre>