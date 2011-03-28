[expect php]
[file]
<?
$a = array(
  array(8,8,8,2,2,2),
  array(1,2,8,7,7,4),
  array(2,4,6,1,8,0),
  array(1,4,7,8,1,0),
  array(1,2,10,20,12,11));

var_dump(array_multisort($a[4],SORT_STRING,SORT_DESC,$a[0],SORT_DESC,$a[1],SORT_DESC,$a[2],$a[3],SORT_DESC));
print_r($a);

$a = array(array("10", 100, 100, "a"), array(1, 3, "2", 1)); 
array_multisort($a[0], SORT_ASC, SORT_STRING, 
               $a[1], SORT_NUMERIC, SORT_DESC); 
print_r($a);               
               
$a1 = array("10", 100, 100, "a"); 
$a2 = array(1, 3, "2", 1); 
array_multisort($a1, $a2);                

print_r($a1);
print_r($a2);  
?>