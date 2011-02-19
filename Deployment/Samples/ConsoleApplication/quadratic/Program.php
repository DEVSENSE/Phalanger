<?php

	// Example using command line parameters
	
	function usage()
	{
		echo "\n";
		echo "Usage: quadratic a=<a> b=<b> c=<c>\n";
		echo "\n";
		echo "Consider a*x^2 + b*x + c = 0 equalisation.\n";
		echo "       <a> absolute term by x^2\n";
		echo "       <b> absolute term by x\n";
		echo "       <c> absolute term of the equation\n";
		echo "\n";
	
		sleep(2);	
		exit;
	}
	
	function process_parameters(&$a, &$b, &$c)
	{
		global $argc, $argv;
		
		$a = null;
		$b = null;
		$c = null;

		if ($argc != 4) // first is executable file
			usage();
		
		for ($i = 1; $i < $argc; $i++)
		{
			if (strpos($argv[$i], "a=") === 0)
				$la = substr($argv[$i], 2);
			else if (strpos($argv[$i], "b=") === 0)
				$lb = substr($argv[$i], 2);
			else if (strpos($argv[$i], "c=") === 0)
				$lc = substr($argv[$i], 2);
		}
		
		if (!isset($la) || !isset($lb) || !isset($lc))
			usage();

		$a = $la;
		$b = $lb;
		$c = $lc;
	}
	
	echo "Quadratic calculator sample\n";
	
	$a = null; $b = null; $c = null;
	process_parameters($a, $b, $c);
	
	if ($a == 0) // linear equation
	{
		if ($b == 0)
		{
			if ($c == 0)
				echo "Any x is suitable.\n";
			else
				echo "No x is solution.\n";
		}
		else
		{
			echo "x = ".(-$c/$b)."\n";
		}
	}
	else // quadratic
	{
		$D = $b*$b - 4*$a*$c;
		
		if ($D < 0)
			echo "No x is solution.\n";
		else if ($D == 0)
			echo "x = ".(-$b/(2*$a))."\n";
		else
		{
			$x1 = (-$b + sqrt($D))/(2*$a);
			$x2 = (-$b - sqrt($D))/(2*$a);
			
			echo "x1 = $x1\n";
			echo "x2 = $x2\n";
		}
	}
	
	fgets(STDIN);
?>