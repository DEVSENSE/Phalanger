[expect php]
[file]
<?php

interface IEatable
{
	static function IsGood($what);
}
 
class Apple implements IEatable
{

	public static function IsGood($what, $optionalArg = null)
	{

	}
}

?>