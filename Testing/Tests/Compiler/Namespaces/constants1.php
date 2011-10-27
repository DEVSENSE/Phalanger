[expect php]
[file]
<?php

	namespace
	{
		define("X", "1");
	}

	namespace A
	{
		define("A\X", "2");
		
		echo \X;
		echo X;
	}
	
	namespace B
	{
		echo X;
	}
	
?>