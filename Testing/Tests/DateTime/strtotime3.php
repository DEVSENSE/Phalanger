[expect php]

[file]
<?php
date_default_timezone_set("Europe/Prague");
$futureString = "+1 month";
$checkMonth = "06";
$checkDay = 1;
$year = "2005";
$m = mktime(0, 0, 0, $checkMonth, $checkDay, $year);
$futureTimeStamp = strtotime($futureString, $m);
$futureDateArray = getdate($futureTimeStamp);
echo "$futureString\n$checkMonth\n$checkDay\n$year\n$m\n$futureTimeStamp\n";
echo "{$futureDateArray["seconds"]}\n";
echo "{$futureDateArray["minutes"]}\n";
echo "{$futureDateArray["hours"]}\n";
echo "{$futureDateArray["mday"]}\n";
echo "{$futureDateArray["wday"]}\n";
echo "{$futureDateArray["mon"]}\n";
echo "{$futureDateArray["year"]}\n";
echo "{$futureDateArray["yday"]}\n";
echo "{$futureDateArray["weekday"]}\n";
echo "{$futureDateArray["month"]}\n";
echo "{$futureDateArray[0]}\n";

?>
