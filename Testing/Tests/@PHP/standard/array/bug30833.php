[expect php]
[file]
<?php
include('Phalanger.inc');
$foo = array('abc', '0000');
__var_dump($foo);

$count = array_count_values( $foo );
__var_dump($count);

__var_dump($foo);

echo "Done\n";
?>
