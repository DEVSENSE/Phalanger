[expect php]

[file]
<?php

date_default_timezone_set("Europe/Prague");

echo @strtotime("1999-11-40"), "\n";
echo strtotime(""), "\n";
echo strtotime("01-JAN-70"), "\n";

$str = 'Not Good';
if (($timestamp = @strtotime($str)) === -1) {
   echo "The string ($str) is bogus";
} else {
   echo "$str == " . date('l dS of F Y h:i:s A', $timestamp);
}

?>