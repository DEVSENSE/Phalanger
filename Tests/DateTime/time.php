[expect php]
[file]
<?
error_reporting(E_ALL); // no E_STRICT

$ltime = @localtime(1138476622, true);
echo mktime(0,0,0,0,0,$ltime['tm_year']),"\n";

date_default_timezone_set("UTC");
$utc_time1 = strtotime('1964-01-01 00:00:00 UTC');
$utc_time2 = strtotime('1963-12-31 00:00:00 UTC');
echo $utc_time1, ':', $utc_time2, " - ", $utc_time1 - $utc_time2, "\n";
echo date(DATE_ISO8601, $utc_time1), "\n";
echo date(DATE_ISO8601, $utc_time2), "\n";

echo date('Y-m-d', strtotime('1964-06-06')),"\n";
echo date('Y-m-d', strtotime('1963-06-06')),"\n";
echo date('Y-m-d', strtotime('1964-01-06')),"\n";
?>