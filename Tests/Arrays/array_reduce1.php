[expect]
array
(
  [a] => 1
  [b] => 2
  [c] => 3
  [d] => 4
  [e] => 5
)
array
(
  [0] => 1
  [1] => 2
  [2] => 3
  [3] => 4
  [4] => 5
)
array [empty]
[file]
<?
function &t(&$a, &$b)
{
  static $o = 1;
  
  $a[$b] = $o;
  $b = $o;
  
  $o++;
  
  return $a;
}

$array0 = array("a", "b", "c", "d", "e"); 
$array1 = array();
print_r(array_reduce($array0,"t",$array1)); 
print_r($array0);
print_r($array1);
?>