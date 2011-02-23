[expect]
integer(0)
integer(0)
integer(3)
integer(9)
double(1.0000000000)
int64(9999999800000001)
int64(8589934590)
[file]
<?php
include('Phalanger.inc');
$tests = array(
	array(),
	array(0),
	array(3),
	array(3, 3),
	array(0.5, 2),
	array(99999999, 99999999),
	array(2,sprintf("%u", -1)),
);

foreach ($tests as $v) {
	__var_dump(array_product($v));
}
?>