[expect error]
error PHP2031
<?
	// circular:
	interface J5<:T:> extends J5<:T:> { }
?>