<?
	// timing
	require_once '..\timing.php';

	// tests:
	
	function Ack($m, $n){
	  if($m == 0) return $n + 1;
	  if($n == 0) return Ack($m-1, 1);
	  return Ack($m - 1, Ack($m, ($n - 1)));
	}
	
	function ary($n) {
	  for ($i=0; $i<$n; $i++) {
		  $X[$i] = $i;
	  }
	  for ($i=$n-1; $i>=0; $i--) {
		  $Y[$i] = $X[$i];
	  }
	  $last = $n-1;
	  /*print "$Y[$last]\n";*/
	}
	
	function ary2($n) {
	  for ($i=0; $i<$n;) {
		  $X[$i] = $i; ++$i;
		  $X[$i] = $i; ++$i;
		  $X[$i] = $i; ++$i;
		  $X[$i] = $i; ++$i;
		  $X[$i] = $i; ++$i;
	  
		  $X[$i] = $i; ++$i;
		  $X[$i] = $i; ++$i;
		  $X[$i] = $i; ++$i;
		  $X[$i] = $i; ++$i;
		  $X[$i] = $i; ++$i;
	  }
	  for ($i=$n-1; $i>=0; $i--) {
		  $Y[$i] = $X[$i]; --$i;
		  $Y[$i] = $X[$i]; --$i;
		  $Y[$i] = $X[$i]; --$i;
		  $Y[$i] = $X[$i]; --$i;
		  $Y[$i] = $X[$i]; --$i;
	  
		  $Y[$i] = $X[$i]; --$i;
		  $Y[$i] = $X[$i]; --$i;
		  $Y[$i] = $X[$i]; --$i;
		  $Y[$i] = $X[$i]; --$i;
		  $Y[$i] = $X[$i]; --$i;
	  }
	  $last = $n-1;
	  /*print "$Y[$last]\n";*/
	}
	
	function ary3($n) {
	  for ($i=0; $i<$n; $i++) {
		  $X[$i] = $i + 1;
		  $Y[$i] = 0;
	  }
	  for ($k=0; $k<1000; $k++) {
		  for ($i=$n-1; $i>=0; $i--) {
			 $Y[$i] += $X[$i];
		  }
	  }
	  $last = $n-1;
	  /*print "$Y[0] $Y[$last]\n";*/
	}
	
	function fibo($n){
		return(($n < 2) ? 1 : fibo($n - 2) + fibo($n - 1));
	}
	
	function stak($x1, $y1, $z1) {
	  global $x, $y, $z;

	  $x = $x1;
	  $y = $y1;
	  $z = $z1;
	  return stak_aux();
	}
	function stak_aux() {
	  global $x, $y, $z;

	  if ( !($y < $x)) {
		return $z;
	  } else {
		//no LET :(
		$savedx = $x;
		$savedy = $y;
		$savedz = $z;
		$x = $savedx - 1;
		$newx = stak_aux();
		
		$x = $savedy - 1;
		$y = $savedz;
		$z = $savedx;
		$newy = stak_aux();

		$x = $savedz - 1;
		$y = $savedx;
		$z = $savedy;
		$newz = stak_aux();
		
		$x = $newx;
		$y = $newy;
		$z = $newz;
		return stak_aux();
	  }
	}
	function tak($x, $y, $z) {
	  if ( !($y < $x)) {
		return $z;
	  } else {
		return tak( tak($x - 1, $y, $z),
			tak($y - 1, $z, $x),
			tak($z - 1, $x, $y));
	  }
	}
	
	function hash2($n) {
		for ($i = 0; $i < 1000; $i++) {
			$hash1["foo_$i"] = $hash2["foo_$i"] = $i;
		}
		for ($i = $n; $i > 0; $i--) {
			foreach($hash1 as $key => $value) $hash2[$key] += $value;
		}
		$h = "$hash1[foo_1] $hash1[foo_999] $hash2[foo_1] $hash2[foo_999]";
	}
	
	class Random {
		const IM = 139968;
		const IA = 3877;
		const IC = 29573;
	
		static $LAST = 42;

		static function gen_random ($n) {
			return( ($n * (Random::$LAST = (Random::$LAST * Random::IA + Random::IC) % Random::IM)) / Random::IM );
		}		
	}
	
	function heapsort ($n, &$ra) {
		$l = ($n >> 1) + 1;
		$ir = $n;

		while (1) {
			if ($l > 1) {
				$rra = $ra[--$l];
			} else {
				$rra = $ra[$ir];
				$ra[$ir] = $ra[1];
				if (--$ir == 1) {
				$ra[1] = $rra;
				return;
				}
			}
			$i = $l;
			$j = $l << 1;
			while ($j <= $ir) {
				if (($j < $ir) && ($ra[$j] < $ra[$j+1])) {
				$j++;
				}
				if ($rra < $ra[$j]) {
				$ra[$i] = $ra[$j];
				$j += ($i = $j);
				} else {
				$j = $ir + 1;
				}
			}
			$ra[$i] = $rra;
		}
	}

	function numtest() {
		$j = 0;
		for ($i = 0; $i < 1000000; $i++) {
			$j += $i * 2;
		}
		$x = "$j, $i\n";
	}
	
	class smallobject {

		var $avar = 'hello';

		function smallobject() {
			return "some hairy constructor\n";
		}
		
		function afunc() {
			return $this->avar;
		}
		
	}

	class largeobject {

		var $var1 = 'some string';
		var $var2 = 'some string';
		var $var3 = 'some string';
		var $var4 = 'some string';
		var $var5 = 'some string';
		var $var6 = 'some string';
		var $var7 = 'some string';
		var $var8 = 'some string';
		var $var9 = 'some string';
		var $var10 = 'some string';
		var $var11 = 'some string';
		var $var12 = 'some string';
		var $var13 = 'some string';
		var $var14 = 'some string';
		var $var15 = 'some string';
		var $var16 = 'some string';
		var $var17 = 'some string';
		var $var18 = 'some string';
		var $var19 = 'some string';
		var $var20 = 'some string';
		var $var21 = 'some string';
		var $var22 = 'some string';
		var $var23 = 'some string';
		var $var24 = 'some string';
		var $var25 = 'some string';
		var $var26 = 'some string';
		var $var27 = 'some string';
		var $var28 = 'some string';
		var $var29 = 'some string';
		var $var30 = 'some string';
		var $var31 = 'some string';
		var $var32 = 'some string';
		var $var33 = 'some string';
		var $var34 = 'some string';
		var $var35 = 'some string';
		var $var36 = 'some string';
		var $var37 = 'some string';
		var $var38 = 'some string';
		var $var39 = 'some string';
		var $var40 = 'some string';

		function largeobject() {
			return "some hairy constructor\n";
		}
		
		function afunc11($a) {        
			return "a function\n";
		}

		function afunc12($a) {        
					return "a function\n";
		}

		function afunc13($a) {        
					return "a function\n";
		}

		function afunc14($a) {        
					return "a function\n";
		}

		function afunc15($a) {        
					return "a function\n";
		}

		function afunc16($a) {        
					return "a function\n";
		}

		function afunc17($a) {        
					return "a function\n";
		}

		function afunc18($a) {        
					return "a function\n";
		}

		function afunc19($a) {        
					return "a function\n";
		}

		function afunc20($a) {        
					return "a function\n";
		}

		function afunc21($a) {        
					return "a function\n";
		}
		
	}

	function sieve($n) {
	  $count = 0;
	  while ($n-- > 0) {
		$count = 0;
		$flags = range (0,8192);
		for ($i=2; $i<8193; $i++) {
		  if ($flags[$i] > 0) {
		for ($k=$i+$i; $k <= 8192; $k+=$i) {
		  $flags[$k] = 0;
		}
		$count++;
		  }
		}
	  }
	  //print "Count: $count\n";
	}

	function mystrcat($n) {
	  $str = "";
	  while ($n-- > 0) {
		$str .= "hello\n";
		$str = $str . "goodbye" . "hello\n";
	  }
	  $len = strlen($str);
	  //print "$len\n";
	}
	
	function nestedloop($n) {
		$x = 0;
		for ($a=0; $a<$n; $a++)
		  for ($b=0; $b<$n; $b++)
			for ($f=0; $f<$n; $f++)
			  $x++;
		//print "$x\n";
	}
	
	class Start
	{
		static function Main()
		{
			for ($k = 1; $k <= 3; $k++)
			{
				echo "Benchmark #$k\n";
				echo "============\n";
				
				self::ackermann();
				self::ary();
				self::fibo();
				self::stak();
				self::hash2();
				self::heapsort();
				self::numtest();
				self::objects();
				self::random();
				self::sieve();
				self::mystrcat();
				self::nestedloop();
			}
			
			Timing::OutputResults();
		}

		static function ackermann() {
			Timing::Start("ackermann.php");
			/*print "Ack(3,7): " .*/ Ack(3,7) /*. "\n"*/;
			Timing::Stop();
		}
		
		static function ary() {
			Timing::Start("ary.php");
			ary(100000);
			Timing::Stop();
			
			Timing::Start("ary2.php");
			ary2(100000);
			Timing::Stop();
			
			Timing::Start("ary3.php");
			ary3(1000);
			Timing::Stop();
		}
		
		static function fibo() {
			Timing::Start("fibo.php");
			fibo(30);
			Timing::Stop();
		}
		
		static function stak() {
			Timing::Start("gabriel-stak.php");
			stak(18, 12, 6);
			Timing::Stop();
			
			Timing::Start("gabriel-tak.php");
			tak(18, 12, 6);
			Timing::Stop();
		}
		
		static function hash2() {
			Timing::Start("hash2.php");
			hash2(1000);
			Timing::Stop();
		}
		
		static function heapsort() {
			$N = /*($argc == 2) ? $argv[1] : */10000;
			for ($i=1; $i<=$N; $i++)
				$ary[$i] = Random::gen_random(1);
			
			Timing::Start("heapsort.php");
			heapsort($N,$ary);
			Timing::Stop();
		}
		
		static function numtest() {
			Timing::Start("numtest.php");
			numtest();
			Timing::Stop();
		}
		
		static function objects() {
			$numObjs=5000;

			Timing::Start("objects.php");
			
			// creation of objects
			for ($i=0; $i<$numObjs; $i++) {
				$large[$i] = new largeobject();
				$large[$i]->small[$i] = new smallobject(); 
			}
			
			// calling methods
			for ($i=0; $i<$numObjs; $i++) {
				$large[$i]->afunc11('hi');
				$large[$i]->afunc12('hi');
				$large[$i]->afunc13('hi');
				$large[$i]->afunc14('hi');
				$large[$i]->afunc15('hi');
				$large[$i]->afunc16('hi');
				$large[$i]->afunc17('hi');
				$large[$i]->afunc18('hi');
				$large[$i]->afunc19('hi');
				$large[$i]->afunc20('hi');
				$large[$i]->afunc21('hi');
			}

			// property access
			for ($i=0; $i<$numObjs; $i++) {
				for ($t=1; $t<=40; $t++) {
					$somevar = 'var'.$t;
					$a = $large[$i]->$somevar;
					$b = $large[$i]->small[$i]->avar;
				}
			}

			Timing::Stop();
		}
		
		static function random() {
			$N = 1000000;
			
			Timing::Start("random.php");
			while ($N--) {
				Random::gen_random(100.0);
			}
			Timing::Stop();
		}
		
		static function sieve() {
			Timing::Start("sieve.php");
			sieve(18);
			Timing::Stop();
		}
		
		static function mystrcat() {
		
			$a[] = 'zot';
			$a[] = 'zotzot';
			$a[] = 'zotzotzot';
			$a[] = 'zotzotzotzot';
			$a[] = 'zotzotzotzotzot';
			$a[] = 'zotzotzotzotzotzot';
			$a[] = 'zotzotzotzotzotzotzot';
			$a[] = 'zotzotzotzotzotzotzotzot';
			$a[] = 'zotzotzotzotzotzotzotzotzot';
		
			$b = NULL;
			
			Timing::Start("mystrcat.php");

			mystrcat(10000);
			
			for ($i=0; $i<1000; $i++) {
				foreach ($a as $val) {
					$b .= $val;
				}
			}

			Timing::Stop();
		}
		
		static function nestedloop() {
			Timing::Start("nestedloop.php");
			nestedloop(100);
			Timing::Stop();
		}
	}
?>
