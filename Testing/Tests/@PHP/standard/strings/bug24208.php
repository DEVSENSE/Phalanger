[expect php]
[file]
<?php
require('Phalanger.inc');
$a = $b = $c = "oops";
parse_str("a=1&b=2&c=3");
__var_dump($a, $b, $c);
?>