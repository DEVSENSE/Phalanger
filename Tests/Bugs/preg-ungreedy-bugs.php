[expect php]
[file]
<?php
	
	echo "Test 1:\n";
	$expr = '#^(ab)?$#U';
	$matches = null;
	$ok = preg_match($expr, "abab", $matches, PREG_OFFSET_CAPTURE);
	echo (count($matches) == 0)?"ok":"error";

	echo "\nTest 2:\n";
	$expr = '#^z(a*)?z$#U';
	$matches = null;
	$ok = preg_match($expr, "zaaaz", $matches, PREG_OFFSET_CAPTURE);
	echo ($matches[0][0] == "zaaaz")?"ok":"error";
	echo "\n";
	echo ($matches[1][0] == "aaa")?"ok":"error";
	
	echo "\nTest 3:\n";
	$expr = "#^a$#Am";
	$matches = null;
	$ok = preg_match($expr, "a\na", $matches, PREG_OFFSET_CAPTURE);
	echo ($matches[0][0] == "a")?"ok":"error";
	$ok = preg_match($expr, "c\na", $matches, PREG_OFFSET_CAPTURE);
	echo "\n";
	echo (count($matches) == 0)?"ok":"error";

	
	echo "\nTest 4:\n";
	$expr = '#^(?:(?:\ *(?<= |^)\.(\([^\n\)]+\)|\[[^\n\]]+\]|\{[^\n\}]+\}|(?:<>|>|=|<))(\([^\n\)]+\)|\[[^\n\]]+\]|\{[^\n\}]+\}|(?:<>|>|=|<))??(\([^\n\)]+\)|\[[^\n\]]+\]|\{[^\n\}]+\}|(?:<>|>|=|<))??(\([^\n\)]+\)|\[[^\n\]]+\]|\{[^\n\}]+\}|(?:<>|>|=|<))??)\n)?(\*|\-|\+|\d+\.\ |\d+\)|[IVX]+\.\ |[IVX]+\)|[a-z]\)|[A-Z]\))(\n?)\ +\S.*$#mUm';
	$matches = null;
	$ok = preg_match($expr, "- a\nb\n- c", $matches, PREG_OFFSET_CAPTURE, 4);
	echo ($matches[0][0] == "- c")?"ok":"error";
	echo "\n";
	echo ($matches[5][0] == "-")?"ok":"error";
?>