[expect exact]
1, 2, 3, 4, 5, OK
[file]
<?php

import namespace System:::Collections:::Generic;

function CreateList($t, $vals)
{
	$ret = new i'List'<:$t:>;
	foreach($vals as $v)
		$ret->Add($v);
	return $ret;		
}


$l = CreateList("System:::Int32", array(1,2,3,4,5));

// This causes Common Langauge Runtime 
foreach($l as $key => $value) echo "$value, ";

echo "OK";

?>