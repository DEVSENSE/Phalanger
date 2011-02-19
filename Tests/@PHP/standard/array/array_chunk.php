[expect php]
[file]
<?php
include('Phalanger.inc');
$arrays = array (
	array (),
	array (0),
	array (1),
	array (-1),
	array (0, 2),
	array (1, 2, 3),

	array (1 => 0),
	array (2 => 1),
	array (3 => -1),

	array (1 => 0, 2 => 2),
	array (1 => 1, 2 => 2, 3 => 3),
	array (0 => 0, 3 => 2),
	array (1 => 1, 5 => 2, 8 => 3),

	array (1, 2),
	array (0, 1, 2),
	array (1, 2, 3),
	array (0, 1, 2, 3),
	array (1, 2, 3, 4),
	array (0, 1, 2, 3, 4),
	array (1, 2, 3, 4, 5, 6, 7, 8, 9, 10),
	array (0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10),

	array ("a" => 1),
	array ("b" => 1, "c" => 2),
	array ("p" => 1, "q" => 2, "r" => 3, "s" => 4, "u" => 5, "v" => 6),

	array ("a" => "A"),
	array ("p" => "A", "q" => "B", "r" => "C", "s" => "D", "u" => "E", "v" => "F"),
);

foreach ($arrays as $item) {
	echo "===========================================\n";
	__var_dump ($item);
	echo "-------------------------------------------\n";
	for ($i = 0; $i < (sizeof($item) + 1); $i++) {
		echo "[$i]\n";
		__var_dump (@array_chunk ($item, $i));
		__var_dump (@array_chunk ($item, $i, TRUE));
		__var_dump (@array_chunk ($item, $i, FALSE));
		echo "\n";
	}
	echo "\n";
}
echo "end\n";
?>