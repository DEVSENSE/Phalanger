[expect php]
[file]
<?php

class foo
{
	function bar()
	{
		$args = func_get_args();
		
		foreach($args as $arg)
		{
			echo $arg;
		}		
		
	}
}

$x = new foo();
$x->bar('ahoj',13);


?>