[expect php]
[file]

<?php
include 'fs.inc';
$FileName = "touch.txt";

date_default_timezone_set("Europe/Prague");

if (touch($FileName)) {
    echo "'$FileName' modification time changed";
} else {
    echo "Sorry Could Not change modification time of $FileName";
}

touch("touch.txt", mktime(05, 30, 30, 03, 28, 1982), mktime(17, 17, 17, 04, 17, 2004));

touch("winter.txt", mktime(23, 59, 59, 12, 31, 2011));
touch("summer.txt", mktime(03, 04, 05, 06, 07, 2008));


$fmt = 'F j, Y; [H:i:s] (H\h)';

D(array(
  date($fmt, $t = filectime("test.txt")) . " ctime test.txt ($t)"
  ,date($fmt, $t = filemtime("test.txt")) . " mtime test.txt ($t)"

  ,date($fmt, $t = filectime("touch.txt")) . " ctime touch.txt ($t)"
  ,date($fmt, $t = filemtime("touch.txt")) . " mtime touch.txt ($t)"
  ,date($fmt, $t = fileatime("touch.txt")) . " atime touch.txt ($t)"

  ,date($fmt, $t = filemtime("summer.txt")) . " mtime summer.txt ($t)"
  ,date($fmt, $t = filemtime("winter.txt")) . " mtime winter.txt ($t)"
));
// Note: do not use 'atime' - is is not immutable

?> 