[expect php]
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