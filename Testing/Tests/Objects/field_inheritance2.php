[expect php]
[file]
<?php

class A {
	
	private $a = 0;
	protected $b = 0;
	public $c = 0;
	
	function foo()
	{
		var_dump($this->a, $this->b, $this->c);
	}
	
	function bar()
	{
		echo __METHOD__ . "\n";
		var_dump($this->a, $this->b, $this->c);
	}
 }

 class B extends A {
 
	protected $a = 1;
	protected $b = 1;
	//private $c = 1;
	
	function foo()
	{
		var_dump($this->a, $this->b, $this->c);
	}
 }
  
$x = new A;
$x->foo();

$x = new B;
$x->foo();
$x->bar();

?>