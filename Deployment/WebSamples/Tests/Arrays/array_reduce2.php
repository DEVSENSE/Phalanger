[expect]
array
{
  [0] => integer(1)
  [1] => integer(2)
  [2] => integer(3)
  [3] => integer(4)
  [4] => integer(5)
}
integer(15)
integer(1200)
integer(1)
[file]
<?
function rsum($v, $w) { 
   $v += $w; 
   return $v; 
} 

function rmul($v, $w) { 
   $v *= $w; 
   return $v; 
} 

$a = array(1, 2, 3, 4, 5); 
$x = array(); 
$b = array_reduce($a, "rsum"); 
$c = array_reduce($a, "rmul", 10); 
$d = array_reduce($x, "rsum", 1); 
var_dump($a,$b,$c,$d);
?>