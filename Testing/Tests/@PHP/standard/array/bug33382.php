[expect php]
[file]
<?php
require('Phalanger.inc');
$array = array(1,2,3,4,5);

sort($array);

__var_dump(array_reverse($array));

echo "Done\n";
?>