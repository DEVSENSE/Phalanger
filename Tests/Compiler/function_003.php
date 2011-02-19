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
		$par = "Set in B->f() method";	
	}	
}

$o = new A();
$o->a = new B();
$r =& $o->a->f($a);
__var_dump($o, $r, $a);
?>