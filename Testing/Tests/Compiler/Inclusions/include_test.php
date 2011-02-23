[expect php]
[file]

<?php
	$x = 0;
	if ($x == 0)
	{
		include "include_test_a.inc";
	}
	else
	{
		// just to have include_test_b.inc in the assembly (does it spoil the test?)
		include "include_test_b.inc";
	}
?>
