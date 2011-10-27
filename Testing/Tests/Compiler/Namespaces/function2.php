[expect php]
[file]
<?php

	namespace
	{
		function foo(){ echo __FUNCTION__; }
	}	

	namespace A
	{
		function foo(){ echo __FUNCTION__; }
		
		\foo();
	}
	
	namespace B
	{
		foo();
		\A\foo();
	}
	
?>