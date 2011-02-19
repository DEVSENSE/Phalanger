[expect php]
[file]
<?php
$func = create_function('','
	static $foo = 0;
	return $foo++;
');
echo ($func()),"\n";
echo ($func()),"\n";
echo ($func()),"\n";
?>