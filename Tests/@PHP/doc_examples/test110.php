[expect php]

[file]
<?php
$input = array(1,2);

function takes_array($input)
{
    echo "$input[0] + $input[1] = ", $input[0]+$input[1];
    echo '$input[0] + $input[1] = ', $input[0]+$input[1];
}
?>
