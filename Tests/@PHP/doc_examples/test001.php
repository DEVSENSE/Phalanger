[expect php]

[file]
<?php
for ($i = 0; $i < 2; $i++)
{
	if ($i) { 
?>
This is true.
<?php 
	} else { 
?>
This is false.
<?php
	}
}
?>
