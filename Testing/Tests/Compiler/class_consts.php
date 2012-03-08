[expect php]
[file]
<?
	define('A', 123);	// constant's value unknown at compile time
	
	echo "Global deferred constant:\n";	
	var_dump( A );	// through Operator
	
	class X
	{
		const A = A;
	}
	
	echo "Known class deferred constant:\n";
	var_dump( X::A );	// Operator GetConstantValue
	var_dump( constant('X::A') );	// class library function accessing defined constants
	
	if (true)
	{
		class Y
		{
			const A = A;
		}
	}
	
	echo "Unknown class deferred constant:\n";
	var_dump( Y::A );	// Operator GetConstantValue
	
	
?>
