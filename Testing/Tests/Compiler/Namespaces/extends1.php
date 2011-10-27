[expect php]
[file]
<?php

	namespace
	{
		class X
		{
			static function foo()
			{
				echo __METHOD__;
			}
		}
	}

	namespace A
	{
		class X extends \X
		{
			
		}

		X::foo();
	}
	
?>