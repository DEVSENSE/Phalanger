[expect php]
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