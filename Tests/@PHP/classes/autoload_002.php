[expect php]
[file]
<?php

include('Phalanger.inc');

function __autoload($class_name)
{
	eval(file_get_contents(dirname(__FILE__) . '/' . $class_name . '.p5c'));
	echo __FUNCTION__ . '(' . $class_name . ")\n";
}

__var_dump(get_class_methods('autoload_root'));

?>