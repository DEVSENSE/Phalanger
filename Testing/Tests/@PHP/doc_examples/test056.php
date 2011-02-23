[expect php]

[file]
<?php
function Test()
{
    static $a = 0;
    echo $a;
    $a++;
}

test();
test();
test();
test();

?>
