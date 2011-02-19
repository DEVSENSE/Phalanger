<?php

	class A
	{
		protected $x;
		
		function __construct($a)
		{
			$this->x = $a;
			echo "Hello! I'm a class A and I am constructed just now.\n";
		}
		
		function foo($x)
		{
			$this->x = $x;
			
			echo "Class A: I have just stored $x passed as parameter to my protected field \$x.\n";
		}
		
		function write()
		{
			echo "Protected \$x field: {$this->x}\n";
		}
	}
	

	class B extends A
	{
		function foo($x)
		{
			$this->x = "bar";
			
			echo "Class B: My overridden method foo has been called. I have stored what I like to \$x private field.\n";
		}
	}

?>