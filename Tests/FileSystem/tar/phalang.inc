<?

/*
This file is for compatibility purposes.

Phalanger version 2.0 has strings by default in unicode. In order to
force Phalanger to convert a string to bytes it's necessary to use
to_binary($obj) phalanger's function. In PHP the function doesn't
exist. In order not to throw fatal error Unknown function error in 
PHP follows to_Binary implementation in PHP.
*/

function defineBinaryFnc()
{
if (!PHALANGER)
{
	function to_binary($obj)
	{	
		return $obj;
	}
}
}

@defineBinaryFnc();

?>