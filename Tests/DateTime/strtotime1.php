[expect php]

[file]
<?php
date_default_timezone_set("Europe/Prague");
echo " 1: ",strtotime("30 September 2000"), "\n";
echo " 2: ",strtotime("30 September 2000 +1 day"), "\n";
echo " 3: ",strtotime("30 September 2000 +1 week"), "\n";
echo " 4: ",strtotime("30 September 2000 +1 week 2 days 4 hours 2 seconds"), "\n";
echo " 5: ",strtotime("next Thursday"), "\n";
echo " 6: ",strtotime("last Monday"), "\n";
echo " 7: ",strtotime("2004-12-31"), "\n";
echo " 8: ",strtotime("2005-04-15"), "\n";
echo " 9: ",strtotime("last Wednesday"), "\n";
echo "10: ",strtotime("04/05/2005"), "\n";
echo "11: ",strtotime("1 September 2000 -1 week"), "\n";
echo "12: ",strtotime("Thu, 31 Jul 2003 13:02:39 -0700"), "\n";
echo "13: ",strtotime("today 00:00:00"), "\n";
echo "14: ",strtotime("last Friday"), "\n";
echo "15: ",strtotime("2004-12-01"), "\n";
echo "16: ",strtotime("1 September 2000 - 1week"), "\n";
echo "17: ",strtotime("1 September 2000 +10 seconds"), "\n";
echo "18: ",strtotime("2004-04-04 02:00:00 GMT"), "\n";
echo "19: ",strtotime("2004-04-04 01:59:59 UTC"), "\n";
echo "20: ",strtotime("2004-06-13 09:20:00.0"), "\n";
echo "21: ",strtotime("2004-04-04 02:00:00"), "\n";
echo "22: ",strtotime("last sunday 12:00:00"), "\n";
echo "23: ",strtotime("last sunday"), "\n";
echo "24: ",strtotime("01-jan-70 01:00"), "\n";
echo "25: ",strtotime("01-jan-70 02:00"), "\n";
echo "26: ",strtotime("next Monday", mktime(0,0,0,3,0, 2004));

?>