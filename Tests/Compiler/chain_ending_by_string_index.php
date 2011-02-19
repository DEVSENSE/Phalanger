[expect php]
[file]
<?

$a = array("");
$a[0][0] = "f";

var_dump($a);

$a = array("bubu");
@$a[0][0] = "xyz";

var_dump($a);

class C { public $f; }

$c = new C;
$c->f = "kuku";
$c->f[2] = "x";

var_dump($c);

class B { static $f; }

B::$f = "hello";
B::$f[3] = "x";
var_dump(B::$f);

eval('class A { static $f; }');

A::$f = "hello";
A::$f[3] = "x";
var_dump(A::$f);

?>