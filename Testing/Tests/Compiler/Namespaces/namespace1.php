[expect php]
[file]
<?php

	namespace N
	{
	
		class X
		{
			static function foo()
			{
				echo __NAMESPACE__;
			}
		} 
	}
	namespace
	{
		N\X::foo();
	}
?>