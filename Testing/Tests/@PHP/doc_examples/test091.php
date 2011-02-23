[expect php]

[file]
<?php
$arr = array(1,5,8,45,54,32,7);

while (list ($key, $value) = each ($arr)) {
    if (!($key % 2)) { // skip odd members
        continue;
    }
    print($value);
}

$i = 0;
while ($i++ < 5) {
    echo "Outer\n";
    while (1) {
        echo "&nbsp;&nbsp;Middle\n";
        while (1) {
            echo "&nbsp;&nbsp;Inner\n";
            continue 3;
        }
        echo "This never gets output.\n";
    }
    echo "Neither does this.\n";
}
?>
