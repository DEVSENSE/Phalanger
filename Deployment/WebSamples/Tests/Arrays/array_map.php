[expect]
array
(
  [0] => array
  (
    [0] => 1
    [1] => A
    [2] => 0
  )
  [1] => array
  (
    [0] => 2
    [1] => B
    [2] => 0
  )
  [2] => array
  (
    [0] => 3
    [1] => C
    [2] => 
  )
)
array
(
  [0] => 1 A 0
  [1] => 2 B 0
  [2] => 3 C 
)
array
(
  [0] => 1
  [1] => 2
  [2] => 3
)
array
(
  [0] => A
  [1] => B
  [2] => C
)
array
(
  [0] => x
  [1] => x
)
[file]
<?
  function f($x,$y,&$z)
  {
    $result = "$x $y $z";
    $z = 'x';
    return $result;
  }
  
  $a = array(1,2,3);
  $b = array('A','B','C');
  $c = array(0,0);
  
  print_r(array_map(null,$a,$b,$c));
  print_r(array_map("f",$a,$b,$c));

  print_r($a);
  print_r($b);
  print_r($c);
?>  