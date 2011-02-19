[expect php]
[file]
<?php
  require('Phalanger.inc');

$foo = 'bar';
__var_dump(strval($foo));
define('FOO', 'BAR');
__var_dump(strval(FOO));
__var_dump(strval('foobar'));
__var_dump(strval(1));
__var_dump(strval(1.1));
__var_dump(strval(true));
__var_dump(strval(false));
?>