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
 
	static public $a = 1;
	static public $b = 1;
	static public $c = 1;
 
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