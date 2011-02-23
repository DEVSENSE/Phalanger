[expect php]
[file]
<?php
	error_reporting(0);

	$ar = array();
	for ($count = 0; $count < 10; $count++) {
		$ar[$count]        = "$count";
		$ar[$count]['idx'] = "$count";
	}

	for ($count = 0; $count < 10; $count++) {
		echo $ar[$count]." -- ".$ar[$count]['idx']."\n";
	}
	$a = "0123456789";
	$a[9] = $a[0];
	echo ($a);
?>
