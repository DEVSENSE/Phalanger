[expect php]
[file]
<?php
$input = array('a', 'foo', 'barbazbax');
foreach($input AS $i) {
	for($n=0; $n<5; $n++) {
		echo str_repeat($i, $n)."\n";
	}
}
?>
