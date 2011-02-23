[expects]
integer(1)
integer(1)
integer(1)
integer(2)
integer(2)
integer(2)
array
{
  ['_a'] => &integer(2)
  ['_b'] => &integer(2)
}
integer(1)
integer(1)
integer(1)
integer(2)
integer(2)
integer(2)
array
{
  ['_a'] => &integer(2)
  ['_b'] => &integer(2)
}
[file]
<?php
function test($use_extract) {
	$a = 1;
	$b = 1;

	$arr = array(
		'_a' => $a,
		'_b' => &$b
	);

	var_dump($a, $b);

	if ($use_extract) {
		extract($arr, EXTR_REFS);
	} else {
		$_a = &$arr['_a'];
		$_b = &$arr['_b'];
	}

	$_a++;
	$_b++;

	var_dump($a, $b, $_a, $_b, $arr);
}

test(false);
test(true);

?>
