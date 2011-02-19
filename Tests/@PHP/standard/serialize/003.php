[expect php]
[file]
<?php
include('Phalanger.inc');

foreach(array(1e2, 5.2e25, /*bug?: 85.29e-23,*/ 9e-9) AS $value) {
	echo ($ser = serialize($value))."\n";
	__var_dump(unserialize($ser));
	echo "\n";
}
?>
