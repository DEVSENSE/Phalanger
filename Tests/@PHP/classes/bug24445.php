[expect php]
[file]
<?php
require('Phalanger.inc');
class Test { }
__var_dump(get_parent_class('Test'));
$t = new Test;
__var_dump(get_parent_class($t));
?>