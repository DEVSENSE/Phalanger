[expect php]
[file]
<?php
require('Phalanger.inc');
  echo "Begin\n";
  define("THE_CONST",123);
  function f($a=array(THE_CONST=>THE_CONST)) {
    __var_dump($a);
  }
  f();
  f();
  f();
  echo "Done";
?>