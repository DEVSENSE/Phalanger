[expect php]

[file]
<?php
$arr1 = array(2, 3);
$arr2 = $arr1;
$arr2[1] = 4; // $arr2 is changed,
             // $arr1 is still array(2, 3)
echo "{$arr1[1]} {$arr2[1]}";
             
$arr3 = &$arr1;
$arr3[] = 4; // now $arr1 and $arr3 are the same

echo "{$arr1[2]} {$arr3[2]}";

?>
