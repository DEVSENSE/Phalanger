[expect php]
[file]
<?php
	error_reporting(E_ALL & ~E_NOTICE);

	$beer = 'Heineken';
	echo "$beer's taste is great\n"; // works, "'" is an invalid character for varnames
	echo "He drank some $beers\n";   // won't work, 's' is a valid character for varnames
	echo "He drank some ${beer}s\n"; // works
	echo "He drank some {$beer}s\n"; // works
?>
