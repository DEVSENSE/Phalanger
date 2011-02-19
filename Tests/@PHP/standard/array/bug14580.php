[expect php]
[file]
<?php
	$arr = array ("foo\0bar" => "foo\0bar");
	$key = key($arr);
	echo strlen($key), ': ';
	echo urlencode($key), "\n";
?>

