[expect php]
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