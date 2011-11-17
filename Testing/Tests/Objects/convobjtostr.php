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
//string

 var_dump((string)$x);
 var_dump((string)$y);
 

?>