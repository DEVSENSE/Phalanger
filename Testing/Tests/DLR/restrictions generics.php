[clr]
[expect]
A
B
C
A
B
C
[file]
<?php

class A{}
class B{}
class C{}

class X
{
    function foo<:T:>()
    {
        echo get_class(new T) . "\n";
    }
	
	function bar<:T:>()
	{
		$this->foo<:T:>();
	}
}

$x = new X;

$x->bar<:A:>();
$x->bar<:B:>();
$x->bar<:C:>();
$x->bar<:A:>();
$x->bar<:B:>();
$x->bar<:C:>();

?>