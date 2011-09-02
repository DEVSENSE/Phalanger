[expect exact]
ok
[file]
<?php

	$x = 0;
	if (!$x)
		if (true)
		{
			echo 'ok';
		}
		
?>