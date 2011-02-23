[expect php]
[file]
<?php

$arr = array(1 => "one", 2 => "two", 3 => "three");

foreach($arr as $key => $val) {
	$val = $key;
}

foreach($arr as $key => $val) echo "$key => $val\n";

foreach($arr as $key => &$val) {
	$val = $key;
}

foreach($arr as $key => $val) echo "$key => $val\n";

?>