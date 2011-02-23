[expect php]
[file]
<?php
include('Phalanger.inc');

$arrays = array (
	array (),
	array (0),
	array (1),
	array (-1),
	array (0, 2, 3, 4, 5),
	array (1, 2, 3, 4, 5),
	array ("" => 1),
	array ("a" => 1),
	array ("Z" => 1),
	array ("one" => 1),
	array ("ONE" => 1),
	array ("OnE" => 1),
	array ("oNe" => 1),
	array ("one" => 1, "two" => 2),
	array ("ONE" => 1, "two" => 2),
	array ("OnE" => 1, "two" => 2),
	array ("oNe" => 1, "two" => 2),
	array ("one" => 1, "TWO" => 2),
	array ("ONE" => 1, "TWO" => 2),
	array ("OnE" => 1, "TWO" => 2),
	array ("oNe" => 1, "TWO" => 2),
	array ("one" => 1, "TwO" => 2),
	array ("ONE" => 1, "TwO" => 2),
	array ("OnE" => 1, "TwO" => 2),
	array ("oNe" => 1, "TwO" => 2),
	array ("one" => 1, "tWo" => 2),
	array ("ONE" => 1, "tWo" => 2),
	array ("OnE" => 1, "tWo" => 2),
	array ("oNe" => 1, "tWo" => 2),
	array ("one" => 1, 2),
	array ("ONE" => 1, 2),
	array ("OnE" => 1, 2),
	array ("oNe" => 1, 2),
	array ("ONE" => 1, "TWO" => 2, "THREE" => 3, "FOUR" => "four"),
	array ("one" => 1, "two" => 2, "three" => 3, "four" => "FOUR"),
	array ("ONE" => 1, "TWO" => 2, "three" => 3, "four" => "FOUR"),
	array ("one" => 1, "two" => 2, "THREE" => 3, "FOUR" => "four")
);

foreach ($arrays as $item) {
	__var_dump(array_change_key_case($item));
	__var_dump(array_change_key_case($item, CASE_UPPER));
	__var_dump(array_change_key_case($item, CASE_LOWER));
	echo "\n";
}
echo "end\n";
?>