<?php /* The Great Computer Language Shootout
   http://shootout.alioth.debian.org/

   contributed by Peter Baltruschat
*/
function Transformation_Compose($tr, $a)
{
   return array(
      gmp_mul($tr[0], $a[0]),
      gmp_add(gmp_mul($tr[0], $a[1]), gmp_mul($tr[1], $a[3])),
      gmp_add(gmp_mul($tr[2], $a[0]), gmp_mul($tr[3], $a[2])),
      gmp_add(gmp_mul($tr[2], $a[1]), gmp_mul($tr[3], $a[3]))
   );
}
function Transformation_Compose2($y, $a)
{
   return array(
      gmp_mul(10, $a[0]),
      gmp_add(gmp_mul(10, $a[1]), gmp_mul(gmp_mul(-10, $y), $a[3])),
      $a[2],
      $a[3]
   );
}
function Transformation_Extract($tr, $j)
{
   return gmp_div_q(
      gmp_add(gmp_mul($tr[0], $j), $tr[1]),
      gmp_add(gmp_mul($tr[2], $j), $tr[3])
   );
}
function Transformation_Next(&$tr)
{
   $tr[3] = (++$tr[0]<<1) + 1;
   $tr[1] = $tr[3]<<1;
   $tr[2] = 0;
   return $tr;
}
function Pidigit_Next(&$pd, $times)
{
   $digits = '';
   $z = $pd[0];
   do
   {
      $y = Transformation_Extract($z, 3);
      do
      {
         $z = Transformation_Compose($z, Transformation_Next($pd[1]));
         $y = Transformation_Extract($z, 3);
      }
      while(0 != gmp_cmp(Transformation_Extract($z, 4), $y));
      $z = Transformation_Compose2($y, $z);
      $digits .= gmp_strval($y);
   }
   while(--$times);
   $pd[0] = $z;
   return $digits;
}

class PidigitsTest
{
    private static function pidigits($n)
    {
    //$n = (int) $argv[1];
        $i = 0;
        $pidigit = array(array(1, 0, 0, 1), array(0, 0, 0, 0));

        while($n)
        {
           if($n < 10)
           {
              printf("%s%s\t:%d\n", Pidigit_Next($pidigit, $n), str_repeat(' ', 10 - $n), $i + $n);
              break;
           }
           else
           {
              printf("%s\t:%d\n", Pidigit_Next($pidigit, 10), $i += 10);
           }
           $n -= 10;
        }
    }

    static function main()
    {
        Timing::Start("NBody");
		self::pidigits(1000);
		Timing::Stop();	
    }

}
?>
