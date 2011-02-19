[expect php]
[file]
<?php
  require('Phalanger.inc');
	// invalid input: __var_dump(@substr_count("", ""));
	// invalid input: __var_dump(@substr_count("a", ""));
	__var_dump(@substr_count("", "a"));
	__var_dump(@substr_count("", "a"));
	__var_dump(@substr_count("", chr(0)));
	
	$a = str_repeat("abcacba", 100);
	__var_dump(@substr_count($a, "bca"));
	
	$a = str_repeat("abcacbabca", 100);
	__var_dump(@substr_count($a, "bca"));

	__var_dump(substr_count($a, "bca", 200));
	__var_dump(substr_count($a, "bca", 200, 50));
?>