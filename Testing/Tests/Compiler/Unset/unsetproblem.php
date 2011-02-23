[expect php]
[file]
<?php

// Initial array with initial values
class Test {
	public $arr = array(1 => 'InitialValue');

	function doBug() {
		// Reference to element '1'
		$ref = &$this->arr[1];

		// Destroy reference (shouldn't touch $arr[1])
		unset($ref);
	
		// Returns $arr[1] destroyed
		print_r($this->arr);
		//die(); 
	}
}

// Reproduce BUG

$test = new Test();
$test->doBug();


?>