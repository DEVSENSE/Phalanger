[expect exact]
x => array(3)
{
  [0] => integer(1)
  [1] => integer(2)
  [2] => integer(3)
}
[file]
<?

//
// Tests whether PhpArray's foreach enumerator correctly 
// dereferences and deeply copies values.
//

$x = array(1,2,3);
$a = array("x" => &$x);
foreach ($a as $k => $v)
{
  $x[1] = 10;
  echo "$k => ";
  var_dump($v);
}
?>