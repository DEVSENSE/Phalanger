[expect php]
[file]
<?php
include('Phalanger.inc');
$data = array(
	'Test1',
	'teST2'=>0,
	5=>'test2',
	'abc'=>'test10',
	'test21'
);

__var_dump($data);

natsort($data);
__var_dump($data);

natcasesort($data);
__var_dump($data);
?>