[expect php]
[file]
<?
function key_compare_func($key1, $key2)
{
   if ($key1 == $key2)
       return 0;
   else if ($key1 > $key2)
       return 1;
   else
       return -1;
}

$array1 = array('blue'  => 1, 'red'  => 2, 'green'  => 3, 'purple' => 4);
$array2 = array('green' => 5, 'blue' => 6, 'yellow' => 7, 'cyan'  => 8);
var_dump(array_diff_key($array1, $array2));

$array1 = array(1 => 2,2 => 3);
$array2 = array("02" => 10);
var_dump(array_diff_key($array1, $array2));

$array1 = array('blue'  => 1, 'red'  => 2, 'green'  => 3, 'purple' => 4);
$array2 = array('green' => 5, 'blue' => 6, 'yellow' => 7, 'cyan'  => 8);

var_dump(array_diff_ukey($array1, $array2, 'key_compare_func'));

$array1 = array('blue'  => 1, 'red'  => 2, 'green'  => 3, 'purple' => 4);
$array2 = array('green' => 5, 'blue' => 6, 'yellow' => 7, 'cyan'  => 8);

var_dump(array_intersect_key($array1, $array2));

$array1 = array('blue'  => 1, 'red'  => 2, 'green'  => 3, 'purple' => 4);
$array2 = array('green' => 5, 'blue' => 6, 'yellow' => 7, 'cyan'  => 8);

var_dump(array_intersect_ukey($array1, $array2, 'key_compare_func'));
?>