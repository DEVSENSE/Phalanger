[expect php]
[file]
<?php
require('Phalanger.inc');

class Caller {
	public $x = array(1, 2, 3);
	
	function __call($m, $a) {
		echo "Method $m called:\n";
		__var_dump($a);
		return $this->x;
	}
}

$foo = new Caller();
$a = $foo->test(1, '2', 3.4, true);
__var_dump($a);

?>