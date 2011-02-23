[expect php]
[file]
<?
	$a = array(NULL, FALSE, TRUE, 0, 123456, "", "Master Yoda",
		array(), array(1, 2, 3, ""), new stdClass());
		
	echo serialize($a) . "\n";
	
	$b = unserialize(serialize($a));
	
	if ($a == $b) echo "OK1\n";
		
	$x = serialize(new stdClass);
	$x = str_replace("std", "XYZ", $x);
	
	$y = unserialize($x);
	if ($y instanceOf __PHP_Incomplete_Class) echo "OK2\n";
	
	echo serialize($y);


/*	$b = array();
	$b[] =& $b;	
	$a[] = $b;
	
	
	$b = array();	
	$b[0] = 123;
	
	var_dump($a);*/
?>
