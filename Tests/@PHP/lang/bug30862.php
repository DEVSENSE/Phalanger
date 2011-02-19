[expect php]
[file]
<?php 
include('Phalanger.inc');
class T {
	static $a = array(false=>"false", true=>"true");
}
__var_dump(T::$a);

define("X",0);
define("Y",1);
class T2 {
	static $a = array(X=>"false", Y=>"true");
}
__var_dump(T2::$a);
?>