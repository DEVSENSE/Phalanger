[expect exact]
array
(
  [1] => January
  [2] => February
  [3] => March
)

[file]
<?php
$firstquarter  = array(1 => 'January', 'February', 'March');
print_r($firstquarter);
?>
