[expect php]
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
