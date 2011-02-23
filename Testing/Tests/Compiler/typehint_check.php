[expect exact] 
ahoj
hello
[file]
<?

class Class1
{
	public function print_hello()
	{
		echo "ahoj\n";
	}
}


interface I
{
	function foo(Class1 $x);
}


class A implements I
{
	public function foo(Class1 $z)
	{
		$z->print_hello();
	}
}

$x = new Class1;
$a = new A;

$a->foo($x);

class C implements J {}
interface J {}

function f(array $a,array &$b, C $c, C &$d, J $j)
{
  echo "hello\n";
}

$a = array();
f(array(1,2,3),$a,new C,new C, new C);

?>