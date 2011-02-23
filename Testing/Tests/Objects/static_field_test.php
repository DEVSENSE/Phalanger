[expect exact] 131

[file]
<?
	class Class1
	{
		public static $x = 1;
		protected static $y = 2;
		private static $z = 3;
				
		public static function Foo1()
		{
			echo self::$z;
		}
	};
	
	class Class2 extends Class1
	{
		public static $y;
		private static $z;
	};
		
	echo Class2::$x;
	Class1::Foo1();
	echo Class1::${"x"};
	//echo Class2::$y;
?>