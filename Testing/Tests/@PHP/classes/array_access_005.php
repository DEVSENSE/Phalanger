[expect php]
[file]
<?php 

require('Phalanger.inc');

class Peoples implements ArrayAccess {
	public $person;
	
	function __construct() {
		$this->person = array(array('name'=>'Joe'));
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
$people->person[0]['name'] = $people->person[0]['name'] . 'Foo';
__var_dump($people->person[0]['name']);
$people->person[0]['name'] .= 'Bar';
__var_dump($people->person[0]['name']);

echo "---ArrayOverloading---\n";

$people = new Peoples;

__var_dump($people[0]);
__var_dump($people[0]['name']);
// PHP disability: __var_dump($people->person[0]['name'] . 'Foo'); // impossible to assign this since we don't return references here
$x = $people[0]; // creates a copy
// PHP disability: $x['name'] .= 'Foo';
$people[0] = $x;
__var_dump($people[0]);
// PHP disability: $people[0]['name'] = 'JoeFoo';
__var_dump($people[0]['name']);
// PHP disability: $people[0]['name'] = 'JoeFooBar';
__var_dump($people[0]['name']);

?>