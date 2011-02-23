[expect php]
[file]
<?php

include('Phalanger.inc');

function __autoload($class_name)
{
	eval(file_get_contents(dirname(__FILE__) . '/' . strtolower($class_name) . '.p5c'));
	echo __FUNCTION__ . '(' . $class_name . ")\n";
}

__var_dump(interface_exists('autoload_interface', false));
__var_dump(class_exists('autoload_implements', false));

$o = new Autoload_Implements;
__var_dump($o);
__var_dump($o instanceof autoload_interface);
unset($o);

__var_dump(interface_exists('autoload_interface', false));
__var_dump(class_exists('autoload_implements', false));

?>