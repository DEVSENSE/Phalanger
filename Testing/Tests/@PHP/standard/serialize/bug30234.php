[expect php]
[file]
<?php
include('Phalanger.inc');
include('Autoload.inc');
//error_reporting(0);

__var_dump(interface_exists('autoload_interface', false));
__var_dump(class_exists('autoload_implements', false));

$o = unserialize('O:19:"Autoload_Implements":0:{}');

__var_dump($o);
__var_dump($o instanceof autoload_interface);
unset($o);

__var_dump(interface_exists('autoload_interface', false));
__var_dump(class_exists('autoload_implements', false));

?>