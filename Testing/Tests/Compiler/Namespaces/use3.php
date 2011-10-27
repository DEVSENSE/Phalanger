[expect php]
[file]
<?php

	namespace A
	{
		use A as F;

		class X
		{
			static function foo()
			{
				$x = new F\X;
				$x->bar();
			}
			
			function bar()
			{
				echo __METHOD__;
			}
		}

		X::foo();
	}
	
?>