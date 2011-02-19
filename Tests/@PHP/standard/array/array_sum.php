[expect]
integer(500500)
int64(5000050000)
[file]
<?php
include('Phalanger.inc');
$i = 0;
while ($i++ < 1000) {
	$a[] = $i;
	$b[] = (string)$i;
}
$s1 = array_sum($a);
$s2 = array_sum($b);
__var_dump($s1, $s2);

$j = 0;
while ($j++ < 100000) {
	$c[] = $j;
	$d[] = (string) $j;
}
$s3 = array_sum($c);
$s4 = array_sum($d);
__var_dump($s3, $s4);
?>