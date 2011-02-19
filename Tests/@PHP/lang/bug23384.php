[expect php]
[file]
<?php
require('Phalanger.inc');
define('TEN', 10);
class Foo {
    const HUN = 100;
    function test($x = Foo::HUN) {
        static $arr2 = array(TEN => 'ten');
        static $arr = array(Foo::HUN => 'ten');

        __var_dump($arr);
        __var_dump($arr2);
        __var_dump($x);
    }
}

@Foo::test();   
echo Foo::HUN."\n";
?>