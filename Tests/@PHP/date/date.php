[expect php]
[file]
<?php

$tmp = "cr";
putenv ("TZ=GMT0");

for($a = 0;$a < strlen($tmp); $a++){
	echo $tmp[$a], ': ', date($tmp[$a], 1043324459)."\n";
}

putenv ("TZ=MET");

for($a = 0;$a < strlen($tmp); $a++){
	echo $tmp[$a], ': ', date($tmp[$a], 1043324459)."\n";
}
?>