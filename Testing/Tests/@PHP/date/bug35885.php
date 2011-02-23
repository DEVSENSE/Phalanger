[expect php]
[file]
<?php

date_default_timezone_set("UTC");

$ts = date(DATE_ISO8601, strtotime('NOW'));
$ts2 = date(DATE_ISO8601, time());

echo ($ts == $ts2) ? "T" : "F";

?>