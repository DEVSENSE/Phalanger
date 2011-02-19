[expect php]
[file]
<?php

class X{

var $x = 'Init';

	function optimizedLocals() {
		echo "optimizedLocals:\n";
		// Reference to field x
		$ref = &$this->x;

		$indirectref = 'ref';
		
		// Destroys x, but the values still exists and it's pointed by ref
		unset($$indirectref);
		
		// Returns $arr[1] destroyed
		print_r($this);
		//die(); 
	}

}

$x = new X();
$x->optimizedLocals();
print_r($x);

$x->x = 'Init';

//this works

echo "this works:\n";
// Reference to field x
$ref = &$x->x;

$indirectref = 'ref';

// Destroys x, but the values still exists and it's pointed by ref
unset($$indirectref);

// Returns $arr[1] destroyed
print_r($x);
//die(); 

?>