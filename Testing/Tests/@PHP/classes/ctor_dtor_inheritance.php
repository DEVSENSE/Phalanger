[expect php]
[file]
<?php
include('Phalanger.inc');

// This test checks for:
// - inherited constructors/destructors are not called automatically
// - base classes know about derived properties in constructor/destructor
// - base class constructors/destructors know the instanciated class name

class base {
	public $name;

	function __construct() {
		echo __CLASS__ . "::" . __FUNCTION__ . "\n";
		$this->name = 'base';
		__var_dump($this);
	}
	
	function __destruct() {
		// Phalanger ND: echo __CLASS__ . "::" . __FUNCTION__ . "\n";
		//__var_dump($this);
	}
}

class derived extends base {
	public $other;

	function __construct() {
		$this->name = 'init';
		$this->other = 'other';
		__var_dump($this);
		parent::__construct();
		echo __CLASS__ . "::" . __FUNCTION__ . "\n";
		$this->name = 'derived';
		__var_dump($this);
	}

	function __destruct() {
		parent::__destruct();
		// Phalanger ND: echo __CLASS__ . "::" . __FUNCTION__ . "\n";
		// Phalanger ND: __var_dump($this);
	}
}

echo "Testing class base\n";
$t = new base();
unset($t);
echo "Testing class derived\n";
$t = new derived();
unset($t);

echo "Done\n";
?>
