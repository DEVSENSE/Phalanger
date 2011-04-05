[expect php]
[file]
<?
$input = array(1 => "a", "X" => "b", 2 => "c", "Y" => "d", 3 => "e");

print_r(array_slice($input, 2));
print_r(array_slice($input, -2, 1));
print_r(array_slice($input, 0, 3));
print_r(array_slice($input, 2, -1));
print_r(array_slice($input, 2, -1, true));
?>
