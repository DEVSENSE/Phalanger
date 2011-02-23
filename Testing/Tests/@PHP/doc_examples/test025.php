[expect exact]
array
(
  [0] => 1
  [1] => 2
  [2] => 3
  [3] => 4
  [4] => 5
)
array [empty]
array
(
  [5] => 6
)
array
(
  [0] => 6
  [1] => 7
)

[file]
<?php
// Create a simple array.
$array = array(1, 2, 3, 4, 5);
print_r($array);

// Now delete every item, but leave the array itself intact:
foreach ($array as $i => $value) {
    unset($array[$i]);
}
print_r($array);

// Append an item (note that the new key is 5, instead of 0 as you
// might expect).
$array[] = 6;
print_r($array);

// Re-index:
$array = array_values($array);
$array[] = 7;
print_r($array);
?>
