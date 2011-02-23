[expect] Array to string conversion

[expect]
array
{
  [3] => string(3) "1.5"
  [6] => string(5) "58.54"
}

[file]
<?php
$array = array("hello", null, 12, 1.5, array(1.5, 3, "foo"), "bar", 58.54);

// return all array elements containing floating point numbers
$fl_array = preg_grep("/^(\d+)?\.\d+$/", $array);

var_dump($fl_array);

?> 