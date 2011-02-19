[expect php]

[file]
<?php

$pattern = array('/one/', '/two/');
$replace = array('uno', 'dos');
$subject = "test one, one two, one two three";

echo preg_replace($pattern, $replace, $subject, 1);
?>
