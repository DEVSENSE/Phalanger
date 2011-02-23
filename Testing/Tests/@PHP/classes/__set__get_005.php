[expect php]
[file]
<?php
require('Phalanger.inc');
class Test
{
	protected $x;

	function __get($name) {
		echo __METHOD__ . "\n";
		if (isset($this->x[$name])) {
			return $this->x[$name];
		} 
		else
		{
			return NULL;
		}
	}

	function __set($name, $val) {
		echo __METHOD__ . "\n";
		$this->x[$name] = $val;
	}
}

class AutoGen
{
	protected $x;

	function __get($name) {
		echo __METHOD__ . "\n";
		if (!isset($this->x[$name])) {
			$this->x[$name] = new Test();
		}
		return $this->x[$name];
	}

	function __set($name, $val) {
		echo __METHOD__ . "\n";
		$this->x[$name] = $val;
	}
}

$foo = new AutoGen();
$foo->bar->baz = "Check";

__var_dump($foo->bar);
__var_dump($foo->bar->baz);

?>