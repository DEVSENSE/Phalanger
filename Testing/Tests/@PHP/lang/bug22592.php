[expect php]
[file]
<?php
require('Phalanger.inc');

error_reporting(0);
$i = 4;
$s = "string";

$result = "* *-*";
__var_dump($result);
$result[6] = '*';
__var_dump($result);
$result[1] = $i;
__var_dump($result);
$result[3] = $s;
__var_dump($result);
$result[7] = 0;
__var_dump($result);
$a = $result[1] = $result[3] = '-';
__var_dump($result);
$b = $result[3] = $result[5] = $s;
__var_dump($result);
$c = $result[0] = $result[2] = $result[4] = $i;
__var_dump($result);
$d = $result[6] = $result[8] = 5;
__var_dump($result);
$e = $result[1] = $result[6];
__var_dump($result);
__var_dump($a, $b, $c, $d, $e);
$result[-1] = 'a';
__var_dump($result);

?>