[expect exact]
array
(
  [0] => BLUE
  [1] => RED
)

array
(
  [0] => BLUE
  [1] => RED
)

[file]
<?php
$colors = array("blue", "red");

foreach ($colors as $key => $color) {
    // won't work:
    //$color = strtoupper($color);
    
    // works:
    $colors[$key] = strtoupper($color);
}
print_r($colors);
?>

<?php
foreach ($colors as $key => $color) {
    // won't work:
    $color = strtoupper($color);
    
    // works:
    //$colors[$key] = strtoupper($color);
}
print_r($colors);
?>
