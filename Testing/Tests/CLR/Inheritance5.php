[expect]
[file]
<?
	// everything ok:
	
	abstract class CC { abstract function mm(); }
	class DD extends CC { function Mm() {} } 
	
	interface II0 { function mm(); }
	interface II1 { function Mm(); }
	
	class CCC implements II0, II1 { function MM(){} }
	
	interface I0 { function mm(); }
	interface I1 extends I0 { }
	interface I2 { function Mm(); }
	interface I3 { function mM(); }

	abstract class A implements I1 { function MM() { }  }
	abstract class B extends A implements I2 { }
	abstract class C extends B implements I3 { }
	class D extends C { function mm() { } }
?>