[expect error]
error PHP2031
[file]
<?
	// circular:
	interface J1<:T:> extends J2, J3<:T:>, J4 { }
	interface J2 { }
	interface J3<:T:> { }
	interface J4 extends J2, J1<:int:> { }
?>