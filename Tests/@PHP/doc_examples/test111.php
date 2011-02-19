[expect php]

[file]
<?php
function add_some_extra(&$string)
{
    $string .= 'and something extra.';
}

$str = 'This is a string, ';
add_some_extra($str);
echo $str;    // outputs 'This is a string, and something extra.'

?>
