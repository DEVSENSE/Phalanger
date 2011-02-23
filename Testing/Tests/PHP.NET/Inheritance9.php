[expect error]
error PHP1018
[file]
<?
	class C
	{
		private function f()
		{
		}
	}
	
	interface I
	{
		function f();
	}
	
	class D extends C implements I
	{
		// f is not implemented here
	}
	
?>