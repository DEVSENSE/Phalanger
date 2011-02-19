[expect php]
[file]
<?php
include('Phalanger.inc');
putenv('TZ=America/Montreal');

$time = mktime(1,1,1,1,1,2005);
foreach (array('B','d','h','H','i','I','L','m','s','t','U','w','W','y','Y','z','Z') as $v) {
	__var_dump(idate($v, $time));
}

?>