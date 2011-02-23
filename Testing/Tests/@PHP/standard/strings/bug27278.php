[expect php]
[file]
<?php
require('Phalanger.inc');
function foo ($a)
{
	$a=sprintf("%02d",$a);
	__var_dump($a);
}

$x="02";
__var_dump($x);
foo($x);
__var_dump($x);

?>