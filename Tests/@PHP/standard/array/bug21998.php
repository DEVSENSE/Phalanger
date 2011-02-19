[expect php]
[file]
<?php
  include('Phalanger.inc');
$a = array("a", "b", "c");

__var_dump(key($a));
__var_dump(array_pop($a));
__var_dump(key($a));      
__var_dump(array_pop($a));
__var_dump(key($a));      
__var_dump(array_pop($a));
__var_dump(key($a));      

?>