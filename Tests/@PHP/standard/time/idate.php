[expect php]
[file]
<?php
putenv ("TZ=GMT0");

$tmp = "UYzymndjHGhgistwLBIW";
for($a = 0;$a < strlen($tmp); $a++){
	echo $tmp[$a], ': ', idate($tmp[$a], 1043324459)."\n";
}
?>