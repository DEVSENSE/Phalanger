[expect php]
[file]
<?php
include('Phalanger.inc');
error_reporting(E_ALL);
class obj {
	function method() {}
}

$o->root=new obj();

ob_start();
__var_dump($o);
$x=ob_get_contents();
ob_end_clean();

$o->root->method();

ob_start();
__var_dump($o);
$y=ob_get_contents();
ob_end_clean();
if ($x == $y) {
    print "success";
} else {
    print "failure
x=$x
y=$y
";
}
?>
