[expect php]
[file]
<?php

function printme($a)
{
foreach ($a as $k => $v) echo "[$k] => $v\n";
}

printme(scandir('.'));
printme(scandir('./'));
printme(scandir('.\\'));
printme(scandir('C:\\'));

?>