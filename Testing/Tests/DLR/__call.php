[expect php]
[file]
<?php

class Output
{
    function __call($name, $params)
    {
        return true;
    }
}

if (true)
{
    function bar($x)
    {
        return true;
    }
}

$x = new Output;
bar( $x->foo() );

?>