[expect php]
[file]
<?php

	namespace A
	{
		class X{ static function foo(){echo __METHOD__;} }
		class Y{ static function foo(){echo __METHOD__;} }
		
		eval('use A\Y as X; X::foo();');
		X::foo();
	}
	
?>