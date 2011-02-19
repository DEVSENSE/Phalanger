[expect php]
[file]
<?
function f1()
{
  $ar1 = array("a" => &$ar1);
  $ar2 = array("a" => array(1,2,3));
  $ar3 = array_merge_recursive($ar1, $ar2);
  
  $ar3[] = "w";
  $ar3["a"][] = "x";
  $ar3["a"]["a"][] = "y";
  $ar3["a"]["a"]["a"][] = "z";
  
  var_dump($ar3);
}

function f2()
{
  $a = array("a" => &$a);
  $b = array_change_key_case($a,CASE_UPPER);
  $a[] = "z";
  var_dump($b);
}

function f3()
{
  $a = array("a" => &$a);
  sort($a);
  $a[] = "z";
  var_dump($a);
}

function f4()
{
  $x = array(1,2,3);
  $a = array("a" =>& $x);
  
  $c = array_merge_recursive($a,$a);
  
  var_dump($c);
}

function f5()
{
  $a = array("a" => array(1,2,3));
  
  $c = array_merge_recursive($a,$a);
  
  var_dump($c);
}

function f6()
{
  $x = "xx";
  $y =& $x;
  $a = array("a" =>& $x);
  $b = array("a" => 2);
  
  $c = array_merge_recursive($a,$b);
  
  var_dump($c);
}

function f7()
{
  $a = array("a" => &$a);
  $b = array("a" => &$b);
  
  @$x = array_merge_recursive($a,$b);
  
  var_dump($x);
}

for($i=1;$i<=7;$i++)
{
  $f = "f$i";
  echo "$f:\n";
  $f();
}
?>