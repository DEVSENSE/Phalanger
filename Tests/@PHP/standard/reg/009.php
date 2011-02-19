[expect php]
[file]
<?php
	$a=split("[[:space:]]","this is	a\ntest");
	echo count($a) . "\n";
	for ($i = 0; $i < count($a); $i++) {
          echo $a[$i] . "\n";
        }
?>
