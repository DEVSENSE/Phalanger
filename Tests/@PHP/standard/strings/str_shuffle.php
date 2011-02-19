[expect php]
[file]
<?php
  require('Phalanger.inc');
  
$s = '123';
str_shuffle($s);
__var_dump($s);
?>