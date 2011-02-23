[expect php]
[file]
<?php
	include('Phalanger.inc');

	function f(&$x)
	{
	echo "f($x)\n";
	$x = 10;
	}
	function g1()
	{
	$a = 1;
	$b = 2;
	$c = 3;

	f($a = 'x');
	u($a = 'x');

	__var_dump($a,$b,$c);
	}
	function g2()
	{
	$a = "b";
	$b = 2;
	$c = 3;

	f($$a = $b = 'x');
	u($$a = $b = 'x');

	__var_dump($a,$b,$c);
	}
	function g3()
	{
	$a = "b";
	$b = 2;
	$c = 3;

	f($$a = $b = 'x');
	u($$a = $b = 'x');

	__var_dump($a,$b,$c);
	}

	function g4()
	{
	$a = "b";
	$b = 2;
	$c = 3;

	f($$a = $b = 'x');
	u($$a = $b = 'x');

	__var_dump($a,$b,$c);
	eval(";");
	}
	function g5()
	{
	$a = 1;
	$b = 2;
	$c = 3;

	f($a[1][2] = $b = 'x');
	u($a[1][2] = $b = 'x');

	__var_dump($a,$b,$c);
	}
	function g6()
	{
	$a = 1;
	$b = 2;
	$c = 3;

	f($a = $b =& $c);
	u($a = $b =& $c);

	__var_dump($a,$b,$c);
	}
	function g7()
	{
	$a = 1;
	$b = 2;
	  
	f($a = $a =& $b);
	u($a = $a =& $b);
	  
	__var_dump($a,$b);
	}
	function g8()
	{
	class A { static $x; }
	 
	f($x->x[1][2]->a[1][2] = 1);
	f(A::$x[1] = 1);
	f(A::$x = 1);
	  
	u($x->x[1][2]->a[1][2] = 1);
	u(A::$x[1] = 1);
	u(A::$x = 1);
	}
	class X
	{
	private $q = 0;
	  
	function __get($field)
	{
		echo "__get($field) = $this->q\n";
		return $this->q;
	}
	  
	function __set($field,$value)
	{
		echo "__set($field,$value)\n";
		$this->q = $value;
	}
	}

	function g9()
	{
	$x = new X;
	echo "known:\n";
	f($x->p += 1);
	echo "unknown:\n";
	u($x->p += 2);
	}

	function g10()
	{
	$x = false;
	if ($x)
	{
		u($x[1][2]->f()->a[3][4][5]);
		u($x[1][2]->f()->a[3][4][5]+=$x[1][0]->f($a[]=$a[1])->a[3][4][5] *= $f->a->a->a->a);
		u($a->x[$a->u($x->x)->q += ${u($x += 1)}->f(${u($a->u($x->x)->q += ${u($x += 1)}->f(${u($a = 1)} =& $z)->x[1] = 1)} = 1)] *= $q =& $r);
	}  
	}

	$x = true;
	if ($x) { function u($x) { echo "u($x)\n"; } }

	for($i=1;$i<=10;$i++)
	{
	echo "\ng$i:\n";
	$g = "g$i";
	@$g();
	}

	echo "Done.";
?>
