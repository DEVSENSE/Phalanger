[expect] Warning:
[expect] Missing argument #2
[expect]
Making a bowl of raspberry .

[file]
<?php
function makeyogurt($type = "acidophilus", $flavour)
{
    return "Making a bowl of $type $flavour.\n";
}
 
echo makeyogurt("raspberry");   // won't work as expected
?>
