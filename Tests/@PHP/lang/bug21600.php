[expect php]
[file]
<?php
require('Phalanger.inc');
$tmp = array();
$tmp['foo'] = "test";
$tmp['foo'] = &bar($tmp['foo']);
__var_dump($tmp);

unset($tmp);

$tmp = array();
$tmp['foo'] = "test";
$tmp['foo'] = &fubar($tmp['foo']);
__var_dump($tmp);

function bar($text){
  return $text;
}

function fubar($text){
  $text = &$text;
  return $text;
}
?>
