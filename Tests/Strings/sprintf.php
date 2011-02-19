[expect php]
[file]

<?php

date_default_timezone_set("Europe/Prague");

$isodate = sprintf("%04d-%02d-%02d", date('Y'), date('m'), date('d'));
echo "DATE: [$isodate]\n"

?>  
 
Example 2. sprintf(): formatting currency

<?php
$money1 = 68.75;
$money2 = 54.35;
$money = $money1 + $money2;
 echo $money . "\n"; // will output "123.1";
$formatted = sprintf("%01.2f", $money);
 echo $formatted . "\n"; // will output "123.10"
?>  
 
