[expect php]
[file]
<?php

	function foo($a)
	{
		echo (is_a($a, "zzz"));
	}
	
	foo(null);

?>