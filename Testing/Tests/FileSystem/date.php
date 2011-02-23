[expect php]
[file]

<?php
include 'fs.inc';

date_default_timezone_set("Europe/Prague");

// Dates relative to: March 10th, 2001, 5:16:18 pm
$dt = mktime(17, 16, 18, 3, 10, 2001) ; // [int hour [, int minute [, int second [, int month [, int day [, int year [, int is_dst]]]]]]])

$res = array();
$fmt = array_merge(range('a','z'), range('A','Z'));
sort($fmt);

foreach ($fmt as $s)
{
  // skips 'T' as it outputs different time zone name than Phalanger:
  if ($s != 'T')
    $res[] = "$s --> {" .date($s, $dt). "}";
}

D($res);

?>  
 

