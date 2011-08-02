[clr]
[expect exact]
int(2)
DONE
[file]
<?
	function foo()
	{
		$x = System:::DateTime::$UtcNow;
		$y = $x->DayOfWeek;
		var_dump($y);
	}
	foo();
	echo 'DONE';
?>
