[expect php]
[file]
<?php
	$delimiter = "|";

	echo "delimiter: $delimiter\n";
	implode($delimiter, array("foo", "bar"));
	echo "delimiter: $delimiter\n";
?>