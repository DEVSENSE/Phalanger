[expect php]
[file]
<?php
include('Phalanger.inc');
$str = 'O:9:"TestClass":0:{}';
$obj = unserialize($str);
__var_dump($obj);
echo serialize($obj)."\n";
__var_dump($obj);
echo serialize($obj)."\n";
__var_dump($obj);
?>