[expect php]
[file]
<?php
require('Phalanger.inc');
class foo {
}


$a = new foo();
			    
$arr = array(0=>&$a, 1=>&$a);
@implode(",",$arr);
__var_dump($arr)
?>