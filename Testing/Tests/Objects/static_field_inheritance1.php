[expect php]
[file]
<?php

class A {
	
	static private $a = 0;
	static protected $b = 0;
	static public $c = 0;
	
	static function foo()
	{
		var_dump( self::$a );
		var_dump( self::$b );
		var_dump( self::$c );
	}
 }

 class B extends A {
 
	static private $a = 1;
	//static private $b = 1;
	//static private $c = 1;	// err
 
	static function foo()
	{
		var_dump( self::$a );
		var_dump( self::$b );
		var_dump( self::$c );
	}
 }
 
A::foo();
B::foo();

?>