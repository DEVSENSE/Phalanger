[expect]
OK
[file]
<?
	class A
	{
	}
	
	interface I
	{
		function f(A $a, $b);
		static function g(A $a, $b);
	}
	
	class C implements I
	{
		function f(A $a, $b) { }
		static function g(A $a, $b) { }
	}
	
	abstract class D implements I
	{
		abstract function f(A $a, $b);
		abstract static function g(A $a, $b);
	}
	
	echo "OK";
?>