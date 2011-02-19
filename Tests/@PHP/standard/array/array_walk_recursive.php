[expect php]
[file]
<?php
include('Phalanger.inc');
function foo($value) {
	echo $value . " foo\n";
}

function bar($value) {
	echo $value . " bar\n";
}

$arr = array (1,2,3);
__var_dump (array_walk_recursive ($arr, 'foo'));
__var_dump (array_walk_recursive ($arr, 'bar'));

?>