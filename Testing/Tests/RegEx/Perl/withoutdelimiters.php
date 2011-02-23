[exact php]
[file]
<?php

require_once ('Phalanger.inc');

$pattern = '((?i)rah)\s+\1';

preg_match($pattern,"RAH RAH",$matches);
__var_dump($matches);

preg_match($pattern,"rah RAH",$matches);
__var_dump($matches);

preg_match($pattern,"rah rah",$matches);
__var_dump($matches);

?>