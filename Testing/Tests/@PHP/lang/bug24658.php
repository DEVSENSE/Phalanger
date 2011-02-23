[expect php]
[file]
<?php
require('Phalanger.inc');
error_reporting(0);

class foo {}
function no_typehint($a) {
	__var_dump($a);
}
function typehint(foo $a) {
	__var_dump($a);
}
function no_typehint_ref(&$a) {
	__var_dump($a);
}
function typehint_ref(foo &$a) {
	__var_dump($a);
}
$v = new foo();
$a = array(new foo(), 1, 2);
no_typehint($v);
typehint($v);
no_typehint_ref($v);
typehint_ref($v);
echo "===no_typehint===\n";
array_walk($a, 'no_typehint');
echo "===no_typehint_ref===\n";
array_walk($a, 'no_typehint_ref');
echo "===typehint===\n";
array_walk($a, 'typehint');
echo "===typehint_ref===\n";
?>