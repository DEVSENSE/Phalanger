[expect php]
[file]
<?php 

require('Phalanger.inc');
error_reporting(E_ALL & ~E_NOTICE);
// NOTE: This will become part of SPL

class ArrayReferenceProxy implements ArrayAccess
{
	private $object;
	private $element;
	
	function __construct(ArrayAccess $object, array &$element)
	{
		echo __METHOD__ . "(array)\n";
		$this->object = $object;
		$this->element = &$element;
	}

	function offsetExists($index) {
		echo __METHOD__ . "(".gettype($this->element).", $index)\n";
		return array_key_exists($index, $this->element);
	}

	function offsetGet($index) {
		echo __METHOD__ . "(".gettype($this->element).", $index)\n";
		return isset($this->element[$index]) ? $this->element[$index] : NULL;
	}

	function offsetSet($index, $value) {
		echo __METHOD__ . "(".gettype($this->element).", $index, $value)\n";
		$this->element[$index] = $value;
	}

	function offsetUnset($index) {
		echo __METHOD__ . "(".gettype($this->element).", $index)\n";
		unset($this->element[$index]);
	}
}

class Peoples implements ArrayAccess
{
	public $person;
	
	function __construct()
	{
		$this->person = array(array('name'=>'Foo'));
	}

	function offsetExists($index)
	{
		return array_key_exists($index, $this->person);
	}

	function offsetGet($index)
	{
		return new ArrayReferenceProxy($this, $this->person[$index]);
	}

	function offsetSet($index, $value)
	{
		$this->person[$index] = $value;
	}

	function offsetUnset($index)
	{
		unset($this->person[$index]);
	}
}

$people = new Peoples;

__var_dump($people->person[0]['name']);
$people->person[0]['name'] = $people->person[0]['name'] . 'Bar';
__var_dump($people->person[0]['name']);
$people->person[0]['name'] .= 'Baz';
__var_dump($people->person[0]['name']);

echo "===ArrayOverloading===\n";

$people = new Peoples;

__var_dump($people[0]);
__var_dump($people[0]['name']);
$people[0]['name'] = 'FooBar';
__var_dump($people[0]['name']);
$people[0]['name'] = $people->person[0]['name'] . 'Bar';
__var_dump($people[0]['name']);
// Phalanger invokes one more ctor: $people[0]['name'] .= 'Baz';
__var_dump($people[0]['name']);
unset($people[0]['name']);
__var_dump($people[0]);
__var_dump($people[0]['name']);
$people[0]['name'] = 'BlaBla';
__var_dump($people[0]['name']);

?>