[expect php]
[file]
<?php
require('Phalanger.inc');
function f1() {
  $c = extract(array("" => 1),EXTR_PREFIX_SAME,"prefix");
  echo "Extracted:";
  __var_dump($c);
  __var_dump(get_defined_vars());
}
function f2() {
  $a = 1;
  $c = extract(array("a" => 1),EXTR_PREFIX_SAME,"prefix");
  echo "Extracted:";
  __var_dump($c);
  __var_dump(get_defined_vars());
}
function f3() {
  $a = 1;
  $c = extract(array("a" => 1),EXTR_PREFIX_ALL,"prefix");
  echo "Extracted:";
  __var_dump($c);
  __var_dump(get_defined_vars());
}
function f4() {
  $c = extract(array("" => 1),EXTR_PREFIX_ALL,"prefix");
  echo "Extracted:";
  __var_dump($c);
  __var_dump(get_defined_vars());
}
function f5() {
  $c = extract(array("111" => 1),EXTR_PREFIX_ALL,"prefix");
  echo "Extracted:";
  __var_dump($c);
  __var_dump(get_defined_vars());
}

f1();
f2();
f3();
f4();
f5();
?>