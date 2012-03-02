[expect php]
[file]

<?php

$f = fopen("rwseek.txt", "w+b");

fwrite($f, "AAAA BBBB CCCC");
fseek($f, 5);
$s = fread($f, 5);
fseek($f, 0);
fwrite($f, $s);
fseek($f, 10);
$s = fread($f, 5);
fseek($f, 5);
fwrite($f, $s);
fseek($f, 0);
fpassthru($f);
fclose($f);
unlink("rwseek.txt");

?>