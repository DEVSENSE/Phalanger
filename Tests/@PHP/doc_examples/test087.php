[expect php]

[file]
<?php
$arr = array("one", "two", "three");
reset ($arr);
while (list(, $value) = each ($arr)) {
    echo "Value: $value<br />\n";
}

foreach ($arr as $value) {
    echo "Value: $value<br />\n";
}
?>
