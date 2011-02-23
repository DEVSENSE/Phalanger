[expect php]
[file]
<?php
require('Phalanger.inc');
class test {
	public $p1 = 1;
	public $p2 = 2;
	public $p3;
};

$obj = new test;
$obj->p2 = 'A';
$obj->p3 = 'B';
$copy = clone $obj;
$copy->p3 = 'C';
echo "object\n";
__var_dump($obj);
echo "Clown\n";
__var_dump($copy);
echo "Done\n";
?>