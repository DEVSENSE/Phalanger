<?php
/* The Computer Language Benchmarks Game
   http://shootout.alioth.debian.org/
   contributed by Thomas GODART (based on Greg Buchholz's C program) 
   modified by anon
*/

class MandelbrotTest
{
    static function mandelbrot($h)
    {
        /*ob_implicit_flush(1);
        ob_start(NULL, 4096);
        */

        //$h = ($argc == 2) ? ((int)$argv[1]) : 600;
        $w = $h;

        //printf ("P4\n%d %d\n", $w, $h);

        $bit_num = 128;
        $byte_acc = 0;

        $yfac = 2.0 / $h;
        $xfac = 2.0 / $w;

        for ($y = 0 ; $y < $h ; ++$y)
        {
           $result = array('c*');

           $Ci = $y * $yfac - 1.0;

           for ($x = 0 ; $x < $w ; ++$x)
           {
              $Zr = 0; $Zi = 0; $Tr = 0; $Ti = 0.0;

              $Cr = $x * $xfac - 1.5;

              do {
                 for ($i = 0 ; $i < 50 ; ++$i)
                 {
                    $Zi = 2.0 * $Zr * $Zi + $Ci;
                    $Zr = $Tr - $Ti + $Cr;
                    $Tr = $Zr * $Zr;
                    if (($Tr+($Ti = $Zi * $Zi)) > 4.0) break 2;
                 }
                 $byte_acc += $bit_num;
              } while (FALSE);

              if ($bit_num === 1) {
                 $result[] = $byte_acc;
                 $bit_num = 128;
                 $byte_acc = 0;
              } else {
                 $bit_num >>= 1;
              }
           }
           if ($bit_num !== 128) {
              $result[] = $byte_acc;
              $bit_num = 128;
              $byte_acc = 0;
           }
           /*echo*/ call_user_func_array('pack', $result);
        }
    }

    static function main()
    {
        Timing::Start("Mandelbrot");
		self::mandelbrot(600);
		Timing::Stop();		
    }

}