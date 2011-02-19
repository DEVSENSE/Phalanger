[expect php]

[file]
<?php
$arr = array("one", "two", "three");

echo "While - list, each\n";
reset($arr);
while (list($key, $value) = each ($arr)) {
    echo "Key: $key; Value: $value<br />\n";
}

echo "Foreach\n";
foreach ($arr as $key => $value) {
    echo "Key: $key; Value: $value<br />\n";
}
?>
