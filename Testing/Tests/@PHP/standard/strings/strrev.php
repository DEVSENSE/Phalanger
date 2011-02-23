[expect php]
[file]
<?php
  require('Phalanger.inc');
	$i = 0;
	$str = '';

	while ($i<256) {
		$str .= chr($i++);
	}
	
	__var_dump(md5(strrev($str)));
	__var_dump(strrev(NULL));
	__var_dump(strrev(""));
?>