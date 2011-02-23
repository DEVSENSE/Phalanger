[expect php]
[file]
<?php
	putenv("TZ=GMT");
	echo gmdate("Y-m-d H:i:s", strtotime("20 VI. 2005"));
?>
