[expect php]
[file]
<?php
require('Phalanger.inc');
$a = -12.3456;
$test = sprintf("%04d", $a);
__var_dump($test, bin2hex($test));
$test = sprintf("% 13u", $a);
__var_dump($test, bin2hex($test));
?>