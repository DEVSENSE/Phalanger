[expect php]
[file]
<?php
	error_reporting(E_ALL & ~E_NOTICE);

	$a = 1;
	$b =& $a;
	unset ($a);

	echo $a.$b;
	if (isset($a)) echo "A";
	if (isset($b)) echo "B";
?>
