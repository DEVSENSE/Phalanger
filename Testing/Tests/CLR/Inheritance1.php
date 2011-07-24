[expect error]
error PHP2031
[file]
<?
	// circular:
	interface I1 extends I2, I3, I4 { }
	interface I2 { }
	interface I3 { }
	interface I4 extends I2, I1 { }
?>