[expect php]
[file]
<?php

class Y
{
	var $objects;
	
	function __construct(){
	  print "Construyendo Y\n";
	  $this->objects = 7;
	}
	
   function __destruct() {
       print "Destruyendo Y\n";
   }
}

class X {

var $inner_stuff;

var $objects = 'Init Value';

	function __construct(){
	 $this->inner_stuff[] = new Y();
	}

	function foo() 
	{
		echo "1";
		$this->inner_stuff[0]->objects  = & $this->objects;
		
		echo "2";
		$this->inner_stuff[0] = 23;// the reference is lost
	
		echo "3";	
		//deep copy occurs without reference - so deep copy copies everything
		$checkpoint = (array)$this;
		
		//since $this->object isn't longer referenced $chekpoint should be affected by this change
		$this->objects = 54;
		
		var_dump($this);
		var_dump($checkpoint);
		
		
	}

}

$x = new X();

echo "call foo";

$x->foo();

$checkpoint = (array)$x;

$x->objects = 1004;

echo "x: ";
var_dump($x);
echo "<br/>checkpoint ";
var_dump($checkpoint);


?>