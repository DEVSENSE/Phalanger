[pure]
[expect exact]
T2::f
g2
[file]
<?
	class P
	{
		public static function Main()
		{
			if ($x)
			{
				function g() { echo "g1\n"; }
				class T { function f() { echo "T1::f\n"; } }
			}
			else
			{
				function g() { echo "g2\n"; }
				class T { function f() { echo "T2::f\n"; } }
			}
			
			$t = new T;
			$t->f();
			g();
		}
	}		
?>