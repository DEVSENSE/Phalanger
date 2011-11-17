[expect php]
[file]
<?php

class Foo{

}

class Bar{
	var $x = 1;
	
	function __toString()
	{
	  return "b";
	}
}

$x = new Foo();
$y = new Bar();


 var_dump( $x + " - ");
 var_dump( $y + " - ");



?>