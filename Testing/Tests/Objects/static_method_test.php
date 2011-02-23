[expect exact] 1A2BA2BAC

[file]
<?
	interface IFace
	{
		static function StaticFun();
	};

	class Class1 implements IFace
	{
		protected static function f()
		{
			echo 'A';
		}
	
		public static function StaticFun()
		{
			echo 1;
			self::f();
		}
	};
	
	class Class2 extends Class1
	{
		protected static function f()
		{
			echo 'B';
		}
	
		public static function StaticFun()
		{
			echo 2;
			self::f();
			parent::f();
		}
	};

	if (true)
	{	
		class Class3 extends Class2
		{
			public static function f()
			{
				echo 'C';
			}
		};
	}
		
	Class1::StaticFun();
	Class2::StaticFun();
	Class3::StaticFun();
	Class3::f();
?>