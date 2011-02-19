[expect php]

[file]
<?php
function &returns_reference()
{
    $someref = "ahoj";
    return $someref;
}

$newref =& returns_reference();
echo $newref;
?>
