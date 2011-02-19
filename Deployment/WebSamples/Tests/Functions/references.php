[expect exact]
string(19) "Set in f() function"
object(A)(1)
{
  ["a"] => object(B)(0)
  {
  }
}
NULL
string(20) "Set in B->f() method"

[file]
<?
function f(&$par)
{
	$par = "Set in f() function";
}

f($a);
var_dump($a);

class A
{
	public $a;
}	

class B
{
	function f(&$par)
	{
		$par = "Set in B->f() method";	
	}	
}

$o = new A();
$o->a = new B();
$r =& $o->a->f($a);
var_dump($o, $r, $a);
?>