<?php

    include '..\timing.php';

	include "nbody.php";
	include "fannkuch.php";
	include "binary-trees.php";
    include "mandelbrot.php";
    //include "pidigits.php";
    //include "spectral-norm.php";

	class Start
	{
		const LOOP_COUNT = 10000000;
	
		static function Main()
		{
			for ($k = 1; $k <= 3; $k++)
			{
				echo "Benchmark #$k\n";
				echo "============\n";
			
				NBodyTest::main();
				//FannkuchTest::main();
				//BinaryTreesTest::main();
				//MandelbrotTest::main();
                //PidigitsTest::main(); // requires GMP
                //SpectralNormTest::main(); // some streams
			}

            Timing::OutputResults();
		}
        
	}
?>
