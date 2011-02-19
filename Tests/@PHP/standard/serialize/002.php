[expect php]
[file]
<?php
include('Phalanger.inc');
__var_dump(unserialize('b:0;'));
__var_dump(unserialize('b:1;'));
__var_dump(unserialize('i:823;'));
__var_dump(unserialize('s:0:"";'));
__var_dump(unserialize('s:3:"foo";'));
__var_dump(unserialize('a:1:{i:0;s:2:"12";}'));
__var_dump(unserialize('a:2:{i:0;a:0:{}i:1;a:0:{}}'));
__var_dump(unserialize('a:3:{i:0;s:3:"foo";i:1;s:3:"bar";i:2;s:3:"baz";}'));
__var_dump(unserialize('O:8:"stdClass":0:{}'));
?>
