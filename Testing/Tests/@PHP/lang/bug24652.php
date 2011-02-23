[expect php]
[file]
<?php
require('Phalanger.inc');
  /* This works */
  $f = array('7' => 0);
  __var_dump($f);
  __var_dump(array_key_exists(7, $f));
  __var_dump(array_key_exists('7', $f));

  print "----------\n";
  /* This doesn't */
  $f = array_flip(array('7'));
  __var_dump($f);
  __var_dump(array_key_exists(7, $f));
  __var_dump(array_key_exists('7', $f));
?>