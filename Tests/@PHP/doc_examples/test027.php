[expect php]

[file]
<?php

function f($code,$message)
{
  echo "ERROR: $code\n";
}

error_reporting(E_ALL);
set_error_handler("f");

$array = array(1, 2);
$count = count($array);
for ($i = 0; $i < $count; $i++) {
    echo "\nChecking $i: \n";
    
    echo "Bad: ";
    echo $array['$i'];
    echo "\n";
    
    echo "Good: ";
    echo $array[$i];
    echo "\n";
    
    echo "Bad: ";
    echo "{$array['$i']}";
    echo "\n";
    
    echo "Good: ";
    echo "{$array[$i]}";
    echo "\n";
}

?>
