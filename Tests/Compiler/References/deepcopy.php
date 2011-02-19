[expect php]
[file]
<?php

$globalobjects = '';

function mymain()
{
	global $globalobjects;
	
	$myobjects = array(1,array(2,3));
	
	$o = & $myobjects[1];//creates reference

	//xdebug_debug_zval('myobjects');
	
	$globalobjects = $myobjects;// deep copy, ref_count of the reference is increased
	
	echo "<br/>";
	
	//xdebug_debug_zval('myobjects');
		echo "<br/>";
	
	//eval('echo "ahoj";');

	$myobjects[1] = 7;	
}

 function foo(&$hovno)
 {

 }

mymain();

	//reference in array shouldn't act as a reference any more
	echo "globalobjects: ";
	var_dump($globalobjects);

 
?>