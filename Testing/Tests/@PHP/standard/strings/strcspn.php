[expect php]
[file]
<?php
  require('Phalanger.inc');
$a = "22222222aaaa bbb1111 cccc";
$b = "1234";
__var_dump($a);
__var_dump($b);
__var_dump(strcspn($a,$b));
__var_dump(strcspn($a,$b,9));
__var_dump(strcspn($a,$b,9,6));
?>