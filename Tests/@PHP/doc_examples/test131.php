[expect php]

[file]
<?php
class A {
    function A($i) {
        $this->value = $i;
        // try to figure out why we do not need a reference here
        $this->b = new B($this);
    }

    function createRef() {
        $this->c = new B($this);
    }

    function echoValue() {
        echo "<br />","class ",get_class($this),': ',$this->value;
    }
}


class B {
    function B(&$a) {
        $this->a = &$a;
    }

    function echoValue() {
        echo "<br />","class ",get_class($this),': ',$this->a->value;
    }
}

// try to understand why using a simple copy here would yield
// in an undesired result in the *-marked line
$a =& new A(10);
$a->createRef();

$a->echoValue();
$a->b->echoValue();
$a->c->echoValue();

$a->value = 11;

$a->echoValue();
$a->b->echoValue(); // *
$a->c->echoValue();

?>
