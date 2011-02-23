[expect php]
[file]
<?php
require('Phalanger.inc');
class dooh {
    public $blah;
}
$d = new dooh;
__var_dump(is_subclass_of($d, 'dooh'));
?>