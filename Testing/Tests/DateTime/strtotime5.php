[expect php]
[file]
<?
date_default_timezone_set("UTC");
echo strtotime("2000-10-10T10:12:30.000"),"\n";

echo strtotime('5 january 2006+3day+1day'),"\n";
echo strtotime('5 january 2006+3day +1day'),"\n";
echo strtotime('5 january 2006 +3 day +1 day'),"\n";
echo strtotime('5 january 2006+3 day+1 month'),"\n";

echo date('D', strtotime('monday')),"\n";
echo date('D', strtotime('mon')),"\n";
echo date('D', strtotime('tue')),"\n";
echo date('D', strtotime('wed')),"\n";
echo date('D', strtotime('thu')),"\n";
echo date('D', strtotime('fri')),"\n";
echo date('D', strtotime('sat')),"\n";
echo date('D', strtotime('sun')),"\n";

echo strtotime("11/20/2005 8:00 AM\r\n"),"\n";
?>