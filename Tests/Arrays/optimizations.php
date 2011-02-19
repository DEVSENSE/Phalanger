[expect php]
[file]
<?
	error_reporting(E_ALL & ~E_NOTICE);

	$a = array("a" => "b", "0" => "c", "10" => 4, 5 => 8);
	$s = "hello";
	
	$a[1] = 3;
	$a["x"] = 4;
	$a["0"] = 5;
	$s[1] = 'a';
	$s["2g"] = 'b';
	$s["g"] = 'c';
	
	echo $a[1], "\n", $a["x"], "\n", $a["0"], "\n", $s[1], "\n", $s["2g"], "\n", $s["g"], "\n";
	echo strtolower($a), "\n", $s, "\n";
?>