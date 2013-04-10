--TEST--
DateTime: Skip invalid date/time.
--SKIPIF--
<?php if (!function_exists('date_create')) die('skip');?>
--FILE--
<?php 
date_default_timezone_set("Europe/Amsterdam");
$ts = date_create("Sun Mar 27 01:59:59 2005");
echo date_format($ts, DateTime::RFC822), "\n";
$ts->modify("+1 second");
echo date_format($ts, DateTime::RFC822), "\n";
?>
===DONE===
--EXPECTF--
Sun, 27 Mar 05 01:59:59 +0100
Sun, 27 Mar 05 03:00:00 +0200
===DONE===
