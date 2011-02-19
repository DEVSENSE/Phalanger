[expect php]
[file]
<?php
	putenv('TZ='); // clean TZ so that it doesn't bypass the ini option
	ini_set("date.timezone","US/Hawaii");
	echo strtotime("2005-06-18 22:15:44");
?>