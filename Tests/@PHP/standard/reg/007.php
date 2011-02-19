[expect php]
[file]
<?php 
	$a="abcd";
	$b=ereg_replace("abcd","",$a);
	echo "strlen(\$b)=".strlen($b);
?>