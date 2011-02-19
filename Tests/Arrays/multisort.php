[expect]
bool(true)
array
(
  [0] => array
  (
    [0] => 2
    [1] => 8
    [2] => 2
    [3] => 2
    [4] => 8
    [5] => 8
  )
  [1] => array
  (
    [0] => 7
    [1] => 2
    [2] => 7
    [3] => 4
    [4] => 8
    [5] => 1
  )
  [2] => array
  (
    [0] => 1
    [1] => 4
    [2] => 8
    [3] => 0
    [4] => 6
    [5] => 2
  )
  [3] => array
  (
    [0] => 8
    [1] => 4
    [2] => 1
    [3] => 0
    [4] => 7
    [5] => 1
  )
  [4] => array
  (
    [0] => 20
    [1] => 2
    [2] => 12
    [3] => 11
    [4] => 10
    [5] => 1
  )
)
array
(
  [0] => array
  (
    [0] => 10
    [1] => 100
    [2] => 100
    [3] => a
  )
  [1] => array
  (
    [0] => 1
    [1] => 3
    [2] => 2
    [3] => 1
  )
)
array
(
  [0] => 10
  [1] => a
  [2] => 100
  [3] => 100
)
array
(
  [0] => 1
  [1] => 1
  [2] => 2
  [3] => 3
)


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