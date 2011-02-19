[expect php]
[file]
<?php
function test ($b) {
	$b++;
	return($b);
}
$a = test(1);
echo $a;
?>