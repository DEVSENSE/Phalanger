[expect errors]
error PHP1021
warning PHP0020
error PHP1022
error PHP1022
error PHP1020
warning PHP0020
warning PHP0020
error PHP1081
warning PHP0020
error PHP1081
error PHP1021
error PHP1022
error PHP1020
error PHP1092
error PHP1092
error PHP1092
[file]
<?
	interface I { }
	interface J { }
	class C { }
	class D { }
	interface K<:T:> { }
	class E<:T:> { }
	
	interface I1 extends I {}
	interface I2 extends C {} 
	interface I3 extends U {} 
	class E1 extends D {} 
	class E2 extends I {}
	class E3 extends I implements I,D,J { }
	class E4 extends D implements I,J { }
	class E5 extends U { }
	class E6 extends D implements U { }
	
	interface K2<:T:> extends I<:T:> { }
	interface K3<:T:> extends K<:T:> { }
	interface K4 extends K2<:I<:U:>:> { }
	
	interface K5 extends E<:int:> { }
	class F extends K<:E<:int:>:> implements E<:int:> { }
?>
