[expect php]
[file]
<?php

class Element
{
	public function ThrowException ()
	{
		throw new Exception();
	}

	public static function CallBack(Element $elem)
	{
		$elem->ThrowException();
	}
}

$arr = array(new Element(), new Element(), new Element());

try
{
  @array_map(array('Element', 'CallBack'), $arr);
}
catch (Exception $e)
{
}  

echo "Done\n";
?>