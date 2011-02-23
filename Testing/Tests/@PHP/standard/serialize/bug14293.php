[expect php]
[file]
<?php
include('Phalanger.inc');

error_reporting(E_ALL & ~E_NOTICE);

class t
{
	function t()
	{
		$this->a = 'hello';
	}

	function __sleep()
	{
		echo "__sleep called\n";
		return array('a','b');
	}	
}

$t = new t();
$data = serialize($t);
echo "$data\n";
$t = unserialize($data);
__var_dump($t);

?>