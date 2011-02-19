[expect php]

[file]
<?
	function dump($a)
	{
		foreach ($a as $key => $val)
		{
			echo "$key => ";
			if (is_array($val))
			{
				echo "\n";
				foreach ($val as $key2 => $val2)
				{
					echo "  $key2 => $val2\n";
				}
			}
			else echo "$val\n";
		}
	}

	define("USER_CONSTANT", 123);

	chdir(dirname(__FILE__));
	dump(parse_ini_file("sample.ini", false));
	dump(parse_ini_file("sample.ini", true));
	
	dump(parse_ini_string("
	[sec1]
	x=1
	y=2
	[sec2]
	ano=Yes
	ne=No
	[sec3]
	multiline=\"
		line1
		line2
		line3\"
	",false))
?>
