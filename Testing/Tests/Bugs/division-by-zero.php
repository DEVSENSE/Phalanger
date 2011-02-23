[expect ct-warning]
PHP0150: Division by zero
[expect ct-warning]
PHP0150: Division by zero
[expect ct-warning]
PHP0150: Division by zero
[expect ct-warning]
PHP0150: Division by zero
[expect php]

[file]
<?php
	$a = 0;
	$b = 1;

	echo "DIVISION\n";
	$r = $b/0;
	var_dump($r);
	$r = 1/0;
	var_dump($r);
	$r = $b/$a;
	var_dump($r);
	$r = 1/$a;
	var_dump($r);
	
	echo "MODULO\n";
	$r = $b%0;
	var_dump($r);
	$r = 1%0;
	var_dump($r);
	$r = $b%$a;
	var_dump($r);
	$r = 1%$a;
	var_dump($r);
?>