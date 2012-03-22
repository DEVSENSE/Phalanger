[expect php]
[file]
<?
$a = array(1,2,3);
foreach ($a as $v)
{
  echo "$v\n";
}

foreach ($a as $k => $v)
{
  echo "$k-$v\n";
}

$i = 10;
foreach ($a as $k =>& $v)
{
  echo "$k-$v\n";
  $v = $i++;
}
var_dump($a);

$a = array(0,1,2,3,4,5);
$i = 0;
foreach ($a as $k =>& $v)
{
  $v+=100;

  if ($i++==0)
  {
    $a[] = 4;
    $a[1] = 1;
    unset($a[3]);
  }
}
var_dump($a);
?>