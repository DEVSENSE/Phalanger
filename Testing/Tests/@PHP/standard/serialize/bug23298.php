[expect php]
[file]
<?php
include('Phalanger.inc');
	$foo = 1.428571428571428647642857142;
	$bar = unserialize(serialize($foo));
	__var_dump(($foo === $bar));
?>
