[expect php]
[file]
<?php

class Foo{

}

class Bar{
	var $x = 1;
}

$x = new Foo();
$y = new Bar();

//bool


if ($x) 
	echo "x instantied, ";
else
	echo "x empty, ";
	

if ($y) 
	echo "y instantied";
else
	echo "y empty";

	echo "\n";
	
//int
 var_dump( (int)$x);
 var_dump( (int)$y);
 

	echo "\n";
//double

 var_dump( (double)$x);
 var_dump( (double)$y);

	echo "\n";

//string at convobjtostr.php

	
//array

 var_dump((array)$x);
 var_dump((array)$y);
 
	echo "\n";



?>