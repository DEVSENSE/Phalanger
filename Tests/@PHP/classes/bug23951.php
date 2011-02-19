[expect php]
[file]
<?php
require('Phalanger.inc');

define('FOO1', 1);
define('FOO2', 2);

class A {
    
    public $a_var = array(FOO1=>'foo1_value', FOO2=>'foo2_value');
    
}

class B extends A {
 
    public $b_var = 'foo';   
            
}

$a = new A;
$b = new B;

__var_dump($a);
__var_dump($b->a_var);
__var_dump($b->b_var);

?>