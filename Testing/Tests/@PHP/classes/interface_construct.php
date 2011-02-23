[expect php]
[file]
<?php
error_reporting(4095);

interface test {
	// originally:
	// (will not work in PHP 5.2 anyway)
	//public function __construct($foo);
	
	// changed to:
	public function __construct();
}

class foo implements test {
	public function __construct() {
		echo "foo\n";
	}
}

$foo = new foo;

?>