<?

function __autoload($class_name)
{
	switch($class_name)
	{
		case "autoload_implements":
			class autoload_implements implements autoload_interface 
			{
				function testFunction()
				{
					return true;
				}
			}
			break;
			
		case "autoload_interface":	
			interface autoload_interface 
			{
				function testFunction();
			}
			break;
	}
	echo __FUNCTION__ . '(' . $class_name . ")\n";
}

?>