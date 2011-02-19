[expect php]
[file]
<?php
date_default_timezone_set('Europe/Oslo');
$d = array();
$d[] = strtotime("2005-07-14 22:30:41");
$d[] = strtotime("2005-07-14 22:30:41 GMT");

foreach($d as $date) {
	echo date(DATE_ISO8601, $date), "\n";
}
?>