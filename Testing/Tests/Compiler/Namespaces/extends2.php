[expect php]
[file]
<?php

	namespace
	{
		// dynamically created class, so anything what is extending \X is incomplete and has to be deferred
		eval('class X
		{
			static function foo()
			{
				echo __METHOD__;
			}
		}');
	}

	namespace A
	{
		class X extends \X
		{
			
		}

		X::foo();
	}
	
?>