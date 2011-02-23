[expect php]
[file]
<?
date_default_timezone_set("Europe/Prague");
$year = 2000;
$first_day = mktime(0,0,0,1,1,$year);
$a = getdate($first_day);

$i = 0;
for($day = -$a["wday"]; $day<366*5; $day++, $i++)
{
  $d = mktime(0,0,0,1,$day,$year);

  if ($i % 7 == 0)
  {
    echo "\n",strftime("%U/%W ",$d);
  }
  echo strftime("%d ",$d);
}
?>