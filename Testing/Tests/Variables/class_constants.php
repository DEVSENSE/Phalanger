[expect php]
[file]
<?  
	class A
	{
		const X = 'x1';
		const Y = 'y1';
		const Z = 'z1';
	}

	class X extends A
	{
		const A = '123';
		const B = '456';
		
		static function test()
		{
			var_dump(defined('A'));
			var_dump(defined('B'));
			var_dump(defined('self::A'));
			var_dump(defined('self::B'));
			var_dump(defined('self::C'));
			var_dump(defined('self::X'));
			var_dump(defined('self::Y'));
			var_dump(defined('self::Z'));
			
			var_dump(constant('A'));
			var_dump(constant('B'));
			var_dump(constant('C'));
			var_dump(constant('X::A'));
			var_dump(constant('X::B'));
			//var_dump(constant('X::C'));
			var_dump(constant('self::X'));
			var_dump(constant('self::Y'));
			var_dump(constant('self::Z'));
		}
	}
	
	define('A', 'global A');
	define('B', 'global A');
	define('X::C', 'global X::C');
	
	var_dump(defined('A'));
	var_dump(defined('B'));
	var_dump(defined('C'));
	var_dump(defined('X::A'));
	var_dump(defined('X::B'));
	var_dump(defined('X::C'));
	X::test();
?>