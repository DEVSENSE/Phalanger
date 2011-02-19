[expect] 1256
[expect] Notice
[expect] Notice
[expect] Notice

[file]
<?php
$arr = array(5 => 1, 12 => 2);

$arr[] = 56;    // This is the same as $arr[13] = 56;
                // at this point of the script

$arr["x"] = 42; // This adds a new element to
                // the array with key "x"

echo $arr[5];
echo $arr[12];
echo $arr[13];
echo $arr[14];

                
unset($arr[5]); // This removes the element from the array

echo $arr[5];

unset($arr);    // This deletes the whole array

echo $arr;

?>
