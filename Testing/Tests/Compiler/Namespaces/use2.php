[expect php]
[file]
<?php

	namespace
	{
		class X
		{
			static function foo(){ echo __METHOD__; }
		}
		
		X::foo();
		\A\X::foo();
		\B\X::foo();
	}	

	namespace A
	{
		class X
		{
			static function foo(){ echo __METHOD__; }
		}
		
		X::foo();
		\A\X::foo();
		\B\X::foo();
	}
	
	namespace B
	{
		class X
		{
			static function foo(){ echo __METHOD__; }
		}
		
		X::foo();
		\A\X::foo();
		\B\X::foo();
		
		use A\X as M;
		M::foo();
	}
	
?>