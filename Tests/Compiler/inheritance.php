[expect] ahoj 

[file]
<?php

eval('class A { function f() { echo "ahoj"; } }');

class B extends A { }

B::f();

?>
