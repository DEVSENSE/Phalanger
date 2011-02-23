[expect php]
[file]
<?php

class X {
var $objects = 7;

var $objects2 = 22;

	function foo() 
	{
		$o = & $this->objects;
		
		$o = & $this->objects2;
		//reference to $this->objects should be death now
		//reference to $this->objects2 is created
		
		$checkpoint = (array)$this;
		
		$this->objects = 54;
		$this->objects2 = 222;
		
		var_dump($this);
		var_dump($checkpoint);
		
		// it's necesary to close $o and copied reference in $checkpoint
		
	}

}

$x = new X();

$x->foo();

$checkpoint = (array)$x;

$x->objects = 1004;
$x->objects2 = 777;

echo "x: ";
var_dump($x);
echo "<br/>checkpoint ";
var_dump($checkpoint);


?>