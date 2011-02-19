[expect php]
[file]
<?php

ini_set("magic_quotes_sybase","on");
test();
ini_set("magic_quotes_sybase","off");
test();

function test(){
	$buf = 'g\g"\0g'."'";
	$slashed = addslashes($buf);
	echo "$buf\n";
	echo "$slashed\n";
	echo stripslashes($slashed."\n");
}
?>
