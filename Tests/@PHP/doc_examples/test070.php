[expect php]

[file]
<?php
$cache = array();

// this works for any expression, not just functions:
$value = @$cache[$key]; 
// will not issue a notice if the index $key doesn't exist.

echo "no warning";
?>
