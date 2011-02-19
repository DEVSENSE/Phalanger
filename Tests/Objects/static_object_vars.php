[expect exact]
array(1) {
  ["y"]=>
  integer(2)
}
array(1) {
  ["y"]=>
  integer(2)
}
array(2) {
  ["y"]=>
  integer(2)
  ["x"]=>
  &integer(1)
}
y - 2
[file]
<?

class X
{
    static $x = 1;
    var $y = 2;
}
$x = new X();
var_dump((array)$x);
var_dump(get_object_vars($x));
var_dump(get_class_vars("X"));

foreach ($x as $key => $value)
 echo "$key - $value\n"

?>