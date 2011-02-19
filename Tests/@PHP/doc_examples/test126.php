[expect php]

[file]
<?php
class A
{
    function A()
    {
        echo "I am the constructor of A.<br />\n";
    }

    function B()
    {
        echo "I am a regular function named B in class A.<br />\n";
        echo "I am not a constructor in A.<br />\n";
    }
}

class B extends A
{
    function C()
    {
        echo "I am a regular function.<br />\n";
    }
}

// This will call A() as a constructor.
$b = new B;
?>
