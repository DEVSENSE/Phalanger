[expect php]
[file]
<?php
include('Phalanger.inc');
class test
{
	public $a, $b;

	function test()
	{
		$this->a = 7;
		$this->b = 2;
	}

	function __sleep()
	{
		$this->b = 0;
	}
}

$t['one'] = 'ABC';
$t['two'] = new test();

__var_dump($t);

$s =  @serialize($t);
echo $s . "\n";

__var_dump(unserialize($s));
?>