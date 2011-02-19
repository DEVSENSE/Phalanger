[expect php]
[file]
<?php
require('Phalanger.inc');
error_reporting(0);

class foo {
    public $fubar = 'fubar';
}

function &foo(){
    $GLOBALS['foo'] = &new foo();
    return $GLOBALS['foo'];
}
$bar = &foo();
__var_dump($bar);
__var_dump($bar->fubar);
unset($bar);
$bar = &foo();
__var_dump($bar->fubar);

$foo = &foo();
__var_dump($foo);
__var_dump($foo->fubar);
unset($foo);
$foo = &foo();
__var_dump($foo->fubar);
?>