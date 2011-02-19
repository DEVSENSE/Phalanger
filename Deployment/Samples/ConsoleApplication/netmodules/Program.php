<?php

	// including scripts example
	
	include "a.php";
	require_once "b.php";
	
	f(12345);
	
	$a = new A("AAA");
	$a->write();
	$a->foo("hello");
	$a->write();
	
	$b = new B("BBB");
	$b->write();
	$b->foo("bye");
	$b->write();
	
	fgets(STDIN);
	
?>