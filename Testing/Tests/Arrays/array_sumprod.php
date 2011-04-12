[expect php]
[file]
<?
$a = array(1,2,3,"1fghfhf","5e-1",.8,"-0x10"); 
$b = array(1,2,3); 
$c = array(1,2,PHP_INT_MAX,PHP_INT_MAX); 

var_dump(array_sum($a),array_product($a));
var_dump(array_sum($b),array_product($b));
var_dump(array_sum($c),array_product($c));

var_dump("0x10" == 16);
var_dump("-0x10" == -16);
?>
