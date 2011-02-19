[expect php]
[file]
<?php
require('Phalanger.inc');

class base {
	protected $p1 = 'base:1';
	public $p2 = 'base:2';
	public $p3 = 'base:3';
	public $p4 = 'base:4';
	public $p5 = 'base:5';
	private $p6 = 'base:6';
	public function __clone() {
	}
};

class test extends base {
	public $p1 = 'test:1';
	public $p3 = 'test:3';
	public $p4 = 'test:4';
	public $p5 = 'test:5';
	public function __clone() {
		$this->p5 = 'clone:5';
	}
}

$obj = new test;
$obj->p4 = 'A';
$copy = clone $obj;
echo "object\n";
__var_dump($obj);
echo "Clown\n";
__var_dump($copy);
echo "Done\n";
?>