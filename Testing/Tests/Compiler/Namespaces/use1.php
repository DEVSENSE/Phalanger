[expect php]
[file]
<?php

	namespace
	{
		use A as X;

		class A
		{
			static function foo()
			{
				echo __METHOD__;
			}
		}

		X::foo();
	}

	namespace B
	{
		use A as X;
		X::foo();
	}
?>