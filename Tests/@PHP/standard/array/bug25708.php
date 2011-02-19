[expect php]
[file]
<?php
include('Phalanger.inc');
function foo($ref, $alt) {
	unset($GLOBALS['a']);
	unset($GLOBALS['b']);
	$GLOBALS['a'] = 1;
	$GLOBALS['b'] = 2;

	$org_a = $GLOBALS['a'];
	$org_b = $GLOBALS['b'];

	if ($ref) {
		global $a, $b;
	} else {
		/* zval temp_var(NULL); // refcount = 1
		 * a = temp_var[x] // refcount = 2
		 */
		$a = NULL;
		$b = NULL;
	}

	echo "--\n";
	if ($alt) {
		$a = &$GLOBALS['a'];
		$b = &$GLOBALS['b'];
	} else {
		extract($GLOBALS, EXTR_REFS);
	}
	echo "--\n";
	$a = &$GLOBALS['a'];
	$b = &$GLOBALS['b'];
	echo "--\n";
	$GLOBALS['b'] = 3;
	echo "--\n";
	$a = 4;
	echo "--\n";
	$c = $b;
	echo "--\n";
	$b = 'x';
	echo "--\n";
	echo "----";
	if ($ref) echo 'r';
	if ($alt) echo 'a';
	echo "\n";
}

$a = 'ok';
$b = 'ok';
$_a = $a;
$_b = $b;

foo(false, true);
foo(true, true);
foo(false, false);
foo(true, false);
?>