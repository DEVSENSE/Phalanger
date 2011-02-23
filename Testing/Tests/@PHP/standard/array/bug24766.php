[expect php]
[file]
<?php
  include('Phalanger.inc');

error_reporting(E_ALL);

$a = unpack('C2', "\0224V");
$b = array(1 => 18, 2 => 52);
$k = array_keys($a);
$l = array_keys($b);
$i=$k[0];
__var_dump($a[$i]);
$i=$l[0];
__var_dump($b[$i]);
?>