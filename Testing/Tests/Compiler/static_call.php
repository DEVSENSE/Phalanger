[expect php]
[file]
<?php

abstract class Bar
{

	var $color = "yellow";

	public function Foo()
	{
		echo "Foo";
		//return $this->color;
	}
}

class ChocolateBar extends Bar
{

	public function Boo()
	{
		echo self::Foo();//call to a non-static function statically from non-static function
	}

	public static function BooStatic()
	{
		echo self::Foo();//call to a non-static function statically from static function
	}
}

$a = new ChocolateBar();

$a->Boo();
@ChocolateBar::BooStatic();

call_user_func(array($a,"Foo"));//callback to a non-static function non-staticaly
@call_user_func(array("ChocolateBar","Foo"));//callback to a non-static function statically

?>