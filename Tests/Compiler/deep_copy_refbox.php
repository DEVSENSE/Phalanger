[expect php]
[file]
<?
	include('Phalanger.inc');

	/*
	  
	Checks whether return values are deeply copied if they are passed further by reference and
	reference boxing takes place. 
	  
	*/
	class A
	{
		public static function f()
		{
		global $z;
		$x =& $z;
		return $x;
		}

		public function g()
		{
		global $z;
		$x =& $z;
		return $x;
		}
	}

	function f()
	{
		global $z;
		$x =& $z;
		return $x;
	}

	function g(&$x)
	{
		$x[] = 1;
	}

	$z = array(1);
	$a = new A;

	g(f());
	__var_dump($z);

	g(A::f());
	__var_dump($z);

	$g = "g";
	$g(A::f());
	__var_dump($z);

	g($a->g());
	__var_dump($z);
?>
