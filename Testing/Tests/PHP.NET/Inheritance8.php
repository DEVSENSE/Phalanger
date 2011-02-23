[expect]
[file]
<?
	// everything ok:
	
	interface I extends J, K
	{
		function ff();
		function gg();
	}
	
	interface J { } 
	interface K { }
		
	abstract class X implements I
	{
		function FF() { }
	}
	
	class Y	extends X implements I
	{
		function gG() {}
	}
	
	class V
	{
		function fF() { }
	}
	
	class U extends V
	{
		function Gg() { }
	}
	
	class Z	extends U implements I
	{
	}
	
?>