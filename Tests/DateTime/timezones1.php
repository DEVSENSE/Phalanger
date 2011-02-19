[expect php]
[file]
<?
date_default_timezone_set("UTC");
echo date("H:i:s m/d/Y",$ts0 = strtotime("2000-10-10T10:12:30.000"))," = $ts0\n";
$lt = localtime($ts0,true);
echo "is_dst=",$lt["tm_isdst"],"\n";

date_default_timezone_set("Europe/Prague");
echo date("H:i:s m/d/Y",$ts1 = strtotime("2000-10-10T10:12:30.000"))," = $ts1\n";
$lt = localtime($ts1,true);
echo "is_dst=",$lt["tm_isdst"],"\n";

echo "\n";
echo $ts0 - $ts1,"\n";
?>