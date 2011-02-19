[expect php]

[file]
<?php
$bar = 1;
$arr[somefunc($bar)] = 'èauky';

echo $arr[somefunc($bar)];

function somefunc($a)
{
	return $a."func";
}
?>
