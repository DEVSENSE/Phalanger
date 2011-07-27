[expect php]
[file]
<?php

class trida
{
    function add($x, $y)
    {
        return 4 + $x + $y;
    }
}

function test($x,$arg1,$arg2)
{
	echo $x->add($arg1,$arg2)."\n";//call site
}

// prepare test arguments

$arr = array(13,4,5);
$number = 7;
$refnumber = &$number;

class x{var $bar = 10;}

$x = new x();
$trida = new trida();

//global test
test($trida,10, 12);
test($trida,11.2, 12);
test($trida,"text", 12);
test($trida,$arr[1], 12);
test($trida,$x->bar, 12);
test($trida,$refnumber, 12);

echo "---------\n";

// function test
function notglobal()
{
	global $trida;
	global $x;
	global $arr;
	global $refnumber;
	
	test($trida,10, 12);
	test($trida,11.2, 12);
	test($trida,"text", 12);
	test($trida,$arr[1], 12);
	test($trida,$x->bar, 12);
	test($trida,$refnumber, 12);
}

notglobal();

echo "---------\n";

//method test
class method_test
{
	function m()
	{
		global $trida;
		global $x;
		global $arr;
		global $refnumber;
	
		test($trida,10, 12);
		test($trida,11.2, 12);
		test($trida,"text", 12);
		test($trida,$arr[1], 12);
		test($trida,$x->bar, 12);
		test($trida,$refnumber, 12);
	}
}
$m = new method_test();
$m->m();




?>