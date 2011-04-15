<?
	interface I
	{
	}
	
	class C implements I
	{
	}
	
	class Program
	{
		public static function Main()
		{
			ob_start();
		
			$lib = new Library;
			$lib->f();
			var_dump(new LibX);

			$x = 1;

			if ($x)
			{
				class Q {} 
			}
			
			eval('	
				class P extends Q { }
			');
			
			eval('	
				class A extends C { }
				class D extends A { }
				class E extends LibX { }
			');
			
			class F extends P { }
			
			var_dump(new Q, new P, new A, new D, new E, new F);
   		
   		$out = ob_get_contents();
   		
   		ob_end_clean();
   		
   		$EXPECTED = "object(LibX)(0) {
}
object(Q)(0) {
}
object(P)(0) {
}
object(A)(0) {
}
object(D)(0) {
}
object(E)(0) {
}
object(F)(0) {
}
";	
   		echo $out == $EXPECTED ? "OK" : "ERROR", "\n";
		}
	}
?>