[expect]
[file]
<?
	// everything ok:
	
	interface I1
	{
		function f();
	}

	interface I2 extends I1
	{
		function f();
	}

	interface I3 extends I1, I2
	{
		function f();
	}
	
	class C implements I3
	{
		function f() {}
	}
	
	interface J1
	{
		function f();
	}
	
	interface J2 extends J1
	{ 
	
	}

	class X implements J2, I3, I1, I2
	{
		public function F() { }
	}
	
?>