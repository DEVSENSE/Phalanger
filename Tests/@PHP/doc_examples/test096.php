[expect php]

[file]
<?php
$i = 4;

switch ($i):
case 0:
    echo "i equals 0";
    break;
case 1:
    echo "i equals 1";
    break;
case 2:
    echo "i equals 2";
    break;
default:
    echo "i is not equal to 0, 1 or 2";
endswitch;
?>

<?php
$i = 0;

switch ($i):
case 0:
    echo "i equals 0";
    break;
case 1:
    echo "i equals 1";
    break;
case 2:
    echo "i equals 2";
    break;
default:
    echo "i is not equal to 0, 1 or 2";
endswitch;
?>
