[expect php]
[file]
<?php
require('Phalanger.inc');
class Test {
	private $real_a = 2;
	
	function __set($property, $value) {
	  if ($property = "a") {
	    $this->real_a = $value;
	  }
	}

	function __get($property) {
	  if ($property = "a") {
	    return $this->real_a;
	  }
	}
}

$obj = new Test;
__var_dump($obj->a);
$t1 = $obj->a++;
__var_dump($obj->a);
echo "---Done---\n";
?>