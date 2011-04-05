[expect php]
[file]
<?
$ar1 = array("color" => array("favorite" => "red"), 1 => array(1,2,3), "q" => null);
$ar2 = array(10, "color" => array("favorite" => "green", "blue"), 1 => array(4,5,6), "q" => null);
$ar3 = array("q" => "x");
print_r(array_merge_recursive($ar1, $ar2, $ar3));

$x = array(1,2,3);
$ar1 = array("a" => &$x);
$ar2 = array("a" => "e");
$a = array_merge_recursive($ar1, $ar2);
print_r($a);
$x = "hello!";
print_r($a);

$x = 1;
$ar1 = array("a" => &$x);
$ar2 = array("b" => 2);
print_r($a = array_merge_recursive($ar1, $ar2));
$x = "xxx";
print_r($a);

$ar1 = array("a" => &$ar1);
$ar2 = array("a" => array(1,2,3));
print_r(array_merge_recursive($ar1, $ar2));

$ar1 = array("a" => array("b" => 1,"c" => 1));
$ar2 = array("a" => array("d" => 2,"b" => 1));
$ar3 = array("a" => array("d" => 3));
print_r(array_merge_recursive($ar1, $ar2, $ar3));

$x = null;
$ar1 = array("a" => &$x);
$ar2 = array("a" => null);
print_r($a = array_merge_recursive($ar1, $ar2));
$x = "bye";
print_r($a);
?>