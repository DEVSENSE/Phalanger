[expect php]
[file]
<?php 

require('Phalanger.inc');

class Peoples implements ArrayAccess {
	public $person;
	
	function __construct() {
		$this->person = array(array('name'=>'Foo'));
	}

	function offsetExists($index) {
		return array_key_exists($this->person, $index);
	}

	function offsetGet($index) {
		return $this->person[$index];
	}

	function offsetSet($index, $value) {
		$this->person[$index] = $value;
	}

	function offsetUnset($index) {
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

__var_dump($people[0]['name']);
// PHP disability: $people[0]['name'] = 'FooBar';
__var_dump($people[0]['name']);
// PHP disability: $people[0]['name'] = $people->person[0]['name'] . 'Bar';
__var_dump($people[0]['name']);
// PHP disability: $people[0]['name'] .= 'Baz';
__var_dump($people[0]['name']);

?>