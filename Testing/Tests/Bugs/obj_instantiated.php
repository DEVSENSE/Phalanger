[expect php]
[file]
<?php

class Foo{

}

class Bar{
	var $x = 1;
}

$x = new Foo();

if ($x) 
	echo "x instantied, ";
else
	echo "x empty, ";
	
$y = new Bar();

if ($y) 
	echo "y instantied";
else
	echo "y empty";

?>