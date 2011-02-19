[expect php]
[file]
<?php
require('Phalanger.inc');

$obj = new stdClass;

echo get_class($obj)."\n";

echo "Done\n";
?>