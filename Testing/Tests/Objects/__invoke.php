[expect php]
[file]
<?php

function foo($x)
{
    echo __FUNCTION__ . "($x)\n";
}

class A
{
    private function __invoke($x)
    {
        echo __METHOD__ . "($x)\n";
    }
}

class B
{
    public function __invoke($x)
    {
        echo __METHOD__ . "($x)\n";
    }
}

class C
{
    function __toString()
    {
        return "foo";
    }
}

class D
{
    function __toString()
    {
        return "bar";
    }
}

class E extends A
{
	
}

foreach(array("A","B","C","D","E") as $c)
{
    $obj = new $c;
    $name = null;
    
	echo "\n{new $c}(123):\n";
    if (is_callable($obj)) $obj(123);
	else echo "-";
    	
    echo "\nis_callable(new $c):\n";
    var_dump(is_callable($obj, true, $name));
    var_dump($name);

    var_dump(is_callable($obj, false, $name));
    var_dump($name);
        
    echo "\nis_callable(array($c,'__invoke')):\n";
    var_dump(is_callable(array($c,"__invoke"), true, $name));
    var_dump($name);

    var_dump(is_callable(array($c,"__invoke"), false, $name));
    var_dump($name);

    echo "\nis_callable(array($c,'something_non_existing')):\n";
    var_dump(is_callable(array($c,"something_non_existing"),true,$name));
    var_dump($name);

    var_dump(is_callable(array($c,"something_non_existing"),false,$name));
    var_dump($name);

	if ($c != 'C' && $c != 'D')
	{
		echo "\narray_walk( array(1), new $c ):\n";
		$x = array(1);
		array_walk( $x, $obj );
	}
}

?>