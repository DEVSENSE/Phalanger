[expect errors]
warning PHP0020
warning PHP0020
[file]
<?
	class C { }

	[AttributeUsage(AttributeTargets::All, $AllowMultiple => true, $Inherited => false)]
	class MyAttribute extends System:::Attribute
	{
		private $a, $b;
		public $c;
		
		public function __construct($a, $b)
		{
			$this->a = $a;
			$this->b = $b;
		}
	}	
	
	[My(1,2)]
	class P
	{
		public static function Main()
		{
			create_function('$x','return $x + 1;');
			create_function('int $x','return $x + 1;');
			create_function('bool &$x','return $x + 1;');
			create_function('C &$x','return $x + 1;');
			create_function('U &$x','return $x + 1;');
			create_function('[My(1,2)]U &$x','return $x + 1;');
			create_function('$nested','return $nested . create_function(\'$z\',\'return $z + 1;\');');
		}
	}
?>