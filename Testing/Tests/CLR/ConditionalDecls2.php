[pure]
[expect exact]
object(T<:C:>)(0) {
}
[file]
<?
	class Program
	{
		public static function Main()
		{
			class C { }
			class T<:Q = C:> { function f(Q $x) { echo "T\n"; } }
			
			$t = new T;
			var_dump($t);
		}
	}
?>