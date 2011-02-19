[expect php]
[file]
<?php
include('Phalanger.inc');
date_default_timezone_set("UTC");

__var_dump(date_default_timezone_get());
$a = gettimeofday();
unset($a["sec"]);
unset($a["usec"]);
__var_dump($a);
?>
