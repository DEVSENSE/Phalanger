<?php

    /*
     * Copyright (c) 2005, Damien Mascord <tusker@tusker.org> All rights reserved.
     * 
     * Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following
     * conditions are met:
     * 
     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following
     * disclaimer in the documentation and/or other materials provided with the distribution. Neither the name of the <ORGANIZATION>
     * nor the names of its contributors may be used to endorse or promote products derived from this software without specific
     * prior written permission. THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR
     * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
     * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
     * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
     * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
     * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
     * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
     * 
     */



    $re = array("#^(([^:]+)://)?([^:/]+)(:([0-9]+))?(/.*)#", // URL match
        "#(([^:]+)://)?([^:/]+)(:([0-9]+))?(/.*)#", // URL match without starting ^
        "#usd [+-]?[0-9]+.[0-9,0-9]#", // Canonical US dollar amount
        "#\b(\w+)(\s+\1)+\b#", // Duplicate words
        "#\{(\d+):(([^}](?!-} ))*)#" // this is meant to match against the "some more text and ..." but it causes ORO Matcher
    								  // to fail, so we won't include this by default... it is also WAY too slow to test
                                      // we will test large string 10 times
    );

	 $str = array(
        "http://www.linux.com/",
        "http://www.thelinuxshow.com/main.php3",
        "usd 1234.00",
        "he said she said he said no",
        "same same same",
        "{1:\n" . "this is some more text - and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more\n"
            . "this is some more text and some more and some more and even more at the end\n" . "-}\n" // very large bit of text...
		);
		
		
    $expectedMatch[0][0] = true;
    $expectedMatch[0][1] = true;
    $expectedMatch[0][2] = false;
    $expectedMatch[0][3] = false;
    $expectedMatch[0][4] = false;
    $expectedMatch[0][5] = false;
    $expectedMatch[1][0] = true;
    $expectedMatch[1][1] = true;
    $expectedMatch[1][2] = false;
    $expectedMatch[1][3] = false;
    $expectedMatch[1][4] = false;
    $expectedMatch[1][5] = false;
    $expectedMatch[2][0] = false;
    $expectedMatch[2][1] = false;
    $expectedMatch[2][2] = true;
    $expectedMatch[2][3] = false;
    $expectedMatch[2][4] = false;
    $expectedMatch[2][5] = false;
    $expectedMatch[3][0] = false;
    $expectedMatch[3][1] = false;
    $expectedMatch[3][2] = false;
    $expectedMatch[3][3] = false;
    $expectedMatch[3][4] = true;
    $expectedMatch[3][5] = false;
    $expectedMatch[4][0] = false;
    $expectedMatch[4][1] = false;
    $expectedMatch[4][2] = false;
    $expectedMatch[4][3] = false;
    $expectedMatch[4][4] = false;
    $expectedMatch[4][5] = false;

    define("debug", false);
	define("html", true);
    define("ITERATIONS", 10000);


  
    // org.apache.regexp.* test
    if (debug) echo("Testing QUERUCS...");

    $startTime = microtime(true);
    for ($regnum = 0; $regnum < count($re); $regnum++)
    {
        if (debug)
        {
            echo("New regnum " . $regnum . "...\n");
        }

		$testedAgainstLargeString = 0;
		for ($itter = 0; $itter < ITERATIONS; $itter++)
		{
			for ($strnum = 0; $strnum < count($str); $strnum++)
			{
				if (debug && ($itter % 1000) == 0)
				{
					echo("Iteration/regex number/string number " . $itter . "/" . $regnum . "/" . $strnum . "... ");
				}

				if (debug && ($itter % 1000) == 0)
				{
					echo($re[$regnum] . " against " . $str[$strnum] . ":");
				}

				// only test the big one 10 iterations only per regex
				if ($testedAgainstLargeString > 10 && $strnum == 5)
				{
					break;
				}

				$iterStarTime = microtime(true);
				$b = preg_match($re[$regnum], $str[$strnum]);
				$matches[$regnum][ $strnum] = ($b == $expectedMatch[$regnum][ $strnum]);
				$timeTaken[$regnum][ $itter][ $strnum] = (microtime(true) - $iterStarTime)*1000;
				
				// count how many times we test against the large string on this regex
				if ($strnum == 5)
				{
					$testedAgainstLargeString++;
				}

				if (debug && ($itter % 1000) == 0)
				{
					echo($b);
				}

				if (debug && ($itter % 1000) == 0)
				{
					echo(" took " . $timeTaken[$regnum][ $itter][ $strnum] . "ms" . "\n");
				}
			}
		}

    }
    $endTime = microtime(true);
    printResult("QUERUCS", $timeTaken, ($endTime - $startTime) * 1000, $matches);

	
	function printResult($regexName, &$matrix, $totalTime, $matches)
    {
		global $re,$str;
		
        // timeTaken[regnum,itter,strnum]
        if (html)
        {
            echo("<table>");
            echo("<tr><th colspan=\"3\"><h2>Regular expression library:</h2></th><td colspan=\"3\"><h2>" . $regexName
                . "</h2></td></tr>");
        }
        else
        {
            echo("------------------------------------------");
            echo("Regular expression library: " . $regexName . "\n");
        }
        for ($ire = 0; $ire <count($re); $ire++)
        {
            if (html)
            {
                echo("<tr><th>RE:</th><td colspan=\"5\">" . $re[$ire] . "</td></tr>");
                echo("<tr><th>MS</th><th>MAX</th><th>AVG</th><th>MIN</th><th>DEV</th><th>INPUT</th><th>MATCH</th></tr>");
            }
            else
            {
                echo("RE: " . $re[$ire]);
                echo("  MS\tMAX\tAVG\tMIN\tDEV\tINPUT\tMATCH");
            }
            for ($istr = 0; $istr < count($str); $istr++)
            {
                $total = 0;
                $sumOfSq = 0;
                $min = PHP_INT_MAX;
                $max = 1-PHP_INT_MAX;
                for ($i = 0; $i < ITERATIONS; $i++)
                {
                    $elapsed = @$matrix[$ire][$i][$istr];
                    $total += $elapsed;
                    $sumOfSq += $elapsed * $elapsed;
                    if ($elapsed < $min)
                    {
                        $min = $elapsed;
                    }
                    if ($elapsed > $max)
                    {
                        $max = $elapsed;
                    }
                }
                // calc std dev
                $stdDev = sqrt(($sumOfSq - (($total * $total) / ITERATIONS)) / (ITERATIONS - 1));

                if (html)
                {
                    echo("<tr><td>" . $total . "</td><td>" . $max . "</td><td>" . $total / ITERATIONS
                        . "</td><td>" . $min . "</td><td>" . $stdDev . "</td><td>" . $str[$istr] . "</td><td>" . $matches[$ire][$istr]
                        . "</td></tr>");
                }
                else
                {
                    echo("  " . $total . "\t" . $max . "\t" . $total / ITERATIONS . "\t" . $min . "\t" . $stdDev
                        . "\t'" . $str[$istr] . "\t'" . $matches[$ire][$istr] . "'");
                }
            }
        }
        if (html)
        {
            echo("<tr><th colspan=\"3\"><h2>Total time taken:</h2></th><td colspan=\"3\"><h2>" . $totalTime
                . "</h2></td></tr>");
            echo("</table>");
        }
        else
        {
            echo("Total time taken: " . $totalTime);
            echo("------------------------------------------");
        }
    }
	

?>