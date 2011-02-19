[expect php]
[file]
<?php

	interface I
	{
		function foo($a);		
	}

	function test(I $x)
	{
		$x->foo(0);
		$x->foo(0, 1);
		$x->foo(0, 1, 2);
	}
	
	class A implements I
	{
		function FOO($a, $b = 'B')
		{
			echo "\nA - ".$a.$b;
		}
	}
	
	class B extends A
	{
		function Foo($a, $b = 'B', $c = 'C')
		{
			echo "\nB - ".$a.$b.$c;
		}
	}
	
	class X
	{
		function fOO($a, $b = 'B', $c = 'C')
		{
			echo "\nX - ".$a.$b.$c;
		}
	}
	
	class C extends X implements I
	{
	
	}
	
	class D extends C
	{
		function fOO($a, $b = 'B', $c = 'C', $d = 'D')
		{
			echo "\nD - ".$a.$b.$c.$d;
		}
	}
	
	test(new A);
	test(new B);
	test(new C);
	test(new D);
	
?>