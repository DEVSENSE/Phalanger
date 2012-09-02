[clr]
[expect exact]
OK
[file]
<?
	function foo()
	{
		$x = System\DateTime::$UtcNow;
		$s = serialize($x);
		//var_dump($s);
		$t = unserialize($s);
		//var_dump($t);
	}

	foo();
	
	echo "OK";
?>
