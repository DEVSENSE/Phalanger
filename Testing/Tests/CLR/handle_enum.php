[clr]
[expect exact]
int(2)
DONE
[file]
<?
	function foo()
	{
		$x = new \System\DateTime(2011,8,16);
		$y = $x->DayOfWeek;
		var_dump($y);
	}
	foo();
	echo 'DONE';
?>
