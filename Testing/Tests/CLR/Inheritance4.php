[expect error]
error PHP2031
[file]
<?
	// ok:
	interface J1<:T:> { }
	interface J2 { }
	interface J7<:T:> { }
	interface J6<:T:> extends J7<:J6<:int:>:> { }
	interface J8<:T:> extends J1<:J6<:J2:>:> { }
	
	// ok, but there is a bug in CLR:
	interface J<:T:> extends J7<:J<:J<:T:>:>:> { }
?>