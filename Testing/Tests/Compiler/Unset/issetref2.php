[expect php]
[file]
<?php

class Test {
	public $x = 'Init';
	
	function doBug() {
		echo "doBug:\n";
		// Reference to field x
		$ref = &$this->x;

		// Destroy reference (shouldn't touch $x)
		unset($ref);

		var_dump(isset($ref));
		
		// Returns $arr[1] destroyed
		print_r($this);
		//die(); 
	}
	
	function ithastowork() {
		echo "ithastowork:\n";
		// Reference to field x
		$ref = &$this->x;

		// Destroys x, but the values still exists and it's pointed by ref
		unset($this->x);

		var_dump(isset($this->x));
		
		// Returns $arr[1] destroyed
		print_r($this);
		print_r($ref);
		//die(); 
	}
}

function alsoBug()
{	
	global $test;
	echo "\nalsobug:\n";
	// Reference to field x
	$ref = &$test->x;

	// Destroy reference (shouldn't touch $x)
	unset($ref);

	var_dump(isset($ref));
	
	// Returns $arr[1] destroyed
	print_r($test);
	//die(); 

}

// Reproduce BUG

$test = new Test();
$test->doBug();

$test->x = 'Init';

$test->ithastowork();

$test->x = 'Init';

alsoBug();


$test->x = 'Init';

//this works
echo "this works:\n";

// Reference to field x
$ref = &$test->x;

// Destroy reference (shouldn't touch $x)
unset($ref);

var_dump(isset($ref));

// Returns $arr[1] destroyed
print_r($test);

?>