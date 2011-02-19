[expect php]
[file]
<?php
require('Phalanger.inc');
class Id {
        private $id="priv";

        public function tester($obj)
        {
	        	$obj->id = "bar";
        }
}

$id = new Id();
@$obj->foo = "bar";
$id->tester($obj);
__var_dump($obj);
?>
