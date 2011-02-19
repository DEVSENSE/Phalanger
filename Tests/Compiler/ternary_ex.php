[expect exact]
array(2)
{
  [1] => integer(1)
  [0] => integer(0)
}
array(4)
{
  [0] => integer(1)
  [1] => integer(2)
  [2] => integer(3)
  [3] => integer(4)
}
array(2)
{
  [1] => integer(1)
  [2] => integer(2)
}
integer(1)
[file]
<?
$a = array(

function_exists('key') ? 1:0 => function_exists('key') ? 1:0,
function_exists('unknown') ? 1:0 => function_exists('unknown') ? 1:0

);

$b = array(1,2,3,4);

$c = array(1=>1,2=>2);

$d = $a ? 1 : 2;

var_dump($a,$b,$c,$d);
?>