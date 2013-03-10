[expectf]
Thu, 19 Aug 93 23:59:59 -1200
Sat, 21 Aug 93 00:00:00 +1200
Sun, 27 Mar 05 01:59:59 +0100
Sun, 27 Mar 05 03:00:00 +0200
Sun, 30 Oct 05 01:59:59 +0200
Sun, 30 Oct 05 03:00:00 +0100
[file]
<?php 
  if (!function_exists('date_create')) die("SKIP");

// .Net 4 seems to be unaware that Kwajalein clocks
// skipped Aug 20 as they went ahead by 24 hours.
// Also, the timezone on Aug 19 is -12, not +12
// as it changed from KWAT (UTC-12) to MHT (UTC+12).
// http://www.timeanddate.com/worldclock/clockchange.html?n=2243&year=1993
date_default_timezone_set("Pacific/Kwajalein");
$ts = date_create("Thu Aug 19 1993 23:59:59");
echo date_format($ts, DateTime::RFC822), "\n";
$ts->modify("+1 second");
echo date_format($ts, DateTime::RFC822), "\n";

date_default_timezone_set("Europe/Amsterdam");
$ts = date_create("Sun Mar 27 01:59:59 2005");
echo date_format($ts, DateTime::RFC822), "\n";
$ts->modify("+1 second");
echo date_format($ts, DateTime::RFC822), "\n";

$ts = date_create("Sun Oct 30 01:59:59 2005");
echo date_format($ts, DateTime::RFC822), "\n";
$ts->modify("+ 1 hour 1 second");
echo date_format($ts, DateTime::RFC822), "\n";
?>