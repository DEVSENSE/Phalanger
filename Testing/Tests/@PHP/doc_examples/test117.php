[expect php]

[file]
<?php
function small_numbers()
{
    return array (0, 1, 2);
}
list ($zero, $one, $two) = small_numbers();
echo "$zero, $one, $two";
?>
