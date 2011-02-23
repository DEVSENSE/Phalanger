[expect php]
[file]
<?php
include('Phalanger.inc');
$a = array(1 => 2);
shuffle($a);
__var_dump($a);

$a = array(1 => 2);
array_multisort($a);
__var_dump($a);
?>
