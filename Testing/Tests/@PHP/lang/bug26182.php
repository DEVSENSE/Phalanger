[expect php]
[file]
<?php
require('Phalanger.inc');


class A {
    function NotAConstructor ()
    {
        if (isset($this->x)) {
            //just for demo
        }
    }
}

$t = new A ();

__var_dump($t);

?>
