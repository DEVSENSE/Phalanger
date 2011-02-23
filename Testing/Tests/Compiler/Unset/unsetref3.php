[expect php]
[file]
<?php

class Test {
	public $x = 'Init';
	
	function ithastowork() {
		// Reference to field x
		$ref = &$this->x;

		// Destroys x, but the values still exists and it's pointed by ref
		unset($this->x);
		
		// Returns $arr[1] destroyed
		print_r($ref);
		//die(); 
	}
}

// Reproduce BUG

$test = new Test();
$test->ithastowork();

$test->x = 'Init';





?>