[expect php]
[file]
<?php

function my_error_handler($error, $errmsg='', $errfile='', $errline=0, $errcontext='')
{
	echo "error";
	$errcontext = '';
}
                                                                                        
set_error_handler('my_error_handler');

function test()
{
	echo "Undefined index here: '";
	echo $data['HTTP_HEADER'];
	echo "'\n";
}
test();
?>