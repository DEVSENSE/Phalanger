[expect]
[file]
<?
	// everything ok:
	
	abstract class CC<:T:> { abstract function mm(); }
	class DD<:T:> extends CC<:T:> { function Mm() {} } 
	
	interface II0<:T:> { function mm(); }
	interface II1<:T:> { function Mm(); }
	
	class CCC<:T:> implements II0<:T:>, II1<:T:> { function MM(){} }
	
	interface I0<:T:> { function mm(); }
	interface I1<:T:> extends I0<:int:>, I0<:string:> { }
	interface I2<:T:> { function Mm(); }
	interface I3<:T:> { function mM(); }

	abstract class A<:T:> implements I1<:T:> { function MM() { }  }
	abstract class B<:T:> extends A<:T:> implements I2<:T:> { }
	abstract class C<:T:> extends B<:T:> implements I3<:T:> { }
	class D<:T:> extends C<:T:> { function mm() { } }
?>