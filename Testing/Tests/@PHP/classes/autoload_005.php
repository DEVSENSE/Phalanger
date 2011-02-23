[expect php]
[file]
<?php

include('Phalanger.inc');

function __autoload($class_name)
{
	__var_dump(class_exists($class_name, false));
	eval(file_get_contents(dirname(__FILE__) . '/' . $class_name . '.p5c'));
	echo __FUNCTION__ . '(' . $class_name . ")\n";
}

__var_dump(class_exists('autoload_derived', false));
__var_dump(class_exists('autoload_derived', false));

class Test
{
    function __destruct() {
        echo __METHOD__ . "\n";
        $o = new autoload_derived;
        __var_dump($o);
    }
}

$o = new Test;
unset($o);

?>