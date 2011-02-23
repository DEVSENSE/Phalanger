[expect]
0
1
2
3
4
5
6
7
8
9
10
11
12
13
14
15
16
17
18
array
(
  [0] => 10
  [1] => 9
  [2] => 8
  [3] => 7
  [4] => 6
  [5] => 5
  [6] => 4
  [7] => 3
  [8] => 2
  [9] => 1
)
[file]
<?
function my_cmp($a,$b)
{
  static $i = 0;
  
  echo "$i\n";
  $i++;
  return $a==$b ? 0 : ($a < $b ? +1 : -1);
}

$array = array(1,2,3,4,5,6,7,8,9,10);

usort($array,"my_cmp");

print_r($array);
?>