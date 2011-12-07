[expect php]
[file]
<?php
// Initialize the array with a fixed length
$array = new SplFixedArray(5);

$array[1] = 2;
$array[4] = "foo";
$array->setSize(10);
$array[9] = "asdf";

foreach ($array as $k => $v)
	echo "$k: $v\n";
?>