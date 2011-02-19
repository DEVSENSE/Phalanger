[expect php]
[file]
<?php
require('Phalanger.inc');

function handler($errno, $errstr, $errfile, $errline, $context)
{
	echo __FUNCTION__ . "\n";
}

set_error_handler('handler');

function foo($x) {
	return "foo";
}

$output = array();
++$output[foo("bar")];

__var_dump($output);

echo "Done";
?>