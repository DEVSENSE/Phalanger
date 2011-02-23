[expect php]
[file]
<?
include('Phalanger.inc');
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
		$par = "Set in B->f() method.\n";	
		return "ahoj";
	}	
}

class C
{
	function &f(&$par)
	{
		echo "Method C->f() return what it gets.\n";
		return $par;
	}	
}

$o = new A();
$o->a = new B();
$r =& $o->a->f($a);
__var_dump($o, $r, $a);

$o = new A();
$o->a = new C();
$a = "Set via \$a.\n";
$r =& $o->a->f($a);
__var_dump($o, $r, $a);
$r = "Set via \$r\n";
__var_dump($o, $r, $a);

?>