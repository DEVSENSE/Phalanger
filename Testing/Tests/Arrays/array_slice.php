[expect exact]
array
(
  [0] => c
  [Y] => d
  [1] => e
)
array
(
  [Y] => d
)
array
(
  [0] => a
  [X] => b
  [1] => c
)
array
(
  [0] => c
  [Y] => d
)
array
(
  [2] => c
  [Y] => d
)
[file]
<?
$input = array(1 => "a", "X" => "b", 2 => "c", "Y" => "d", 3 => "e");

print_r(array_slice($input, 2));
print_r(array_slice($input, -2, 1));
print_r(array_slice($input, 0, 3));
print_r(array_slice($input, 2, -1));
print_r(array_slice($input, 2, -1, true));
?>
