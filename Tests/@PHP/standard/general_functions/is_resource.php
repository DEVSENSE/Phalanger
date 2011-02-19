[expect php]
[file]
<?php
require('Phalanger.inc');
	$f = fopen(__FILE__, 'r');
	fclose($f);
	__var_dump(is_resource($f));
?>