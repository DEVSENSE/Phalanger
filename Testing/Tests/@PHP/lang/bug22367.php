[expect php]
[file]
<?php
require('Phalanger.inc');
error_reporting(0);

class foo
{
	public $test = array(0, 1, 2, 3, 4); 

	function a($arg) {
		__var_dump(array_key_exists($arg, $this->test));
		return $this->test[$arg];
	}

	function b() {
		@$this->c();

		$zero = $this->test[0];
		$one = $this->test[1];
		$two = $this->test[2];
		$three = $this->test[3];
		$four = $this->test[4];
		return array($zero, $one, $two, $three, $four);
	}

	function c() {
		return $this->a($this->d());
	}

	function d() {}
}

class bar extends foo
{
	public $i = 0;
	public $idx;

	function bar($idx) {
		$this->idx = $idx;
	}

	function &a($arg){
		return parent::a($arg);
	}
	function d(){
		return $this->idx;
	}
}

$a = new bar(5);
__var_dump($a->idx);
$a->c();
$b = $a->b();
__var_dump($b);
__var_dump($a->test);

$a = new bar(2);
__var_dump($a->idx);
@$a->c();
$b = $a->b();
__var_dump($b);
__var_dump($a->test);

?>